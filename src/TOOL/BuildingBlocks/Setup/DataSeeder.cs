using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Setup;

public class DataSeeder
{
    private readonly HttpClient _http;
    private readonly string _receiverUrl;
    private readonly string _namespace;
    private readonly string _repoUrl;
    private readonly string _gitRef;
    private readonly string _itemsFolder;
    private readonly string _bearerToken;
    private readonly IDeserializer _yaml;

    public DataSeeder(HttpClient httpClient)
    {
        _http = httpClient;
        _receiverUrl =
            Environment.GetEnvironmentVariable("RECEIVER_URL")
            ?? "http://localhost:5080/api/v1/seed";
        _namespace = Environment.GetEnvironmentVariable("TEAMAI_NS") ?? "ravvnen.consulting";
        _repoUrl =
            Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")
            ?? "github.com/ravvnen/test-repo";
        _gitRef = Environment.GetEnvironmentVariable("GITHUB_REF") ?? "main";
        _itemsFolder =
            Environment.GetEnvironmentVariable("TEAMAI_ITEMS_DIR")
            ?? "/Users/ravvnen/Masters/test-repo/items";
        _bearerToken = Environment.GetEnvironmentVariable("TEAMAI_TOKEN") ?? "dev-token";

        _yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    // Ask user if they have git-pulled the most updated repo before running this. If no, exit, if yes, continue.
    public void ConfirmReady()
    {
        Console.WriteLine(
            $"[seeder] About to seed items from '{_itemsFolder}' to '{_receiverUrl}'"
        );
        Console.Write("Have you git-pulled the latest changes and want to continue? (y/n): ");
        var input = Console.ReadLine();
        if (!string.Equals(input, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[seeder] Aborting seeding process.");
            Environment.Exit(0);
        }
    }

    public async Task SeedAllAsync()
    {
        if (!Directory.Exists(_itemsFolder))
        {
            Console.WriteLine($"[seeder] items folder '{_itemsFolder}' does not exist.");
            return;
        }

        var yamlFiles = Directory.GetFiles(_itemsFolder, "*.yaml", SearchOption.AllDirectories);
        if (yamlFiles.Length == 0)
        {
            Console.WriteLine($"[seeder] no *.yaml files under '{_itemsFolder}'.");
            return;
        }

        foreach (var file in yamlFiles)
        {
            try
            {
                var yamlText = await File.ReadAllTextAsync(file);
                var map = _yaml.Deserialize<Dictionary<string, object?>>(yamlText) ?? new();

                if (
                    !map.TryGetValue("item_id", out var itemIdObj)
                    || itemIdObj is not string itemId
                    || string.IsNullOrWhiteSpace(itemId)
                )
                {
                    Console.WriteLine($"[seeder] SKIP '{file}': missing item_id.");
                    continue;
                }

                var title = map.TryGetValue("title", out var t) ? t?.ToString()?.Trim() ?? "" : "";
                var content = map.TryGetValue("content", out var c)
                    ? c?.ToString()?.Trim() ?? ""
                    : "";
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"[seeder] SKIP '{file}': empty content.");
                    continue;
                }

                var labels = new List<string>();
                if (map.TryGetValue("labels", out var l) && l is IEnumerable<object?> raw)
                    labels = raw.Select(x => x?.ToString())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                // provenance hash over RAW BYTES, not re-encoded string
                var bytes = await File.ReadAllBytesAsync(file);
                using var sha256 = SHA256.Create();
                var blobHash = Convert.ToHexString(sha256.ComputeHash(bytes)).ToLowerInvariant();

                var ev = new
                {
                    event_type = "im.proposal.v1",
                    ns = _namespace,
                    sha = $"seed-{itemId}-v1", // deterministic seed id
                    ci = "n/a",
                    emitted_at = DateTime.UtcNow.ToString("o"),
                    item_id = itemId,
                    title,
                    content,
                    labels,
                    source = new
                    {
                        kind = "seeded", // "seeded" or "gh-webhook"
                        repo_url = _repoUrl,
                        @ref = _gitRef, // '@ref' → JSON "ref"
                        path = file.Replace('\\', '/'),
                        blob_sha = blobHash,
                    },
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, _receiverUrl)
                {
                    Content = JsonContent.Create(
                        ev,
                        options: new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            DefaultIgnoreCondition = System
                                .Text
                                .Json
                                .Serialization
                                .JsonIgnoreCondition
                                .WhenWritingNull,
                        }
                    ),
                };

                // Add required GitHub webhook headers
                req.Headers.Add("X-GitHub-Event", "seeded_proposal");
                req.Headers.Add("X-GitHub-Delivery", Guid.NewGuid().ToString());
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _bearerToken
                );

                // Print out the event being sent (for debugging)
                var jsonPayload = await req.Content!.ReadAsStringAsync();
                Console.WriteLine($"[seeder] === SENDING REQUEST ===");
                Console.WriteLine($"[seeder] File: {file}");
                Console.WriteLine($"[seeder] URL: {_receiverUrl}");
                Console.WriteLine($"[seeder] Headers:");
                foreach (var header in req.Headers)
                {
                    Console.WriteLine(
                        $"[seeder]   {header.Key}: {string.Join(", ", header.Value)}"
                    );
                }
                Console.WriteLine($"[seeder] JSON Payload:");

                // print it out but with the JSON indented for readability
                using var doc = JsonDocument.Parse(jsonPayload);
                var indentedJson = JsonSerializer.Serialize(
                    doc,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                Console.WriteLine($"[seeder] {indentedJson}");
                Console.WriteLine($"[seeder] === END REQUEST ===");

                var resp = await _http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();

                Console.WriteLine($"[seeder] POST {file} → {resp.StatusCode}");
                if (!resp.IsSuccessStatusCode)
                    Console.WriteLine($"[seeder]   response: {body}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[seeder] ERROR '{file}': {ex.Message}");
            }
        }
    }
}

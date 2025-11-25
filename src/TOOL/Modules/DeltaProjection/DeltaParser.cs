using System.Text.Json;
using Buildingblocks.Utils;

namespace Modules.DeltaProjection;

/// <summary>
/// Pure parser for DELTA events. No external dependencies.
/// Converts JSON bytes to DeltaEvent model.
/// </summary>
public static class DeltaParser
{
    /// <summary>
    /// Parse a DELTA event from JSON bytes.
    /// Returns null if parsing fails or required fields are missing.
    /// </summary>
    public static DeltaEvent? Parse(byte[] data)
    {
        if (data is null || data.Length == 0)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;

            // Required fields
            if (
                !JsonHelper.TryGetString(root, "ns", out var ns)
                || !JsonHelper.TryGetString(root, "item_id", out var itemId)
                || !JsonHelper.TryGetString(root, "policy_version", out var policyVersion)
            )
            {
                return null;
            }

            // Validate source exists
            if (!root.TryGetProperty("source", out var source))
            {
                return null;
            }

            var type = JsonHelper.GetStringOrEmpty(root, "type");
            var title = JsonHelper.GetStringOrEmpty(root, "title");
            var content = JsonHelper.GetStringOrEmpty(root, "content");
            var labels =
                root.TryGetProperty("labels", out var labelsEl)
                && labelsEl.ValueKind == JsonValueKind.Array
                    ? JsonSerializer.Serialize(
                        labelsEl
                            .EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                    )
                    : "[]";

            var newVersion = root.GetProperty("new_version").GetInt32();
            var baseVersion =
                root.TryGetProperty("base_version", out var bv)
                && bv.ValueKind == JsonValueKind.Number
                    ? bv.GetInt32()
                    : 0;

            var occurredAt = JsonHelper.TryGetDateTimeOffset(root, "occurred_at") ?? DateTimeOffset.UtcNow;
            var emittedAt = JsonHelper.TryGetDateTimeOffset(root, "emitted_at") ?? DateTimeOffset.UtcNow;

            var repo = JsonHelper.GetStringOrEmpty(source, "repo");
            var srcRef = JsonHelper.GetStringOrEmpty(source, "ref");
            var path = JsonHelper.GetStringOrEmpty(source, "path");
            var blobSha = JsonHelper.GetStringOrEmpty(source, "blob_sha");

            var inputEventId = JsonHelper.GetStringOrEmpty(root, "input_event_id");

            return new DeltaEvent
            {
                Type = type,
                Ns = ns,
                ItemId = itemId,
                Title = title,
                Content = content,
                LabelsJson = labels,
                BaseVersion = baseVersion,
                NewVersion = newVersion,
                InputEventId = inputEventId,
                PolicyVersion = policyVersion,
                OccurredAt = occurredAt,
                EmittedAt = emittedAt,
                Repo = repo,
                Ref = srcRef,
                Path = path,
                BlobSha = blobSha,
            };
        }
        catch
        {
            return null;
        }
    }
}

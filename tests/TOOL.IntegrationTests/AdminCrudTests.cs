using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using NATS.Client.Core;
using NATS.Client.JetStream;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests for Admin CRUD operations (v5.0-01)
/// Verifies admin events flow through EVENTS → Promoter → DELTAS → Projection
/// </summary>
public class AdminCrudTests : IAsyncLifetime
{
    private NatsConnection? _nats;
    private NatsJSContext? _js;
    private readonly string _ns = "ravvnen.consulting";
    private readonly string _projectionDbPath;

    public AdminCrudTests()
    {
        _projectionDbPath = Environment.GetEnvironmentVariable("RULES_DB_PATH") ?? "rules.db";
    }

    public async Task InitializeAsync()
    {
        try
        {
            var natsUrl = Environment.GetEnvironmentVariable("NATS_URL") ?? "nats://localhost:4222";
            _nats = new NatsConnection(new NatsOpts { Url = natsUrl });
            await _nats.ConnectAsync();
            _js = new NatsJSContext(_nats);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NATS initialization failed (tests will be skipped): {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_nats is not null)
        {
            await _nats.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminCreateRule_EmitsDelta_AppearsInProjection()
    {
        // Skip if NATS is not available
        if (_js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if projection database doesn't exist
        if (!File.Exists(_projectionDbPath))
        {
            Console.WriteLine(
                $"Projection database not found at {_projectionDbPath}, skipping test"
            );
            return;
        }

        // Arrange: Generate unique item_id to avoid conflicts
        var itemId = $"admin-test-create-{Guid.NewGuid():N}";
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        var adminEvent = new
        {
            type = "Admin.RuleCreateRequested",
            ns = _ns,
            item_id = itemId,
            action = "create",
            title = "Test Admin Rule",
            content = "This rule was created by an admin for testing purposes.",
            labels = new[] { "test", "admin-created" },
            admin_metadata = new
            {
                user_id = "test-admin@example.com",
                reason = "Integration test for admin create",
                bypass_review = true,
                expected_version = (int?)null,
            },
            source = new
            {
                repo = "admin.override",
                @ref = "manual",
                path = $"admin/test-admin/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                blob_sha = ComputeSha256("This rule was created by an admin for testing purposes."),
            },
            occurred_at = occurredAt,
            event_id = eventId,
        };

        var json = JsonSerializer.Serialize(
            adminEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );
        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        // Act: Publish admin event to EVENTS stream
        var subject = $"Admin.RuleCreateRequested.{_ns}.{itemId}";
        var ack = await _js.PublishAsync(subject, Encoding.UTF8.GetBytes(json), headers: headers);

        Console.WriteLine($"Published {subject} (stream={ack.Stream}, seq={ack.Seq})");

        // Wait for Promoter + DeltaConsumer to process (generous timeout for CI)
        await Task.Delay(2000);

        // Assert: Verify rule exists in projection DB
        await using var db = new SqliteConnection($"Data Source={_projectionDbPath}");
        await db.OpenAsync();

        var row = await db.QuerySingleOrDefaultAsync<(
            string Title,
            string Content,
            int Version,
            int IsActive
        )>(
            "SELECT title, content, version, is_active FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, row);
        Assert.Equal("Test Admin Rule", row.Title);
        Assert.Equal("This rule was created by an admin for testing purposes.", row.Content);
        Assert.Equal(1, row.Version); // First version
        Assert.Equal(1, row.IsActive); // Active

        Console.WriteLine(
            $"✅ Admin-created rule verified in projection DB: {itemId} v{row.Version}"
        );
    }

    [Fact]
    public async Task AdminUpdateRule_IncrementsVersion_UpdatesProjection()
    {
        // Skip if NATS is not available
        if (_js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if projection database doesn't exist
        if (!File.Exists(_projectionDbPath))
        {
            Console.WriteLine(
                $"Projection database not found at {_projectionDbPath}, skipping test"
            );
            return;
        }

        // Arrange: First, create a rule
        var itemId = $"admin-test-update-{Guid.NewGuid():N}";
        await CreateTestRuleAsync(itemId, "Initial Title", "Initial content.");

        // Wait for creation to propagate
        await Task.Delay(2000);

        // Verify initial state
        await using var db = new SqliteConnection($"Data Source={_projectionDbPath}");
        await db.OpenAsync();

        var beforeUpdate = await db.QuerySingleOrDefaultAsync<(int Version, string Title)>(
            "SELECT version, title FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, beforeUpdate);
        Assert.Equal(1, beforeUpdate.Version);
        Assert.Equal("Initial Title", beforeUpdate.Title);

        // Act: Publish admin update event
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        var updateEvent = new
        {
            type = "Admin.RuleEditRequested",
            ns = _ns,
            item_id = itemId,
            action = "update",
            title = "Updated Title",
            content = "Updated content by admin.",
            labels = new[] { "test", "admin-updated" },
            admin_metadata = new
            {
                user_id = "test-admin@example.com",
                reason = "Integration test for admin update",
                bypass_review = true,
                expected_version = 1, // Expect current version to be 1
            },
            source = new
            {
                repo = "admin.override",
                @ref = "manual",
                path = $"admin/test-admin/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                blob_sha = ComputeSha256("Updated content by admin."),
            },
            occurred_at = occurredAt,
            event_id = eventId,
        };

        var json = JsonSerializer.Serialize(
            updateEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );
        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        var subject = $"Admin.RuleEditRequested.{_ns}.{itemId}";
        var ack = await _js.PublishAsync(subject, Encoding.UTF8.GetBytes(json), headers: headers);

        Console.WriteLine($"Published {subject} (stream={ack.Stream}, seq={ack.Seq})");

        // Wait for processing
        await Task.Delay(2000);

        // Assert: Verify version incremented and content updated
        var afterUpdate = await db.QuerySingleOrDefaultAsync<(
            int Version,
            string Title,
            string Content,
            int IsActive
        )>(
            "SELECT version, title, content, is_active FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, afterUpdate);
        Assert.Equal(2, afterUpdate.Version); // Version incremented
        Assert.Equal("Updated Title", afterUpdate.Title);
        Assert.Equal("Updated content by admin.", afterUpdate.Content);
        Assert.Equal(1, afterUpdate.IsActive);

        Console.WriteLine($"✅ Admin-updated rule verified: {itemId} v{afterUpdate.Version}");
    }

    [Fact]
    public async Task AdminUpdateRule_WithVersionConflict_LogsConflict()
    {
        // Skip if NATS is not available
        if (_js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if projection database doesn't exist
        if (!File.Exists(_projectionDbPath))
        {
            Console.WriteLine(
                $"Projection database not found at {_projectionDbPath}, skipping test"
            );
            return;
        }

        // Arrange: Create a rule
        var itemId = $"admin-test-conflict-{Guid.NewGuid():N}";
        await CreateTestRuleAsync(itemId, "Conflict Test", "Original content.");

        // Wait for creation
        await Task.Delay(2000);

        // Act: Publish update with WRONG expected_version (simulating concurrent edit)
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        var conflictEvent = new
        {
            type = "Admin.RuleEditRequested",
            ns = _ns,
            item_id = itemId,
            action = "update",
            title = "Should Not Apply",
            content = "This update should be rejected due to version conflict.",
            labels = new[] { "test" },
            admin_metadata = new
            {
                user_id = "test-admin@example.com",
                reason = "Testing conflict detection",
                bypass_review = true,
                expected_version = 999, // Wrong version (actual is 1)
            },
            source = new
            {
                repo = "admin.override",
                @ref = "manual",
                path = $"admin/test-admin/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                blob_sha = ComputeSha256("This update should be rejected due to version conflict."),
            },
            occurred_at = occurredAt,
            event_id = eventId,
        };

        var json = JsonSerializer.Serialize(
            conflictEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );
        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        var subject = $"Admin.RuleEditRequested.{_ns}.{itemId}";
        var ack = await _js.PublishAsync(subject, Encoding.UTF8.GetBytes(json), headers: headers);

        Console.WriteLine(
            $"Published {subject} with expected_version=999 (stream={ack.Stream}, seq={ack.Seq})"
        );

        // Wait for Promoter to process (and skip due to conflict)
        await Task.Delay(2000);

        // Assert: Rule should NOT be updated (version still 1, content unchanged)
        await using var db = new SqliteConnection($"Data Source={_projectionDbPath}");
        await db.OpenAsync();

        var row = await db.QuerySingleOrDefaultAsync<(int Version, string Title, string Content)>(
            "SELECT version, title, content FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, row);
        Assert.Equal(1, row.Version); // Version unchanged
        Assert.Equal("Conflict Test", row.Title); // Original title preserved
        Assert.Equal("Original content.", row.Content); // Original content preserved

        Console.WriteLine(
            $"✅ Version conflict correctly prevented update: {itemId} remains at v{row.Version}"
        );
    }

    [Fact]
    public async Task AdminDeleteRule_EmitsRetraction_MarksInactive()
    {
        // Skip if NATS is not available
        if (_js is null)
        {
            Console.WriteLine("NATS unavailable, skipping test");
            return;
        }

        // Skip if projection database doesn't exist
        if (!File.Exists(_projectionDbPath))
        {
            Console.WriteLine(
                $"Projection database not found at {_projectionDbPath}, skipping test"
            );
            return;
        }

        // Arrange: Create a rule to delete
        var itemId = $"admin-test-delete-{Guid.NewGuid():N}";
        await CreateTestRuleAsync(itemId, "To Be Deleted", "This rule will be deleted.");

        // Wait for creation
        await Task.Delay(2000);

        // Verify rule exists and is active
        await using var db = new SqliteConnection($"Data Source={_projectionDbPath}");
        await db.OpenAsync();

        var beforeDelete = await db.QuerySingleOrDefaultAsync<(int Version, int IsActive)>(
            "SELECT version, is_active FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, beforeDelete);
        Assert.Equal(1, beforeDelete.IsActive); // Active before delete

        // Act: Publish admin delete event
        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        var deleteEvent = new
        {
            type = "Admin.RuleDeleteRequested",
            ns = _ns,
            item_id = itemId,
            action = "delete",
            title = "",
            content = "",
            labels = Array.Empty<string>(),
            admin_metadata = new
            {
                user_id = "test-admin@example.com",
                reason = "Integration test for admin delete",
                bypass_review = true,
                expected_version = (int?)null,
            },
            source = new
            {
                repo = "admin.override",
                @ref = "manual",
                path = $"admin/test-admin/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                blob_sha = "",
            },
            occurred_at = occurredAt,
            event_id = eventId,
        };

        var json = JsonSerializer.Serialize(
            deleteEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );
        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        var subject = $"Admin.RuleDeleteRequested.{_ns}.{itemId}";
        var ack = await _js.PublishAsync(subject, Encoding.UTF8.GetBytes(json), headers: headers);

        Console.WriteLine($"Published {subject} (stream={ack.Stream}, seq={ack.Seq})");

        // Wait for processing
        await Task.Delay(2000);

        // Assert: Rule should be marked inactive
        var afterDelete = await db.QuerySingleOrDefaultAsync<(int Version, int IsActive)>(
            "SELECT version, is_active FROM im_items_current WHERE ns=@ns AND item_id=@itemId",
            new { ns = _ns, itemId }
        );

        Assert.NotEqual(default, afterDelete);
        Assert.Equal(0, afterDelete.IsActive); // Now inactive
        Assert.True(
            afterDelete.Version > beforeDelete.Version,
            "Version should increment on retraction"
        );

        Console.WriteLine(
            $"✅ Admin-deleted rule verified: {itemId} is_active=0, v{afterDelete.Version}"
        );
    }

    // ===== Helper Methods =====

    private async Task CreateTestRuleAsync(string itemId, string title, string content)
    {
        if (_js is null)
            throw new InvalidOperationException("NATS not initialized");

        var eventId = Guid.NewGuid().ToString("N");
        var occurredAt = DateTimeOffset.UtcNow;

        var createEvent = new
        {
            type = "Admin.RuleCreateRequested",
            ns = _ns,
            item_id = itemId,
            action = "create",
            title,
            content,
            labels = new[] { "test" },
            admin_metadata = new
            {
                user_id = "test-admin@example.com",
                reason = "Test setup",
                bypass_review = true,
                expected_version = (int?)null,
            },
            source = new
            {
                repo = "admin.override",
                @ref = "manual",
                path = $"admin/test-admin/{occurredAt:yyyy-MM-ddTHH:mm:ssZ}",
                blob_sha = ComputeSha256(content),
            },
            occurred_at = occurredAt,
            event_id = eventId,
        };

        var json = JsonSerializer.Serialize(
            createEvent,
            new JsonSerializerOptions { WriteIndented = false }
        );
        var headers = new NatsHeaders
        {
            { "Nats-Msg-Id", $"admin-{eventId}" },
            { "Content-Type", "application/json" },
        };

        var subject = $"Admin.RuleCreateRequested.{_ns}.{itemId}";
        await _js.PublishAsync(subject, Encoding.UTF8.GetBytes(json), headers: headers);

        Console.WriteLine($"[Setup] Created test rule: {itemId}");
    }

    private static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}

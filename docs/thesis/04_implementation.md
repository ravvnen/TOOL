# Chapter 4: Implementation

**Status**: ⚠️ WRITE AFTER V1.0 IS STABLE (target: Dec 1, 2025)
**Estimated length**: 8-12 pages

---

## Writing Instructions

This chapter demonstrates that your design is **feasible and real**. Goals:
1. Document technology choices (why .NET? why NATS?)
2. Describe key implementation challenges and solutions
3. Provide enough detail for reproducibility (others should be able to rebuild)
4. Reflect on engineering lessons learned

**Tone**: Technical, detailed, but not a code dump. Focus on interesting decisions, not boilerplate.

**Structure**: Organize by component (not chronologically).

---

## 4.1 Technology Stack & Dependencies

**Target length**: 1-2 pages

### What to write

**Backend**:
- **.NET 9.0**: Why chosen? (C# strong typing, async/await, cross-platform, NATS client library)
- **ASP.NET Core**: REST API framework
- **NATS.Client**: JetStream consumer/publisher

**Database**:
- **SQLite 3.x**: Why chosen? (see Chapter 3.5)
- **Microsoft.Data.Sqlite**: .NET bindings
- **FTS5 extension**: Full-text search

**Frontend**:
- **React 19**: Component-based UI
- **Vite**: Fast dev server, HMR
- **Tailwind CSS**: Styling

**LLM integration**:
- **OllamaSharp**: Library for local Ollama API
- **OpenAI SDK** (optional): For remote LLMs (GPT-4, Claude via proxy)

**Infrastructure**:
- **Docker** (optional): Containerized deployment
- **GitHub Actions**: CI/CD (if implemented)

**Dependencies table** (include versions):
| Package | Version | Purpose |
|---------|---------|---------|
| .NET SDK | 9.0 | Runtime |
| NATS Server | 2.10+ | Event streaming |
| NATS.Client | 2.x | .NET client |
| Microsoft.Data.Sqlite | 9.x | SQLite driver |
| React | 19.x | Frontend |
| Ollama | Latest | Local LLM |

---

## 4.2 Promoter Module Implementation

**Target length**: 1-2 pages

### What to write

**Class structure**:
- `PromoterService`: Background service, subscribes to EVENTS
- `GatingPolicy`: Interface for pluggable policies
- `AutoApprovePolicy`: Checks trusted authors list
- `ManualReviewPolicy`: Queues for admin approval

**Key code snippets** (pseudocode or simplified C#):
```csharp
public class PromoterService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = await _nats.CreateConsumerAsync("EVENTS", "promoter-consumer");
        await foreach (var msg in consumer.FetchAsync(stoppingToken))
        {
            var evt = JsonSerializer.Deserialize<InstructionEvent>(msg.Data);
            if (await _policy.ShouldApprove(evt))
            {
                var delta = CreateDelta(evt);
                await _nats.PublishAsync("DELTAS", delta);
            }
            else
            {
                await QueueForReview(evt);
            }
            msg.Ack();
        }
    }
}
```

**Challenges**:
- Handling NATS consumer crashes (durable consumer saves offset)
- Idempotency: What if same event is processed twice? (Use event ID as dedup key)

---

## 4.3 DeltaConsumer / Projection Implementation

**Target length**: 2-3 pages

### What to write

**Refactoring story** (PR #63):
- **Before**: Monolithic `DeltaConsumer` class (400+ lines)
- **After**: Split into `DeltaStreamConsumerService`, `DeltaParser`, `Projector`
- **Why**: Separation of concerns, testability

**DeltaStreamConsumerService**:
- Subscribes to DELTAS stream
- Fetches batches of deltas
- Orchestrates: parse → project → ack

**DeltaParser**:
- Parses JSON → `DeltaMessage` domain model
- Validates schema, handles errors

**Projector**:
- Applies delta to SQLite DB
- SQL logic: INSERT OR REPLACE, DELETE
- Updates FTS5 index

**Key code snippet** (SQLite upsert):
```csharp
public async Task ApplyDeltaAsserted(DeltaMessage delta)
{
    await using var conn = new SqliteConnection(_connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO instructions (instructionId, content, version, provenance, updatedAt)
        VALUES ($id, $content, $version, $provenance, $timestamp)
        ON CONFLICT(instructionId) DO UPDATE SET
            content = excluded.content,
            version = excluded.version,
            provenance = excluded.provenance,
            updatedAt = excluded.updatedAt";
    cmd.Parameters.AddWithValue("$id", delta.InstructionId);
    cmd.Parameters.AddWithValue("$content", delta.Content);
    // ... (other params)
    await cmd.ExecuteNonQueryAsync();
}
```

**Challenges**:
- SQLite locking under concurrent writes (solution: single-threaded consumer)
- FTS5 index synchronization (solution: triggers or manual rebuild)
- Error handling: What if DB write fails? (NATS redelivery + exponential backoff)

---

## 4.4 Agent Container & UI / Chat Module

**Target length**: 1-2 pages

### What to write

**Agent.UI (React)**:
- **Components**: ChatWindow, MessageList, PromptInput, RulePanel
- **State management**: React hooks (useState, useEffect) or Context API
- **SSE integration** (v1.5, if implemented): Subscribe to rule updates via Server-Sent Events

**MemoryCompiler integration**:
- API endpoint: `POST /api/chat` with `{ query: "...", topK: 5 }`
- Returns: `{ response: "...", citedRules: [...] }`

**Prompt assembly** (example):
```javascript
const systemPrompt = `You are a helpful assistant. Follow these rules:
${rules.map(r => `[Rule ${r.id}]: ${r.content}`).join('\n')}`;

const response = await ollama.chat({
  model: 'llama3.2',
  messages: [
    { role: 'system', content: systemPrompt },
    { role: 'user', content: userQuery }
  ]
});
```

**Challenges**:
- Handling long rule lists (context window limits)
- Parsing rule citations from LLM output (regex, heuristics)

---

## 4.5 Admin UI / Rule Editor

**Status**: v5.0 (future work), not implemented in v1.0.

**Target length**: 0.5-1 page

### What to write

**Design intent** (mock or wireframe):
- List view: pending approvals, searchable rules
- Detail view: rule content, provenance, approve/reject buttons
- Edit view: markdown editor with live preview

**Implementation plan** (for future work):
- Use React Admin or similar framework
- API endpoints: `GET /api/admin/pending`, `POST /api/admin/approve/:id`

**Why deferred**: Time constraints, CLI seeder sufficient for experiments.

---

## 4.6 Experiment Metrics Framework Integration

**Target length**: 1-2 pages

### What to write

**Instrumentation** (logging, telemetry):
- Logging: Serilog with structured logs (JSON)
- Metrics: Prometheus-style counters (e.g., `deltas_processed_total`, `retrieval_latency_ms`)
- Tracing: (Optional) OpenTelemetry for distributed tracing

**Experiment runner** (TOOL.Evaluation project):
- Loads prompts from `data/prompts.json`
- Runs each prompt with/without rules (Condition A vs B)
- Logs: responses, cited rules, latency
- Outputs: CSV or JSON for analysis

**Key code snippet**:
```csharp
var results = new List<ExperimentResult>();
foreach (var prompt in prompts)
{
    var responseWithRules = await RunWithRules(prompt);
    var responseWithoutRules = await RunWithoutRules(prompt);
    results.Add(new ExperimentResult
    {
        PromptId = prompt.Id,
        ResponseA = responseWithRules,
        ResponseB = responseWithoutRules,
        Latency = ...
    });
}
await File.WriteAllTextAsync("results.json", JsonSerializer.Serialize(results));
```

**Challenges**:
- Ensuring determinism (temperature=0, fixed seed)
- Handling LLM API rate limits (exponential backoff)

---

## 4.7 Logging, Instrumentation & Debug Tooling

**Target length**: 1 page

### What to write

**Structured logging** (Serilog):
- Log levels: Debug, Info, Warn, Error
- Context enrichment: `EventId`, `SequenceNumber`, `InstructionId`
- Sinks: Console, File, (optional) Seq, Elasticsearch

**Debug endpoints**:
- `GET /api/debug/events`: Fetch recent EVENTS
- `GET /api/debug/deltas`: Fetch recent DELTAS
- `GET /api/debug/db`: Query projection DB

**NATS CLI tools**:
- `nats stream ls`, `nats stream info DELTAS`
- `nats consumer ls DELTAS`

---

## 4.8 Engineering Challenges & Lessons

**Target length**: 2 pages

### What to write

**Challenge 1: NATS consumer idempotency**:
- Problem: What if delta is delivered twice?
- Solution: Track `last_processed_sequence` in DB, skip duplicates

**Challenge 2: SQLite locking**:
- Problem: Concurrent writes block
- Solution: Single-threaded Projector, consider WAL mode

**Challenge 3: FTS5 index staleness**:
- Problem: Index not updated after `INSERT`
- Solution: Use triggers or manual `INSERT INTO instructions_fts SELECT ...`

**Challenge 4: Parsing LLM citations**:
- Problem: LLM may not follow citation format exactly
- Solution: Regex + fuzzy matching, log unparseable responses

**Lessons learned**:
1. **Start simple**: SQLite + FTS5 was right choice for v1.0 (avoid premature complexity)
2. **Test idempotency early**: Replay tests caught many bugs
3. **Observe everything**: Structured logs + NATS CLI saved hours of debugging
4. **Refactor when needed**: DeltaConsumer split improved maintainability (PR #63)

---

## Chapter Checklist

- [ ] All components documented (Promoter, Projection, Agent UI)
- [ ] Key code snippets included (simplified, not full listings)
- [ ] Challenges and solutions explained
- [ ] Lessons learned reflect on design choices
- [ ] Reproducibility: Could another engineer rebuild this?

---

## Resources

- Your codebase: `src/TOOL/`, `src/Agent.UI/`
- Recent PRs (e.g., #63, #60, etc.)
- Git history for timeline of features

---

## Next Steps

1. Stabilize v1.0 implementation
2. Run pilot experiments (10-20 prompts)
3. Write this chapter after code freeze
4. Proceed to Chapter 5 (Methodology)

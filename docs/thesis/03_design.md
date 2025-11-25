# Chapter 3: Design & Architecture

**Status**: ✅ MOSTLY READY TO WRITE NOW (some sections depend on v1.0 stability)
**Target completion**: November 24, 2025
**Estimated length**: 12-18 pages

---

## Writing Instructions

This chapter is your **design blueprint**. Goals:
1. Define requirements (what properties must the system have?)
2. Present the architecture (components, data flow)
3. Justify design decisions (why event sourcing? why NATS? why FTS5?)
4. Discuss trade-offs (what did you sacrifice for what gains?)

**Tone**: Technical but accessible. Use diagrams heavily. Assume reader understands distributed systems basics.

**Key principle**: Every design choice must be justified in terms of requirements. E.g., "We use event sourcing (not snapshots) because requirement R3 (replayability) demands deterministic state reconstruction."

---

## 3.1 Requirements & Design Goals

**Target length**: 2 pages

### What to write

**Functional requirements** (what the system must DO):
- **FR1: Rule injection**: Agents receive relevant rules in prompts based on query context
- **FR2: Multi-agent sharing**: All agents read from the same rule memory (consistency)
- **FR3: Versioning**: Rules have versions, system retains history
- **FR4: Provenance**: Responses cite rule IDs and Git commits
- **FR5: Real-time updates**: Rule changes propagate to agents within seconds
- **FR6: Governance**: Admins can approve/reject/edit rules (v5.0, or "design intent")

**Non-functional requirements** (properties the system must HAVE):
- **NFR1: Consistency**: Same input + same memory state → same output (temperature=0)
- **NFR2: Replayability**: Can reconstruct memory state at any point by replaying events
- **NFR3: Explainability**: Can trace decisions to specific rules and their provenance
- **NFR4: Freshness**: Rule updates visible to agents within 5 seconds (target)
- **NFR5: Low latency**: Rule retrieval + injection adds < 200ms to response time
- **NFR6: Auditability**: All changes (CRUD, admin actions) logged immutably

**Anti-requirements** (what you explicitly DON'T aim for in v1.0):
- NOT aiming for 1000s of agents (target: 1-10 concurrent agents)
- NOT aiming for millions of rules (target: 10s to 100s)
- NOT aiming for 99.99% uptime (proof-of-concept, not production)
- NOT aiming for automatic rule extraction (v6.0, future work)

**Design goals** (principles guiding architecture):
- **DG1: Simplicity**: Prefer simple, understandable components over complex optimizations
- **DG2: Observability**: Every state change should be traceable
- **DG3: Determinism**: Avoid non-deterministic behavior (e.g., random sampling in retrieval)
- **DG4: Modularity**: Components should be swappable (e.g., FTS5 → RAG, SQLite → Postgres)

### Writing tips
- Use tables to organize requirements (ID, description, rationale)
- Prioritize requirements (MUST, SHOULD, COULD, WON'T - MoSCoW method)
- Explain WHY each requirement matters (trace back to Chapter 1 problem statement)

---

## 3.2 System Overview & Data Flow

**Target length**: 2-3 pages + diagram

### What to write

**High-level architecture diagram** (create in draw.io, Figma, or Mermaid):

```
┌─────────────────┐
│  GitHub Repo    │  (rules as .md files)
│   (or Seeder)   │
└────────┬────────┘
         │ webhook / manual seed
         ▼
┌─────────────────┐
│ EVENTS stream   │  (raw events: InstructionUpserted, InstructionDeleted)
│  (JetStream)    │
└────────┬────────┘
         │ Promoter subscribes
         ▼
┌─────────────────┐
│ Promoter Service│  (gating logic: auto-approve, manual review, rules)
└────────┬────────┘
         │ emits
         ▼
┌─────────────────┐
│ DELTAS stream   │  (approved changes: DeltaAsserted, DeltaRetracted)
│  (JetStream)    │
└────────┬────────┘
         │ DeltaConsumer subscribes
         ▼
┌──────────────────────────┐
│ Projection Service       │  (DeltaConsumer: parse, project, update DB)
│ (DeltaStreamConsumer,    │
│  DeltaParser, Projector) │
└──────────┬───────────────┘
           │ writes to
           ▼
┌─────────────────┐        ┌─────────────────┐
│ Projection DB   │        │ Vector DB       │  (v3.0, future)
│ (SQLite + FTS5) │        │ (pgvector)      │
└────────┬────────┘        └─────────────────┘
         │
         │ MemoryCompiler queries
         ▼
┌─────────────────┐
│ Application API │  (REST endpoints, MCP server)
│  (TOOL API)     │
└────────┬────────┘
         │
    ┌────┴────┬────────────┐
    ▼         ▼            ▼
┌────────┐ ┌────────┐ ┌────────┐
│Agent UI│ │Agent UI│ │ MCP    │
│(Alice) │ │(Bob)   │ │Client  │
└────────┘ └────────┘ └────────┘
```

**Data flow narrative** (walk through an example):
1. **Rule change**: Dev pushes commit to `rules/api/authentication.md`
2. **GitHub webhook** → TOOL API `/api/events/ingest` → publishes `InstructionUpserted` to EVENTS stream
3. **Promoter** subscribes to EVENTS, applies gating logic:
   - If author is trusted → auto-approve
   - Else → queue for admin review
4. **DELTAS emitted**: `DeltaAsserted(im:api.auth@v5, content=..., provenance={commit:abc123})`
5. **DeltaConsumer** (Projection Service) subscribes to DELTAS:
   - Parse delta (DeltaParser)
   - Update DB (Projector): INSERT or UPDATE into `instructions` table
   - Update FTS5 index for search
6. **Agent query**: User asks "How do I authenticate API requests?"
7. **MemoryCompiler**:
   - Extracts keywords: "authenticate", "API"
   - Queries FTS5: `SELECT * FROM instructions_fts WHERE instructions_fts MATCH 'authenticate API' ORDER BY rank`
   - Returns top-5 rules with provenance
8. **Prompt injection**: Rules inserted into system prompt: `[Rule im:api.auth@v5]: Always use JWT tokens for API authentication.`
9. **LLM generates response**, cites rule: "You should use JWT tokens [im:api.auth@v5]."
10. **SSE push notification**: Agent UI receives update via Server-Sent Events (v1.5, if implemented)

### Writing tips
- Diagram is crucial (consider this the most important figure in your thesis)
- Use numbered steps for clarity
- Explain each component's role briefly (detailed in 3.3-3.7)

---

## 3.3 EVENTS, DELTAS, AUDIT Streams

**Target length**: 2 pages

### What to write

**Why three streams?** (CQRS + audit pattern):
- **EVENTS**: Raw input, untrusted (e.g., any GitHub push, manual seed)
- **DELTAS**: Approved changes, ready for projection (governance boundary)
- **AUDIT**: Admin actions, compliance log (who approved/rejected what, when)

**EVENTS stream**:
- **Purpose**: Capture raw events from external sources (GitHub, UI, API)
- **Schema**: `InstructionUpserted`, `InstructionDeleted`, `InstructionTagged`, etc. (see SCHEMAS.md)
- **Retention**: Durable (never deleted), replayable
- **Example**:
  ```json
  {
    "eventType": "InstructionUpserted",
    "instructionId": "im:api.auth",
    "content": "Always use JWT tokens...",
    "author": "alice@example.com",
    "source": "github",
    "provenance": { "commit": "abc123", "repo": "rules", "path": "api/auth.md" }
  }
  ```

**DELTAS stream**:
- **Purpose**: Approved changes that projection should apply
- **Schema**: `DeltaAsserted`, `DeltaRetracted` (minimal, immutable facts)
- **Retention**: Durable, replayable (this is the "source of truth" for memory state)
- **Example**:
  ```json
  {
    "deltaType": "DeltaAsserted",
    "instructionId": "im:api.auth@v5",
    "content": "Always use JWT tokens...",
    "provenance": { "commit": "abc123", "approvedBy": "admin@example.com" },
    "timestamp": "2024-10-15T10:30:00Z",
    "sequenceNumber": 42
  }
  ```

**AUDIT stream**:
- **Purpose**: Log admin actions (approve, reject, manual CRUD)
- **Schema**: `AuditLogEntry` with `action`, `actor`, `resourceId`, `timestamp`
- **Retention**: Durable (compliance requirement)
- **Example**:
  ```json
  {
    "action": "APPROVE_INSTRUCTION",
    "actor": "admin@example.com",
    "resourceId": "im:api.auth",
    "details": { "reason": "Security team approved new auth policy" },
    "timestamp": "2024-10-15T10:29:55Z"
  }
  ```

**Why NATS JetStream?**:
- Lightweight (compared to Kafka)
- Built-in durability, replay, and retention
- Supports multiple consumers (Promoter, DeltaConsumer, Admin UI)
- Easy local development (single binary, no JVM)

**Trade-off**:
- NATS is less mature than Kafka for large-scale deployments
- For proof-of-concept, simplicity wins over scalability

### Writing tips
- Include example JSON payloads (inline or as figures)
- Explain the "why" for each stream (what would break if we merged them?)
- Reference SCHEMAS.md for full schema definitions

---

## 3.4 Promoter: Policy & Decision Logic

**Target length**: 2 pages

### What to write

**Role**: Gatekeeper between EVENTS and DELTAS. Decides: "Should this event become a delta?"

**Gating policies** (v1.0 may implement subset; document design intent for all):
1. **Auto-approve trusted authors**: If `event.author in trustedList`, emit delta immediately
2. **Manual review**: Else, queue for admin approval (stored in `pending_approvals` table)
3. **Content validation**: Reject events with invalid schema, empty content, etc.
4. **Rate limiting**: Reject if author has > N pending events (anti-spam)
5. **Conflict detection** (v2.0+): If rule X conflicts with rule Y, flag for review

**State machine** (for manual review path):
```
EVENTS → [Promoter] → PENDING → [Admin approves] → DELTAS
                              └→ [Admin rejects] → AUDIT (rejected)
```

**Implementation** (in v1.0):
- Promoter is a background service (hosted in TOOL API or separate process)
- Subscribes to EVENTS stream (NATS durable consumer)
- Queries `admins` table for trusted authors
- Publishes to DELTAS stream (if approved) or `pending_approvals` DB (if needs review)

**Why separate Promoter from Projection?**:
- **Separation of concerns**: Gating logic vs. state updates
- **Auditability**: AUDIT stream logs Promoter decisions
- **Flexibility**: Can change gating policy without redeploying Projection service

**Trade-off**:
- Adds complexity (3 streams instead of 1)
- Latency overhead (~10-50ms for Promoter decision)
- Justified by governance requirement (NFR6)

### Writing tips
- Use a state diagram or flowchart
- Provide pseudocode for gating logic
- Discuss what happens if Promoter crashes (NATS redelivers unacked messages)

---

## 3.5 Projection / DeltaConsumer / Database

**Target length**: 2-3 pages

### What to write

**Role**: Consume DELTAS, update Projection DB to reflect current memory state.

**Architecture** (post-refactor, see PR #63):
- **DeltaStreamConsumerService**: NATS consumer, fetches deltas, orchestrates pipeline
- **DeltaParser**: Parse `DeltaAsserted` / `DeltaRetracted` JSON → domain models
- **Projector**: Apply delta to DB (INSERT, UPDATE, DELETE SQL)

**Database schema** (SQLite):
- **`instructions` table**: Current state (instructionId, content, version, provenance, createdAt, updatedAt)
- **`instruction_history` table**: All versions (for audit, time-travel queries)
- **`instructions_fts` virtual table**: FTS5 index for full-text search

**Why SQLite?**:
- Simple, serverless (no separate DB process)
- FTS5 built-in (efficient full-text search)
- Good enough for proof-of-concept (10s-100s of rules)
- Easy to inspect (sqlite3 CLI)

**Trade-off**:
- SQLite is single-writer (not suitable for high-concurrency writes)
- No distributed transactions
- Future: migrate to PostgreSQL + pgvector for v3.0 (RAG)

**Idempotency** (NFR2: Replayability):
- DELTAS have sequence numbers (1, 2, 3, ...)
- Projector tracks `last_processed_sequence` in DB
- On replay: skip already-processed deltas (idempotent updates)
- If delta N is reprocessed, DB state should not change

**Concurrency model**:
- Single-threaded consumer (v1.0) for simplicity
- Future: partition by instructionId hash for parallel processing

**Error handling**:
- If Projector fails (e.g., DB locked), NATS redelivers delta
- Exponential backoff for transient errors
- Dead-letter queue for persistent errors (v2.0+)

### Writing tips
- Include SQL schema (or reference SCHEMAS.md)
- Explain idempotency mechanism (this is key for H3: Replayability)
- Discuss DeltaConsumer refactor (PR #63) as engineering lesson

---

## 3.6 MemoryCompiler & Prompt Injection

**Target length**: 2 pages

### What to write

**Role**: Given user query, retrieve relevant rules and inject into LLM prompt.

**Retrieval pipeline** (v1.0: FTS5):
1. **Query preprocessing**: Extract keywords (e.g., "authenticate API" → ["authenticate", "API"])
2. **FTS5 search**: `SELECT * FROM instructions_fts WHERE instructions_fts MATCH ?`
3. **Ranking**: FTS5's BM25-based scoring (built-in)
4. **Top-K selection**: Return top-5 rules (configurable)
5. **Provenance attachment**: Each rule includes `instructionId`, `version`, `commit`

**Prompt injection strategies**:
- **System prompt injection**: Insert rules into system prompt (before user message)
  ```
  You are a helpful assistant. Follow these rules:
  [Rule im:api.auth@v5]: Always use JWT tokens...
  [Rule im:logging@v2]: Use structured JSON logs...
  ```
- **User message augmentation** (alternative): Append rules to user query
- **Separate "memory" section** (v2.0+): Use XML tags `<memory>...</memory>`

**Relevance threshold** (optional):
- If FTS5 score < threshold, don't inject (avoid noise)
- Threshold tuned via pilot experiments

**Why FTS5 (not RAG)?**:
- **Simplicity**: No need to manage embeddings, vector DB
- **Explainability**: Exact keyword matches are traceable
- **Determinism**: Same query → same results (given same DB state)
- **Speed**: FTS5 query < 10ms for 100s of rules

**Trade-off**:
- FTS5 struggles with semantic similarity (e.g., "auth" vs "authentication" may not match)
- No handling of synonyms, paraphrases
- Future: v3.0 RAG (hybrid FTS5 + embeddings)

**Comparison table** (include in thesis):
| Feature | FTS5 (v1.0) | RAG (v3.0, future) |
|---------|-------------|---------------------|
| Retrieval method | BM25 (lexical) | Dense embeddings (semantic) |
| Determinism | ✅ Yes | ⚠️ Depends on embedding model version |
| Explainability | ✅ Keyword match visible | ⚠️ Black-box similarity |
| Setup complexity | ✅ Low (built-in) | ❌ High (vector DB, embeddings) |
| Semantic matching | ❌ No | ✅ Yes |

### Writing tips
- Provide example of before/after prompt injection
- Explain why determinism is important (NFR1: Consistency)
- Foreshadow v3.0 RAG evaluation (Chapter 5)

---

## 3.7 Admin / CRUD / Oversight Paths

**Target length**: 1-2 pages

### What to write

**Status**: v5.0 (future work), but document design intent.

**Admin UI capabilities**:
- **View pending approvals**: List events awaiting review
- **Approve/reject**: Emit delta (approve) or log rejection (AUDIT)
- **Manual CRUD**: Create, edit, delete rules (bypass GitHub)
- **Provenance view**: See Git history, commit diffs
- **Search rules**: Query projection DB with filters

**Governance workflow**:
```
1. Dev pushes rule to GitHub
2. Promoter queues for review (if untrusted author)
3. Admin reviews in UI:
   - Sees: rule content, author, provenance (GitHub PR link)
   - Actions: Approve → DELTAS, Reject → AUDIT, Edit → new EVENTS
4. If approved, agents see rule within 5s (SSE push)
```

**CRUD → EVENTS → DELTAS flow**:
- Admin creates rule in UI → emit `InstructionUpserted` to EVENTS → Promoter (auto-approve admin) → DELTAS
- Ensures consistency: ALL changes go through event streams (no direct DB writes)

**Why not implement in v1.0?**:
- Time constraint (prioritize core architecture + evaluation)
- Can simulate via CLI seeder for experiments
- v5.0 adds polish, not core functionality

### Writing tips
- Include mockup or wireframe of Admin UI (if available)
- Explain why CRUD still goes through streams (auditability)
- Be honest: "This is design intent; v1.0 uses CLI seeder for simplicity."

---

## 3.8 Replay, Snapshotting & Idempotency

**Target length**: 2 pages

### What to write

**Replay mechanism** (H3: Replayability):
- **Goal**: Reconstruct DB state at sequence N by replaying DELTAS 1..N
- **Algorithm**:
  ```
  1. Clear projection DB (or create new empty DB)
  2. Subscribe to DELTAS from sequence 1
  3. For each delta (DeltaAsserted / DeltaRetracted):
     - Apply to DB (Projector logic)
  4. Stop at sequence N
  5. Verify: compare replayed DB to snapshot at N (if available)
  ```
- **Idempotency requirement**: Applying delta D twice should have same effect as applying once
  - Example: `DeltaAsserted(im:api.auth@v5)` → INSERT or UPDATE (upsert semantics)

**Snapshotting** (v7.0, optional):
- **Purpose**: Speed up replay (don't replay 1 million deltas)
- **Strategy**: Periodically save DB snapshot at sequence N
- **Replay from snapshot**: Load snapshot at N, replay deltas N+1..M
- **Trade-off**: Adds complexity (snapshot storage, invalidation)

**Idempotency in practice**:
- Use `instructionId` as primary key (not autoincrement ID)
- `DeltaAsserted` → `INSERT OR REPLACE`
- `DeltaRetracted` → `DELETE` (idempotent: deleting non-existent row is no-op)

**Testing replayability** (Chapter 6.4):
- Capture DB state at sequence N
- Replay deltas 1..N into empty DB
- Compare: `diff(replayed_db, original_db)` should be empty
- Metric: SRA (State Reconstruction Accuracy) = 1.0 if match

**Why replayability matters**:
- **Debugging**: "What did the agent know at time T?"
- **Auditing**: "Show me the state when decision X was made"
- **Testing**: Replay deltas, verify behavior

### Writing tips
- Provide pseudocode or flowchart for replay algorithm
- Explain idempotency with concrete examples (SQL statements)
- Discuss what happens if delta is malformed (skip? crash? DLQ?)

---

## 3.9 Trade-offs & Design Decisions

**Target length**: 2 pages

### What to write

Reflect on key decisions and their trade-offs. Use a table or list format.

| Decision | Rationale | Trade-off |
|----------|-----------|-----------|
| **Event sourcing (not snapshots)** | Enables replay (NFR2), audit (NFR6) | More complex than direct DB writes |
| **Three streams (not one)** | Separates raw input (EVENTS), approved changes (DELTAS), audit (AUDIT) | Higher latency (~50ms), more moving parts |
| **NATS (not Kafka)** | Lightweight, easy local dev | Less mature, fewer connectors |
| **SQLite (not Postgres)** | Serverless, FTS5 built-in | Single-writer, no distributed transactions |
| **FTS5 (not RAG in v1.0)** | Simple, deterministic, explainable | Misses semantic similarity |
| **Shared DB (not per-agent DB)** | Ensures consistency (NFR1) | Single point of failure (mitigated by durability of DELTAS) |
| **Manual seeder (not auto-extraction)** | v6.0 deferred for time | Requires human curation of rules |

**Alternative designs considered**:
1. **Direct DB updates (no event streams)**:
   - Pro: Simpler, lower latency
   - Con: No audit trail, no replay, harder to reason about state changes
2. **Per-agent memory (not shared)**:
   - Pro: Agent isolation, no contention
   - Con: Consistency impossible (agents diverge)
3. **Vector DB only (no FTS5)**:
   - Pro: Semantic search, scalable
   - Con: Non-deterministic, complex setup, harder to explain

**Design philosophy**:
- **Prefer simplicity** over premature optimization (Knuth: "Premature optimization is the root of all evil")
- **Prefer observability** over black-box systems (all state changes visible in streams)
- **Prefer modularity** over monoliths (can swap FTS5 → RAG, SQLite → Postgres)

### Writing tips
- Be honest about limitations (this builds credibility)
- Explain what you would change if redoing this (shows maturity)
- Connect trade-offs back to requirements (e.g., "We sacrificed X to achieve NFR2")

---

## Chapter Checklist

Before moving to Chapter 4, ensure:

- [ ] All design decisions are justified in terms of requirements
- [ ] Architecture diagram is clear and complete
- [ ] Each component's role is explained (EVENTS, DELTAS, Promoter, Projection, MemoryCompiler)
- [ ] Trade-offs are discussed honestly
- [ ] Transition to Chapter 4 (Implementation) is smooth

---

## Resources

- Your docs/ARCHITECTURE.md (detailed component descriptions)
- Your docs/SCHEMAS.md (event/delta schemas)
- Recent PRs (e.g., #63 DeltaConsumer refactor) for implementation details

---

## Next Steps

1. Create diagrams (architecture, data flow, state machines)
2. Draft sections 3.1-3.6, 3.9 (can do now)
3. Draft section 3.7 (Admin UI) as "design intent" (v5.0 not implemented)
4. Draft section 3.8 (Replay) after testing replayability (v2.0)
5. Get feedback from advisor
6. Proceed to Chapter 4 (Implementation)

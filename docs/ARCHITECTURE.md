# TOOL System Architecture

**Version:** 0.2

TOOL is an event-sourced instruction memory system where multiple agents share a consistent, auditable rulebook with real-time updates, provenance tracking, and administrative oversight.

## Core Properties

| Property | Description |
|----------|-------------|
| **Consistency** | Agents with identical inputs and memory state produce the same output (especially at temperature = 0) |
| **Freshness** | Rule updates propagate with low latency to all agents via event streaming |
| **Explainability** | Every answer traceable to rule IDs and provenance (commit, repo, path) |
| **Administrative control** | Admins can review, override, add, edit, or delete rules/proposals |
| **Replayability** | Full system state can be reconstructed by replaying DELTAS from beginning |
| **Scalability** | Modular design supports evolution, multiple clients, service splitting |

---

## High-Level Architecture

```
             GitHub Webhooks / Seeder
                     │
                     ▼
                  EVENTS (JetStream)
                     │
                 Promoter Service
                     │ emits
                     ▼
                DELTAS (JetStream)
                     │
            Projection + Embedding Service
                     │
        Projection DB (rules, history, provenance)
        Vector store / embeddings index
                     │
                     ▼
             Application API & MCP Server
                     │
   ┌──────────────────┼──────────────────────────┐
   ▼                  ▼                          ▼
Agent UI / Local      IDE / MCP Client           Admin UI
(LLM + chat UI)       (could be Claude, Cursor)   (rule management)
```

---

## Components

### 1. Promoter Service

**Responsibilities:**
- Subscribes to `EVENTS` stream (e.g. `evt.seed.proposal.v1`, `evt.im.proposal.v1`)
- Runs gating/policy logic (canonicalization, deduplication)
- Inserts into promoter DB (versions, seen events)
- Emits `DELTAS` events (`im.upsert.v1`, `im.retract.v1`)
- Records audit decisions (skip, defer, promote, override)

**Database:**
- `promoter_items`: Current state per namespace/item
- `promoter_item_versions`: Full version history
- `promoter_seen_events`: Idempotency tracking
- `promoter_audit`: Decision audit trail

---

### 2. Projection / Embedding Service

**Responsibilities:**
- Durable consumer of `DELTAS` stream
- Maintains projection tables:
  - `im_items_current` (active rules snapshot)
  - `im_items_history` (full version history)
  - `source_bindings` (Git provenance: repo, ref, path, blob_sha)

**Embedding & Vector Index (v3.0 RAG):**
- Embedding computation happens HERE (in projection service, not Promoter)
- On `im.upsert.v1`: Compute embedding via local model (e.g., sentence-transformers) → upsert to vector DB (pgvector, Qdrant, or Milvus)
- On `im.retract.v1`: Mark vector entry as inactive or delete from index
- **Fallback:** If embedding service fails, skip vector upsert but continue SQL projection (vector DB failures must not block rule updates)

**Idempotency & Determinism:**
- Must be idempotent: Applying same DELTA twice yields identical state
- Version numbers act as natural deduplication keys
- Content hashes ensure byte-for-byte reproducibility

**Event Retention & Replay:**
- `DELTAS` stream must be retained indefinitely (or archival policy defined)
- Supports full replay: Drop projection DB → replay all DELTAS from sequence 1 → reconstruct identical state
- **Future (v7.0):** Periodic snapshots + delta tail for faster recovery

**Catch-up & Resync:**
- Agents missing DELTAS (network partition, downtime) can resync:
  - Query `GET /api/v1/state?ns=X` to get current IM hash
  - If hash differs from local cache, fetch full rulebook snapshot or replay missed deltas
- Projection service tracks highest applied sequence number for idempotent replay

---

### 3. Application API & MCP Server

**REST API (Core):**
- HTTP endpoints for chat, search, rule retrieval, admin CRUD
- Synchronous request-response pattern
- Authentication: API key or Bearer token
- Used by Agent UI, Admin UI, and programmatic clients

**MCP Server (Optional Tooling Layer - v4.0):**
- JSON-RPC protocol for IDE/tool integration (Claude Desktop, Cursor, etc.)
- Exposes TOOL as discoverable "tools" and "resources" in MCP ecosystem

**MCP-specific capabilities:**
  - Tool execution: `compile_memory`, `search_rules`, `why` (provenance)
  - Resources: `im://rules/current`, `im://rules/{item_id}` (browsable rule snapshots)
  - Streaming updates (future): Push DELTA notifications via SSE or WebSocket

**Shared Responsibilities:**
- Handles authentication, authZ (agents vs admins)
- Enforces business policies for admin edits, overrides, conflicts
- Logs all operations for metrics & audit

**Why separate MCP from REST?**
- REST is stateless, MCP supports richer tool/resource semantics
- MCP enables external clients (IDEs) to discover and invoke TOOL without custom integration
- Both layers share the same underlying business logic (projection queries, admin workflows)

---

### 4. Agent UI / Local LLM

**UI (React or similar) that:**
- Presents chat interface
- Shows memory injected, allows insertion of rules, etc.

**Real-time update notifications (Hybrid Push + Pull):**

**Push path (primary for UX):**
- Opens SSE (Server-Sent Events) connection to `/api/v1/deltas/stream`
- Server pushes lightweight notifications on each DELTA: `{ ns, item_id, version, event_type, seq }`
- UI displays pop-up/badge: "New rule available: {title}"
- Fetches full rule content on-demand if user clicks notification

**Pull path (fallback for reliability):**
- Maintains `last_known_seq` in local storage
- On reconnection or network recovery: `GET /api/v1/deltas?since={last_known_seq}`
- Reconciles any missed DELTAs (deduplication via sequence numbers)
- Polling interval: 30-60s as safety net (not primary mechanism)

**Idempotency & ordering:**
- Track highest applied sequence number per namespace
- Deduplicate events received via both push and pull
- Apply DELTAs in sequence order to maintain consistency

**Calls API / MCP server to:**
- Fetch relevant rules (`GET /api/v1/search`, `/compile-memory`)
- Compile memory JSON with current IM state
- Submit prompt + memory to LLM (local Ollama or remote)

**Local cache (optional):**
- May maintain SQLite or in-memory cache of active rules
- Invalidate cache entries on DELTA notifications
- Reduces API round-trips for repeated compile-memory calls

---

### 5. Admin UI / Dashboard (v5.0)

Web interface (within Agent UI or separate) that:
- Shows pending proposals
- Shows rule listing, version history, provenance
- Allows admins to add, edit, delete, approve/reject proposals
- Diff views, rollback, confirm operations
- Calls admin endpoints in the API

---

## Event Flow

```
1. GitHub Push / Seeder
   ↓
2. EVENTS stream (evt.seed.proposal.v1)
   ↓
3. Promoter (gating logic)
   ├─ promoter_audit (decision logged)
   └─ DELTAS stream (im.upsert.v1)
      ↓
4. Projection Service (DELTAS consumer)
   ├─ im_items_current (updated)
   ├─ im_items_history (appended)
   ├─ source_bindings (provenance)
   └─ im_vectors (embeddings, v3.0)
      ↓
5. Agent UI (SSE push notification)
   ↓
6. MemoryCompiler (FTS5 + optional vector search)
   ↓
7. LLM Response (with rule citations)
```

---

## Tech Stack

- **Backend:** .NET 9.0 (ASP.NET Core Web API)
- **Event Streaming:** NATS JetStream (durable streams, replay)
- **Database:** SQLite + FTS5 (rule storage + full-text search)
- **Vector DB (v3.0):** pgvector, Qdrant, or Milvus
- **Frontend:** React 19 + Vite (Agent UI)
- **LLM:** Ollama (local) or remote APIs

---

## See Also

- [Database Schemas](./SCHEMAS.md)
- [API Reference](./API.md)
- [Thesis Narrative](./THESIS.md)
- [Evaluation Metrics](../METRICS.md)
- [Version Roadmap](../VERSIONS.md)

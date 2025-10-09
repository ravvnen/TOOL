# TOOL Database Schemas

This document defines the database schemas for TOOL's event-sourced architecture.

---

## Promoter Database

The Promoter service maintains its own state for gating logic, version tracking, and audit trails.

### `promoter_items`

Current state per namespace/item (latest version).

```sql
CREATE TABLE promoter_items (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  is_active INTEGER NOT NULL,
  policy_version TEXT NOT NULL,
  source_repo TEXT NOT NULL,
  source_ref TEXT NOT NULL,
  source_path TEXT NOT NULL,
  source_blob_sha TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id)
);
```

**Key columns:**
- `content_hash`: SHA-256 of normalized content (for deduplication)
- `policy_version`: Version of gating policy that processed this item
- `source_*`: Git provenance (repo, ref, path, blob SHA)

---

### `promoter_item_versions`

Full version history (all versions ever emitted).

```sql
CREATE TABLE promoter_item_versions (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  input_event_id TEXT NOT NULL,
  policy_version TEXT NOT NULL,
  source_repo TEXT NOT NULL,
  source_ref TEXT NOT NULL,
  source_path TEXT NOT NULL,
  source_blob_sha TEXT NOT NULL,
  emitted_at TEXT NOT NULL,
  PRIMARY KEY (ns, item_id, version)
);
```

**Key columns:**
- `input_event_id`: Link back to original EVENTS stream message
- `emitted_at`: Timestamp when DELTA was emitted (for latency metrics)

---

### `promoter_seen_events`

Idempotency tracking: prevents processing same event twice.

```sql
CREATE TABLE promoter_seen_events (
  ns TEXT NOT NULL,
  event_id TEXT NOT NULL,
  PRIMARY KEY (ns, event_id)
);
```

**Usage:**
- Before processing event: `SELECT * FROM promoter_seen_events WHERE event_id = ?`
- If exists: skip (already processed)
- If not: process event, then `INSERT INTO promoter_seen_events`

---

### `promoter_audit`

Decision audit trail for all promoter actions.

```sql
CREATE TABLE promoter_audit (
  ns TEXT NOT NULL,
  decision_id TEXT PRIMARY KEY,
  item_id TEXT,
  action TEXT,              -- "promote", "skip", "defer", "override"
  reason_code TEXT,         -- "new", "updated", "duplicate", "invalid"
  reason_detail TEXT,       -- Human-readable explanation
  policy_version TEXT,
  input_subject TEXT,       -- NATS subject of input event
  input_hash TEXT,          -- Hash of input content
  prior_version INTEGER,    -- Previous version (if update)
  prior_hash TEXT,          -- Previous content hash
  new_version INTEGER,      -- New version (if promoted)
  is_same_hash INTEGER,     -- 1 if content unchanged (duplicate)
  delta_type TEXT,          -- "im.upsert.v1" or "im.retract.v1"
  delta_subject TEXT,       -- NATS subject of emitted DELTA
  delta_msg_id TEXT,        -- NATS message ID of DELTA
  deltas_stream TEXT,       -- Stream name (usually "DELTAS")
  deltas_seq INTEGER,       -- Sequence number in DELTAS stream
  received_at TEXT,         -- When event received
  decided_at TEXT,          -- When decision made
  published_at TEXT,        -- When DELTA published (if applicable)
  latency_ms INTEGER,       -- published_at - received_at
  emitted_at TEXT
);
```

**Usage:**
- Every promoter decision (skip, promote, override) gets logged here
- Used for debugging, metrics (latency Δme = t_emit - t_received), and compliance

---

## Projection Database

The Projection service consumes DELTAS and maintains the canonical rulebook state.

### `im_items_current`

Current active rules (snapshot).

```sql
CREATE TABLE im_items_current (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  is_active INTEGER NOT NULL,   -- 1 = active, 0 = retracted
  policy_version TEXT NOT NULL,
  occurred_at TEXT NOT NULL,     -- Timestamp from DELTA event
  emitted_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id)
);
```

**Key columns:**
- `is_active`: 0 if rule was retracted via `im.retract.v1`
- `occurred_at`: Logical timestamp of when rule changed (for ordering)

**FTS5 Index (SQLite):**
```sql
CREATE VIRTUAL TABLE im_items_fts USING fts5(
  item_id UNINDEXED,
  title,
  content,
  content=im_items_current,
  content_rowid=rowid
);
```

---

### `im_items_history`

Full version history (append-only log).

```sql
CREATE TABLE im_items_history (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  title TEXT NOT NULL,
  content TEXT NOT NULL,
  labels_json TEXT NOT NULL,
  is_active INTEGER NOT NULL,
  policy_version TEXT NOT NULL,
  occurred_at TEXT NOT NULL,
  emitted_at TEXT NOT NULL,
  PRIMARY KEY(ns, item_id, version)
);
```

**Usage:**
- Every DELTA event appends a row here
- Never updated or deleted (immutable audit trail)
- Used for `/why` endpoint (version history), replay verification

---

### `source_bindings`

Git provenance per version.

```sql
CREATE TABLE source_bindings (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  repo TEXT NOT NULL,           -- e.g., "github.com/ravvnen/rules"
  ref TEXT NOT NULL,            -- e.g., "main", "refs/heads/feat/foo"
  path TEXT NOT NULL,           -- e.g., "api/idempotency.md"
  blob_sha TEXT NOT NULL,       -- Git blob SHA-256
  PRIMARY KEY(ns, item_id, version)
);
```

**Usage:**
- Every version links back to exact Git commit/file
- Used for explainability (PCR metric), audit compliance

---

## Vector Database (v3.0 RAG)

For semantic search with embeddings. Schema example for Postgres + pgvector:

### `im_vectors`

```sql
CREATE TABLE im_vectors (
  ns TEXT NOT NULL,
  item_id TEXT NOT NULL,
  version INTEGER NOT NULL,
  embedding VECTOR(384),    -- pgvector extension (384-dim for sentence-transformers)
  doc JSONB,                -- { title, content, labels } for fallback
  PRIMARY KEY(ns, item_id, version)
);

-- Index for vector similarity search (HNSW or IVF)
CREATE INDEX im_vectors_embedding_idx ON im_vectors
USING hnsw (embedding vector_cosine_ops);
```

**Alternative vector DBs:**
- **Qdrant:** Native vector DB with better performance for large datasets
- **Milvus:** Distributed vector search engine
- **SQLite + sqlite-vss:** Lightweight vector extension for SQLite

---

## Event Schemas (NATS Messages)

### EVENTS Stream

**Subject:** `evt.seed.proposal.v1` or `evt.im.proposal.v1`

**Payload:**
```json
{
  "event_id": "uuid-v4",
  "ns": "ravvnen.consulting",
  "item_id": "api.idempotency",
  "title": "Idempotent Request Standard",
  "content": "...",
  "labels": ["api", "reliability"],
  "source": {
    "repo": "github.com/ravvnen/rules",
    "ref": "main",
    "path": "api/idempotency.md",
    "blob_sha": "abc123..."
  },
  "timestamp": "2025-10-08T14:30:00Z"
}
```

---

### DELTAS Stream

**Subject:** `im.upsert.v1` or `im.retract.v1`

**Payload (upsert):**
```json
{
  "ns": "ravvnen.consulting",
  "item_id": "api.idempotency",
  "version": 3,
  "title": "Idempotent Request Standard",
  "content": "...",
  "labels": ["api", "reliability"],
  "is_active": true,
  "policy_version": "v1.0",
  "source": {
    "repo": "github.com/ravvnen/rules",
    "ref": "main",
    "path": "api/idempotency.md",
    "blob_sha": "abc123..."
  },
  "occurred_at": "2025-10-08T14:30:00Z",
  "emitted_at": "2025-10-08T14:30:01Z"
}
```

**Payload (retract):**
```json
{
  "ns": "ravvnen.consulting",
  "item_id": "api.idempotency",
  "version": 4,
  "is_active": false,
  "occurred_at": "2025-10-08T14:35:00Z",
  "emitted_at": "2025-10-08T14:35:01Z"
}
```

---

### AUDITS Stream (future)

**Subject:** `audit.promoter.decision.v1`

**Payload:**
```json
{
  "decision_id": "uuid-v4",
  "ns": "ravvnen.consulting",
  "item_id": "api.idempotency",
  "action": "promote",
  "reason_code": "updated",
  "prior_version": 2,
  "new_version": 3,
  "delta_msg_id": "nats-msg-id",
  "latency_ms": 42,
  "decided_at": "2025-10-08T14:30:01Z"
}
```

---

## See Also

- [Architecture Overview](./ARCHITECTURE.md)
- [API Reference](./API.md)
- [Version Roadmap](../VERSIONS.md)

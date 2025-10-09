# TOOL API Reference

TOOL exposes both REST HTTP APIs and optional MCP (Model Context Protocol) interfaces for agent/IDE integration.

---

## REST API Endpoints

### Webhooks

| Method | Path | Purpose | Input | Output |
|--------|------|---------|-------|--------|
| POST | `/api/v1/webhooks/seed` | Receive seeded proposals | Proposal JSON | 200 OK / error |
| POST | `/api/v1/webhooks/github-event` | Receive GitHub push events | GitHub payload | 200 OK / error |

**Example seed payload:**
```json
{
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
  }
}
```

---

### Agent / Query Endpoints

| Method | Path | Purpose | Input | Output |
|--------|------|---------|-------|--------|
| POST | `/api/v1/chat` | Chat with LLM + memory | `{ prompt, topK?, temperature?, model? }` | `{ compiled_memory, llm_response }` |
| GET | `/api/v1/state?ns=...` | Get namespace status | Query param: `ns` | `{ ns, active_items, im_hash }` |
| GET | `/api/v1/search?ns=...&q=...&k=...` | Search rules (FTS5 + vector) | Query params | List of rule snippets |
| GET | `/api/v1/why?ns=...&id=...` | Get provenance for rule | Query params | Provenance + history |
| POST | `/api/v1/compile-memory` | Compile memory JSON | `{ ns, prompt, topK }` | Memory JSON |

**Example `/api/v1/chat` request:**
```json
{
  "prompt": "How should I handle retries in my API?",
  "topK": 5,
  "temperature": 0.0,
  "model": "llama3:8b"
}
```

**Response:**
```json
{
  "compiled_memory": {
    "ns": "ravvnen.consulting",
    "generated_at": "2025-10-08T14:30:00Z",
    "rules": [
      {
        "item_id": "api.retry-strategy",
        "version": 2,
        "title": "Exponential Backoff Retry Standard",
        "content": "...",
        "labels": ["api", "reliability"]
      }
    ]
  },
  "llm_response": "Based on [im:api.retry-strategy@v2], you should implement exponential backoff..."
}
```

---

### Admin Endpoints (v5.0)

#### Proposal Management

| Method | Path | Purpose | Input | Output |
|--------|------|---------|-------|--------|
| GET | `/api/v1/admin/proposals?ns=...` | List proposals for review | Query param | List of proposals |
| GET | `/api/v1/admin/proposals/{id}` | Get proposal detail | Path param | Full proposal object |
| POST | `/api/v1/admin/proposals/{id}/approve?ns=...` | Approve proposal | — | `{ success: true }` |
| POST | `/api/v1/admin/proposals/{id}/reject?ns=...` | Reject proposal | — | `{ success: true }` |

#### Rule CRUD

| Method | Path | Purpose | Input | Output |
|--------|------|---------|-------|--------|
| GET | `/api/v1/admin/rules?ns=...` | List current rules | Query param | List of rules |
| GET | `/api/v1/admin/rules/{itemId}?ns=...` | Get rule detail | Path param | Full rule + history |
| POST | `/api/v1/admin/rules?ns=...` | Create new rule | `{ title, content, labels }` | Created rule |
| PUT | `/api/v1/admin/rules/{itemId}?ns=...` | Edit existing rule | `{ title, content, labels }` | Updated rule |
| DELETE | `/api/v1/admin/rules/{itemId}?ns=...` | Delete/retract rule | — | `{ success: true }` |

**Example create rule:**
```json
POST /api/v1/admin/rules?ns=ravvnen.consulting
{
  "title": "New Rule Title",
  "content": "Rule content here...",
  "labels": ["api", "security"]
}
```

**Provenance tracking for admin actions:**
- Source: `repo="admin.override"`, `ref="manual"`, `path="admin/{user_id}/{timestamp}"`, `blob_sha=content_hash`
- All admin edits emit DELTAS (same schema as promoter-generated deltas)
- Audit records include: `admin_user_id`, `action` (create/update/delete), `reason` (optional), `timestamp`, `diff` (old vs new content)

---

### Debug Endpoints

| Method | Path | Purpose | Input | Output |
|--------|------|---------|-------|--------|
| GET | `/api/v1/debug/items?ns=...` | Debug listing | Query param | List of items |
| GET | `/api/v1/debug/item/{itemId}?ns=...` | Debug detail | Path param | Current + history + provenance |

---

## MCP (Model Context Protocol) Interface (v4.0)

MCP is a JSON-RPC protocol for IDE/tool integration (Claude Desktop, Cursor, etc.).

### Tools

```typescript
// Search rules by keyword/vector
search_rules({ ns, q, k })
  → list of { item_id, version, title, snippet, labels }

// Compile memory JSON for prompt
compile_memory({ ns, prompt, k })
  → memory JSON { ns, generated_at, rules: [...] }

// Get full rule with provenance
get_rule({ ns, item_id })
  → full rule with provenance

// Get provenance + version history
why({ ns, item_id })
  → provenance + history

// Admin: List proposals
admin_list_proposals({ ns })
  → list of proposals

// Admin: Approve proposal
admin_approve_proposal({ ns, proposal_id })
  → { success: true }

// Admin: Reject proposal
admin_reject_proposal({ ns, proposal_id })
  → { success: true }

// Admin: Create rule
admin_create_rule({ ns, title, content, labels })
  → created rule

// Admin: Update rule
admin_update_rule({ ns, item_id, title, content, labels })
  → updated rule

// Admin: Delete rule
admin_delete_rule({ ns, item_id })
  → { success: true }
```

### Resources

```typescript
// Snapshot of all active rules
im://rules/current
  → JSON array of all active rules

// Full JSON of a rule + history
im://rules/{itemId}
  → { current, history, provenance }

// SSE stream of recent DELTAS (optional, future)
mcp://changes/sse
  → Server-Sent Events stream
```

### Prompts

```typescript
// System prompt template with memory injection
apply_rulebook_prompt
  → System instruction that includes memory JSON, demands citations, etc.
```

**Discovery:**
Clients discover available tools/resources/prompts via:
- `tools/list` → returns all available tools
- `resources/list` → returns all available resources
- `prompts/list` → returns all available prompts
- `tools/call` → invoke a specific tool

---

## Memory / Prompt Composition Workflow

1. **Agent (or UI) sends user prompt to API/MCP:**
   ```json
   { "prompt": "...", "topK": 5, "ns": "ravvnen.consulting" }
   ```

2. **API calls `compile_memory(ns, prompt, topK)`:**
   - Internally, service fetches relevant rules via:
     - **FTS5 full-text search** (lexical matching)
     - **Vector similarity search** (v3.0 RAG, semantic matching)
   - Scores rules by relevance, selects top K

3. **Builds memory JSON:**
   ```json
   {
     "ns": "ravvnen.consulting",
     "generated_at": "2025-10-08T14:30:00Z",
     "rules": [
       {
         "item_id": "api.idempotency",
         "version": 3,
         "title": "Idempotent Request Standard",
         "content": "...",
         "labels": ["api", "reliability"]
       }
     ]
   }
   ```

4. **API prepares LLM messages:**
   ```
   System: Your system template (e.g., "You are a software architecture assistant...")
   System: Memory JSON serialized to string
   User: {prompt}
   ```

5. **Forward to LLM (local Ollama or remote)**, get response

6. **Return to UI/client:**
   ```json
   {
     "compiled_memory": { ... },
     "llm_response": "Based on [im:api.idempotency@v3], you should use request IDs..."
   }
   ```

---

## Admin / Proposal Workflow

### Proposal Lifecycle

```
1. GitHub Push / Seeder
   ↓
2. EVENTS stream (evt.seed.proposal.v1)
   ↓
3. Promoter (gating logic)
   ├─ SKIP → Record audit, do nothing
   ├─ DEFER → Add to proposal pool for admin review
   └─ PROMOTE → Emit DELTA, update projection
```

**Deferred proposals:**
- Appear in `/api/v1/admin/proposals?ns=...`
- Admin inspects title/content/diff + provenance
- Admin approves or rejects:
  - **Approve:** System re-runs promotion or forcibly emits DELTA event
  - **Reject:** System records audit and discards/archives proposal

**Once rules are in `im_items_current`:**
- Visible to agents via `/api/v1/search` and `/compile-memory`
- Included in memory JSON for LLM prompts

---

### Admin Bypass / Direct CRUD

**Admins can directly create/edit/retract rules, bypassing the webhook → proposal → promoter path.**

**Provenance tracking:**
- Source: `repo="admin.override"`, `ref="manual"`, `path="admin/{user_id}/{timestamp}"`, `blob_sha=content_hash`
- All admin edits emit DELTAS (same schema as promoter-generated deltas)
- Audit records include:
  - `admin_user_id`
  - `action` (create/update/delete)
  - `reason` (optional free-text)
  - `timestamp`
  - `diff` (old vs new content, for updates)

**Conflict handling:**
- If admin edits a rule that has pending proposals, system flags conflict in admin UI
- Admin must acknowledge conflict:
  - "Ignore pending proposals" (admin edit takes precedence)
  - "Review proposals first" (reject admin edit, force review)
- Admin edits take precedence: version number incremented past any pending proposals

**Consistency guarantee:**
- Admin actions go through same DELTAS stream → projection service
- No direct DB writes (maintains event sourcing integrity)
- Idempotent: Admin re-applying same edit produces same outcome

---

## Authentication & Authorization

**Current (v1.0 MVP):**
- No authentication (development only)
- Trust model: all clients trusted
**Future (v5.0 Admin CRUD):**
- API key or Bearer token authentication
- Role-based access control (RBAC):
  - `agent`: Read-only access (search, compile-memory, chat)
  - `admin`: Full CRUD access (proposals, rules, audit)
- JWT tokens with claims: `{ role, ns, user_id }`

---

## See Also

- [Architecture Overview](./ARCHITECTURE.md)
- [Database Schemas](./SCHEMAS.md)
- [Thesis Narrative](./THESIS.md)
- [Version Roadmap](../VERSIONS.md)

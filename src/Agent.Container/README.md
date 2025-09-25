# Agent.Container

A lightweight agent container service that:

- Consumes DELTA events from the `DELTAS` JetStream (subject pattern `delta.>`)
- Maintains an Instruction Memory (IM) in SQLite (current + history + source bindings)
- Exposes HTTP endpoints for health, state hash, provenance (why), search, and a stubbed chat interface
- Optional local LLM integration via `LLM_URL` (POST JSON call)

## Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| AGENT_NAME | Agent instance name (for durable consumer id) | `agent-{MachineName}` |
| NS | Namespace scope for items | `default.ns` |
| NATS_URL | NATS server URL | `nats://localhost:4222` |
| AGENT_DB_PATH | SQLite file path | `agent-im.db` |
| LLM_URL | Local LLM endpoint (optional) | (empty) |

## Build & Run

From repo root:

```fish
# build
cd src
 dotnet build Agent.Container/Agent.Container.csproj

# run (listening on default Kestrel ports)
dotnet run --project Agent.Container/Agent.Container.csproj
```

To override namespace & NATS:

```fish
env NS=my.ns NATS_URL=nats://127.0.0.1:4222 dotnet run --project Agent.Container/Agent.Container.csproj
```

## Endpoints

- GET `/health` – basic readiness
- GET `/state` – count + hash of active items
- GET `/why?id=ITEM_ID` – provenance for specific item
- GET `/search?q=term&k=10` – naive or FTS search (if FTS table present)
- POST `/v1/chat` – stubbed chat: `{ "prompt": "..." }`

## Data Model (Agent)

Tables created on startup (if not exist):
- `im_items_current` – active snapshot
- `im_items_history` – version history
- `source_bindings` – repo/ref/path/blob_sha per version
- Optional FTS: `im_fts` + triggers for title/content

## DELTA Consumption

The background service durable name: `agent-{AGENT_NAME}`.
It filters `delta.>` and then ignores events whose `ns` does not match configured `NS`.

## Future Enhancements

- Auth / API keys
- Structured ranking of memory selection
- Streaming LLM responses
- Metrics & tracing

---

Generated scaffold; adjust as needed.

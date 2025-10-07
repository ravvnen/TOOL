# TOOL - The Organized Operating Language

> **Master's Thesis Project**: Event-Sourced, Deterministic Instruction Memory System for Multi-Agent AI

## Overview

TOOL is a centralized, event-sourced rulebook system that provides **deterministic**, **auditable**, and **versioned** instruction memory for AI agents. Built on NATS JetStream with SQLite FTS5 for semantic search.

**Key Features:**
- Event Sourcing with full audit trail (EVENTS → DELTAS → AUDITS)
- Deterministic retrieval: same query + same state = same results
- Provenance tracking: every rule linked to Git source
- FTS5 full-text search with query sanitization
- Memory-as-a-Service REST API

## Architecture

```
User Prompt
    ↓
TOOL API: /api/v1/compile-memory
    ├─ MemoryCompiler (FTS5 + Scoring)
    ├─ SELECT rules FROM im_items_current
    └─ Build JSON with provenance
        ↓
{
  "ns": "ravvnen.consulting",
  "generated_at": "2025-10-07T...",
  "rules": [
    {
      "id": "im:api.idempotency@v1",
      "title": "Idempotent Request Standard",
      "content": "...",
      "provenance": {
        "repo": "github.com/ravvnen/rules",
        "ref": "main",
        "path": "api/idempotency.md",
        "blob_sha": "abc123..."
      }
    }
  ]
}
        ↓
Agent.UI: Local LLM (Ollama)
    └─ Response with citations [im:api.idempotency@v1]
```

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- NATS Server
- Node.js 18+ (for Agent.UI)
- Ollama (optional, for local LLM testing)

### Setup

**Terminal 1 - NATS server:**
```bash
nats-server -js -m 8222 -sd ./nats_store
```

**Terminal 2 - TOOL API:**
```bash
dotnet run --project ./src/TOOL/
```

**Terminal 3 - Agent.UI:**
```bash
cd src/Agent.UI
npm install
npm run dev
```

Visit http://localhost:3000 for the debug UI with Chat, Search, and Compile Memory tabs.

## Development

```bash
# Format code
make format

# Build
make build

# Install pre-commit hooks
make install-hooks
```

## API Endpoints

### `POST /api/v1/compile-memory`
Compile context-specific memory from prompt.

```json
{
  "prompt": "How do I handle retries?",
  "topK": 5,
  "ns": "ravvnen.consulting"
}
```

### `GET /api/v1/search?q=<query>&k=<limit>&ns=<namespace>`
Full-text search across active rules.

### `GET /api/v1/state?ns=<namespace>`
Get namespace state (active item count, IM hash).

### `GET /api/v1/debug/items?ns=<namespace>`
List all items in database.

## Event Sourcing Flow

```
1. Seed Proposal → NATS EVENTS stream
2. Promoter → Evaluates → NATS DELTAS stream
3. DeltaConsumer → Applies to SQLite
4. MemoryCompiler → Reads from im_items_current
```

## Tech Stack

- .NET 9.0 (ASP.NET Core Web API)
- NATS JetStream (Event streaming)
- SQLite + FTS5 (Rule storage + search)
- React 19 + Vite (Debug UI)
- Ollama (Local LLM)

## License

MIT License

---

**Ravvnen** - Master's Thesis

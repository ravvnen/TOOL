# Multi-Agent Rulebook System (TOOL)

[![CI Pipeline](https://github.com/ravvnen/p0/actions/workflows/ci.yml/badge.svg)](https://github.com/ravvnen/p0/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/ravvnen/p0/branch/main/graph/badge.svg)](https://codecov.io/gh/ravvnen/p0)
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

> **Master's Thesis Project**: Event-Sourced, Deterministic Instruction Memory System for Multi-Agent AI Systems

## 🎯 Overview

TOOL (The Organized Operating Language) is a centralized, event-sourced rulebook system that provides **deterministic**, **auditable**, and **versioned** instruction memory for AI agents. Built on NATS JetStream with SQLite FTS5 for semantic search.

### Key Features

- ✅ **Event Sourcing**: Full audit trail via NATS JetStream (EVENTS → DELTAS → AUDITS)
- ✅ **Deterministic Retrieval**: Same query + same IM state = same results
- ✅ **Provenance Tracking**: Every rule linked to Git repo, ref, path, blob SHA
- ✅ **FTS5 Search**: Porter-tokenized full-text search with query sanitization
- ✅ **Memory-as-a-Service**: REST API for compiling context-specific memory JSON
- ✅ **Idempotent Operations**: Content-addressable storage with deduplication

## 📊 Thesis Metrics

| Metric | Target | Status |
|--------|--------|--------|
| Precision@3 | > 50% | ✅ |
| Recall@5 | > 40% | ✅ |
| Avg Latency | < 100ms | ✅ |
| P99 Latency | < 200ms | ✅ |
| Test Coverage | > 60% | ✅ |
| Determinism | 100% | ✅ |

## 🏗️ Architecture

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
    ├─ System prompt with IM JSON
    └─ Response with citations [im:api.idempotency@v1]
```

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK
- NATS Server
- Node.js 18+ (for Agent.UI)
- Ollama (optional, for local LLM testing)

### Terminal 1 - Setup NATS server
```bash
nats-server -js -m 8222 -sd ./nats_store
```

### Terminal 2 - Run TOOL API
```bash
dotnet run --project ./src/TOOL/
```

### Terminal 3 - Seed rules (one-time)
```bash
dotnet run --project ./src/TOOL/ --seed-only
```

### Terminal 4 - Run Agent.UI (debug interface)
```bash
cd src/Agent.UI
npm install
npm run dev
```

Visit http://localhost:3000 for the debug UI with Chat, Search, and Compile Memory tabs.

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Run benchmarks for thesis metrics
dotnet test --filter "FullyQualifiedName~Benchmark"

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

```
tests/TOOL.Tests/
├── Unit/              # MemoryCompiler, Promoter, DeltaConsumer tests
├── Integration/       # Full API endpoint tests
├── Benchmarks/        # Precision@K, Recall@K, MRR, NDCG, Latency
└── Fixtures/          # Test data with 10 known rules corpus
```

## 📈 Quality Gates

PRs to `main` require:

- ✅ All tests passing
- ✅ Code coverage > 60%
- ✅ Benchmark metrics within thesis thresholds
- ✅ Code formatting (`dotnet format`)
- ✅ No unresolved TODO/FIXME comments

## 🎓 Thesis Contributions

1. **Deterministic Memory Compilation**: Reproducible rule selection via event sourcing
2. **Provenance-First Design**: Every rule traceable to source commit
3. **Hybrid Search Strategy**: FTS5 + fallback scoring for robustness
4. **Memory-as-a-Service Pattern**: Centralized IM, distributed inference
5. **Comprehensive Metrics**: Precision, Recall, F1, MRR, NDCG for evaluation

## 📖 API Endpoints

### `POST /api/v1/compile-memory`
Compile context-specific memory from prompt.

**Request:**
```json
{
  "prompt": "How do I handle retries?",
  "topK": 5,
  "ns": "ravvnen.consulting"
}
```

**Response:**
```json
{
  "ns": "ravvnen.consulting",
  "generated_at": "2025-10-07T12:34:56Z",
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
```

### `GET /api/v1/search?q=<query>&k=<limit>&ns=<namespace>`
Full-text search across active rules.

### `GET /api/v1/state?ns=<namespace>`
Get namespace state (active item count, IM hash).

### `GET /api/v1/debug/items?ns=<namespace>`
List all items in database.

## 🔬 Event Sourcing Flow

```
1. GitHub Webhook/Manual Seed
   → POST /api/v1/seed

2. Seed Proposal
   → NATS publish to EVENTS stream
   → Event: { kind: "proposed", item_id: "api.timeout", ... }

3. Promoter Consumes
   → Evaluate against policy
   → NATS publish to DELTAS stream
   → Delta: { action: "upsert", item_id: "api.timeout", ... }

4. DeltaConsumer Applies
   → Upsert to im_items_current
   → Insert to im_items_history
   → Update FTS5 index

5. MemoryCompiler Reads
   → SELECT FROM im_items_current WHERE is_active=1
   → FTS5 MATCH sanitized(prompt)
   → Return with provenance
```

## 🛠️ Tech Stack

- **.NET 9.0**: ASP.NET Core Web API
- **NATS JetStream**: Event streaming with persistence
- **SQLite + FTS5**: Content-addressable rule storage with full-text search
- **Dapper**: Lightweight ORM
- **React 19 + Vite**: Debug UI with Chat, Search, Compile tabs
- **Ollama**: Local LLM for testing (llama3)
- **xUnit + FluentAssertions**: Testing framework

## 📝 CI/CD Pipelines

### CI Pipeline (`.github/workflows/ci.yml`)
- Runs on every push/PR to `main`
- Build + Unit Tests + Integration Tests
- Code coverage reporting (Codecov)
- Benchmark execution on PRs

### PR Quality Checks (`.github/workflows/pr-checks.yml`)
- Code formatting verification
- Test coverage threshold (>60%)
- TODO/FIXME detection
- Thesis metrics reporting (comments on PR)

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 👤 Author

**Ravvnen** - Master's Thesis

---
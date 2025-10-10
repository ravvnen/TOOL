# TOOL - The Organized Operating Language

> **Master's Thesis Project**: Event-sourced instruction memory system for multi-agent AI

TOOL is a deterministic, auditable rulebook system that provides versioned instruction memory for AI agents. Built on NATS JetStream with SQLite FTS5 for full-text search.

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** | System architecture, components, event flow, tech stack |
| **[API.md](./docs/API.md)** | REST API endpoints, MCP protocol, request/response examples |
| **[SCHEMAS.md](./docs/SCHEMAS.md)** | Database schemas, event formats, provenance tracking |
| **[THESIS.md](./docs/THESIS.md)** | Research narrative, contributions, experiments (H1-H5) |
| **[VERSIONS.md](./docs/VERSIONS.md)** | Roadmap, milestones, task breakdown (v1.0 → v7.0) |
| **[evaluation/README.md](./evaluation/README.md)** | Evaluation framework, test datasets, analysis tools |

**Quick links:**
- [CLAUDE.md](./CLAUDE.md) - Project overview for Claude Code
- [CONTRIBUTING.md](./CONTRIBUTING.md) - Development guidelines

---

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- NATS Server
- Node.js 18+ (for Agent.UI)
- Ollama (optional, for local LLM testing)

### Setup

Look at ```CONTRIBUTING.md```

```

Visit **http://localhost:3000** for the debug UI with Chat, Search, and Compile Memory tabs.

---

## 🛠️ Development

```bash
# Format code
make format

# Build all projects
make build

# Install pre-commit hooks
make install-hooks
```

---

## 🔄 Replay & Verification

TOOL supports deterministic state reconstruction via event replay for audit, disaster recovery, and thesis validation (H3).

### Run Replay Test

```bash
# Run integration test to verify SRA = 1.00
dotnet test tests/TOOL.IntegrationTests --filter "ReplayCorrectnessTests"
```

### Trigger Replay via API

```bash
# Replay all DELTAS for namespace
curl -X POST http://localhost:5000/api/v1/admin/replay \
  -H "Content-Type: application/json" \
  -d '{"ns":"ravvnen.consulting"}'

# Response:
# {
#   "ns": "ravvnen.consulting",
#   "eventsProcessed": 145,
#   "activeCount": 39,
#   "imHash": "6F95CF81852EC...",
#   "replayTimeMs": 1234,
#   "startedAt": "2025-10-10T12:00:00Z",
#   "completedAt": "2025-10-10T12:00:01Z"
# }
```

### Run Replay Experiments (H3)

```bash
# Run 10 replay trials and compute statistics
./scripts/run_replay_experiments.sh 10

# Analyze results
python3 analysis/replay_statistics.py experiments/replay_results_*.jsonl

# Output:
# Trials: 10
# SRA Mean: 1.0000 (95% CI: [1.0000, 1.0000])
# Deterministic: ✅ YES
# Hypothesis Supported: ✅ YES
```

---

## 📁 Project Structure

```
TOOL/
├── docs/                   # Documentation
│   ├── ARCHITECTURE.md     # System design
│   ├── API.md             # Endpoint reference
│   ├── SCHEMAS.md         # Database schemas
│   ├── THESIS.md          # Research narrative
│   └── VERSIONS.md        # Roadmap
├── src/
│   ├── TOOL/              # Main API (.NET)
│   │   ├── Modules/
│   │   │   ├── Promotion/         # Event gating & DELTA emission
│   │   │   ├── DeltaProjection/   # DELTAS → projection DB
│   │   │   ├── MemoryManagement/  # Rule retrieval & compilation
│   │   │   ├── SeedProcessing/    # Webhook ingestion
│   │   │   └── Replay/            # Event replay engine (v2.0)
│   │   ├── Infrastructure/        # NATS, SQLite, messaging
│   │   └── Configuration/         # DI, service registration
│   ├── Agent.UI/          # Debug UI (React + Vite)
│   └── TOOL.Evaluation/   # Evaluation infrastructure
├── tests/
│   └── TOOL.IntegrationTests/  # Integration tests (H3 validation)
├── analysis/              # Statistical analysis scripts
├── experiments/           # Experiment results & logs
└── scripts/               # Build, deployment, experiment runners
```

---

## 📄 License

MIT License

---

**Ravvnen** - Master's Thesis, 2025

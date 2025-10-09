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

## 📁 Project Structure

```
TOOL/
├── docs/                   # Documentation
│   ├── ARCHITECTURE.md     # System design
│   ├── API.md             # Endpoint reference
│   ├── SCHEMAS.md         # Database schemas
│   ├── THESIS.md          # Research narrative
│   └── VERSIONS.md        # Roadmap
├── evaluation/            # Evaluation framework
│   ├── data/              # Test datasets
│   ├── analysis/          # Statistical analysis
│   ├── experiments/       # Experiment runs
│   ├── protocols/         # Evaluation protocols
│   └── scripts/           # Automation scripts
├── src/
│   ├── TOOL/              # Main API (.NET)
│   ├── Agent.UI/          # Debug UI (React + Vite)
│   └── TOOL.Evaluation/   # Evaluation infrastructure
└── scripts/               # Build & deployment scripts
```

---

## 📄 License

MIT License

---

**Ravvnen** - Master's Thesis, 2025

# TOOL: Event-Sourced Instruction Memory for AI Agents

> **Master's Thesis Project**: Deterministic, auditable rulebook system for multi-agent LLM systems
> **Author**: Ravvnen
> **Status**: v1.0 in progress (see [VERSIONS.md](./VERSIONS.md))

---

## What is TOOL?

TOOL (The Organized Operating Language) is an event-sourced instruction memory system that enables multiple AI agents to share a **consistent**, **fresh**, and **explainable** rulebook. Rules are versioned, auditable, and traceable to Git provenance.

### Core Properties

- **Consistency**: Identical inputs + same memory state = same output (temperature=0)
- **Freshness**: Rule updates propagate in real-time via event streaming (NATS JetStream)
- **Replayability**: Full system state reconstructable by replaying DELTAS from beginning
- **Explainability**: Every answer traceable to rule IDs and Git provenance
- **Human oversight**: Admins can review, edit, approve/reject rules (v5.0)

---

## Quick Start

See [README.md](./README.md) for setup instructions.

**Prerequisites:**
- .NET 9.0 SDK
- NATS Server
- Node.js 18+ (for Agent.UI)
- Ollama (optional, for local LLM)

**Terminal 1 - NATS:**
```bash
nats-server -js -m 8222 -sd ./nats_store
```

**Terminal 2 - TOOL API:**
```bash
dotnet run --project ./src/TOOL/
```

**Terminal 3 - Agent UI:**
```bash
cd src/Agent.UI && npm install && npm run dev
```

Visit http://localhost:3000 for debug UI.

---
## Workflow
Clauds development process is explciity stated in 
| **[CLAUDE_DEV_PROCESS.md](.CLAUDE_DEV_PROCESS.md)** |

## Documentation

### Technical Documentation

| Document | Description |
|----------|-------------|
| **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** | System components, event flow, tech stack |
| **[SCHEMAS.md](./docs/SCHEMAS.md)** | Database tables, event formats (EVENTS, DELTAS, AUDITS) |
| **[API.md](./docs/API.md)** | REST/MCP endpoints, workflows, authentication |

### Research Documentation

| Document | Description |
|----------|-------------|
| **[THESIS.md](./docs/THESIS.md)** | Research narrative, contributions, related work |
| **[METRICS.md](./docs/METRICS.md)** | Evaluation framework, experiments (H1-H5) |
| **[VERSIONS.md](./docs/VERSIONS.md)** | Roadmap, milestones, priorities (v1.0 → v7.0) |

### Additional Docs

| Document | Description |
|----------|-------------|
| **[DEVELOPMENT.md](./DEVELOPMENT.md)** | Developer setup, coding standards |
| **[CONTRIBUTING.md](./CONTRIBUTING.md)** | How to contribute |
| **[docs/annotation_protocol.md](./docs/annotation_protocol.md)** | Ground truth labeling protocol |
| **[docs/sse_implementation_plan.md](./docs/sse_implementation_plan.md)** | Real-time push notifications plan |

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
Projection Service
    │
Projection DB (SQLite + FTS5)
Vector DB (pgvector, v3.0)
    │
    ▼
Application API
    │
┌───┼───────────┐
▼   ▼           ▼
Agent UI    MCP Client    Admin UI
```

**Event Flow:**
1. Rule change (GitHub push / manual seed) → **EVENTS** stream
2. Promoter (gating logic) → **DELTAS** stream
3. Projection service → SQLite DB (current + history)
4. Agent UI (SSE push notification) → MemoryCompiler (FTS5 search)
5. LLM response with rule citations (e.g., `[im:api.idempotency@v3]`)

See [ARCHITECTURE.md](./docs/ARCHITECTURE.md) for details.

---

## Research Hypotheses

| Hypothesis | Metric | Status |
|------------|--------|--------|
| **H1: Correctness** | Human ratings (Condition A vs B) | 🔴 Not tested |
| **H2: Retrieval Quality** | Precision@5, MRR, NDCG@10 | 🔴 Not tested |
| **H3: Replayability** | SRA (State Reconstruction Accuracy) | 🟡 Implementation in progress (v2.0) |
| **H4: Freshness** | Latency (merge → UI display) | 🟡 SSE implementation in progress |
| **H5: Consistency** | Cited rule ID agreement (temp=0) | 🔴 Not tested |

See [METRICS.md](./docs/METRICS.md) for detailed experimental protocols.

---

## Roadmap & Priorities

| Version | Priority | Description | Status |
|---------|----------|-------------|--------|
| **v1.0** | **MUST** | MVP / Proof of Concept | 🟡 In progress |
| **Evaluation** | **MUST** | 50 prompts, labels, baseline experiments | 🔴 Not started |
| **v2.0** | **MUST** | Replay engine (proves H3) | 🔴 Not started |
| **v3.0** | **SHOULD** | RAG / Hybrid retrieval | 🔴 Not started |
| **v4.0** | **COULD** | MCP (Model Context Protocol) | 🔴 Not started |
| **v5.0** | **COULD** | Admin CRUD / Governance UI | 🔴 Not started |
| **v6.0** | **COULD** | Rule extraction from Markdown | 🔴 Not started |
| **v7.0** | **WON'T** | Snapshots, rollback, performance optimizations | 🔴 Deferred (future work) |

See [VERSIONS.md](./docs/VERSIONS.md) for detailed task breakdown.

**Timeline:**
- Oct 8-22 (2 weeks): Finish v1.0, pilot experiments
- Oct 22 - Nov 26 (5 weeks): v2.0 replay engine
- Nov 26 - Dec 24 (4 weeks): v3.0 RAG (if v2.0 finishes early)
- Dec 24 - Jan 10 (2 weeks): Run experiments, analysis
- Jan 10 - Mar 1 (7 weeks): **Thesis writing**

---

## Tech Stack

- **Backend:** .NET 9.0 (ASP.NET Core Web API)
- **Event Streaming:** NATS JetStream (durable streams, replay)
- **Database:** SQLite + FTS5 (rule storage + full-text search)
- **Vector DB (v3.0):** pgvector, Qdrant, or Milvus
- **Frontend:** React 19 + Vite (Agent UI)
- **LLM:** Ollama (local) or remote APIs

---

## Key Files & Directories

```
p0/
├── CLAUDE.md              ← You are here (project overview)
├── README.md              ← Setup instructions
├── VERSIONS.md            ← Roadmap & milestones
├── METRICS.md             ← Evaluation framework
├── Makefile               ← Build, format, test commands
├── TOOL.sln               ← .NET solution
├── src/
│   ├── TOOL/              ← Core API (Promoter, Projection, API)
│   ├── Agent.UI/          ← React frontend
│   └── TOOL.Evaluation/   ← Experiment runner
├── docs/
│   ├── ARCHITECTURE.md    ← System design
│   ├── SCHEMAS.md         ← Database schemas
│   ├── API.md             ← API reference
│   └── THESIS.md          ← Research narrative
├── data/                  ← Test datasets (prompts, labels)
├── experiments/           ← Experiment logs, results
├── analysis/              ← Jupyter notebooks for analysis
└── scripts/               ← Helper scripts
```

---

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for development workflow, coding standards, and how to submit PRs.

**Quick commands:**
```bash
make format      # Format code (dotnet format)
make build       # Build solution
make test        # Run tests
make install-hooks  # Install pre-commit hooks
```

---

## License

MIT License

---

## Contact

- **Author**: Ravvnen
- **Thesis**: Master's in Computer Science
- **Repository**: [GitHub](https://github.com/ravvnen/p0) (if public)
- **Issues**: Report bugs/feedback at [Issues](https://github.com/ravvnen/p0/issues) (if public)

---

**Last Updated**: 2025-10-08

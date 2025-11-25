# Implementation → Background Mapping

**Purpose:** Work backwards from what's actually implemented to determine what background is necessary and to what depth.

**Date:** November 12, 2025

---

## 📊 Implementation Status Summary

From VERSIONS.md:

| Version | Status | Description |
|---------|--------|-------------|
| **v1.0** | 🟡 **In Progress** | MVP: Promoter, Projection, MemoryCompiler (FTS5), Agent UI, evaluation framework |
| **v2.0** | ✅ **COMPLETE** | Replay engine, retractions (im.retract.v1), determinism tests, SRA validation |
| **v3.0** | 🔴 Not Started | RAG / Hybrid retrieval (embeddings, vector DB) |
| **v4.0** | 🔴 Not Started | MCP (Model Context Protocol) integration |
| **v5.0** | 🔴 Not Started | Admin CRUD / Governance UI |
| **v6.0** | 🔴 Not Started | Rule Extraction from Markdown |
| **v7.0** | 🔴 Deferred | Snapshots, rollback, performance optimizations |

**Key Insight:** Your thesis will focus on **v1.0 (baseline) + v2.0 (replay)** as primary contributions, with v3.0-v7.0 as future work.

---

## 🏗️ What's Actually Implemented

### **v1.0 Components (Core System)**

✅ **Promoter Service**
- Subscribes to EVENTS stream
- Canonicalization, deduplication
- Emits DELTAS (im.upsert.v1)
- Records audit decisions in promoter_audit table
- **Files:** `src/TOOL/Modules/Promotion/PromoterService.cs`

✅ **Projection Service**
- Consumes DELTAS stream (durable consumer)
- Maintains SQLite projection DB:
  - `im_items_current` (active rules)
  - `im_items_history` (version history)
  - `source_bindings` (Git provenance)
- **Files:** `src/TOOL/Modules/DeltaProjection/DeltaStreamConsumerService.cs`, `DeltaProjector.cs`, `DeltaParser.cs`

✅ **MemoryCompiler Service**
- Full-text search using SQLite FTS5 (lexical, NOT semantic)
- Retrieves relevant rules for given prompt
- Builds memory JSON for LLM injection
- **Files:** `src/TOOL/Modules/MemoryManagement/MemoryCompilerService.cs`

✅ **Agent UI**
- React 19 + Vite frontend
- Chat interface, search, compile memory tabs
- Real-time updates via SSE (push notifications for DELTAs)
- **Files:** `src/Agent.UI/`

✅ **NATS JetStream**
- Event streaming infrastructure
- EVENTS stream (proposals)
- DELTAS stream (approved changes)
- Durable consumers, replay support

✅ **Evaluation Framework**
- H1-H5 hypotheses defined
- Experiment infrastructure scaffolded
- **NOT YET RUN** (experiments pending)
- **Files:** `evaluation/experiments/h1_correctness/`, `h2_retrieval/`, `h3_replayability/`

---

### **v2.0 Components (Replay & Retractions) ✅ COMPLETE**

✅ **Replay Engine**
- Drop projection DB → replay all DELTAS → reconstruct state
- Supports both im.upsert.v1 and im.retract.v1
- Computes SRA (State Reconstruction Accuracy)
- **Files:** `src/TOOL/Modules/Replay/ReplayEngine.cs`, `ReplayController.cs`

✅ **Retraction Support**
- im.retract.v1 event type
- Soft delete (is_active=0) in projection
- Version history preserved
- **Implementation:** Promoter.cs:308-487, DeltaProjector.cs:277-319

✅ **Determinism Tests**
- Integration tests validate SRA = 1.00
- Multiple replay consistency checks
- **Files:** `tests/TOOL.IntegrationTests/ReplayCorrectnessTests.cs`

✅ **Idempotency Safeguards**
- `promoter_seen_events` table
- `deltas_seen_events` table
- Nats-Msg-Id headers

---

### **What's NOT Implemented Yet**

❌ **RAG / Embeddings (v3.0)**
- No vector DB (pgvector/Qdrant/Milvus)
- No embedding model integration
- No semantic/hybrid retrieval
- Current: FTS5 lexical search only

❌ **MCP Protocol (v4.0)**
- No external client integration
- No IDE plugin support
- Current: REST API only

❌ **Admin CRUD UI (v5.0)**
- No governance UI for rule management
- No admin approval workflow (Promoter has logic, but no UI)
- Current: Manual seeding via API/webhook

❌ **Rule Extraction (v6.0)**
- No Markdown → rule parser
- No extraction pipeline
- Current: Manual rule creation

---

## 🎯 Implementation → Background Mapping

### **Principle:** Background sections should **only cover what's necessary** to understand v1.0 + v2.0 (what's actually implemented).

---

### **Section 2.1: Agent Memory Architectures**

#### **Why Needed:**
- Shows that existing agent memory systems (Park, MemGPT, Reflexion) are **single-agent focused**
- No shared memory for multi-agent consistency
- No versioning, no governance, no provenance

#### **Implementation Mapping:**
| Background Concept | TOOL Implementation |
|-------------------|---------------------|
| Episodic memory (Park) | TOOL: Shared institutional memory (not episodic) |
| Single-agent memory | TOOL: Multi-agent shared projection DB (`im_items_current`) |
| No versioning | TOOL: Event-sourced DELTAS, `im_items_history` |
| No provenance | TOOL: `source_bindings` (Git commit, file, blob SHA) |
| No real-time updates | TOOL: NATS JetStream + SSE push notifications |

#### **Depth Required:** 2-2.5 pages
- 1 paragraph per system (Park, MemGPT, Reflexion, LangChain, AutoGPT)
- Comparison table (0.5 page)
- Synthesis paragraph (1 para) → gap: multi-agent, versioning, governance

#### **Citations:** 5-6 papers
- Park et al., 2023; Packer et al., 2023; Shinn et al., 2023; Chase, 2022; Richards, 2023; Wang et al., 2024 (survey)

---

### **Section 2.3: Centralized Instruction Management** ⭐ **MOST IMPORTANT**

#### **Why Needed:**
- **Direct comparison** to state-of-the-art: GitHub Copilot, Claude, Cursor
- TOOL builds on their approach (centralized instruction files) but adds:
  1. Event-sourced versioning (not just Git)
  2. Real-time propagation (not manual pull)
  3. Audit trails (not just Git log)
  4. Governance workflows (not just PR)

#### **Implementation Mapping:**
| Feature | Copilot | Claude | Cursor | TOOL |
|---------|---------|--------|--------|------|
| **Storage** | `.github/copilot-instructions.md` | Cloud (project) | `.cursorrules` | Event DB (DELTAS) |
| **Versioning** | Git only | ❌ Overwrite | Git only | ✅ Event-sourced DELTAS + Git |
| **Real-time** | ⚠️ Manual pull | ⚠️ Eventual | ⚠️ Manual | ✅ NATS + SSE push |
| **Audit trail** | Git log | ❌ | Git log | ✅ `promoter_audit` + `source_bindings` |
| **Governance** | ❌ PR only | ❌ | ❌ PR only | ⚠️ v5.0 (Admin UI planned) |
| **Multi-agent** | ❌ | ❌ | ❌ | ✅ Shared projection |
| **Time-travel** | Git checkout | ❌ | Git checkout | ✅ Replay engine (v2.0) |

**Implemented:**
- Promoter Service (gating logic, canonicalization)
- DELTAS stream (versioned updates)
- Projection DB (current + history)
- Real-time push via NATS + SSE
- Provenance tracking (`source_bindings`)

**Not Yet Implemented:**
- Admin UI (v5.0) - governance workflow exists in Promoter, but no UI

#### **Depth Required:** 3-4 pages (LONGEST section)
- Full paragraphs per tool (Copilot, Claude, Cursor, PromptLayer)
- Detailed comparison table (1 page)
- Limitations & gaps (1-2 paragraphs)
- This is your main state-of-the-art comparison!

#### **Citations:** 4-5 sources
- GitHub, 2024; Anthropic, 2024; Cursor, 2024; PromptLayer, 2023; Helicone, 2023

---

### **Section 2.4: Retrieval & Context Injection**

#### **Why Needed:**
1. **Prompt Engineering**: Explains in-context learning foundation (connects to H1 correctness)
2. **RAG**: Brief mention that TOOL uses FTS5 baseline, RAG is v3.0 future work

#### **Implementation Mapping:**
| Background Concept | TOOL Implementation |
|-------------------|---------------------|
| In-context learning (Brown et al., 2020) | TOOL: Memory injection via MemoryCompiler |
| RAG (Lewis et al., 2020) | TOOL: Uses FTS5 (lexical) baseline, NOT semantic RAG |
| Embedding retrieval | ❌ Not implemented (v3.0 future work) |
| Hybrid search | ❌ Not implemented (v3.0 future work) |

**Implemented:**
- MemoryCompiler with FTS5 full-text search
- Prompt injection (memory JSON → system prompt)

**Not Implemented:**
- Embedding model
- Vector DB
- Semantic/hybrid retrieval

#### **Depth Required:** 2 pages
- RAG: 1 paragraph overview, note it's future work
- Prompt engineering: 1 paragraph (few-shot, chain-of-thought)
- Connection to TOOL: Memory injection ≈ in-context learning
- **Keep brief** since RAG not implemented yet

#### **Citations:** 5-6 papers
- Lewis et al., 2020; Gao et al., 2023 (RAG survey); Brown et al., 2020; Wei et al., 2022; Kojima et al., 2022; Ouyang et al., 2022

---

### **Section 2.5: Event Sourcing & Provenance** ⭐ **FOUNDATIONAL**

#### **Why Needed:**
- **Event sourcing is the core architectural pattern** of TOOL
- v2.0 replay engine (COMPLETE) validates H3 (replayability)
- Provenance tracking enables explainability

#### **Implementation Mapping:**
| Event Sourcing Concept | TOOL Implementation |
|------------------------|---------------------|
| **Event Log** | NATS EVENTS stream (proposals) |
| **Approved Changes** | NATS DELTAS stream (im.upsert.v1, im.retract.v1) |
| **Projection** | SQLite DB (`im_items_current`, `im_items_history`) |
| **Replay** | ReplayEngine.cs: Drop DB → replay DELTAS → reconstruct |
| **Audit Trail** | `promoter_audit` table, `source_bindings` |
| **Provenance** | Git commit hash, repo, path, blob SHA |
| **Idempotency** | `promoter_seen_events`, `deltas_seen_events` |
| **Determinism** | Content hashes, version numbers, SRA = 1.00 |

**v2.0 Replay Engine (COMPLETE):**
- `ReplayEngine.cs`: Stateless replay from DELTAS stream
- `ReplayController.cs`: API endpoint (`POST /api/v1/admin/replay`)
- Integration tests: `ReplayCorrectnessTests.cs`
- Experiment harness: `scripts/run_replay_experiments.sh`
- Analysis: `analysis/replay_statistics.py`

#### **Depth Required:** 2-3 pages (SUBSTANTIAL)
- Event sourcing pattern (2 paragraphs): Fowler, Kleppmann
- Application to LLM agent memory (1 paragraph): Why it's rare but valuable
- Provenance tracking (1 paragraph): Why/where provenance (Buneman, Cheney)
- TOOL's architecture mapping (table/diagram)
- **This section is critical** - shows your main contribution!

#### **Citations:** 6-7 papers
- Fowler, 2005; Vernon, 2013; Kleppmann, 2017; Richardson, 2018; Buneman et al., 2001; Cheney et al., 2009; Kreps et al., 2011 (Kafka)

---

### **Section 2.6: Summary & Gap Analysis**

#### **Why Needed:**
- Synthesize all previous sections
- State the research gap TOOL addresses
- Lead into Chapter 3 (your solution)

#### **Gap TOOL Addresses:**
No existing system provides **all of**:
1. ✅ **Shared memory** for multi-agent consistency (v1.0 projection DB)
2. ✅ **Event-sourced versioning** and replay (v2.0 replay engine COMPLETE)
3. ⚠️ **Governance workflows** with human oversight (v5.0 planned, Promoter logic exists)
4. ✅ **Full provenance tracking** (source_bindings)
5. ✅ **Real-time propagation** (NATS + SSE)

#### **Depth Required:** 1 page
- 5 paragraphs (one per gap)
- Clear statement of what TOOL provides
- **NOTE:** Governance UI (v5.0) not implemented yet, but Promoter has audit logic

#### **Citations:** None (synthesis)

---

## 🔄 Revised Outline Validation

The **realistic outline** (`chapter_02_background_OUTLINE_REALISTIC.md`) is **well-aligned** with your actual implementation!

| Section | Pages | Rationale |
|---------|-------|-----------|
| 2.1 Agent Memory | 2-2.5 | Shows gap: no multi-agent, versioning, governance |
| 2.3 Centralized Tools | 3-4 | **MOST IMPORTANT** - direct comparison to Copilot/Claude/Cursor |
| 2.4 RAG + Prompt | 2 | Brief (RAG is v3.0 future work, prompt eng. explains H1) |
| 2.5 Event Sourcing | 2-3 | **FOUNDATIONAL** - v2.0 replay engine COMPLETE! |
| 2.6 Gap Analysis | 1 | Synthesize all sections |
| **TOTAL** | **10-12** | **Realistic for Master's thesis** |

---

## 🚀 Key Insights for Writing

### **What to Emphasize:**

1. **Section 2.3 (Centralized Tools) is your main comparison** - Copilot/Claude/Cursor are acknowledged in Chapter 1, detailed comparison here
2. **Section 2.5 (Event Sourcing) is your main contribution** - v2.0 replay is COMPLETE and validates H3!
3. **v3.0 (RAG) is future work** - mention briefly in 2.4, don't overemphasize
4. **v5.0 (Admin UI) is future work** - mention in gap analysis, but Promoter audit logic exists

### **What NOT to Do:**

❌ Don't write extensively about RAG/embeddings (not implemented)
❌ Don't write extensively about MCP (not implemented)
❌ Don't claim full governance UI exists (v5.0 planned)
❌ Don't write PhD-level exhaustive coverage (Master's = focused!)

### **What to Highlight:**

✅ v2.0 replay engine is **COMPLETE** - this proves H3 (determinism)!
✅ Promoter + DELTAS + Projection = core event-sourced architecture
✅ FTS5 baseline (lexical search) is what you're evaluating (H2)
✅ Provenance tracking (`source_bindings`) enables explainability
✅ Real-time push (NATS + SSE) proves freshness (H4)

---

## 📝 Writing Priority

Based on this mapping, here's the recommended writing order:

**1. Section 2.3 (Centralized Tools) - WRITE FIRST** ⭐
- Most important state-of-the-art comparison
- Move detailed tool descriptions from Chapter 1 here
- 3-4 pages, detailed comparison table

**2. Section 2.5 (Event Sourcing) - WRITE SECOND** ⭐
- Foundational to your contribution
- v2.0 replay engine is COMPLETE!
- 2-3 pages, architecture mapping

**3. Revise Section 2.1 (Agent Memory) - WRITE THIRD**
- Trim existing draft from 3 pages → 2-2.5 pages
- Make concise (1-2 paragraphs per system)

**4. Section 2.4 (RAG + Prompt) - WRITE FOURTH**
- Brief (2 pages)
- Focus on in-context learning connection to H1
- Mention RAG as future work

**5. Section 2.6 (Gap Analysis) - WRITE LAST**
- 1 page synthesis
- Clear research gap statement

---

## ✅ Validation Checklist

Before finalizing Chapter 2, verify:

- [ ] Every background concept maps to something you **actually implemented** (v1.0 + v2.0)
- [ ] v3.0-v7.0 features mentioned only as **future work** (brief)
- [ ] Section 2.3 is longest (3-4 pages) - state-of-the-art comparison
- [ ] Section 2.5 emphasizes v2.0 replay engine (COMPLETE!)
- [ ] Total length: 10-12 pages (20-25% of 60-page thesis)
- [ ] Total citations: 20-25 (Master's level, not PhD)
- [ ] Each system described concisely (1-2 paragraphs, not full pages)
- [ ] Comparison tables used for visual clarity
- [ ] Gap analysis clearly states what no existing system provides

---

**Status:** Mapping complete! Ready to start writing Section 2.3 (Centralized Tools). 🚀

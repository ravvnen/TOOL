# TOOL: Thesis Summary for Supervisor Meeting

**Student:** Ravvnen
**Date:** December 2024
**Thesis Title:** Event-Sourced Instruction Memory for Multi-Agent LLM Systems

---

## The Problem (Why This Matters)

Large Language Models (ChatGPT, Claude, etc.) have a critical weakness: **they forget rules**. When organizations deploy AI agents that must follow policies, regulations, or business rules, there's no reliable way to:

1. Ensure all agents follow the **same rules consistently**
2. **Trace** why an AI gave a specific answer
3. **Update rules** and have all agents immediately reflect changes
4. **Prove** the system behaves deterministically (same input = same output)

This is a real problem for enterprises deploying AI at scale.

---

## My Solution: TOOL

TOOL is an **event-sourced instruction memory** system. Think of it as a shared, auditable "rulebook" that multiple AI agents can read from.

**Key Innovation:** Every rule change is recorded as an immutable event. This means:
- We can **replay** all events and reconstruct any past state
- Every AI answer can **cite exactly which rules** it used
- Rule updates **propagate instantly** to all connected agents
- The system is **deterministic** and auditable

---

## Architecture Diagrams

Visual diagrams are available in `/diagrams/` folder. Render at: https://www.plantuml.com/plantuml/uml/

| Diagram | What It Shows |
|---------|---------------|
| `event_flow.puml` | High-level: EVENTS → Promoter → DELTAS → Projection |
| `c4_1_context.puml` | System context: TOOL + external actors |
| `c4_2_container.puml` | Internal components: API, NATS, SQLite, UI |
| `seq_2_agent_query.puml` | How an agent queries rules and gets cited answers |
| `hypotheses_mapping.puml` | How H1-H5 map to system components |

---

## Research Questions

| # | Question |
|---|----------|
| RQ1 | Does structured rule memory improve AI answer quality? |
| RQ2 | Can we retrieve the right rules for a given question? |
| RQ3 | Is the system truly replayable (same events = same state)? |
| RQ4 | How fast do rule updates reach the AI agents? |
| RQ5 | Do multiple agents give consistent answers? |

---

## What I Have Built (Implementation Status)

| Component | Status | Description |
|-----------|--------|-------------|
| Event Streaming | ✅ Done | NATS JetStream for durable event logs |
| Rule Projection | ✅ Done | SQLite database with full-text search |
| Agent API | ✅ Done | REST API for agents to query rules |
| Memory Compiler | ✅ Done | Injects relevant rules into LLM prompts |
| Replay Engine | ✅ Done | Reconstructs state from event history |
| Agent UI | ✅ Done | React frontend for testing/debugging |
| RAG/Embeddings | 🔄 Planned | Vector search for semantic retrieval |
| Admin UI | 🔄 Planned | Human oversight and rule editing |

---

## Evaluation Plan

| Hypothesis | Method | Dataset |
|------------|--------|---------|
| H1: Correctness | Human evaluation (A/B comparison) | 50 test prompts |
| H2: Retrieval | Precision@5, MRR, NDCG | 50 prompts with ground truth |
| H3: Replayability | Automated replay verification | 1000+ events |
| H4: Freshness | Latency measurement | Timestamp analysis |
| H5: Consistency | Cross-agent agreement | 50 prompts × 3 agents |

---

## Academic Contribution

This thesis contributes:

1. **Novel architecture** combining event sourcing with LLM instruction memory
2. **Formal metrics** for evaluating rule-based agent systems
3. **Empirical evidence** on consistency, freshness, and replayability
4. **Open-source implementation** as research artifact

---

## Questions I Need Feedback On

1. Is the **scope appropriate** for a master's thesis?
2. Are the **5 hypotheses** the right ones to test?
3. Is the **evaluation methodology** rigorous enough?
4. What would make this thesis **excellent** vs. just acceptable?
5. Are there **related works** I should definitely cite?

---

## Timeline to Submission (March 1, 2025)

- **Dec-Jan:** Complete RAG implementation, run all experiments
- **Jan-Feb:** Write thesis chapters, analyze results
- **Feb:** Review, revisions, final submission


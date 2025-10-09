# Thesis Narrative: Event-Sourced Instruction Memory for Multi-Agent Systems

## 1. Introduction & Motivation

Modern large language models (LLMs) are powerful at generating fluent text, reasoning, and synthesizing knowledge, but they have critical limitations in **long-term consistency**, **traceability**, and **governance**. In practice, teams often need to enforce evolving rules, policies, and compliance constraints (e.g. security rules, business logic, regulatory guidelines). If an LLM "forgets" a rule, or its outputs drift, this can lead to unpredictable or undesirable behavior.

The motivation of this thesis is to bridge the gap between black-box generative LLMs and a more structured, auditable, rule-driven **instruction memory** that agents consult and reason over. We address questions such as:

- How can multiple agent instances maintain **consistent, fresh, explainable memory** over changing rules?
- How can we support **human oversight / admin editing** of rules while preserving auditability and minimal divergence?
- How do we measure whether agents remain **consistent, responsive, and justifiable** under rule changes?

TOOL implements a concrete architecture to explore these questions through an event-sourced, deterministic instruction memory system.

---

## 2. Problem & Research Gaps

Here are the key problems this thesis seeks to address, and where prior literature is weak or silent.

### 2.1 Memory Drift & Consistency in Agent Systems

In real-world systems, multiple agents (or even the same agent at different times) may need to reason using the same institutional memory (rules, policies). Without a shared, consistent memory, their responses may diverge. Current LLM systems often embed memory ad hoc (prompts, caches) which is not robust under updates or scaling.

### 2.2 Lack of Explainability & Traceability in LLM Answers

LLMs are "black boxes." If an AI system recommends or enforces something, teams need to justify: "why did you decide that?" To support that, system decisions should tie back to explicit rules, provenance (commit, author, version). This is central in fields like **Explainable AI (XAI)** where transparency and accountability are crucial.

### 2.3 Administrative Oversight & Governance

Often, rules evolve, exceptions arise, or humans need to edit or override memory. Systems rarely support clean admin CRUD over policies with audit trails. Integrating human-in-the-loop edits while preserving consistency and audit is a nontrivial design challenge.

### 2.4 Scalability & Freshness of Rule Propagation

As the rule space grows, or multiple agents spin up, we need a mechanism to propagate rule changes quickly, cheaply, and reliably without reloading entire models or recomputing from scratch. Replay of full logs every query is too expensive.

### 2.5 Replayability & Determinism in Agent Memory

Event-sourced systems promise that the current state is fully derivable from an immutable event log. For agent systems, this means:
- **Reproducibility**: Replay DELTAS → reconstruct exact state
- **Auditability**: Every change has a causal trace
- **Time travel**: Inspect system state at any past timestamp

Existing LLM systems lack this property, making debugging and compliance verification difficult.

### 2.6 Evaluation Methods in Agent-Rule Systems

There is little standard methodology for evaluating multi-agent systems with rulebooks: e.g. how do you measure "consistency across agents," "freshness of updates," or "explainability correctness"?

Thus this work fills a gap at the intersection of **agent memory architectures**, **explainable AI**, **event sourcing**, and **human governance of AI behavior**.

---

## 3. Contributions

TOOL embodies several novel contributions:

### 3.1 Event-Sourced Instruction Memory Architecture

- Use of **EVENTS → Promoter → DELTAS → Projection** pipeline
- **Idempotent, stateful representation** (current + history)
- **Full replay support**: Drop projection DB → replay all DELTAS from sequence 1 → reconstruct identical state
- **Audit trail**: Every rule change, promoter decision, and admin action logged with provenance

### 3.2 Rule-Injected Agent Containers with Provenance Support

- Each agent can subscribe to DELTAS (or local DB) and maintain memory
- **MemoryCompiler** injects structured JSON into prompts (FTS5 + optional vector search)
- LLM outputs cite rule IDs (e.g., `[im:api.idempotency@v3]`)
- System can answer `/why` by looking up provenance (Git commit, file, blob SHA)

### 3.3 Admin CRUD & Oversight Integration (v5.0)

- Admin UI / API endpoints to review, approve, override, add, edit, delete rules
- Audit logs record every admin and promoter decision, with rationale and diff
- Ensures **humans remain in control** of institutional memory
- Admin edits emit DELTAS (same event sourcing integrity)

### 3.4 Metrics & Experimental Framework

- **Correctness (H1)**: Baseline comparison (vanilla LLM vs TOOL-augmented)
- **Retrieval Quality (H2)**: Precision@K, MRR, NDCG for rule search
- **Replayability (H3)**: State Reconstruction Accuracy (SRA), idempotency verification
- **Freshness (H4)**: Latency metrics (merge → emit → apply → compile)
- **Consistency (H5)**: Cited rule ID agreement across repeated runs

### 3.5 Separable / Evolvable System Design

- Unified API + modular core, but architecture supports splitting services later
- **MCP (Model-Context Protocol)** integration allows IDE / external client access (v4.0)
- Room for **vector/RAG integration** (embedding retrieval) as v3.0 enhancement

Together, these provide a working testbed for exploring how **structured memory + LLMs + human oversight** can co-exist in a controlled, measurable system.

---

## 4. System Architecture → Research Goals Mapping

| Goal / Research Question | Architectural Feature | Explanation / Justification |
|--------------------------|----------------------|----------------------------|
| **Consistency across agents** | Shared DELTAS + projection DB + memory injection | All agents see the same rule deltas in order; memory injection ensures consistent context |
| **Freshness / low latency** | Durable NATS consumers, streaming updates, incremental projection | Instead of replaying full logs per query, state is updated incrementally |
| **Explainability / traceability** | Audit logs (`promoter_audit`), `source_bindings`, memory IDs in responses | Every answer can be traced to specific rule versions and sources |
| **Replayability / determinism** | Immutable DELTAS stream, idempotent projection, content hashes | Drop DB → replay → reconstruct identical state (proves H3) |
| **Human oversight & control** | Admin CRUD, proposals, overrides integrated | Admins can intervene, fix rule gaps, approve/reject proposals |
| **Evaluation & measurement** | Metric definitions + instrumentation in pipeline | Collect data on divergence, latency, explanation accuracy, replay fidelity |
| **Scalability & extensibility** | Modular monolith → service split, vector integration later | Separate business logic from API, making it easier to refactor or extend |

---

## 5. Relation to Broader AI / Memory Theory

To situate this work in existing research:

### Memory Architectures in AI Agents
Recent work explores combining episodic, semantic, and procedural memory in agents. TOOL creates a structured, symbolic long-term memory (rulebook) that agents consult, external to the LLM's neural weights.

### Memory-Augmented Neural Networks
The idea that neural systems benefit from external memory stores (e.g. Neural Turing Machines, Differentiable Neural Computers) is well studied. TOOL externalizes memory into a rule database, which acts as a separate, inspectable memory module.

### Explainable AI (XAI)
TOOL operationalizes explainability by forcing rule citations, provenance, auditability. This aligns with research that calls for transparent AI systems in safety-critical domains (medical, legal, financial).

### Cognitive / Symbolic Architectures
TOOL's rulebook + memory injection echoes symbolic architectures (e.g. production systems, Soar, ACT-R) where rules are matched against working memory. TOOL bridges this with modern LLM reasoning.

### Event Sourcing & Distributed Systems
Concepts from event sourcing (immutable logs, replay, idempotency) and distributed systems (consensus, causal ordering) inform TOOL's architecture, ensuring deterministic state reconstruction.

### Human-in-the-Loop Systems
Many AI systems require human governance; TOOL's admin CRUD, audit, override capabilities provide a concrete instantiation of this principle, maintaining event sourcing integrity even with manual interventions.

Thus TOOL bridges **symbolic architectures**, **LLM-driven reasoning**, **event sourcing**, and **human oversight** in a unified, measurable framework.

---

## 6. Experimental & Validation Plan

In the thesis, experiments will validate the following claims:

### Experiment 1: Correctness (H1)
**Hypothesis:** Answers produced with TOOL's instruction memory are more correct than vanilla LLM.

**Method:**
- 50 prompts (stratified across 5 categories: API design, security, data modeling, error handling, general)
- Condition A: Vanilla LLM (no memory)
- Condition B: TOOL-augmented LLM
- 2+ human raters, Likert scale (1-5) for accuracy, completeness, safety
- Inter-rater reliability: Cohen's κ > 0.7
- Paired t-test, α = 0.017 (Bonferroni correction)

**Expected outcome:** Condition B significantly outperforms Condition A (p < 0.017, Cohen's d > 0.5)

---

### Experiment 2: Retrieval Quality (H2)
**Hypothesis:** FTS5 full-text search achieves acceptable retrieval quality (P@5 > 0.70, MRR > 0.75).

**Method:**
- 50-100 prompts annotated with ground truth relevant rules (graded 0/1/2)
- Inter-rater agreement κ > 0.7
- Metrics: Precision@5, Recall@10, MRR, NDCG@10
- Compare to random baseline (expected P@5 ≈ 0.20)

**Expected outcome:** P@5 > 0.70, MRR > 0.75, significantly better than random (p < 0.017)

---

### Experiment 3: Replayability & Determinism (H3)
**Hypothesis:** Replaying DELTAS from scratch produces byte-for-byte identical state (SRA = 1.00).

**Method:**
- Snapshot current projection DB: `hash_original = SHA256(im_items_current)`
- Drop projection tables
- Replay all DELTAS from NATS stream (sequence 1 → current)
- Compute `hash_replayed = SHA256(im_items_current)`
- Compare: `SRA = (hash_original == hash_replayed) ? 1.0 : 0.0`
- Run 10+ trials at different snapshot times

**Expected outcome:** SRA = 1.00 (100% success rate), median replay time < 1s for 1000 rules

---

### Experiment 4: Freshness (H4)
**Hypothesis:** Rule updates propagate with low latency (median Δ_total < 100ms, p95 < 200ms).

**Method:**
- Track timestamps: `t_merge`, `t_emit`, `t_apply_agent`, `t_ui_display`
- Compute intervals: Δ_me, Δ_ea, Δ_ma, Δ_total
- Compare push (SSE) vs pull (polling) methods
- Report median, p90, p95, count late arrivals (>SLO)

**Expected outcome:** Push median < 100ms, pull median ~15-30s (demonstrates real-time advantage)

---

### Experiment 5: Consistency (H5)
**Hypothesis:** Repeated runs with temperature=0 produce stable outputs (cited rule agreement > 0.95).

**Method:**
- 20 prompts × 10 runs = 200 total queries
- Fix temperature=0, same prompt, same IM state hash
- Metric: `Agreement = |rules_run1 ∩ ... ∩ rules_run10| / |rules_run1 ∪ ... ∪ rules_run10|`
- Report mean agreement across 20 prompts

**Expected outcome:** Mean agreement > 0.95 (nearly perfect consistency at temperature=0)

---

## 7. Limitations & Risks

**Scalability ceiling:**
- Current architecture (SQLite + FTS5) suitable for 1K-10K rules
- At 10K+ rules: FTS5 latency increases, vector index size grows
- At 100K+ rules: Need PostgreSQL + pgvector, partitioning, caching
- Thesis scope: Target 1K-5K rules (realistic for enterprise rulebook)

**Context window constraints:**
- Large rulesets may exceed LLM context capacity
- Need selection/summarization heuristics (MemoryCompiler)

**Policy conflict / rule collisions:**
- Two rules might conflict; system must detect and resolve or require human resolution

**Latency bottlenecks:**
- Embedding computation (v3.0 RAG), DB hot spots, network latency may slow compilation

**Drift & divergence under fallback:**
- If agent fails to receive DELTAS (network partition), divergence may occur
- Catch-up logic via replay/resync mitigates this

**Explainability trade-off:**
- Mandating rule citations constrains LLM flexibility; may degrade fluency if rules are dense

**Security / authorization:**
- Admin endpoints need robust auth (v5.0); exposing too much could be dangerous

**Dependency on rule correctness:**
- If rulebook is incorrect/incomplete, system recommendations will suffer
- System does not "learn" rules automatically (future work: agent-proposed rules)

---

## 8. Implications, Broader Impact & Future Work

### Governed AI Systems
TOOL's architecture is a model for combining human-authored rule systems with LLM flexibility in domains that require auditability (compliance, legal, finance, healthcare).

### Multi-Agent Coordination
The pattern scales: multiple agents, even across different services, can share a consistent institutional memory via event sourcing.

### MCP / Tool Integration (v4.0)
By exposing memory and reasoning via MCP tool interfaces, external tools, IDEs, or even other agents can interact with TOOL as a "memory / knowledge service."

### Learning & Adaptation (Future Work)
Allow agents to propose new rules, or validate/verify rule suggestions through aggregated usage data.

### Hybrid Rule + Neural Architecture
TOOL sits between symbolic (rule-based) and neural (LLM) systems. Exploring integration with neural embeddings (RAG, v3.0) or neural rule induction would be natural next steps.

### Explainable AI Research Contribution
TOOL provides a working system that operationalizes explainability in LLM + memory systems, which may inform XAI research, especially for generative systems.

### Thesis Contributions to Academia
TOOL supplies:
- An **architecture** (event-sourced instruction memory)
- A **metric framework** (correctness, retrieval, replayability, freshness, consistency)
- A **working prototype** (.NET + NATS + SQLite + React)
- **Empirical evaluation** (H1-H5 experiments with statistical rigor)

This can be referenced by future research on memory, multi-agent systems, explainable AI, or event-sourced architectures.

---

## See Also

- [Architecture Overview](./ARCHITECTURE.md)
- [Database Schemas](./SCHEMAS.md)
- [API Reference](./API.md)
- [Evaluation Metrics](../METRICS.md)
- [Version Roadmap](../VERSIONS.md)

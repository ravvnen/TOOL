# Chapter 1: Introduction

**Status**: ✅ READY TO WRITE NOW
**Target completion**: November 13, 2025
**Estimated length**: 4-6 pages

---

## Writing Instructions

This chapter sets up your entire thesis. Write it FIRST because:
1. It forces you to clarify your scope and claims
2. It helps you decide what's essential vs. optional (RAG? Extractor?)
3. It provides a north star for all other chapters

**Tone**: Accessible but precise. Assume reader is a CS grad student familiar with ML/systems but not an LLM expert.

**What you MUST accomplish**:
- Hook the reader with a compelling problem
- Clearly state what you built and why
- Define your research questions and hypotheses
- Preview your contributions and findings

---

## 1.1 Motivation & Problem Statement

**Target length**: 1.5-2 pages

### What to write

**Opening hook** (1 paragraph):
Start with a concrete scenario that illustrates the problem:
- "Imagine two software developers, Alice and Bob, both using AI coding assistants in the same repository..."
- "Alice asks her agent to suggest logging practices and receives advice to use structured JSON logs..."
- "Bob asks the same question 10 minutes later and is told to use printf-style logs..."
- "Both agents are correct in general, but now the codebase has inconsistent practices and nobody documented the team's decision."


**The divergence problem** (2-3 paragraphs):
- In multi-agent systems, agents lack shared knowledge/memory
- Each agent starts from scratch (or a generic system prompt)
- Knowledge drift: as rules evolve (e.g., team decides "always use async/await"), agents don't learn unless prompts are manually updated
- Current solutions: wikis, Confluence, static docs → agents don't read these, humans forget to update them

**The governance problem** (2-3 paragraphs):
- When agents make decisions (especially in production: code review, security analysis, customer support), we need:
  - **Consistency**: same input → same decision (especially with temperature=0)
  - **Explainability**: "why did the agent say X?" → traceable to specific rules
  - **Auditability**: "which version of rule Y was active when agent made decision Z?"
  - **Freshness**: rule updates propagate quickly to all agents
  - **Human oversight**: admins can approve/reject/edit rules

**Why existing tools fall short** (2-3 paragraphs):
- **RAG systems**: black-box retrieval, no versioning, no governance, not replayable
- **Prompt databases**: static, no event history, manual sync
- **Fine-tuning**: expensive, opaque, can't explain which training data led to output
- **Agent frameworks (LangChain, AutoGPT)**: focus on orchestration, not shared memory/governance

**Problem statement** (1 paragraph, clear thesis statement):
"This thesis addresses the question: *How can we design an instruction memory system for LLM agents that provides consistency, explainability, replayability, and governance, while supporting real-time updates and multi-agent deployments?*"

### Writing tips
- Use concrete examples (logging, security rules, style guides) that resonate with developers
- Don't oversell the problem (acknowledge existing partial solutions, explain gaps)
- Foreshadow your approach (event-sourcing, projection) but don't detail it yet

---

## 1.2 Scope & Research Questions

**Target length**: 1 page

### What to write

**Scope boundaries** (1-2 paragraphs):
What this thesis DOES focus on:
- Rule-based knowledge (explicit instructions: "use async/await", "validate JWT tokens")
- Event-sourced architecture for memory (EVENTS → DELTAS → PROJECTIONS)
- Deterministic retrieval and injection into agent prompts
- Replayability and auditability via NATS JetStream
- FTS5-based retrieval (full-text search) for v1.0

What this thesis DOES NOT focus on:
- General factual knowledge ("what is Python?") → this is for rules, not a knowledge base
- Large-scale multi-agent simulation (focus is 1-10 agents, not 1000s)
- Vector-based retrieval (RAG) → future work or v3.0 if time permits
- Automatic rule extraction from docs → future work (v6.0)
- Production deployment at scale → proof-of-concept system

**Research questions** (bullet list, 3-5 questions):
- RQ1: Can an event-sourced memory architecture provide consistent rule retrieval for multi-agent systems?
- RQ2: Does explicit rule injection improve agent correctness compared to baseline (no rules)?
- RQ3: Can the system deterministically replay DELTA history to reconstruct memory state?
- RQ4: What is the latency of rule updates propagating from source (GitHub) to agent prompts?
- RQ5: How does retrieval quality (FTS5) affect agent performance, and would RAG improve it?

**Hypotheses** (formalize from METRICS.md):
- **H1 (Correctness)**: Agents with injected rules will produce responses rated higher in correctness/alignment than baseline agents (measured by human evaluation on 50 prompts).
- **H2 (Retrieval Quality)**: MemoryCompiler retrieves relevant rules with Precision@5 ≥ 0.8, MRR ≥ 0.7, NDCG@10 ≥ 0.75 on annotated test set.
- **H3 (Replayability)**: DELTA replay achieves State Reconstruction Accuracy (SRA) = 1.0 (perfect match between replayed and current DB state).
- **H4 (Freshness)**: Median latency from GitHub merge → agent UI display < 5 seconds (measured via SSE push notifications).
- **H5 (Consistency)**: Repeated queries (temperature=0) produce identical cited rule IDs with ≥ 95% agreement.

### Writing tips
- Be honest about scope limits (this builds credibility)
- Research questions should be answerable with your evaluation plan
- Hypotheses should be testable and falsifiable

---

## 1.3 Contributions & Claims

**Target length**: 1 page

### What to write

**Primary contributions** (numbered list, 3-5 items):
1. **Architecture**: A novel event-sourced architecture for agent instruction memory, separating raw events (EVENTS), promoted changes (DELTAS), and audit trails (AUDIT).
2. **Replayability**: A deterministic replay mechanism that reconstructs memory state from DELTA history, enabling time-travel debugging and audits.
3. **Explainability**: A provenance system that traces agent outputs to specific rule IDs and Git commits (e.g., `[im:api.auth@v4, commit:abc123]`).
4. **Evaluation framework**: A dataset of 50 annotated prompts + ground-truth rule labels for measuring retrieval quality and correctness.
5. *(If doing RAG)*: A comparison of FTS5 vs. vector-based retrieval showing [expected result].

**Claims** (what you will argue/demonstrate):
- **Claim 1**: Event-sourced memory improves consistency across agents compared to static prompts or no memory.
- **Claim 2**: Rule injection improves correctness (as judged by humans) on domain-specific tasks.
- **Claim 3**: DELTA replay enables deterministic state reconstruction (SRA = 1.0).
- **Claim 4**: The system achieves low-latency rule propagation (< 5s end-to-end).
- **Claim 5** *(weaker)*: FTS5 retrieval is sufficient for small rule sets (~100s of rules), but RAG may be needed at scale.

**Non-contributions** (what you are NOT claiming):
- This is not a general-purpose knowledge graph or RAG system
- This is not a production-scale deployment (it's a proof-of-concept)
- This does not solve automatic rule induction or learning (future work)

### Writing tips
- Contributions = what you built/designed
- Claims = what you will argue in your evaluation
- Be specific (avoid vague claims like "improves performance")
- Acknowledge limitations upfront (builds trust)

---

## 1.4 Thesis Outline

**Target length**: 0.5 pages

### What to write

Brief (2-3 sentence) summary of each chapter:
- **Chapter 2 (Background)**: Reviews agent memory architectures, event sourcing, LLM prompt techniques, and explainability systems, identifying gaps that motivate this work.
- **Chapter 3 (Design)**: Presents the system architecture: EVENTS/DELTAS/AUDIT streams, promoter logic, projection database, MemoryCompiler, and replay mechanism.
- **Chapter 4 (Implementation)**: Describes the .NET/NATS/SQLite implementation, including the DeltaConsumer refactor, Agent.UI, and experiment framework.
- **Chapter 5 (Methodology)**: Defines the evaluation protocol: dataset construction, baselines, metrics (H1-H5), and statistical methods.
- **Chapter 6 (Results)**: Reports experimental findings across consistency, correctness, replayability, freshness, and retrieval quality.
- **Chapter 7 (Discussion)**: Reflects on design insights, limitations, and trade-offs, providing guidelines for future systems.
- **Chapter 8 (Conclusion)**: Summarizes contributions, discusses implications, and proposes future work (RAG, multi-agent scaling, rule learning).

---

## Template for Writing

Use this structure when drafting:

```markdown
# 1 Introduction

## 1.1 Motivation & Problem Statement

[Opening hook: Alice and Bob scenario, 1 paragraph]

[The divergence problem, 2-3 paragraphs]

[The governance problem, 2-3 paragraphs]

[Why existing tools fall short, 2-3 paragraphs]

[Problem statement, 1 paragraph]

## 1.2 Scope & Research Questions

[Scope: what you DO and DON'T address, 1-2 paragraphs]

**Research Questions:**
- RQ1: ...
- RQ2: ...
- ...

**Hypotheses:**
- H1: ...
- H2: ...
- ...

## 1.3 Contributions & Claims

**Contributions:**
1. ...
2. ...
3. ...

**Claims:**
- Claim 1: ...
- Claim 2: ...
- ...

**Non-contributions:**
- ...

## 1.4 Thesis Outline

[2-3 sentence summary per chapter]
```

---

## Checklist Before Moving to Chapter 2

- [ ] Have I clearly stated the problem in terms a CS grad student would understand?
- [ ] Are my research questions answerable with my evaluation plan?
- [ ] Are my hypotheses testable and falsifiable?
- [ ] Do my contributions align with what I've actually built/designed?
- [ ] Have I been honest about scope limits?
- [ ] Does this chapter make me excited to read the rest of the thesis?

---

## References to Pull From

- Your existing docs/THESIS.md (motivation, related work)
- Your METRICS.md (hypotheses H1-H5)
- Your CLAUDE.md (project overview, properties)

## Next Steps After Completing This Chapter

1. Get feedback from advisor (if available)
2. Proceed to Chapter 2 (Background & Related Work)
3. Use this chapter to guide your scope decision (RAG? Extractor?)

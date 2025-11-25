# Chapter 7: Discussion

**Status**: 🚧 BLOCKED - Write in Jan 2025 (after Chapter 6)
**Estimated length**: 6-10 pages

---

## Writing Instructions

This chapter **interprets** your findings and **reflects** on design decisions. Goals:
1. Connect results back to claims and research questions
2. Extract design insights and best practices
3. Acknowledge limitations and threats to validity
4. Provide guidance for future systems

**Tone**: Reflective, critical, honest. Show maturity by discussing what didn't work.

---

## 7.1 Reflection on Claims & Hypotheses

**Target length**: 2 pages

### What to write

**Claim 1: Event-sourced memory improves consistency**:
- **Evidence**: H5 (96% agreement), H3 (SRA=1.0) ✅
- **Interpretation**: Event sourcing + deterministic replay + shared DB → consistency
- **Caveat**: LLM non-determinism (even at temp=0) limits perfect consistency

**Claim 2: Rule injection improves correctness**:
- **Evidence**: H1 (Condition B +0.9 points, p<0.001, d=1.2) ✅
- **Interpretation**: Explicit rules guide LLM, especially for hard/ambiguous queries
- **Caveat**: Only tested in dev domain, need generalization to other domains

**Claim 3: DELTA replay enables deterministic state reconstruction**:
- **Evidence**: H3 (SRA=1.0 for all tests) ✅
- **Interpretation**: Idempotency + sequence numbers → perfect replay
- **Caveat**: Assumes no external dependencies (e.g., DB schema changes)

**Claim 4: Low-latency rule propagation**:
- **Evidence**: H4 (median 3.2s) ✅
- **Interpretation**: Event streaming + FTS5 → near real-time updates
- **Caveat**: Latency dominated by projection (SQLite writes), may not scale to 1000s of agents

**Claim 5: FTS5 retrieval is sufficient for small rule sets**:
- **Evidence**: H2 (P@5=0.84, MRR=0.79) ✅
- **Interpretation**: Lexical search works for 20-50 rules with clear keywords
- **Caveat**: May degrade for larger corpora or ambiguous queries (RAG needed)

**Overall**: 5/5 claims supported by evidence ✅

---

## 7.2 Design Insights & Patterns

**Target length**: 2 pages

### What to write

**Insight 1: Separation of EVENTS and DELTAS is key**:
- **Why**: Governance boundary (untrusted → trusted)
- **Benefit**: Auditability, flexibility (change gating policy without touching projection)
- **Cost**: Added complexity, latency

**Insight 2: FTS5 is underrated for deterministic retrieval**:
- **Why**: Simple, fast, explainable (keyword matches are traceable)
- **When to use**: Small-to-medium rule sets (10s-100s), determinism required
- **When not to use**: Large corpora (1000s+), semantic similarity needed → RAG

**Insight 3: Idempotency requires careful design**:
- **Why**: Replay and redelivery (NATS, failures) demand idempotent operations
- **How**: Use natural keys (instructionId, not autoincrement), upsert semantics
- **Lesson**: Test idempotency early (replay tests caught bugs)

**Insight 4: Human eval is expensive but essential**:
- **Why**: Automated metrics (P@5, MRR) don't capture "correctness" w.r.t. team rules
- **Cost**: 50 prompts x 2 conditions x 5 min/eval = ~8 hours
- **Lesson**: Start with pilot (10 prompts) to refine rubric, then scale

**Insight 5: Explainability via citations is feasible**:
- **Why**: LLMs can learn citation format from few-shot examples
- **Evidence**: 94% of responses include citations, 88% well-formed
- **Caveat**: Requires prompt engineering, post-processing (regex)

**Patterns for future systems**:
1. **Event sourcing for memory**: If auditability/replay matters, use event log (not just snapshots)
2. **Lexical-first, semantic-later**: Start with simple retrieval (FTS5, BM25), add RAG if needed
3. **Shared DB for consistency**: Multi-agent systems need single source of truth
4. **Governance as code**: Gating policies in Promoter, not ad-hoc admin scripts

---

## 7.3 Limitations, Pitfalls & Validity Threats

**Target length**: 2 pages

### What to write

**Limitation 1: Small-scale evaluation**:
- **Issue**: 50 prompts, 20-50 rules, 1-2 LLM models
- **Impact**: External validity uncertain (does this scale to 1000s of rules? Other domains?)
- **Mitigation**: Acknowledge scope, recommend large-scale replication

**Limitation 2: Single domain (software dev)**:
- **Issue**: Rules are dev-specific (API auth, logging, etc.)
- **Impact**: Generalizability to other domains (legal, medical, customer support) unknown
- **Mitigation**: Discuss domain-agnostic design (TOOL is domain-neutral, rules are domain-specific)

**Limitation 3: Manual rule curation**:
- **Issue**: Rules manually written (no auto-extraction from docs)
- **Impact**: Admin overhead (6.25 rules/hour)
- **Mitigation**: v6.0 (future work): Rule extraction from Markdown

**Limitation 4: No large-scale multi-agent testing**:
- **Issue**: Tested with 1-2 agents, not 10+
- **Impact**: Concurrency/contention issues unknown (SQLite single-writer)
- **Mitigation**: Discuss scalability plan (PostgreSQL, partitioning)

**Limitation 5: Human eval subjectivity**:
- **Issue**: "Correctness" ratings are subjective (different evaluators may disagree)
- **Impact**: Inter-rater agreement is key (need κ > 0.7)
- **Mitigation**: Use rubric, multiple raters, reconcile conflicts

**Pitfalls encountered**:
- **Pitfall 1**: FTS5 index not updating (solution: triggers)
- **Pitfall 2**: NATS consumer crashes → deltas lost (solution: durable consumer)
- **Pitfall 3**: LLM citation format inconsistent (solution: prompt engineering + regex post-processing)

**Threats to validity** (recap from Chapter 5.7):
- Internal: Evaluator bias (mitigated by blinding)
- External: Small scale, single domain (acknowledged)
- Construct: Subjective ratings (mitigated by rubric, IAA)
- Conclusion: Multiple comparisons (mitigated by Bonferroni)

---

## 7.4 Guidelines for Future Systems

**Target length**: 1-2 pages

### What to write

**Guideline 1: When to use event-sourced memory**:
- ✅ Use if: Auditability, replay, explainability are required
- ❌ Don't use if: Simple CRUD app, no versioning needs (overhead not justified)

**Guideline 2: When to use FTS5 vs RAG**:
- ✅ FTS5 if: Small corpus (<100 rules), determinism needed, explainability critical
- ✅ RAG if: Large corpus (1000s+), semantic similarity needed, resources available (vector DB)

**Guideline 3: How to design governance workflows**:
- **Auto-approve trusted authors** (low latency, low overhead)
- **Manual review for untrusted** (high oversight, high latency)
- **AUDIT everything** (no back-door DB writes)

**Guideline 4: How to test idempotency**:
- **Replay tests**: Replay events 1..N, compare DB state
- **Duplicate delivery tests**: Process same event twice, verify no corruption
- **Use natural keys**: Avoid autoincrement IDs

**Guideline 5: How to evaluate correctness**:
- **Start with automated metrics** (P@5, MRR) for quick iteration
- **Follow with human eval** (correctness, helpfulness) for final validation
- **Use pilot study** to refine rubric, estimate effect size

---

## 7.5 When This Architecture Is Suitable

**Target length**: 1 page

### What to write

**Good fit** (when TOOL-like architecture is appropriate):
- Multi-agent systems needing **consistent shared memory**
- Domains requiring **auditability** (compliance, finance, healthcare)
- Teams with **evolving rules/policies** (rules change frequently, need versioning)
- Applications needing **explainability** (e.g., "why did agent decide X?")

**Example scenarios**:
1. **Code review bots**: Multiple bots reviewing PRs, all follow same style guide (versioned rules)
2. **Customer support agents**: Multiple agents answering tickets, all follow same policies (versioned SOPs)
3. **Legal document review**: Agents extract clauses, cite regulations (explainability required)

**Poor fit** (when TOOL is overkill):
- Single-agent systems (no consistency needs)
- Static knowledge (rules never change → no need for versioning)
- Real-time systems with <10ms latency requirements (event streaming adds overhead)
- General Q&A (factual retrieval → use standard RAG)

**Design space positioning**:
```
Static prompts <---> TOOL <---> Full RAG
(no memory)      (explicit rules)   (retrieval-augmented)

Simple <---> TOOL <---> Complex
(manual CRUD)  (event-sourced)  (learned rules, multi-hop reasoning)
```

---

## Chapter Checklist

- [ ] All claims/hypotheses reflected upon
- [ ] Design insights extracted (actionable lessons)
- [ ] Limitations acknowledged honestly
- [ ] Guidelines for future systems provided
- [ ] Transition to Chapter 8 (Conclusion) previewed

---

## Timeline

- **Jan 18-19**: Draft sections 7.1-7.2 (reflect on findings)
- **Jan 20-21**: Draft sections 7.3-7.5 (limitations, guidelines)
- **Jan 22**: Chapter 7 complete

---

## Next Steps

1. Complete Chapter 6 (Results)
2. Write this chapter (Jan 18-22)
3. Proceed to Chapter 8 (Conclusion)

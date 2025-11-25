# Chapter 5: Evaluation Methodology

**Status**: ⚠️ DESIGN NOW (Nov 20-Dec 1), FINALIZE AFTER PILOT (Dec 1-8)
**Estimated length**: 8-12 pages

---

## Writing Instructions

This chapter is your **experimental contract**. Goals:
1. Define what you will measure (metrics H1-H5)
2. Explain how you will measure it (protocols, datasets, baselines)
3. Justify your choices (why 50 prompts? why human eval?)
4. Enable reproducibility (anyone should be able to replicate)

**Tone**: Rigorous, methodical. Think "scientific paper methods section."

**Key principle**: A reader should be able to reproduce your experiments from this chapter alone.

---

## 5.1 Evaluation Goals Overview

**Target length**: 1 page

### What to write

**Primary hypotheses** (from METRICS.md, Chapter 1.2):
- **H1 (Correctness)**: Agents with injected rules produce more correct responses than baseline (human evaluation)
- **H2 (Retrieval Quality)**: MemoryCompiler retrieves relevant rules with high precision/recall (automated metrics)
- **H3 (Replayability)**: DELTA replay perfectly reconstructs DB state (SRA = 1.0)
- **H4 (Freshness)**: Rule updates propagate within 5 seconds (latency measurement)
- **H5 (Consistency)**: Temperature=0 produces identical outputs on repeated queries

**Secondary/exploratory questions**:
- Admin overhead: How long to curate rules? (time tracking)
- Usability: How easy is the UI? (informal user feedback)
- Ablation: Does retrieval quality (topK=3 vs 5) affect correctness? (sensitivity analysis)

**Evaluation strategy**:
| Hypothesis | Method | Dataset | Metric |
|------------|--------|---------|--------|
| H1 | Human eval (A/B test) | 50 prompts | Correctness rating (1-5) |
| H2 | Automated (ground truth labels) | 50 prompts + rule labels | Precision@5, MRR, NDCG@10 |
| H3 | Replay test | Synthetic deltas + snapshots | SRA (binary: match or no match) |
| H4 | Latency measurement | 10 rule updates | Median latency (seconds) |
| H5 | Repeated queries | 20 prompts x 5 runs | Agreement (%) on cited rules |

---

## 5.2 Dataset Design: Prompts & Rule Ground Truth

**Target length**: 2-3 pages

### What to write

**Prompt dataset** (50 prompts for H1, H2):
- **Source**: Manually curated (based on typical dev questions)
- **Categories** (10 prompts each):
  1. API authentication ("How do I secure API endpoints?")
  2. Logging practices ("What logging format should I use?")
  3. Error handling ("How to handle null references?")
  4. Code style ("Should I use async/await or callbacks?")
  5. Security ("How to prevent XSS attacks?")
- **Difficulty levels**: Easy (1 relevant rule), Medium (2-3 rules), Hard (4+ rules or ambiguous)

**Ground truth labels** (for H2):
- For each prompt, manually annotate relevant rule IDs (e.g., `[im:api.auth@v3, im:api.rate-limit@v2]`)
- Use annotation protocol (docs/annotation_protocol.md):
  - Label: "relevant" (should be retrieved), "irrelevant" (should not be retrieved)
  - Two annotators, resolve conflicts via discussion
- Inter-annotator agreement: Compute Cohen's kappa (target: κ > 0.7)

**Rule corpus** (for retrieval):
- **Size**: 20-50 rules (v1.0 scope)
- **Format**: Markdown files in `rules/` directory
- **Schema**: Title, content, tags, version, provenance

**Example prompt + ground truth**:
```json
{
  "id": "p001",
  "category": "api_auth",
  "difficulty": "easy",
  "prompt": "How should I authenticate requests to our internal API?",
  "relevantRules": ["im:api.auth@v3"],
  "irrelevantRules": ["im:logging@v2", "im:error-handling@v1"]
}
```

**Baseline condition (no rules)**:
- Same prompts, but no rule injection (plain LLM response)
- Use generic system prompt: "You are a helpful coding assistant."

**Pilot dataset** (10-20 prompts):
- Run before full evaluation to:
  - Test annotation protocol
  - Estimate effect size (for power analysis)
  - Debug experiment runner

---

## 5.3 Baselines & Comparative Systems

**Target length**: 1-2 pages

### What to write

**Baseline A: No rules** (control condition):
- Plain LLM (Ollama Llama 3.2 or GPT-4o-mini)
- Generic system prompt, no rule injection
- Purpose: Test H1 (does rule injection improve correctness?)

**Baseline B: Static prompt** (alternative):
- All rules concatenated into system prompt (no retrieval)
- Purpose: Test if MemoryCompiler (retrieval) adds value over "dump all rules"
- Expected: Static prompt may exceed context window, or inject irrelevant rules (noise)

**Comparative system** (if time permits):
- **LangChain RAG**: Use LangChain's VectorStoreMemory with Chroma
- Purpose: Compare FTS5 vs. dense embeddings (H2)
- Caveat: No versioning, so not apples-to-apples (document this limitation)

**Why no external baselines?**:
- No existing system provides event-sourced, versioned, auditable rule memory
- Closest: LangChain, but lacks governance (not a fair comparison for H3-H5)

---

## 5.4 Experiment Protocols & Controls

**Target length**: 2 pages

### What to write

**Protocol for H1 (Correctness)**:
1. For each of 50 prompts:
   - Run Condition A (no rules): Get response R_A
   - Run Condition B (with rules): Get response R_B
2. Randomize order (present R_A/R_B to evaluator in random order)
3. Evaluator (human) rates each response:
   - Correctness (1-5): How well does it follow team rules?
   - Helpfulness (1-5): How useful is the response?
   - Citation quality (1-5, Condition B only): Are citations accurate?
4. Aggregate: Mean rating, paired t-test (Condition A vs B)

**Controls**:
- Temperature = 0 (deterministic)
- Fixed LLM model/version (e.g., Llama 3.2:3B, snapshot at date X)
- Same retrieval parameters (topK=5)
- Randomize evaluator assignment (if multiple evaluators)

**Protocol for H2 (Retrieval Quality)**:
1. For each of 50 prompts:
   - MemoryCompiler retrieves top-5 rules
   - Compare to ground truth labels
2. Compute metrics:
   - Precision@5 = (# relevant in top-5) / 5
   - Recall@5 = (# relevant in top-5) / (total # relevant)
   - MRR (Mean Reciprocal Rank) = 1 / (rank of first relevant rule)
   - NDCG@10 = normalized discounted cumulative gain (accounts for rank)
3. Aggregate: Mean across 50 prompts, 95% confidence intervals

**Protocol for H3 (Replayability)**:
1. Capture DB snapshot at sequence N (after processing N deltas)
2. Replay deltas 1..N into empty DB
3. Compare: `diff replayed_db original_db`
4. Metric: SRA = 1.0 if match, 0.0 if mismatch
5. Repeat for 10 different snapshots (varying N)

**Protocol for H4 (Freshness)**:
1. Commit rule change to GitHub (or manual seed)
2. Measure timestamps:
   - T0: Commit time
   - T1: EVENT published to NATS
   - T2: DELTA emitted by Promoter
   - T3: Projection DB updated
   - T4: Agent UI receives SSE notification (if implemented)
3. Latency = T4 - T0 (or T3 - T0 if no SSE)
4. Repeat 10 times, report median + 95th percentile

**Protocol for H5 (Consistency)**:
1. For each of 20 prompts:
   - Run 5 times with temperature=0
   - Extract cited rule IDs from each response
2. Compute agreement: % of runs where cited rules are identical
3. Expected: ≥ 95% agreement (5% tolerance for LLM variance)

---

## 5.5 Metrics Definitions & Computation

**Target length**: 2 pages

### What to write

**H1: Correctness (human eval)**:
- **Metric**: Mean correctness rating (1-5 Likert scale)
- **Computation**:
  ```
  correctness_A = mean([rating_i for prompt_i in Condition A])
  correctness_B = mean([rating_i for prompt_i in Condition B])
  delta = correctness_B - correctness_A
  ```
- **Significance test**: Paired t-test (or Wilcoxon signed-rank if non-normal)
- **Effect size**: Cohen's d (small: 0.2, medium: 0.5, large: 0.8)

**H2: Retrieval Quality**:
- **Precision@K**: `P@K = (# relevant in top-K) / K`
- **Recall@K**: `R@K = (# relevant in top-K) / (total # relevant)`
- **MRR**: `MRR = (1/|Q|) * Σ (1 / rank_i)`, where `rank_i` is rank of first relevant rule for query i
- **NDCG@K**:
  ```
  NDCG@K = DCG@K / IDCG@K
  DCG@K = Σ (rel_i / log2(i+1)) for i=1..K
  IDCG@K = DCG@K if results were perfectly ranked
  ```
- **Threshold**: P@5 ≥ 0.8, MRR ≥ 0.7, NDCG@10 ≥ 0.75 (from METRICS.md)

**H3: Replayability**:
- **SRA (State Reconstruction Accuracy)**: Binary (1.0 = match, 0.0 = mismatch)
- **Computation**:
  ```
  SRA = 1.0 if diff(replayed_db, original_db) is empty, else 0.0
  ```
- **Expected**: SRA = 1.0 for all test cases (deterministic replay)

**H4: Freshness**:
- **Latency**: Median time (seconds) from GitHub commit to agent UI update
- **Computation**:
  ```
  latency_i = T4_i - T0_i for each trial i
  median_latency = median(latencies)
  p95_latency = 95th percentile(latencies)
  ```
- **Target**: Median < 5s, p95 < 10s

**H5: Consistency**:
- **Agreement**: Percentage of repeated queries producing identical cited rules
- **Computation**:
  ```
  For each prompt:
    cited_rules = [set of cited rules for each of 5 runs]
    agreement_i = (# runs with mode(cited_rules)) / 5
  overall_agreement = mean(agreement_i)
  ```
- **Target**: ≥ 95% agreement

---

## 5.6 Statistical Methods & Power Analysis

**Target length**: 1-2 pages

### What to write

**Power analysis** (for H1):
- **Question**: Is N=50 prompts sufficient to detect a meaningful difference?
- **Assumptions**:
  - Effect size (Cohen's d): 0.5 (medium effect)
  - Alpha (Type I error): 0.05
  - Power (1 - Type II error): 0.80
- **Calculation**: Using power.t.test in R or G*Power
  - Required N ≈ 34 prompts per condition (paired test)
  - Our N=50 provides power > 0.80 ✅
- **Pilot**: Run 10 prompts first to estimate actual effect size, adjust N if needed

**Multiple comparisons**:
- **Problem**: Testing 5 hypotheses (H1-H5) increases Type I error
- **Correction**: Bonferroni (conservative) or Holm-Bonferroni (less conservative)
  - Adjusted alpha = 0.05 / 5 = 0.01 (Bonferroni)
- **Alternative**: False Discovery Rate (FDR) control (Benjamini-Hochberg)

**Confidence intervals**:
- Report 95% CIs for all metrics (not just p-values)
- Example: "Mean P@5 = 0.82 (95% CI: 0.76-0.88)"

**Non-parametric alternatives**:
- If data are non-normal (Shapiro-Wilk test), use:
  - Wilcoxon signed-rank test (instead of paired t-test)
  - Bootstrap CIs (instead of parametric CIs)

---

## 5.7 Threats to Validity & Mitigations

**Target length**: 1-2 pages

### What to write

**Internal validity** (could confounds bias results?):
- **Threat**: Evaluator bias (human knows which is Condition A/B)
  - **Mitigation**: Blind evaluation (randomize order, anonymize responses)
- **Threat**: LLM variance (even at temp=0, some variance exists)
  - **Mitigation**: Run multiple times, report mean + variance
- **Threat**: Order effects (learning across trials)
  - **Mitigation**: Randomize prompt order

**External validity** (can results generalize?):
- **Threat**: Small rule corpus (20-50 rules, not 1000s)
  - **Mitigation**: Acknowledge scope, discuss scalability in Chapter 7
- **Threat**: Specific domain (software dev rules, not general knowledge)
  - **Mitigation**: State scope clearly, recommend testing in other domains
- **Threat**: Single LLM model (Llama 3.2 or GPT-4o-mini)
  - **Mitigation**: Test on 2+ models if time permits

**Construct validity** (do metrics measure what we claim?):
- **Threat**: Human "correctness" rating is subjective
  - **Mitigation**: Clear rubric, multiple raters, inter-rater agreement (κ)
- **Threat**: Ground truth labels may be incomplete
  - **Mitigation**: Two annotators, reconcile disagreements

**Conclusion validity** (are statistical conclusions sound?):
- **Threat**: Low power (too few samples)
  - **Mitigation**: Power analysis, pilot study
- **Threat**: Multiple comparisons inflate Type I error
  - **Mitigation**: Bonferroni or FDR correction

---

## Chapter Checklist

- [ ] All metrics (H1-H5) clearly defined
- [ ] Protocols are reproducible (step-by-step)
- [ ] Baselines justified
- [ ] Dataset construction explained (prompts, labels, annotation)
- [ ] Power analysis completed (or planned after pilot)
- [ ] Threats to validity addressed
- [ ] Transition to Chapter 6 (Results) is smooth

---

## Resources

- Your docs/METRICS.md (hypotheses, metrics definitions)
- docs/annotation_protocol.md (labeling instructions)
- Pilot dataset: data/prompts_pilot.json (create after drafting this chapter)

---

## Timeline

- **Nov 20-24**: Draft sections 5.1-5.3, 5.5-5.7 (design phase)
- **Nov 25-Dec 1**: Create pilot dataset (10-20 prompts + labels)
- **Dec 1-8**: Run pilot, estimate effect size, finalize section 5.4, 5.6
- **Dec 8**: Chapter 5 complete, ready for full experiments

---

## Next Steps

1. Draft this chapter (outline is ready)
2. Create pilot dataset (10-20 prompts)
3. Run pilot experiments (manual, small-scale)
4. Revise protocols based on pilot findings
5. Proceed to full experiments (Dec 24-Jan 10)
6. Write Chapter 6 (Results) in January

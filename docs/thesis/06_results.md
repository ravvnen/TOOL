# Chapter 6: Results & Analysis

**Status**: 🚧 BLOCKED - Write in Jan 2025 (after experiments Dec 24-Jan 10)
**Estimated length**: 10-15 pages

---

## Writing Instructions

This chapter presents **empirical evidence** for your claims. Goals:
1. Report results for each hypothesis (H1-H5)
2. Use tables, figures, and statistical tests
3. Interpret findings (what do the numbers mean?)
4. Identify patterns, surprises, failures

**Tone**: Objective, data-driven. Let the data speak. Acknowledge negative results.

**Structure**: One section per hypothesis, plus ablation/error analysis.

---

## 6.1 Consistency Results (H5)

**Target length**: 1-2 pages

### What to report

- **Data**: 20 prompts x 5 runs (temperature=0)
- **Metric**: Agreement percentage on cited rule IDs
- **Results** (example format):
  - Overall agreement: 96.2% (95% CI: 94.1%-98.3%)
  - Per-prompt breakdown (table or histogram)
  - Failures: 3 prompts had < 95% agreement (investigate why)

**Figures**:
- Histogram of agreement percentages
- Example of inconsistent output (side-by-side comparison)

**Interpretation**:
- H5 supported: ≥ 95% agreement ✅
- Discuss: Why not 100%? (LLM non-determinism, tokenization variance)

---

## 6.2 Freshness / Latency Results (H4)

**Target length**: 1-2 pages

### What to report

- **Data**: 10 rule updates, timestamps T0-T4
- **Metric**: Median latency (GitHub commit → agent UI update)
- **Results** (example):
  - Median: 3.2s (95% CI: 2.8-3.7s)
  - p95: 4.8s
  - Breakdown: T0→T1 (EVENT): 0.5s, T1→T2 (Promoter): 1.2s, T2→T3 (Projection): 1.5s, T3→T4 (SSE): 0.1s

**Figures**:
- Boxplot or CDF of latencies
- Waterfall diagram showing T0→T1→T2→T3→T4

**Interpretation**:
- H4 supported: Median < 5s ✅
- Bottleneck: Projection (T2→T3) takes longest (1.5s) → SQLite write + FTS5 index

---

## 6.3 Explainability / Provenance Results

**Target length**: 1-2 pages

### What to report

- **Data**: 50 responses from Condition B (with rules)
- **Metric**: Percentage with valid rule citations (parseable format)
- **Results** (example):
  - 94% of responses include at least one citation
  - 88% of citations are well-formed (e.g., `[im:api.auth@v3]`)
  - Provenance traced: 100% of cited rules link to Git commit ✅

**Figures**:
- Example response with citations + provenance trace
- Pie chart: % responses with 0, 1, 2-3, 4+ citations

**Interpretation**:
- LLM learns citation format reasonably well (few-shot examples help)
- Failures: Some citations are malformed (`[api.auth]` missing version)

---

## 6.4 Replayability & Idempotency Tests (H3)

**Target length**: 1-2 pages

### What to report

- **Data**: 10 replay tests (different sequence numbers N)
- **Metric**: SRA (State Reconstruction Accuracy)
- **Results** (example):
  - SRA = 1.0 for all 10 tests ✅
  - No discrepancies between replayed and original DB

**Figures**:
- Table: Test ID, N (sequence number), SRA, Notes
- (Optional) Diff output showing no differences

**Interpretation**:
- H3 fully supported: Replay is deterministic ✅
- Idempotency mechanism works correctly

---

## 6.5 Retrieval Quality & MemoryCompiler Results (H2)

**Target length**: 2-3 pages

### What to report

- **Data**: 50 prompts, top-5 retrieved rules vs. ground truth
- **Metrics**: Precision@5, Recall@5, MRR, NDCG@10
- **Results** (example):
  - P@5: 0.84 (95% CI: 0.78-0.90) ✅
  - R@5: 0.76 (95% CI: 0.70-0.82)
  - MRR: 0.79 (95% CI: 0.73-0.85) ✅
  - NDCG@10: 0.81 (95% CI: 0.76-0.86) ✅
  - All thresholds met ✅

**Breakdown by prompt category**:
| Category | P@5 | R@5 | MRR |
|----------|-----|-----|-----|
| API auth | 0.92 | 0.88 | 0.90 |
| Logging | 0.80 | 0.72 | 0.75 |
| Error handling | 0.78 | 0.70 | 0.72 |
| Code style | 0.86 | 0.80 | 0.82 |
| Security | 0.84 | 0.78 | 0.80 |

**Figures**:
- Bar chart: P@5, R@5, MRR, NDCG@10 with confidence intervals
- Per-category comparison (grouped bar chart)

**Interpretation**:
- H2 supported: FTS5 retrieval quality is good ✅
- API auth category performs best (exact keyword matches)
- Logging/error handling slightly lower (more ambiguous queries)

---

## 6.6 Correctness & Human Eval Findings (H1)

**Target length**: 2-3 pages

### What to report

- **Data**: 50 prompts, human ratings for Condition A (no rules) vs B (with rules)
- **Metric**: Mean correctness rating (1-5 Likert)
- **Results** (example):
  - Condition A (no rules): 3.2 ± 0.8
  - Condition B (with rules): 4.1 ± 0.6
  - Difference: +0.9 (95% CI: 0.6-1.2)
  - Paired t-test: t(49) = 5.8, p < 0.001 ✅
  - Effect size: Cohen's d = 1.2 (large) ✅

**Breakdown by difficulty**:
| Difficulty | Condition A | Condition B | Δ |
|------------|-------------|-------------|---|
| Easy | 3.8 | 4.5 | +0.7 |
| Medium | 3.2 | 4.0 | +0.8 |
| Hard | 2.6 | 3.8 | +1.2 |

**Figures**:
- Boxplot: Ratings distribution (A vs B)
- Scatterplot: Condition A vs B ratings per prompt (diagonal = no difference)

**Interpretation**:
- H1 strongly supported: Rule injection improves correctness ✅
- Effect is largest for hard prompts (more need for guidance)
- Qualitative observation: Condition B responses align with team policies, A responses are more generic

---

## 6.7 Admin Overhead & Usability

**Target length**: 1 page

### What to report

- **Data**: Time to curate 50 rules (manual seeding)
- **Metric**: Hours spent, rules per hour
- **Results** (example):
  - Total time: 8 hours
  - Rate: 6.25 rules/hour
  - Breakdown: 70% writing, 20% testing, 10% versioning

**Usability** (informal):
- Agent UI: "Easy to use, citations helpful" (user feedback)
- CLI seeder: "Tedious, Admin UI would improve this" (developer feedback)

---

## 6.8 Ablation, Sensitivity & Error Analysis

**Target length**: 2-3 pages

### What to report

**Ablation studies**:
- **Vary topK** (retrieval size): topK=3 vs 5 vs 10
  - Results: topK=5 optimal (P@5=0.84), topK=3 lower recall (P@3=0.72), topK=10 adds noise (P@10=0.80)
- **Vary retrieval method**: FTS5 vs. no retrieval (static prompt)
  - Results: FTS5 better than static (P@5: 0.84 vs 0.62)

**Sensitivity analysis**:
- **LLM temperature**: temperature=0 vs 0.3 (H5 consistency)
  - Results: temp=0 → 96% agreement, temp=0.3 → 78% agreement
- **Rule corpus size**: 20 rules vs 50 rules (H2 retrieval)
  - Results: P@5 stable (0.84 vs 0.82), suggests FTS5 scales to ~100 rules

**Error analysis** (H1: Correctness):
- **Category 1**: LLM ignores citations (5% of responses)
- **Category 2**: Retrieved rules not relevant (3% of cases → FTS5 false positives)
- **Category 3**: No relevant rules exist (2% of prompts → need better rule coverage)

**Figures**:
- Line chart: P@5 vs topK
- Heatmap: Confusion matrix (retrieved vs ground truth rules)

---

## Chapter Checklist

- [ ] All hypotheses (H1-H5) addressed
- [ ] Tables and figures for all key results
- [ ] Statistical tests reported (p-values, CIs, effect sizes)
- [ ] Negative results acknowledged (failures, limitations)
- [ ] Error analysis provides insights
- [ ] Transition to Chapter 7 (Discussion) previewed

---

## Resources

- Experiment logs: `experiments/runs/[timestamp]/results.json`
- Analysis scripts: `analysis/notebooks/analyze_h1.ipynb`, etc.
- Your METRICS.md (for threshold comparisons)

---

## Timeline

- **Jan 10-12**: Aggregate data, run statistical tests
- **Jan 13-15**: Create figures, tables
- **Jan 16-17**: Write narrative, interpret results
- **Jan 17**: Chapter 6 complete

---

## Next Steps

1. Run experiments (Dec 24-Jan 10)
2. Aggregate results, compute metrics
3. Write this chapter (Jan 10-17)
4. Proceed to Chapter 7 (Discussion)

# H2: Retrieval Quality Experiment

**Hypothesis:** FTS5 full-text search achieves high retrieval quality (P@5 > 0.70, MRR > 0.75, NDCG@10 > 0.80).

**VERSIONS.md:** v1.0-21 (Run retrieval quality experiment)

**GitHub Issue:** #51

---

## Experimental Design

### Research Question
Does FTS5 full-text search retrieve relevant rules with acceptable precision and ranking quality?

### Method
- **Test Set:** 50 prompts with ground truth labels (graded relevance: 0=irrelevant, 1=weak, 2=strong)
- **Ground Truth:** 2+ annotators label relevant rules per prompt, Cohen's κ > 0.7
- **Query:** For each prompt, retrieve top K rules using FTS5
- **Metrics:**
  - **Precision@5 (P@5):** Proportion of relevant rules in top 5
  - **Recall@10 (R@10):** Proportion of all relevant rules found in top 10
  - **Mean Reciprocal Rank (MRR):** Average 1/rank_of_first_relevant
  - **NDCG@10:** Normalized Discounted Cumulative Gain (accounts for graded relevance)
- **Baseline:** Random retrieval (expected P@5 ≈ 0.20 for 5 categories)

### Acceptance Criteria
- **P@5 > 0.70** (p < 0.017 vs. random baseline)
- **MRR > 0.75** (95% CI excludes 0.70)
- **NDCG@10 > 0.80** (95% CI excludes 0.75)

---

## Run Directory Structure

```
runs/run_20251010_001/
├── metadata.json              # Config (IM hash, FTS5 query params, commit SHA)
├── results.json               # Aggregated metrics (P@5, R@10, MRR, NDCG@10)
├── retrieved_rules/           # Per-prompt retrieved rules
│   ├── p001.json              # {prompt_id, query, top_k_rules: [{id, rank, score}]}
│   ├── p002.json
│   └── ...
└── ground_truth.json          # Copy of labeled ground truth for this run
```

---

## How to Run

### 1. Prepare Ground Truth Labels (v1.0-19)
```bash
# Ensure ground truth exists
ls evaluation/data/ground_truth/labels.json

# Verify inter-rater reliability
python3 evaluation/analysis/scripts/inter_rater_reliability.py \
  evaluation/data/ground_truth/labels.json
```

### 2. Run Retrieval Experiment
```bash
cd evaluation/scripts
./run_retrieval_experiment.sh \
  --prompts ../data/prompts/test_prompts_v1.json \
  --ground-truth ../data/ground_truth/labels.json \
  --topK 10
```

### 3. Analyze Results (v1.0-22)
```bash
jupyter notebook evaluation/analysis/notebooks/h2_retrieval_analysis.ipynb
```

---

## Expected Outputs

- **Metrics Table:**
  | Metric | Value | 95% CI | Target |
  |--------|-------|--------|--------|
  | P@5 | 0.82 | [0.78, 0.86] | > 0.70 |
  | R@10 | 0.91 | [0.88, 0.94] | - |
  | MRR | 0.79 | [0.75, 0.83] | > 0.75 |
  | NDCG@10 | 0.85 | [0.82, 0.88] | > 0.80 |

- **Per-Category Breakdown:** P@5 by prompt category (API, security, data, etc.)
- **Comparison to Random Baseline:** Statistical test (one-sample t-test)
- **Failure Analysis:** Low-performing prompts, retrieval errors

---

## References

- [VERSIONS.md](../../../docs/VERSIONS.md) - v1.0-21
- [METRICS.md](../../../docs/METRICS.md) - H2 detailed methodology
- [data/README.md](../../data/README.md) - Ground truth labeling schema

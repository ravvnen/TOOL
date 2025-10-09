# Analysis Scripts & Notebooks

Statistical analysis tools and Jupyter notebooks for TOOL thesis experiments.

## Setup

Install Python dependencies:

```bash
pip install -r analysis/requirements.txt
```

Or use a virtual environment:

```bash
python3 -m venv venv
source venv/bin/activate  # On macOS/Linux
pip install -r analysis/requirements.txt
```

## Scripts

### `power_analysis.py`
Compute statistical power and required sample sizes for experiments.

**Usage:**
```bash
python3 analysis/power_analysis.py
```

**Output:**
- Power tables for H1 (Correctness), H2 (Retrieval Quality)
- Sample size recommendations for 80% power
- Interactive calculator for custom scenarios

**Example:**
```
H1: CORRECTNESS (Paired t-test, α=0.017 Bonferroni)
n          d=0.3 (small)       d=0.5 (medium)      d=0.8 (large)
50         0.42                0.82                0.98

RECOMMENDED: n=50 gives 82% power for medium effect (d=0.5)
```

### `inter_rater_reliability.py` (TODO)
Compute Cohen's kappa for annotation agreement.

**Usage:**
```bash
python3 analysis/inter_rater_reliability.py data/annotations_pilot.json
```

**Output:**
- Cohen's κ with 95% CI
- Agreement matrix
- Flagged disagreements (>2 points apart)

## Notebooks

### `baseline_comparison_analysis.ipynb` (TODO)
Analyzes H1 (Correctness) experiment results.

**Inputs:**
- `experiments/baseline_comparison/run_*/ratings.json`
- `data/test_prompts_v1.json`

**Outputs:**
- Paired t-test results (mean difference, p-value, Cohen's d, 95% CI)
- Condition comparison plots (violin plots, box plots)
- Per-category breakdown
- Annotator agreement analysis

**Metrics computed:**
- Mean rating per condition (A vs B)
- Statistical significance (p < 0.017 with Bonferroni)
- Effect size (Cohen's d)
- 95% confidence intervals

### `retrieval_quality_analysis.ipynb` (TODO)
Analyzes H2 (Retrieval Quality) experiment results.

**Inputs:**
- `experiments/retrieval_quality/run_*/results.json`
- `data/test_prompts_v1.json` (ground truth labels)

**Outputs:**
- P@5, R@5, F1@5, MRR, NDCG@10 scores
- Per-category breakdown
- Comparison to random baseline
- Rank stability analysis

### `replayability_analysis.ipynb` (TODO)
Analyzes H3 (Replayability) experiment results.

**Inputs:**
- `experiments/replayability/run_*/results.json`

**Outputs:**
- SRA (State Reconstruction Accuracy) across trials
- Replay time distribution (median, P95, P99)
- Idempotency verification results

## Reproducibility

All notebooks include:
- Exact package versions (from `pip freeze`)
- Random seed settings
- Data provenance (commit SHA, file hashes)
- Timestamp of analysis run

To reproduce results:
1. Check out the same commit: `git checkout <SHA>`
2. Install exact package versions: `pip install -r analysis/requirements.txt`
3. Run notebook: `jupyter notebook analysis/<notebook>.ipynb`
4. Compare outputs to archived results in `experiments/*/analysis_output.html`

## Statistical Tests Reference

| Hypothesis | Test | Assumptions | Interpretation |
|------------|------|-------------|----------------|
| H1: Correctness | Paired t-test | Normality (robust for n≥30), paired data | p<0.017 → B > A (Bonferroni) |
| H2: Retrieval | One-sample t-test | Normality, P@5 > threshold | p<0.017 → P@5 > 0.70 |
| H3: Replayability | Descriptive | None (binary pass/fail) | SRA = 1.00 required |

## Common Commands

**Run power analysis:**
```bash
python3 analysis/power_analysis.py
```

**Start Jupyter:**
```bash
jupyter notebook
```

**Export notebook to HTML:**
```bash
jupyter nbconvert --to html analysis/baseline_comparison_analysis.ipynb
```

**Compute inter-rater kappa:**
```python
from sklearn.metrics import cohen_kappa_score
kappa = cohen_kappa_score(rater1, rater2)
print(f"Cohen's κ = {kappa:.2f}")
```

## References

- Cohen, J. (1988). *Statistical Power Analysis for the Behavioral Sciences* (2nd ed.)
- Krippendorff, K. (2004). *Content Analysis: An Introduction to Its Methodology*
- Manning, C. D., et al. (2008). *Introduction to Information Retrieval* (Ch. 8: Evaluation)

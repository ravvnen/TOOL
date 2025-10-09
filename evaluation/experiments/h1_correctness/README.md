# H1: Correctness Experiment

**Hypothesis:** TOOL-augmented LLM produces more correct responses than vanilla LLM.

**VERSIONS.md:** v1.0-20 (Run baseline comparison)

**GitHub Issue:** #35

---

## Experimental Design

### Research Question
Does instruction memory improve LLM response accuracy for software engineering prompts?

### Method
- **Design:** Paired within-subjects comparison
- **Conditions:**
  - **A (Control):** Vanilla LLM (no rulebook injection)
  - **B (Treatment):** TOOL-augmented LLM (with compiled memory JSON)
- **Sample:** 50 prompts stratified across 5 categories (API design, security, data modeling, error handling, general)
- **Evaluation:** 2-3 human annotators rate responses on 5-point Likert scale
- **Statistical Test:** Paired t-test, α = 0.017 (Bonferroni correction)

### Acceptance Criteria
- **Primary:** Condition B mean rating > Condition A mean rating, p < 0.017
- **Effect Size:** Cohen's d > 0.5 (medium effect)
- **Inter-rater Reliability:** Cohen's κ > 0.7

---

## Files

| File | Purpose |
|------|---------|
| `annotation_protocol.md` | Human evaluation protocol (annotator training, rating scale, workflow) |
| `runs/run_YYYYMMDD_NNN/` | Individual experiment runs (see structure below) |

---

## Run Directory Structure

```
runs/run_20251010_001/
├── metadata.json              # Experiment config (model, temperature, IM hash, commit SHA)
├── conditionA/                # Vanilla LLM responses
│   ├── response_p001.json     # Response for prompt p001
│   ├── response_p002.json
│   └── ...
├── conditionB/                # TOOL-augmented responses
│   ├── response_p001.json
│   ├── response_p002.json
│   └── ...
└── ratings.json               # Human annotations (annotator_id, prompt_id, condition, rating)
```

---

## How to Run

### 1. Prepare Test Dataset (v1.0-16, v1.0-17)
```bash
# Ensure test prompts exist
ls evaluation/data/prompts/test_prompts_v1.json

# Ensure rules are seeded
curl http://localhost:5000/api/v1/state?ns=ravvnen.consulting
```

### 2. Generate Responses (Condition A)
```bash
# Run vanilla LLM (no memory injection)
cd evaluation/scripts
./run_baseline.sh --condition A --prompts ../data/prompts/test_prompts_v1.json
```

### 3. Generate Responses (Condition B)
```bash
# Run TOOL-augmented LLM
./run_baseline.sh --condition B --prompts ../data/prompts/test_prompts_v1.json
```

### 4. Recruit & Train Annotators (v1.0-18)
See `annotation_protocol.md` for detailed instructions.

### 5. Collect Ratings (v1.0-19)
```bash
# Pilot study (20% overlap)
python3 evaluation/analysis/scripts/collect_ratings.py --pilot

# Compute Cohen's κ
python3 evaluation/analysis/scripts/inter_rater_reliability.py

# Full annotation (if κ > 0.7)
python3 evaluation/analysis/scripts/collect_ratings.py --full
```

### 6. Analyze Results (v1.0-22)
```bash
jupyter notebook evaluation/analysis/notebooks/h1_correctness_analysis.ipynb
```

---

## Expected Outputs

- **Descriptive Stats:** Mean, SD, 95% CI per condition
- **Paired t-test:** t-statistic, df, p-value, Cohen's d
- **Plots:** Violin plots, box plots, per-category breakdown
- **Conclusion:** Reject/fail to reject H1

---

## References

- [VERSIONS.md](../../../docs/VERSIONS.md) - v1.0-20
- [METRICS.md](../../../docs/METRICS.md) - H1 detailed methodology
- [annotation_protocol.md](./annotation_protocol.md) - Human evaluation protocol

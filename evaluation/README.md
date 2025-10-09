# TOOL Evaluation Framework

This directory contains all evaluation infrastructure for the TOOL thesis project.

## Directory Structure

```
evaluation/
├── README.md                          # This file (overview)
├── data/                              # Test datasets & ground truth
│   ├── prompts/                       # Test prompts (test_prompts_v1.json)
│   ├── ground_truth/                  # Human annotations & labels
│   └── seeds/                         # Baseline rules for evaluation
├── experiments/                       # Experiments organized by hypothesis. Purpose: Lab Protocols - "Here's how to conduct the experiment"
│   ├── h1_correctness/                # H1: Correctness (v1.0-20)
│   │   ├── README.md                  # Experiment design & protocol
│   │   ├── annotation_protocol.md     # Human evaluation methodology
│   │   └── runs/                      # Experiment run outputs.    Purpose: Lab Notebook - "Here is the outputted data"
│   ├── h2_retrieval/                  # H2: Retrieval Quality (v1.0-21)
│   │   ├── README.md                  # Experiment design & protocol
│   │   └── runs/                      # Experiment run outputs.   Purpose: Lab Notebook - "Here is the outputted data"
│   └── h3_replayability/              # H3: Replayability (v2.0-04)
│       ├── README.md                  # Experiment design & protocol
│       └── runs/                      # Experiment run outputs.   Purpose: Lab Notebook - "Here is the outputted data"
├── analysis/                          # Statistical analysis & notebooks. Purpose : Data Analysis - "Here is where we analyze the data"
│   ├── notebooks/                     # Jupyter notebooks for analysis
│   ├── scripts/                       # Analysis scripts (power_analysis.py, etc.)
│   └── requirements.txt               # Python dependencies
└── scripts/                           # Experiment automation scripts
    └── log_experiment.sh              # Create experiment run directories
```

## Evaluation Tasks (v1.0-16 through v1.0-22)

See `docs/VERSIONS.md` for full task descriptions:

- **v1.0-16**: Draft 50 test prompts (stratified across 5 categories)
- **v1.0-17**: Seed 20+ baseline rules into IM
- **v1.0-18**: Recruit & train annotators (Cohen's κ > 0.7)
- **v1.0-19**: Label ground truth (pilot + full dataset)
- **v1.0-20**: Run baseline comparison (vanilla vs TOOL, n=50)
- **v1.0-21**: Run retrieval quality experiment (P@5, MRR, NDCG@10)
- **v1.0-22**: Analysis notebooks (stats, plots, hypothesis tests)

## Primary Hypotheses

### H1: Correctness (PRIMARY)
- **Claim**: TOOL-augmented LLM produces more correct responses than vanilla LLM
- **Method**: Paired t-test, n=50, human ratings (1-5 Likert scale)
- **Significance**: α = 0.017 (Bonferroni correction)

### H2: Retrieval Quality (PRIMARY)
- **Claim**: FTS5 achieves high retrieval quality (P@5 > 0.70, MRR > 0.75)
- **Method**: Labeled test set, compute P@K, MRR, NDCG@10
- **Significance**: α = 0.017

### H3: Replayability (PRIMARY)
- **Claim**: Event sourcing enables deterministic state reconstruction (SRA = 1.00)
- **Method**: Drop projection DB → replay DELTAS → verify hash match
- **Target**: 100% success rate across 10+ trials

### H4: Freshness (SUPPORTING)
- **Claim**: Rule updates propagate with low latency (median < 100ms)
- **Method**: Log timestamps (merge → emit → apply → UI display)

### H5: Consistency (SUPPORTING)
- **Claim**: System produces stable outputs at temperature=0
- **Method**: 10 runs per prompt, measure cited rule agreement > 0.95

## Quick Start

1. **Setup analysis environment:**
   ```bash
   cd evaluation/analysis
   pip install -r requirements.txt
   ```

2. **Create experiment run:**
   ```bash
   cd evaluation/scripts
   ./log_experiment.sh baseline_comparison
   ```

3. **Run analysis notebooks:**
   ```bash
   jupyter lab evaluation/analysis/notebooks/
   ```

## See Also

- [Version Roadmap](../docs/VERSIONS.md) - Full task breakdown
- [Thesis Narrative](../docs/THESIS.md) - Research context & contributions
- [Metrics Documentation](../docs/METRICS.md) - Detailed metric definitions

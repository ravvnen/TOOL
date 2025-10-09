# Data Directory

Stores test datasets, ground truth labels, and data artifacts for experiments.

## Contents

### Test Datasets
- **test_prompts_v1.json:** 50 prompts with ground truth labels for H1 (Correctness) and H2 (Retrieval)
  - Stratified across categories: API design, security, data modeling, error handling, general
  - Graded relevance labels (0=irrelevant, 1=weak, 2=strong)
  - Expected answers for correctness evaluation

### Ground Truth
- **ground_truth_labels.json:** Expert annotations for retrieval evaluation
  - Format: `{"prompt_id": "p001", "relevant_rules": [{"id": "im:api.retry@v1", "relevance": 2}, ...]}`
  - Inter-rater reliability: Cohen's κ > 0.7

### Annotation Outputs
- **annotations_pilot.json:** Pilot study annotations (20% overlap for κ calculation)
- **annotations_full.json:** Full dataset annotations (100 responses rated by 2 annotators)

## Schema: test_prompts_v1.json

```json
{
  "version": "1.0",
  "created": "2025-10-08",
  "num_prompts": 50,
  "categories": ["api_design", "security", "data_modeling", "error_handling", "general"],
  "prompts": [
    {
      "prompt_id": "p001",
      "category": "api_design",
      "text": "How should I handle API retries for transient failures?",
      "difficulty": "medium",
      "relevant_rules": [
        {"id": "im:api.retry@v1", "relevance": 2, "reason": "Directly defines retry policy"},
        {"id": "im:api.idempotency@v1", "relevance": 2, "reason": "Retries require idempotency"},
        {"id": "im:errors.transient@v1", "relevance": 1, "reason": "Defines transient error types"}
      ],
      "expected_answer": "Use exponential backoff with jitter. Max 3 retries. Ensure idempotency tokens for all mutating operations. Only retry transient errors (5xx, timeouts).",
      "annotators": ["expert_001", "expert_002"],
      "inter_rater_kappa": 0.85
    }
  ]
}
```

## Data Quality Checks

Before using dataset in experiments:
- [ ] All 50 prompts have ground truth labels
- [ ] Inter-rater reliability κ > 0.7
- [ ] Stratification: Each category has 8-12 prompts (balanced)
- [ ] Difficulty distribution: Mix of easy (30%), medium (50%), hard (20%)
- [ ] No duplicates or near-duplicates (use fuzzy matching to check)
- [ ] Validate JSON schema with `scripts/validate_dataset.py`

## Privacy & Ethics

- All prompts are **synthetic** or **publicly available examples**
- No real user data, no PII, no proprietary information
- Safe for publication in thesis appendix

## Versioning

- **v1.0:** Initial dataset for baseline comparison experiment
- **v1.1:** (if needed) Corrections after pilot study
- **v2.0:** (future) Expanded dataset with additional categories

## Backup

- Primary: Git repository (`/data/`)
- Backup: Cloud storage (Google Drive, encrypted)
- Archive: Zenodo DOI after thesis submission (for reproducibility)

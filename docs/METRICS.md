A) Correctness (Primary, supports H1)

Goal: Determine whether answers produced using TOOL’s instruction memory are more correct (i.e. follow rules, avoid errors) than those without memory.

Methodology & Metrics

Human Expert Evaluation

Use a fixed set of prompts (e.g. 50)

Two or more domain experts judge each answer on a Likert scale (1–5) for:

Accuracy (follows relevant rules)

Completeness (doesn’t omit important rule-based aspects)

Safety / Soundness (no harmful or rule-violating suggestions)

Compute inter-rater reliability (e.g. Cohen’s κ or Krippendorff α ≥ 0.7)

Report mean ± confidence intervals for each dimension

Baseline Comparison (Condition A vs B)

A: Vanilla LLM (no memory)

B: Tool-augmented LLM

Same prompts, counterbalanced order

Compare mean accuracy (or composite score) via paired t-test

Report effect size (Cohen’s d), p-value, 95% CI

Optional Automated Sanity Checks

Violation Rate: fraction of answers that directly violate a cited rule

Hallucination Rate: fraction of answers that cite non-existent rules

Contradiction Rate: answers internally contradict rules

Primary metric: Human accuracy rating difference (B – A), plus hallucination / violation as checks.

B) Retrieval Quality (Primary, supports H2)

Goal: Measure how well MemoryCompiler retrieves rules relevant to prompts.

Setup

A test set of 50–100 prompts annotated by expert(s) with relevant rules (graded relevance 0/1/2)

Inter-rater agreement κ > 0.7

Metrics

Precision@K (P@K) — fraction of top-K retrieved that are relevant

Recall@K (R@K) — fraction of all relevant rules captured in top K

F1@K — harmonic mean of P@K and R@K

MRR — mean reciprocal rank of the first relevant result

NDCG@K — accounts for graded relevance, discounts lower ranks

Baselines for Comparison

Random ordering

TF-IDF or BM25 simple baseline

Your system (FTS5 + ranking)

Primary retrieval metrics: P@5, MRR, NDCG@10 (choose one as lead, e.g. P@5).

C) Replayability & Determinism (Primary, supports H3)

Goal: Show that the system’s state can be reconstructed exactly from event logs (DELTAS).

Tests & Metrics

State Reconstruction Accuracy (SRA)

Snapshot current projection DB

Rebuild from replaying all DELTAS from start

Compare hash(original) == hash(replayed) → success (1) or failure (0)

Run multiple trials (10+ snapshots)

Idempotency / Duplicate Handling

Apply the same DELTA twice in sequence

Check state unchanged (hash equal)

Rate of success over many trials

Replay Performance / Scalability

Measure replay time and memory usage for N events (e.g. N = 100, 1000, 10000)

Report median, p95, scaling curves

Primary metrics:

SRA rate (target = 1.00)

Idempotency rate (target = 1.00)

D) Freshness (Supporting)

Goal: Evaluate latency of rule propagation from commit to agent UI.

Measurement

Track timestamps at each stage in propagation chain:

t_merge: commit / merge event

t_emit: time promoter emits DELTA

t_apply_agent: when agent applies it

t_ui_display: when UI shows notification (if instrumented)

Compute intervals:

Δ_me = t_emit – t_merge

Δ_ea = t_apply_agent – t_emit

Δ_ma = t_apply_agent – t_merge

Δ_full = t_ui_display – t_merge

Report median, p90, p95; count late arrivals (> SLO threshold).
Compare push vs pull methods of agent synchronization.

E) Consistency (Supporting)

Goal: Under same prompt and memory state, repeated runs (temperature = 0) should converge.

Measurement

For each prompt: run K times (e.g. K = 5)

Compare answer outputs using:

Exact-match: 1 if all identical, else 0

Normalized edit distance (NED) across pairs

Jaccard token overlap across responses

Aggregate across prompts: report mean EM, mean NED, Jaccard.

F) Explainability / Provenance (Supporting)

Goal: Ensure each suggestion is traceable to rules and provenance metadata.

Mechanism & Metrics

Each completion should include cited [R:item_id:vN] and [src:commit_sha]

Agent stores im_snapshot_json representing the IM used

Metrics:

Provenance Correctness Rate (PCR) = 1 if all cited items exist in snapshot and provenance matches real deltas, else 0

Replay Fidelity (RF) = 1 if reconstructed snapshot hash matches recorded hash

(Optional) Quotation alignment: overlap between cited rule text and answer quotes

Primary metric: PCR; RF as consistency check.

G) Admin Overhead & Usability (Exploratory)

Goal: Measure human effort and reliability in admin interactions.

Metrics

Review Time: duration between proposal arrival and admin decision

Intervention Rate: fraction of proposals requiring manual review

Promoter Decision Error Rates (if ground truth provided):

False positive rate (valid rules rejected)

False negative rate (invalid rules accepted)

Admin Edit Propagation Latency: time from admin change → agent UI reflect

Qualitative survey: perceived workload, usability scores

Statistical Standards & Best Practices

Sample size: ≥ 30 prompts; preferably 50+

Paired / within-subject design when comparing conditions A vs B

Random seed freezing for reproducibility

Confidence intervals (95%) on means

Effect sizes, p-values reported

Bonferroni correction for the three primary hypotheses: α = 0.017

Blind labeling / evaluation to avoid bias

Logging: structured logs of all metrics, metadata, experiment conditions
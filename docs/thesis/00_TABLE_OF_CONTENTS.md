# Master's Thesis: Event-Sourced Instruction Memory for AI Agents (TOOL)

**Author**: Ravvnen
**Institution**: [Your University]
**Program**: Master of Science in Computer Science
**Supervisor**: [Advisor Name]
**Expected Submission**: March 2025

---

## Table of Contents

**Preface** . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . ii

**Abstract** . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . iii

**Acknowledgements** . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . iv

---

### Chapter 1: Introduction . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 1
1.1 Motivation & Problem Statement . . . . . . . . . . . . . . . . . . . . . . . . 1
1.2 Scope & Research Questions . . . . . . . . . . . . . . . . . . . . . . . . . . . 3
1.3 Contributions & Claims . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 4
1.4 Thesis Outline . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 5

---

### Chapter 2: Background & Related Work . . . . . . . . . . . . . . . . . . . . . . 6
2.1 Memory, Knowledge & Agent Architectures . . . . . . . . . . . . . . . . . . 6
2.2 Event Sourcing, Audit Logs & Replayability . . . . . . . . . . . . . . . . . . 9
2.3 LLMs, Prompt Memory & Retrieval Techniques . . . . . . . . . . . . . . . . 12
2.4 Explainability, Rule Systems & Governance . . . . . . . . . . . . . . . . . . 15
2.5 Gaps in Existing Approaches . . . . . . . . . . . . . . . . . . . . . . . . . . 18

---

### Chapter 3: Design & Architecture . . . . . . . . . . . . . . . . . . . . . . . . 20
3.1 Requirements & Design Goals . . . . . . . . . . . . . . . . . . . . . . . . . . 20
3.2 System Overview & Data Flow . . . . . . . . . . . . . . . . . . . . . . . . . 22
3.3 EVENTS, DELTAS, AUDIT Streams . . . . . . . . . . . . . . . . . . . . . 24
3.4 Promoter: Policy & Decision Logic . . . . . . . . . . . . . . . . . . . . . . . 27
3.5 Projection / DeltaConsumer / Database . . . . . . . . . . . . . . . . . . . . 29
3.6 MemoryCompiler & Prompt Injection . . . . . . . . . . . . . . . . . . . . . 31
3.7 Admin / CRUD / Oversight Paths . . . . . . . . . . . . . . . . . . . . . . . 33
3.8 Replay, Snapshotting & Idempotency . . . . . . . . . . . . . . . . . . . . . . 35
3.9 Trade-offs & Design Decisions . . . . . . . . . . . . . . . . . . . . . . . . . . 37

---

### Chapter 4: Implementation . . . . . . . . . . . . . . . . . . . . . . . . . . . . 39
4.1 Technology Stack & Dependencies . . . . . . . . . . . . . . . . . . . . . . . 39
4.2 Promoter Module Implementation . . . . . . . . . . . . . . . . . . . . . . . 41
4.3 DeltaConsumer / Projection Implementation . . . . . . . . . . . . . . . . . 43
4.4 Agent Container & UI / Chat Module . . . . . . . . . . . . . . . . . . . . . 45
4.5 Admin UI / Rule Editor . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 47
4.6 Experiment Metrics Framework Integration . . . . . . . . . . . . . . . . . . 48
4.7 Logging, Instrumentation & Debug Tooling . . . . . . . . . . . . . . . . . . 49
4.8 Engineering Challenges & Lessons . . . . . . . . . . . . . . . . . . . . . . . 50

---

### Chapter 5: Evaluation Methodology . . . . . . . . . . . . . . . . . . . . . . . 52
5.1 Evaluation Goals Overview . . . . . . . . . . . . . . . . . . . . . . . . . . . 52
5.2 Dataset Design: Prompts & Rule Ground Truth . . . . . . . . . . . . . . . . 53
5.3 Baselines & Comparative Systems . . . . . . . . . . . . . . . . . . . . . . . 55
5.4 Experiment Protocols & Controls . . . . . . . . . . . . . . . . . . . . . . . . 57
5.5 Metrics Definitions & Computation . . . . . . . . . . . . . . . . . . . . . . . 59
5.6 Statistical Methods & Power Analysis . . . . . . . . . . . . . . . . . . . . . 61
5.7 Threats to Validity & Mitigations . . . . . . . . . . . . . . . . . . . . . . . . 62

---

### Chapter 6: Results & Analysis . . . . . . . . . . . . . . . . . . . . . . . . . . 64
6.1 Consistency Results . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 64
6.2 Freshness / Latency Results . . . . . . . . . . . . . . . . . . . . . . . . . . . 66
6.3 Explainability / Provenance Results . . . . . . . . . . . . . . . . . . . . . . 68
6.4 Replayability & Idempotency Tests . . . . . . . . . . . . . . . . . . . . . . . 70
6.5 Retrieval Quality & MemoryCompiler Results . . . . . . . . . . . . . . . . . 72
6.6 Correctness & Human Eval Findings . . . . . . . . . . . . . . . . . . . . . . 74
6.7 Admin Overhead & Usability . . . . . . . . . . . . . . . . . . . . . . . . . . 76
6.8 Ablation, Sensitivity & Error Analysis . . . . . . . . . . . . . . . . . . . . . 77

---

### Chapter 7: Discussion . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 79
7.1 Reflection on Claims & Hypotheses . . . . . . . . . . . . . . . . . . . . . . . 79
7.2 Design Insights & Patterns . . . . . . . . . . . . . . . . . . . . . . . . . . . 81
7.3 Limitations, Pitfalls & Validity Threats . . . . . . . . . . . . . . . . . . . . 82
7.4 Guidelines for Future Systems . . . . . . . . . . . . . . . . . . . . . . . . . . 84
7.5 When This Architecture Is Suitable . . . . . . . . . . . . . . . . . . . . . . . 85

---

### Chapter 8: Conclusion & Future Work . . . . . . . . . . . . . . . . . . . . . . 87
8.1 Summary of Contributions & Findings . . . . . . . . . . . . . . . . . . . . . 87
8.2 Implications: Theory & Practice . . . . . . . . . . . . . . . . . . . . . . . . 88
8.3 Future Extensions (RAG, Multi-Agent, Rule Learning) . . . . . . . . . . . . 89
8.4 Final Reflections . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 90

---

**Appendix A**: Experiment Datasets & Annotation Protocol . . . . . . . . . . . . 91
**Appendix B**: Full Metrics Definitions & Formulas . . . . . . . . . . . . . . . . 95
**Appendix C**: System Architecture Diagrams . . . . . . . . . . . . . . . . . . . 98
**Appendix D**: Code Samples & API Documentation . . . . . . . . . . . . . . . . 101

**References** . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . . 105

---

## Estimated Page Counts

| Section | Pages | Status |
|---------|-------|--------|
| Front matter (Preface, Abstract, Acknowledgements) | 3-5 | Write last |
| Chapter 1: Introduction | 4-6 | ✅ Write NOW |
| Chapter 2: Background | 10-15 | ✅ Write NOW |
| Chapter 3: Design | 12-18 | ✅ Write NOW |
| Chapter 4: Implementation | 8-12 | ⚠️ Write after v1.0 stable |
| Chapter 5: Methodology | 8-12 | ⚠️ Outline now, finalize after pilot |
| Chapter 6: Results | 10-15 | 🚧 After experiments |
| Chapter 7: Discussion | 6-10 | 🚧 After Chapter 6 |
| Chapter 8: Conclusion | 3-5 | 🚧 Write last |
| Appendices | 10-15 | As needed |
| References | 3-5 | Ongoing |
| **TOTAL** | **77-123 pages** | Typical MSc thesis: 60-100 pages |

---

## Notes

- Page counts assume 11pt font, 1.5 line spacing, standard margins
- Adjust scope if heading toward >100 pages (consider moving sections to appendices)
- Prioritize quality over quantity: a focused 70-page thesis is better than a rambling 120-page one

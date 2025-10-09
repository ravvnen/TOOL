# Annotation Protocol for TOOL Evaluation

**Version:** 1.0
**Date:** 2025-10-08
**Purpose:** Standardized guidelines for human evaluation of LLM responses in baseline comparison experiment (H1: Correctness)

---

## Overview

This protocol ensures **reliable, unbiased, and reproducible** human evaluation of LLM-generated responses. It covers:
1. Annotator qualifications and training
2. Rating scales and criteria
3. Annotation workflow
4. Inter-rater reliability measurement
5. Quality control procedures

---

## 1. Annotator Qualifications

**Required:**
- Graduate student or professional with software engineering experience (2+ years)
- Familiarity with API design, security best practices, data modeling
- Native or fluent English speaker

**Preferred:**
- Experience with REST API design
- Knowledge of event sourcing, NATS, or similar event-driven systems
- Prior annotation experience

**Recruitment:**
- Target: 2-3 annotators
- Compensate at standard hourly rate for research assistants
- Estimated time commitment: 10-15 hours total (training + annotation + adjudication)

---

## 2. Training & Calibration

### Phase 1: Initial Training (1 hour)
1. **Introduction to TOOL:** Explain project goals, what instruction memory is, how it should improve LLM responses
2. **Review rating scale:** Walk through 5-point Likert scale with examples
3. **Practice annotations:** Each annotator independently rates 10 calibration examples
4. **Discussion:** Group review of practice ratings, resolve disagreements, clarify ambiguities

### Phase 2: Pilot Study (1-2 hours)
1. )**Annotate 20% of dataset** (10 prompts × 2 conditions = 20 responses
2. **Compute inter-rater reliability:** Calculate Cohen's κ
3. **Refine guidelines if needed:** If κ < 0.7, discuss disagreements and update this protocol
4. **Re-calibrate:** Repeat pilot until κ > 0.7

### Phase 3: Full Annotation (6-10 hours)
- Proceed with remaining 40 prompts once calibration is successful

---

## 3. Rating Scale & Criteria

### 5-Point Likert Scale: Correctness

**Score = 5: Fully Correct**
- Follows all applicable rules from instruction memory
- No factual errors or contradictions
- Complete response (doesn't miss critical information)
- Safe (no dangerous or insecure recommendations)
- *Example:* "Use exponential backoff with jitter for retries (Rule #api.retry). Ensure idempotency tokens (Rule #api.idempotency). Max 3 retries for transient failures (Rule #errors.transient)."

**Score = 4: Mostly Correct**
- Follows most applicable rules (1 minor omission acceptable)
- No major errors
- Nearly complete (minor details missing)
- Safe
- *Example:* "Use exponential backoff for retries and ensure idempotency." (Missing jitter detail and max retry count, but core advice correct)

**Score = 3: Partially Correct**
- Follows some rules but misses key guidance
- OR contains minor factual errors that don't completely invalidate response
- Incomplete (misses 2+ important points)
- Generally safe but may have edge-case risks
- *Example:* "Retry failed requests with a delay." (Generic advice, doesn't mention exponential backoff, jitter, idempotency, or max retries)

**Score = 2: Major Inaccuracies**
- Violates 1+ rules OR contains major factual errors
- Misleading or incorrect guidance
- May include unsafe recommendations (but not critically dangerous)
- *Example:* "Retry indefinitely until the request succeeds." (Violates max retry limit, could cause infinite loops)

**Score = 1: Incorrect / Dangerous**
- Directly contradicts multiple rules
- Fundamentally wrong guidance
- Dangerous (could lead to security vulnerabilities, data loss, system failures)
- *Example:* "Store API keys in client-side code for convenience." (Violates security rules, critical vulnerability)

### Edge Cases & Guidance

**What if the response is correct but doesn't cite rules?**
- Rate based on content accuracy, not citation format
- Citations are evaluated separately in Metric E (Retrieval Quality)

**What if the response is overly verbose but correct?**
- Score 5 if all information is accurate and complete
- Verbosity itself is not penalized (unless it introduces errors)

**What if the prompt is ambiguous and multiple interpretations are valid?**
- Rate generously: if response is correct under *any* reasonable interpretation, don't penalize
- Make a note in the comments field for later review

**What if I'm uncertain or lack domain expertise for a prompt?**
- Flag the response with "UNCERTAIN" in comments
- Do your best to rate based on general software engineering principles
- Discuss with other annotators during adjudication

---

## 4. Annotation Workflow

### Setup
1. **Blinding:** Annotators are NOT told which condition (A or B) each response belongs to
2. **Randomization:** Responses presented in random order (not grouped by condition or prompt)
3. **Interface:** Use Google Sheets or dedicated annotation tool (e.g., Label Studio)

### Annotation Template (per response)

| Field | Description | Required |
|-------|-------------|----------|
| **response_id** | Unique identifier (e.g., `p001_conditionA`) | Auto-filled |
| **prompt_text** | The user query being answered | Auto-filled |
| **response_text** | The LLM-generated response to rate | Auto-filled |
| **rating** | Score 1-5 (Likert scale) | **Required** |
| **confidence** | How confident are you? (Low / Medium / High) | **Required** |
| **comments** | Notes on edge cases, uncertainties, reasoning | Optional |
| **time_spent** | Seconds spent on this response | Auto-logged |

### Workflow Steps (per response)
1. **Read the prompt** carefully to understand what the user is asking
2. **Read the response** generated by the LLM
3. **Compare against known rules** (you'll have access to a cheat sheet of key rules per category)
4. **Assign a rating** (1-5) based on the rubric above
5. **Mark confidence** (low if uncertain, high if confident)
6. **Add comments** if needed (especially for edge cases or low confidence ratings)
7. **Move to next response**

**Average time per response:** 2-3 minutes (total: ~100 responses × 2.5 min = 4-5 hours per annotator)

---

## 5. Inter-Rater Reliability

### Metrics
- **Cohen's κ (kappa):** Measures agreement between 2 annotators, corrected for chance
  - κ < 0.40: Poor agreement
  - κ = 0.40-0.60: Moderate agreement
  - κ = 0.60-0.80: Substantial agreement
  - **κ > 0.80:** Excellent agreement (our target: κ > 0.7)

### Calculation
- Use Python `sklearn.metrics.cohen_kappa_score` or R `irr::kappa2`
- Compute on pilot set (20% overlap) first, then full dataset

### Adjudication Process
If annotators disagree by **2+ points** on any response:
1. Flag response for adjudication
2. Both annotators review together and discuss reasoning
3. Reach consensus rating OR bring in third annotator to break tie
4. Document adjudication decision in comments

---

## 6. Quality Control

### During Annotation
- **Random spot checks:** PI reviews 10% of annotations for quality
- **Attention checks:** Include 2-3 "obvious" responses with clear correct answers to ensure annotators are paying attention
- **Time monitoring:** Flag responses rated in < 30 seconds (possible rushed rating)

### Post-Annotation
- **Check for biases:**
  - Do annotators consistently rate one condition higher? (Check mean ratings per annotator per condition)
  - Do annotators disagree systematically on certain categories? (Check κ per category)
- **Outlier detection:** Identify responses where annotators strongly disagree (>2 points apart)

---

## 7. Access to Ground Truth Rules

**Annotators will have access to:**
- **Rule cheat sheet:** Condensed 1-page reference for each category (API design, security, etc.)
- **Full rulebook (optional):** Link to complete instruction memory for deep dives
- **Do NOT provide:** Access to which condition (A or B) each response came from

**Example cheat sheet (API Design category):**
```
Key Rules:
- #api.retry: Use exponential backoff with jitter, max 3 retries
- #api.idempotency: All mutating endpoints must accept idempotency tokens
- #api.versioning: Use semantic versioning in URL path (/v1/, /v2/)
- #api.pagination: Use cursor-based pagination for large result sets
```

---

## 8. Ethical Considerations

- **Informed consent:** Annotators sign consent form acknowledging their ratings will be used in thesis
- **Privacy:** No personal data in prompts/responses; all examples are synthetic or anonymized
- **Fair compensation:** Pay annotators standard research assistant hourly rate
- **Voluntary participation:** Annotators can withdraw at any time without penalty

---

## 9. Timeline

| Phase | Duration | Deliverables |
|-------|----------|--------------|
| Recruit annotators | 1 week | 2-3 qualified annotators |
| Training & calibration | 1 week | Pilot study with κ > 0.7 |
| Full annotation | 2 weeks | 100 responses rated by 2 annotators |
| Adjudication | 3 days | Resolve all 2+ point disagreements |
| Final QC & export | 2 days | Clean dataset with ratings + metadata |

**Total:** ~4-5 weeks from recruitment to final dataset

---

## 10. Data Export Format

Final annotated dataset will be exported as JSON:

```json
{
  "metadata": {
    "version": "1.0",
    "date": "2025-11-15",
    "annotators": ["annotator_001", "annotator_002"],
    "inter_rater_kappa": 0.78,
    "num_responses": 100,
    "num_adjudicated": 8
  },
  "annotations": [
    {
      "response_id": "p001_conditionA",
      "prompt_id": "p001",
      "condition": "A",
      "prompt_text": "How should I handle API retries?",
      "response_text": "Use exponential backoff...",
      "ratings": {
        "annotator_001": {"score": 5, "confidence": "high", "time_spent": 120},
        "annotator_002": {"score": 4, "confidence": "medium", "time_spent": 95},
        "consensus": 5,
        "adjudicated": false
      },
      "comments": ["annotator_001: Perfect response, cites all relevant rules"]
    }
  ]
}
```

---

## Appendix A: Calibration Examples

### Example 1: Score = 5 (Fully Correct)

**Prompt:** "How should I version my REST API?"

**Response:** "Use semantic versioning in the URL path (e.g., `/api/v1/users`). Increment the major version when you make breaking changes. Support at least two major versions concurrently to give clients time to migrate. Deprecate old versions with a 6-month notice period."

**Why Score = 5:** Follows #api.versioning, #api.deprecation. Complete, accurate, safe.

---

### Example 2: Score = 3 (Partially Correct)

**Prompt:** "How should I store user passwords?"

**Response:** "Encrypt passwords before storing them in the database."

**Why Score = 3:** Generic advice. Should specify *hashing* (not encryption) with bcrypt/Argon2 + salt. Misses key security details from #security.passwords rule. Not dangerously wrong, but incomplete.

---

### Example 3: Score = 1 (Dangerous)

**Prompt:** "How should I authenticate API requests?"

**Response:** "Send the username and password in the query string for every request."

**Why Score = 1:** Violates #security.auth. Query strings are logged, cached, visible in browser history. Critically insecure. Should use Bearer tokens, OAuth, or API keys in headers.

---

## Appendix B: Annotator Agreement Matrix

After pilot study, generate agreement matrix to visualize inter-rater reliability:

|  | Annotator 1: Score 1 | Score 2 | Score 3 | Score 4 | Score 5 |
|--|---------------------|---------|---------|---------|---------|
| **Annotator 2: Score 1** | 2 | 0 | 0 | 0 | 0 |
| **Score 2** | 0 | 3 | 1 | 0 | 0 |
| **Score 3** | 0 | 1 | 5 | 2 | 0 |
| **Score 4** | 0 | 0 | 1 | 8 | 1 |
| **Score 5** | 0 | 0 | 0 | 1 | 4 |

**Interpretation:** Strong agreement on extremes (1, 5), moderate agreement on middle scores (3, 4). Total agreement: 22/29 = 76%, κ = 0.72 (substantial).

---

## Contact

For questions or clarifications during annotation:
- **PI:** Ravvnen (ravvnen@example.com)
- **Protocol version:** 1.0 (2025-10-08)
- **Updates:** Check `/docs/annotation_protocol.md` for latest version

# H3: Replayability & Determinism Experiment

**Hypothesis:** Event sourcing enables deterministic state reconstruction (SRA = 1.00).

**VERSIONS.md:** v2.0-04 (Full replay correctness test)

**GitHub Issue:** #41

---

## Experimental Design

### Research Question
Does replaying DELTAS from scratch produce byte-for-byte identical state?

### Method
- **Procedure:**
  1. Snapshot current projection DB: `hash_original = SHA256(im_items_current)`
  2. Drop projection tables (or use fresh DB)
  3. Replay all DELTAS from NATS stream (sequence 1 → current)
  4. Compute `hash_replayed = SHA256(im_items_current)`
  5. Compare: `SRA = (hash_original == hash_replayed) ? 1.0 : 0.0`
- **Trials:** 10+ independent replay tests (different snapshot times)
- **Variants:** Test at different IM sizes (100, 500, 1000+ rules)

### Acceptance Criteria
- **SRA = 1.00** (100% success rate across all trials)
- **Replay Time:** Median < 1 second for 1000 rules
- **Idempotency:** Apply same DELTA twice → state unchanged

---

## Run Directory Structure

```
runs/run_20251015_001/
├── metadata.json              # Config (NATS stream, snapshot time, commit SHA)
├── snapshots/                 # Pre-replay state
│   ├── hash_original.txt      # SHA256 of im_items_current before drop
│   ├── im_dump.sql            # Full DB dump (for debugging)
│   └── delta_count.txt        # Number of DELTAs to replay
├── replay_log.json            # Replay timestamps per DELTA
└── results.json               # SRA, replay_time_ms, idempotency_check
```

---

## How to Run

### 1. Run Replay Test
```bash
cd evaluation/scripts
./run_replayability_test.sh --trials 10
```

### 2. Analyze Results (v2.0)
```bash
jupyter notebook evaluation/analysis/notebooks/h3_replayability_analysis.ipynb
```

---

## Expected Outputs

- **SRA Across Trials:**
  | Trial | Original Hash | Replayed Hash | SRA | Replay Time (ms) |
  |-------|---------------|---------------|-----|------------------|
  | 1 | abc123... | abc123... | 1.00 | 842 |
  | 2 | def456... | def456... | 1.00 | 891 |
  | ... | ... | ... | ... | ... |

- **Replay Time Distribution:** Median, P95, P99, scaling curve (rules vs. time)
- **Idempotency Verification:** Double-apply test results
- **Failure Analysis:** If SRA < 1.00, investigate determinism bugs

---

## Troubleshooting

### If SRA < 1.00 (hash mismatch):
1. **Check for non-determinism:**
   - Timestamp generation (use `occurred_at` from DELTA, not `DateTime.UtcNow`)
   - Floating-point precision issues
   - Concurrency bugs (ensure single-threaded replay)
2. **Inspect diffs:**
   ```bash
   diff <(sqlite3 db_original.db "SELECT * FROM im_items_current ORDER BY item_id") \
        <(sqlite3 db_replayed.db "SELECT * FROM im_items_current ORDER BY item_id")
   ```
3. **Check DELTA sequence:**
   - Ensure no gaps in sequence numbers
   - Verify DELTAS stream retention policy

---

## References

- [VERSIONS.md](../../../docs/VERSIONS.md) - v2.0-04
- [METRICS.md](../../../docs/METRICS.md) - H3 detailed methodology
- [ARCHITECTURE.md](../../../docs/ARCHITECTURE.md) - Event sourcing design

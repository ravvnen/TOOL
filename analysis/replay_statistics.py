#!/usr/bin/env python3
"""
Replay Experiment Statistics (H3: Replayability & Determinism)

Analyzes replay experiment results and computes:
- SRA (State Reconstruction Accuracy) mean, std dev, 95% CI
- Replay time metrics (median, p95)
- Hash uniqueness (should be 1 unique hash)
"""

import json
import sys
from pathlib import Path
from typing import List, Dict, Any
import statistics


def load_results(file_path: str) -> List[Dict[str, Any]]:
    """Load JSONL experiment results"""
    results = []
    with open(file_path, "r") as f:
        for line in f:
            if line.strip():
                results.append(json.loads(line))
    return results


def compute_statistics(results: List[Dict[str, Any]]) -> Dict[str, Any]:
    """Compute statistics from replay results"""

    if not results:
        return {"error": "No results to analyze"}

    n = len(results)
    sra_scores = [r["sra_score"] for r in results]
    replay_times = [r["replay_time_ms"] for r in results]
    hashes = [r["im_hash"] for r in results if r["im_hash"] != "unknown"]

    # SRA statistics
    sra_mean = statistics.mean(sra_scores)
    sra_std = statistics.stdev(sra_scores) if n > 1 else 0.0

    # 95% Confidence Interval (assuming normal distribution)
    # CI = mean ± 1.96 * (std / sqrt(n))
    sra_ci_margin = 1.96 * (sra_std / (n**0.5)) if n > 1 else 0.0
    sra_ci_lower = max(0.0, sra_mean - sra_ci_margin)
    sra_ci_upper = min(1.0, sra_mean + sra_ci_margin)

    # Replay time statistics
    replay_time_median = statistics.median(replay_times)
    replay_time_p95 = (
        statistics.quantiles(replay_times, n=100)[94] if n >= 2 else replay_times[0]
    )

    # Hash uniqueness (all should be identical for perfect determinism)
    unique_hashes = len(set(hashes))

    return {
        "hypothesis": "H3: Replayability & Determinism",
        "trials": n,
        "sra": {
            "mean": round(sra_mean, 4),
            "std_dev": round(sra_std, 4),
            "ci_95_lower": round(sra_ci_lower, 4),
            "ci_95_upper": round(sra_ci_upper, 4),
        },
        "replay_time_ms": {
            "median": int(replay_time_median),
            "p95": int(replay_time_p95),
        },
        "hash_analysis": {
            "unique_hashes": unique_hashes,
            "is_deterministic": unique_hashes == 1,
            "expected": 1,
        },
        "conclusion": {
            "sra_perfect": sra_mean == 1.0,
            "deterministic": unique_hashes == 1,
            "hypothesis_supported": sra_mean == 1.0 and unique_hashes == 1,
        },
    }


def print_report(stats: Dict[str, Any]):
    """Pretty-print statistics report"""

    print("=" * 60)
    print("  Replay Experiment Statistics (H3)")
    print("=" * 60)
    print()

    print(f"Trials: {stats['trials']}")
    print()

    print("SRA (State Reconstruction Accuracy):")
    print(f"  Mean:       {stats['sra']['mean']:.4f}")
    print(f"  Std Dev:    {stats['sra']['std_dev']:.4f}")
    print(
        f"  95% CI:     [{stats['sra']['ci_95_lower']:.4f}, {stats['sra']['ci_95_upper']:.4f}]"
    )
    print()

    print("Replay Performance:")
    print(f"  Median Time: {stats['replay_time_ms']['median']} ms")
    print(f"  P95 Time:    {stats['replay_time_ms']['p95']} ms")
    print()

    print("Determinism Check:")
    print(f"  Unique Hashes: {stats['hash_analysis']['unique_hashes']} (expected: 1)")
    print(
        f"  Deterministic: {'✅ YES' if stats['hash_analysis']['is_deterministic'] else '❌ NO'}"
    )
    print()

    print("Conclusion:")
    print(
        f"  SRA Perfect (1.00):     {'✅ YES' if stats['conclusion']['sra_perfect'] else '❌ NO'}"
    )
    print(
        f"  Fully Deterministic:    {'✅ YES' if stats['conclusion']['deterministic'] else '❌ NO'}"
    )
    print(
        f"  Hypothesis Supported:   {'✅ YES' if stats['conclusion']['hypothesis_supported'] else '❌ NO'}"
    )
    print()

    if stats["conclusion"]["hypothesis_supported"]:
        print("🎉 H3 VALIDATED: Event sourcing provides deterministic state reconstruction")
    else:
        print("⚠️  H3 NOT VALIDATED: Investigate non-determinism or replay failures")

    print("=" * 60)


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 replay_statistics.py <results.jsonl>")
        print("Example: python3 replay_statistics.py experiments/replay_results.jsonl")
        sys.exit(1)

    results_file = sys.argv[1]

    if not Path(results_file).exists():
        print(f"Error: File not found: {results_file}")
        sys.exit(1)

    # Load and analyze results
    results = load_results(results_file)
    stats = compute_statistics(results)

    # Print report
    print_report(stats)

    # Save statistics JSON
    output_file = Path(results_file).parent / "replay_statistics.json"
    with open(output_file, "w") as f:
        json.dump(stats, f, indent=2)

    print()
    print(f"📊 Statistics saved to: {output_file}")
    print()


if __name__ == "__main__":
    main()

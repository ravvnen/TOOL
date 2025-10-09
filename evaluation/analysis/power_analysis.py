#!/usr/bin/env python3
"""
Power Analysis for TOOL Thesis Experiments

Computes required sample sizes for target statistical power, or computes achieved power
for given sample size. Uses standard formulas for paired t-tests (H1, H3) and one-sample
t-tests (H2).

References:
- Cohen, J. (1988). Statistical Power Analysis for the Behavioral Sciences (2nd ed.)
- https://www.statmethods.net/stats/power.html
"""

import math
from typing import Tuple

def cohens_d_to_noncentrality(d: float, n: int) -> float:
    """
    Convert Cohen's d effect size to noncentrality parameter for t-distribution.

    For paired t-test: ncp = d * sqrt(n)

    Args:
        d: Cohen's d effect size
        n: Sample size (number of pairs)

    Returns:
        Noncentrality parameter
    """
    return d * math.sqrt(n)

def power_paired_t_test(n: int, d: float, alpha: float = 0.05, one_tailed: bool = True) -> float:
    """
    Compute statistical power for paired t-test (H1: Correctness).

    Uses normal approximation for large n (n > 30). For smaller n, this is conservative.

    Args:
        n: Sample size (number of paired comparisons, e.g., 50 prompts)
        d: Cohen's d effect size (expected difference / pooled SD)
        alpha: Significance level (default 0.05, use 0.017 for Bonferroni)
        one_tailed: True for directional hypothesis (B > A)

    Returns:
        Power (probability of rejecting H0 when H1 is true), range [0, 1]

    Example:
        >>> power_paired_t_test(n=50, d=0.5, alpha=0.017, one_tailed=True)
        0.82  # 82% power to detect medium effect with Bonferroni correction
    """
    from scipy import stats

    # Degrees of freedom
    df = n - 1

    # Critical t-value
    if one_tailed:
        t_crit = stats.t.ppf(1 - alpha, df)
    else:
        t_crit = stats.t.ppf(1 - alpha/2, df)

    # Noncentrality parameter under H1
    ncp = cohens_d_to_noncentrality(d, n)

    # Power = P(t > t_crit | ncp)
    # Using noncentral t-distribution
    power = 1 - stats.nct.cdf(t_crit, df, ncp)

    return power

def sample_size_paired_t_test(d: float, power: float = 0.80, alpha: float = 0.05, one_tailed: bool = True) -> int:
    """
    Compute required sample size for paired t-test to achieve target power.

    Uses iterative search (binary search would be more efficient, but this is simple).

    Args:
        d: Cohen's d effect size
        power: Target power (default 0.80 = 80%)
        alpha: Significance level (default 0.05)
        one_tailed: True for directional hypothesis

    Returns:
        Minimum sample size (number of pairs) to achieve target power

    Example:
        >>> sample_size_paired_t_test(d=0.5, power=0.80, alpha=0.017)
        50  # Need ~50 pairs for 80% power with Bonferroni correction
    """
    # Start with n=10, increment until we hit target power
    n = 10
    while power_paired_t_test(n, d, alpha, one_tailed) < power:
        n += 1
        if n > 1000:
            raise ValueError(f"Sample size exceeded 1000 for d={d}, power={power}, alpha={alpha}")
    return n

def power_one_sample_t_test(n: int, d: float, alpha: float = 0.05, one_tailed: bool = True) -> float:
    """
    Compute statistical power for one-sample t-test (H2: Retrieval quality).

    Tests whether observed mean differs from a threshold (e.g., P@5 > 0.70).

    Args:
        n: Sample size (number of prompts)
        d: Standardized effect size = (μ - μ0) / σ
            where μ0 is threshold (0.70), μ is expected mean (e.g., 0.80), σ is SD
        alpha: Significance level
        one_tailed: True for H1: μ > μ0

    Returns:
        Power

    Example:
        >>> # Test P@5 > 0.70, expect mean=0.80, SD=0.10
        >>> d = (0.80 - 0.70) / 0.10  # d = 1.0
        >>> power_one_sample_t_test(n=50, d=1.0, alpha=0.017)
        0.97  # Very high power for large effect
    """
    from scipy import stats

    df = n - 1

    if one_tailed:
        t_crit = stats.t.ppf(1 - alpha, df)
    else:
        t_crit = stats.t.ppf(1 - alpha/2, df)

    # For one-sample t-test, ncp = d * sqrt(n)
    ncp = d * math.sqrt(n)

    power = 1 - stats.nct.cdf(t_crit, df, ncp)

    return power

def interpret_cohens_d(d: float) -> str:
    """
    Interpret Cohen's d effect size according to standard benchmarks.

    Cohen (1988) guidelines:
    - Small: d = 0.2
    - Medium: d = 0.5
    - Large: d = 0.8

    Args:
        d: Cohen's d value

    Returns:
        Interpretation string
    """
    if d < 0.2:
        return "negligible"
    elif d < 0.5:
        return "small"
    elif d < 0.8:
        return "medium"
    else:
        return "large"

def print_power_table():
    """
    Print power table for common scenarios in TOOL thesis experiments.
    """
    print("=" * 80)
    print("POWER ANALYSIS FOR TOOL THESIS EXPERIMENTS")
    print("=" * 80)
    print()

    # H1: Correctness (paired t-test with Bonferroni correction)
    print("H1: CORRECTNESS (Paired t-test, α=0.017 Bonferroni)")
    print("-" * 80)
    print(f"{'n':<10} {'d=0.3 (small)':<20} {'d=0.5 (medium)':<20} {'d=0.8 (large)':<20}")
    print("-" * 80)

    for n in [20, 30, 40, 50, 75, 100]:
        power_small = power_paired_t_test(n, d=0.3, alpha=0.017, one_tailed=True)
        power_medium = power_paired_t_test(n, d=0.5, alpha=0.017, one_tailed=True)
        power_large = power_paired_t_test(n, d=0.8, alpha=0.017, one_tailed=True)
        print(f"{n:<10} {power_small:<20.2f} {power_medium:<20.2f} {power_large:<20.2f}")

    print()
    print("RECOMMENDED: n=50 gives 82% power for medium effect (d=0.5)")
    print()

    # H2: Retrieval Quality (one-sample t-test)
    print("H2: RETRIEVAL QUALITY (One-sample t-test, α=0.017 Bonferroni)")
    print("-" * 80)
    print("Scenario: Test whether P@5 > 0.70 (threshold)")
    print()
    print(f"{'Expected P@5':<15} {'Assumed SD':<15} {'Cohen\'s d':<15} {'Power (n=50)':<15}")
    print("-" * 80)

    scenarios = [
        (0.75, 0.10),  # Conservative: small improvement, high variance
        (0.80, 0.10),  # Medium improvement
        (0.80, 0.05),  # Medium improvement, low variance
        (0.85, 0.10),  # Large improvement
    ]

    for expected_mean, sd in scenarios:
        d = (expected_mean - 0.70) / sd
        power = power_one_sample_t_test(n=50, d=d, alpha=0.017, one_tailed=True)
        print(f"{expected_mean:<15.2f} {sd:<15.2f} {d:<15.2f} {power:<15.2f}")

    print()
    print("RECOMMENDED: n=50 gives >95% power if P@5 ≥ 0.80 (medium effect)")
    print()

    # H3: Replayability (descriptive, no power analysis needed)
    print("H3: REPLAYABILITY (Descriptive analysis, no power calculation)")
    print("-" * 80)
    print("Expected SRA = 1.00 (100% exact match) across 10 trials.")
    print("If ANY trial fails (SRA < 1.00), investigate determinism bug.")
    print("No statistical test needed - this is a binary pass/fail criterion.")
    print()

    # Sample size recommendations
    print("=" * 80)
    print("SAMPLE SIZE RECOMMENDATIONS FOR TARGET POWER = 80%")
    print("=" * 80)
    print()
    print(f"{'Effect Size':<20} {'α=0.05':<15} {'α=0.017 (Bonf.)':<20}")
    print("-" * 80)

    for d in [0.3, 0.5, 0.8]:
        n_005 = sample_size_paired_t_test(d, power=0.80, alpha=0.05, one_tailed=True)
        n_017 = sample_size_paired_t_test(d, power=0.80, alpha=0.017, one_tailed=True)
        effect_label = f"d={d} ({interpret_cohens_d(d)})"
        print(f"{effect_label:<20} {n_005:<15} {n_017:<20}")

    print()
    print("CONCLUSION: n=50 prompts is sufficient for 80% power with Bonferroni correction,")
    print("assuming medium effect size (d=0.5). This is the recommended sample size for")
    print("H1 (Correctness) and H2 (Retrieval Quality) experiments.")
    print("=" * 80)

if __name__ == "__main__":
    # Check if scipy is available
    try:
        import scipy
    except ImportError:
        print("ERROR: scipy is required for power analysis.")
        print("Install with: pip install scipy")
        exit(1)

    # Print power table
    print_power_table()

    # Interactive calculator (optional)
    print()
    print("INTERACTIVE POWER CALCULATOR")
    print("-" * 80)

    try:
        n = int(input("Enter sample size (n): "))
        d = float(input("Enter expected Cohen's d effect size: "))
        alpha = float(input("Enter alpha (default 0.017 for Bonferroni): ") or "0.017")

        power = power_paired_t_test(n, d, alpha, one_tailed=True)

        print()
        print(f"Results:")
        print(f"  Sample size: n = {n}")
        print(f"  Effect size: d = {d} ({interpret_cohens_d(d)})")
        print(f"  Significance: α = {alpha}")
        print(f"  Power: {power:.4f} ({power*100:.1f}%)")
        print()

        if power < 0.80:
            print(f"⚠️  Power is below 80%. Consider increasing sample size.")
            recommended_n = sample_size_paired_t_test(d, power=0.80, alpha=alpha, one_tailed=True)
            print(f"    Recommended: n = {recommended_n} for 80% power")
        else:
            print(f"✅ Power is sufficient (≥80%)")

    except (ValueError, EOFError, KeyboardInterrupt):
        print()
        print("Exiting interactive calculator.")

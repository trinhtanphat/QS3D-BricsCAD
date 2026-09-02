# Cost benchmark exact representable average

Lane-Key: `issue-5351`

## Purpose

Preserve a mathematically exact benchmark average when the raw aggregate itself exceeds decimal coefficient capacity but the final mean is still exactly representable. This is deterministic Core commercial correctness; No licensed BricsCAD runtime is required.

## Reproduction

Use thirteen historical unit-cost samples: seven zero-valued samples and six `decimal.MaxValue` samples. Their raw exact sum cannot be represented by one decimal, but the exact mean is `36566844237352771197020284770m`. The historical incremental fallback can round intermediate updates and return one less than that representable result.

## Correctness boundary

`CostBenchmarkService` performs an exact rational probe before any rounded average fallback. `CostDecimalMath.TryAverageNonNegativeExactly` accumulates decimal coefficients with `BigInteger`, divides out the sample count exactly, and returns only when the resulting rational has a finite decimal representation within scale 0..28 and the 96-bit decimal coefficient bound.

When the exact rational probe cannot represent the mathematical mean exactly, the historical non-exact fallback remains authoritative. This preserves ordinary decimal division behavior and the existing fail-closed high-magnitude precision guard. There is no binary floating-point fallback.

## Regression

`CostBenchmarkMedianPrecisionSmoke` proves the seven-zero/six-MaxValue mean exactly, repeats the same values in another input order, preserves zero deviation at the exact mean, retains the existing representable high-magnitude control, retains the existing genuinely unrepresentable high-magnitude refusal, and preserves ordinary even/odd median behavior.

## Acceptance

Run the focused preflight, the registered deterministic Core smoke suite, protected `preflight`, and protected `core` on one exact candidate head. Reconcile current protected main non-force if it advances before merge, then require fresh exact-head protected checks and verify the protected main merge contains the same correction.

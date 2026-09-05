# Cost benchmark average precision

Lane-Key: `issue-5318`

## User-visible defect

`CostBenchmarkService` accepts finite non-negative historical unit costs and reports their benchmark average. The old overflow fallback updated a running average at the original monetary magnitude. Near `decimal.MaxValue`, an intermediate half-unit contribution could be unrepresentable and trigger a precision-loss exception even when later samples make the exact final average representable.

A deterministic example is `[decimal.MaxValue - 2, decimal.MaxValue - 1, decimal.MaxValue - 1, decimal.MaxValue]`. Its raw sum cannot be held in `decimal`, but its exact representable final average is `decimal.MaxValue - 1`.

## Correctness contract

The ordinary exact-sum fast path remains unchanged. When the raw exact sum cannot be represented, benchmark averaging uses translation around the sorted minimum sample: subtract the baseline, aggregate/divide the non-negative translated values, then add the translated average back to the baseline through the existing non-zero-contribution precision guard.

Translation keeps clustered high-magnitude differences small without weakening decimal correctness. If the translated average itself, or its final addition back to the baseline, cannot be represented faithfully, the operation must still fail closed rather than silently round away a non-zero contribution.

Median, minimum/maximum, sample-count, historical identity/currency filtering and deviation behavior remain unchanged.

## Deterministic regression

The already-registered `CostBenchmarkMedianPrecisionSmoke` now also proves:

- the four-sample near-ceiling case returns exactly `decimal.MaxValue - 1`;
- a two-sample case whose mathematical mean ends in an unrepresentable half-unit still fails closed at the translated-average rebind;
- ordinary even/odd benchmark controls retain their prior results.

Run the focused source guard:

```text
python scripts/preflight-cost-benchmark-average-precision.py
```

Protected Shared CI runs the auto-discovered guard and deterministic Core smoke suite.

## Runtime boundary

Runtime is `NOT_APPLICABLE`. This is deterministic Core commercial/numeric correctness and requires no licensed BricsCAD `LOCAL_PASS`.

# Measurement/work-item coverage evaluator bound

## Scope

REMOTE_SAFE managed Core validation for `MeasurementWorkItemCoverageEvaluator`. No licensed BricsCAD runtime evidence is required or implied.

## Invariant

Coverage evaluation and the downstream coverage report share the same maximum supported finding count of 10,000. The evaluator must reject an over-budget project before materializing over-budget quantity payload snapshots or appending finding 10,001.

Admission is two-stage:

1. reject more than 10,000 project elements before `Elements.ToArray()`, because every element contributes at least one finding;
2. for an admitted element set, sum each element's quantity contribution (`0 quantities => 1 missing-quantity finding`, otherwise one finding per quantity) and reject cumulative overflow before any `Quantities.ToArray()` snapshot materialization.

`AddFinding` retains a final defensive bound at publication time. The downstream `MeasurementWorkItemCoverageReport` 10,000-item guard remains unchanged.

## Regression coverage

`MeasurementWorkItemCoverageEvaluatorBoundSmoke` proves exactly 10,000 findings are admitted, 10,001 quantities are rejected, and 10,001 missing-quantity elements are rejected at the evaluator admission boundary. `scripts/preflight-measurement-work-item-coverage-evaluator-bound.py` pins the admission-before-materialization ordering.

## Validation

Run the focused preflight, then the normal deterministic Core smoke suite. Merge only from a current, collision-clean exact head with protected `preflight` and `core` SUCCESS under repository policy.

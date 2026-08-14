# Work claim — MAP-03B compact coverage matrix projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map03b-coverage-matrix-20260814`
- Registered UTC: `2026-08-14T01:58:00Z`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: `MAP-03 P1`

## Verified gap

MAP-03A already provides a deterministic per-element `MeasurementWorkItemCoverageReport` with ready/missing/stale/unmapped summary counts. The live Core tree still has no compact category × measurement-item × mapped-work-item projection, so consumers must inspect raw per-element rows to understand repeated coverage states or identify the affected element set for a matrix cell.

## Reserved scope

- new `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageMatrix.cs`
- one focused self-registering Core smoke regression
- this claim file

## Bounded implementation

- project MAP-03A report rows into deterministic compact cells grouped by category, measurement item, mapping/classification/work-item identity, readiness and issue set;
- retain deterministic affected element ids per cell so a later UI can make each issue state actionable without re-scanning raw rows;
- expose report-level summary counts unchanged on the matrix projection;
- preserve nullable measurement/mapping identities for missing-quantity cells instead of inventing sentinel ids;
- reject null report input and preserve immutable/detached output collections;
- do not modify MAP-03A row/report semantics, mapping resolver semantics, persistence, rates/cost, UI/V25/native surfaces, or BricsCAD integration.

## Validation policy

No GitHub Actions will be dispatched. Managed/native PASS will only be reported if actually executed. No force-push.

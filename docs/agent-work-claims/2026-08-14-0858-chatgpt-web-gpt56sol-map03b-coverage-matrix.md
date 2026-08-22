# Work claim — MAP-03B compact coverage matrix projection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map03b-coverage-matrix-20260814`
- Registered UTC: `2026-08-14T01:05:52Z`
- Last updated UTC: `2026-08-14T01:13:45Z`
- Baseline main SHA: `a82b3c993579d00643bfdad862a4cd6d6610a582`
- Priority: `MAP-03 P1`

## Verified gap

MAP-03A already provides a deterministic per-element `MeasurementWorkItemCoverageReport` with ready/missing/stale/unmapped summary counts. The live Core tree had no compact category × measurement-item × mapped-work-item projection, so consumers had to inspect raw per-element rows to understand repeated coverage states or identify the affected element set for a matrix cell.

## Reserved scope

- new `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageMatrix.cs`
- one focused self-registering Core smoke regression
- this claim file

## Completed implementation

- added `MeasurementWorkItemCoverageMatrix` and immutable matrix cells projected from the existing MAP-03A report;
- compacted rows deterministically by category, measurement item, mapping/classification/work-item identity, readiness and issue set;
- retained deterministic unique affected element ids and finding counts per cell for later actionable UI consumption;
- preserved nullable measurement/mapping identities for missing and unmapped states instead of inventing sentinel ids;
- exposed MAP-03A summary counts unchanged at matrix level;
- added a self-registering Core smoke covering compaction, ready/stale separation, summary parity, missing/unmapped identity, detached/read-only output and null/empty input behavior;
- kept MAP-03A evaluator/report semantics, persistence, rates/cost, UI/V25/native and BricsCAD integration unchanged.

## Published commits

- Source: `e0807f0149dd07fbbb1d8608896c2c7dcb503213` — `feat(mapping): add compact coverage matrix projection`
- Smoke regression: `fc7a233a433e3d4e5536c8524f4f5fe1f79647d8` — `test(mapping): cover compact coverage matrix projection`
- Remote ancestry verified through main `d5ab24f28cb4c30034eacec32055ed0e4ab58363`; both commits remain on the current main lineage and later commits do not modify the MAP-03B files.

## Validation

- GitHub source/test content and remote ancestry: verified.
- Static contract review against current MAP-03A report/evaluator: verified.
- Managed smoke execution: not executed in this session; regression source was added but no runtime PASS is claimed.
- GitHub Actions: not dispatched.
- Native/BricsCAD validation: not executed and no PASS is claimed.
- Force-push: not used.

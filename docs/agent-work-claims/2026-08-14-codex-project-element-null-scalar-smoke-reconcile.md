# Work claim — ProjectElement null-scalar smoke reconciliation

- Status: `ACTIVE`
- Agent: `codex-project-element-null-scalar-smoke-reconcile-20260814` (`/root/fix_source_reconcile_desync`)
- Registered: `2026-08-14T15:44:00+07:00`
- Baseline main SHA: `cd18f454698a93b53f7f4bd5f057a1a075dee0d6`
- Priority: next deterministic Core full-smoke blocker after completed relation setter persistability

## Confirmed fixture drift

`ProjectElementNullScalarPersistabilitySmoke.ConstructorTrimAndSetterExactnessRemain` still expects post-construction Family/Floor/Zone assignments to preserve padded text exactly. Completed relation persistability now trims those three optional identity setters and rejects controls. The smoke therefore fails on expected `"  F2  "` versus stored `"F2"` before later registered persistence coverage can run.

The smoke's other contracts remain current and reachable: null assignments become empty immediately, constructor relation inputs trim, empty scalars round-trip through QSDB, and `DrawingFingerprint` preserves exact padded non-null text while null becomes empty. Control rejection/atomicity and relation-null clearing remain covered by the separate `ProjectElementRelationPersistabilitySmoke` and are not changed.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectElementNullScalarPersistabilitySmoke.cs`
- this claim only

Rename the mixed assertion method to describe relation normalization plus fingerprint exactness, and change only its three post-construction relation expectations to `F2`, `L2`, and `Z2`. Preserve every null, empty, constructor, DrawingFingerprint and QSDB round-trip assertion.

## Explicit exclusions

- no production element, relation, persistence/schema or fingerprint behavior changes;
- no focused gate change because no focused gate references this smoke or stale exactness expectation;
- no LOCAL runner/probe/docs, issue `#1005`, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- no edit to `ProjectElementRelationPersistabilitySmoke` or other persistence fixtures; report the next full-smoke blocker rather than absorbing it.

## Validation

- Core Release build and full deterministic Core smoke;
- focused QSDB relation identity, canonical identity and schema gates;
- generic and manual-only policy gates;
- readback that null/empty/control/round-trip and `DrawingFingerprint` exactness coverage remains present.

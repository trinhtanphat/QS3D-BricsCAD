# Work claim — ProjectElement null-scalar smoke reconciliation

- Status: `COMPLETED`
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

## Completion record

- Claim PR `#1232` merged as `4c903ea3065a95356550967b450adf2158ee9078`.
- Test commit `c341b4aac` merged through PR `#1234` as `4541cdaa9db67d53caef127547780fb12c3875bc`.
- The one reserved smoke now expects post-construction Family/Floor/Zone assignments to store `F2`, `L2`, and `Z2`; its method name distinguishes relation normalization from retained `DrawingFingerprint` exactness. All null-to-empty, constructor trim, QSDB empty-scalar round-trip and padded fingerprint assertions remain unchanged. Separate relation control rejection/atomicity and null-clear coverage remain untouched.
- Core Release build PASS with `0 warnings / 0 errors`. QSDB canonical-identities and schema gates PASS; generic and manual-only policy gates PASS.
- The focused QSDB relation-identity gate is blocked outside this lane by a stale source literal: it requires direct `target[key] = RawValue(item, "value");`, while current production reads `RawValue` into a local, validates it, then assigns that local. This claim did not edit the gate or production.
- Full Core smoke advances beyond this null-scalar smoke and stops at the next independent fixture drift: `ProjectFloorZoneMutationIntegritySmoke.FloorAssignmentCanonicalIdentityIsNoOp` expects padded stored FloorId text, while the completed relation setter stores the trimmed identity. This lane did not edit or absorb that blocker.
- No production, focused gate, LOCAL, issue `#1005`, BricsCAD/native/private data or GitHub Actions surfaces were changed/run.

# Work claim — QSDB raw relation validation fixture boundary

- Status: `ACTIVE`
- Agent: `codex-qsdb-relation-reflection-fixture-20260814` (`/root/fix_level_curtain_frame_z`)
- Registered: `2026-08-14T16:10:00+07:00`
- Baseline main SHA: `5f64b21ed23429001d5f1f6e6abe213593b0b857`
- Priority: next deterministic full Core smoke blocker after supported relation writers became canonical

## Confirmed two-layer contract

Supported `ProjectState` active-context and `ProjectElement` relation setters now trim optional identity input, canonicalize null/whitespace to empty, and reject control characters before storage. `QsdbRelationIdentityCanonicalSmoke` still uses those public setters to try to create padded raw state, so its first post-failed-Save assertion expects text that was already normalized before Save ran.

QSDB persistence independently retains a required defensive boundary: `ValidateOptionalCanonicalValue` rejects corrupt/legacy raw project and element relation fields before Save touches or serializes the project, and failed validation must not rewrite that raw state. Canonical public expectations would no longer exercise this boundary.

No open PR or ACTIVE/BLOCKED claim owns the exact smoke or focused gate. Prior relation-writer and RawValue gate claims are `COMPLETED`.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/QsdbRelationIdentityCanonicalSmoke.cs`
- `scripts/preflight-qsdb-relation-identity.py`
- this claim only

Construct canonical valid objects, assert them, then use test-local reflection to inject all six unreachable raw cases: padded/whitespace `_activeFloorId`, padded `_activeZoneId`, and padded element `_familyId`, `_floorId`, `_zoneId`. Assert each injection reached the public getter, retain `InvalidDataException`, exact raw no-mutation and timestamp checks, and leave the empty optional-relation Save/Load round-trip unchanged. Update only the gate's obsolete smoke setter tokens to pin that reflection boundary; preserve all production validator and RawValue hydration checks.

## Explicit exclusions

- no changes to `ProjectState`, `ProjectElement`, `QsdbProjectStore`, schema, migration, metadata hydration or any other production source;
- no other QSDB gate, smoke, persistence or relation fixture changes;
- no LOCAL probe/runner, BricsCAD/native/private data, GitHub Actions, release or packaging work;
- report the next independent full-smoke blocker rather than expanding scope.

## Validation

- focused `preflight-qsdb-relation-identity.py` plus relevant QSDB canonical/schema gates;
- Core Release build and full deterministic Core smoke;
- exact diff/readback for all six injections, reached assertions, failed-Save no-mutation/timestamp assertions and unchanged empty round-trip.

## Completion record

Pending implementation and validation after this claim is merged to `main`.

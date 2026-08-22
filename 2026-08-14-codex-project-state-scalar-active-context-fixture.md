# Work claim — ProjectState scalar active-context fixture reconciliation

- Status: `COMPLETED`
- Agent: `codex-root`
- Registered: `2026-08-14T16:08:00+07:00`
- Baseline main SHA: `1ea25aa7f69c70f72a81187a3d72f1766375a7e3`
- Priority: next deterministic full Core smoke blocker

## Confirmed fixture drift

`ProjectStatePersistedScalarVersioningSmoke` uses one helper that expects exact raw string storage for `DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId`. The completed active-context persistability contract later routed only ActiveZone/ActiveFloor through `SetActiveContextId`, which trims padding and rejects controls before the ordinary persisted-scalar version mutation. The smoke therefore fails on ActiveZone before testing its version/timestamp/idempotence contract.

DrawingPath and DrawingFingerprint must retain exact raw string storage. ActiveZone and ActiveFloor must store the canonical trimmed values while still advancing `ChangeVersion` exactly once, refreshing `UpdatedUtc`, and treating a repeated canonically-equivalent padded assignment as a no-op.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectStatePersistedScalarVersioningSmoke.cs`
- this claim document only

## Planned change

- let the test helper accept a distinct expected stored value;
- retain exact padded expectations for DrawingPath and DrawingFingerprint;
- require `zone-a` / `floor-a` for padded ActiveZone/ActiveFloor writes;
- repeat the same padded write to preserve the no-extra-version/no-extra-timestamp assertion;
- retain snapshot persistence-stamp restoration coverage unchanged.

## Explicit exclusions

- no changes to `ProjectState`, `ProjectStateSnapshot`, active-context services, QSDB/interchange persistence or production source;
- no other smoke/gate fixture expansion;
- no native BricsCAD, LOCAL runners/probes, workflows, release, private data or GitHub Actions;
- report the next independent full-smoke blocker.

## Validation

- Core SmokeTests Release build with zero warnings/errors;
- full registered Core smoke;
- focused persisted-scalar / active-context / snapshot preflights available on final exact SHA;
- exact one-smoke diff/readback.

## Completion record

- Claim PR #1259 merged as `1c3954a60f016a2080ab3178b89c7fba3d84dfbf` (claim head `6e14c7300977b6d4b069540a94631ab9243ed9c7`).
- Implementation commit `783a394dc2c3d7f88834abd8529dcaf4b92da0cb` merged through PR #1262 as `2428e63869a51dc6d9c8d1d77a8cd2e61832fb38`.
- The one-smoke diff retains exact padded storage for DrawingPath/Fingerprint and expects canonical trimmed ActiveZone/ActiveFloor values while preserving exactly-once version/timestamp and repeated padded-write no-op assertions.
- Core SmokeTests Release build passed with 0 warnings / 0 errors; Floor/Zone mutation-integrity, rollback-matrix, generic and manual-only preflights passed.
- Full registered Core smoke advanced and next stopped independently in `QsdbRelationIdentityCanonicalSmoke`, whose padded relation setup is canonicalized before Save.
- No production, native BricsCAD, LOCAL, workflow, private-data or GitHub Actions surface changed or ran.

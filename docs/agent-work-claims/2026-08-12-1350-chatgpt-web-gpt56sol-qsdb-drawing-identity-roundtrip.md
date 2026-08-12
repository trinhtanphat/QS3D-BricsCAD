# Work claim — QSDB drawing identity round-trip

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-drawing-identity-roundtrip-20260812-1350`
- Registered: `2026-08-12T13:50:00+07:00`
- Last Updated: `2026-08-12T14:02:00+07:00`
- Baseline main SHA: `5f711e8eee5b05c4a6f76bba0a5804d93ea8fa44`
- Source branch fix SHA: `45ff5a6340fb90baa66b3ce1af753029e7dd8bb1`
- Regression branch SHA: `15f9c11dd523f23e61b5e5ac7b387af86cd6603f`
- PR: `#926`
- Main merge SHA: `795b9fc917cca82e8ff8a68619b69ae07eaa4174`
- Priority: P1 — accepted QSDB state must round-trip without silent mutation.
- Task Key: `CORE-QSDB-DRAWING-IDENTITY-ROUNDTRIP`

## Confirmed defect

`QsdbProjectStore.Save(...)` validates and serializes `ProjectState.DrawingPath`, project `DrawingFingerprint`, and element `DrawingFingerprint` verbatim. The current QSDB schema intentionally does not declare those attributes as canonical trimmed identifiers. `Load(...)` previously materialized all three through `Value(...)`, which calls `.Trim()`. A project accepted by `Save(...)` could therefore persist drawing identity text successfully and then silently load different values. Relation IDs are not part of this defect because current-schema validation already requires their canonical whitespace form.

## Completed implementation

- Project `DrawingPath` and `DrawingFingerprint` are now materialized with the existing raw-value helper.
- Element `DrawingFingerprint` is now materialized with the same raw-value helper.
- Active Zone/Floor and element Family/Floor/Zone relation IDs remain on their existing canonicalized read path.
- XML text validity, size bounds, schema migration, backup/recovery and save behavior were not changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/QsdbDrawingIdentityRoundTripSmoke.cs` is auto-registered and saves a project whose project drawing path/fingerprint and element drawing fingerprint contain leading/trailing spaces, reloads it, and requires exact ordinal equality. The fixture also verifies canonical active Zone/Floor and element Family/Floor/Zone relation IDs still round-trip.

The exact PR diff was reviewed before merge: two changed files only, with three production read-site substitutions plus the focused smoke. Current-main readback at `f0c2d97ae57dc196caac5c3edd83c97cfd5306e2` confirmed both source and regression remain integrated.

## Validation boundary

No GitHub Actions were dispatched. No full executable Core smoke/build or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only session.

## Completion condition

Completed: QSDB Save→Load no longer silently trims persisted drawing path/fingerprint identity text while canonical relation-id behavior remains unchanged.

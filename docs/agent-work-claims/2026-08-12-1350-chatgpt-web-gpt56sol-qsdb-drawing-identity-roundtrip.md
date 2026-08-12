# Work claim — QSDB drawing identity round-trip

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-drawing-identity-roundtrip-20260812-1350`
- Registered: `2026-08-12T13:50:00+07:00`
- Baseline main SHA: `5f711e8eee5b05c4a6f76bba0a5804d93ea8fa44`
- Priority: P1 — accepted QSDB state must round-trip without silent mutation.
- Task Key: `CORE-QSDB-DRAWING-IDENTITY-ROUNDTRIP`

## Confirmed defect

`QsdbProjectStore.Save(...)` validates and serializes `ProjectState.DrawingPath`, project `DrawingFingerprint`, and element `DrawingFingerprint` verbatim. The current QSDB schema intentionally does not declare those attributes as canonical trimmed identifiers. `Load(...)`, however, materializes all three through `Value(...)`, which calls `.Trim()`. A project accepted by `Save(...)` can therefore persist drawing identity text successfully and then silently load different values. Relation IDs are not part of this defect because current-schema validation already requires their canonical whitespace form.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbDrawingIdentityRoundTripSmoke.cs`
- this claim file

## Intended contract

- Preserve `DrawingPath` and drawing fingerprint attributes exactly as persisted; do not silently trim opaque drawing identity text during load.
- Keep existing canonical validation/materialization behavior for project/element relation IDs unchanged.
- Preserve current XML text validity, size bounds, schema migration, backup/recovery and save semantics.

## Regression plan

Focused auto-registered Core smoke saves a valid project whose project drawing path/fingerprint and element drawing fingerprint contain leading/trailing spaces, then reloads it and requires exact ordinal equality with the saved values. Also verify canonical relation IDs still load canonically. Re-fetch exact source and collision-check before code writes.

## Validation boundary

No GitHub Actions dispatch, full executable smoke/build, or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

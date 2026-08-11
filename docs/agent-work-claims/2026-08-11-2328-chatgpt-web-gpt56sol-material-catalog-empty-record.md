# Work claim — Material catalog empty-record integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:28:00+07:00`
- Baseline main SHA: `07f56571314e63d606b3c1348fd611ee01426abd`
- Priority: evidence-driven remote-safe Core persisted-data hardening

## Reason

`ProjectMaterialCatalog.WriteCustom()` serializes custom materials as exactly one non-empty Base64 record per line, joined with `\n`. `ReadCustom()` currently splits with `StringSplitOptions.RemoveEmptyEntries`, so a tampered persisted catalog containing an empty record line or trailing blank record is silently repaired during read instead of being rejected as non-canonical/corrupted state.

## Reserved scope

Reject empty persisted material-catalog record lines while preserving valid one-record-per-line encoding, strict UTF-8 decoding, material limits, ordering, built-in shadowing checks, Unicode behavior, and all catalog mutation semantics. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogEmptyRecordSmoke.cs`
- this claim file

## Excluded scope

- No changes to Material Catalog UI, material reference rename/delete semantics, XLSX export, Family/Instance material behavior, or BricsCAD V25 runtime.
- No change to valid persisted material record encoding.
- No GitHub Actions dispatch.

## Validation plan

- Create valid catalog metadata via public `UpsertCustom()`, inject an empty record line in the persisted metadata, and assert `GetCustom()` fails closed.
- Assert the untouched canonical metadata still round-trips the same custom material.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The prior strict UTF-8 material catalog claim is `COMPLETED`. No current Material Catalog claim was found; this lane only hardens empty persisted record handling.

## Completion condition

Current `main` rejects empty persisted material catalog records, preserves canonical catalog round-trips, includes focused regression coverage, and this claim is marked `COMPLETED`.

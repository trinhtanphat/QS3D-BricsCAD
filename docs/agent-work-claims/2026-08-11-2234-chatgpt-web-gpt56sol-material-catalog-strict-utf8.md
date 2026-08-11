# Work claim — Material catalog strict UTF-8 decode

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:34:00+07:00`
- Baseline main SHA: `957b46dcb934a1d96046cfbf8376a03fc408413e`
- Priority: evidence-driven remote-safe Core persistence hardening

## Reason

`ProjectMaterialCatalog.Decode()` rejects malformed Base64 syntax but uses the replacement-fallback `Encoding.UTF8.GetString`. A syntactically valid Base64 field containing invalid UTF-8 bytes therefore decodes to replacement characters and is accepted as material data instead of being treated as corrupted persisted catalog state. Other persisted target-state codecs in Core already use strict UTF-8 decoding.

## Reserved scope

Make material catalog Base64 decoding fail closed on invalid UTF-8 byte sequences while preserving valid UTF-8, Base64 layout, material limits, built-in shadowing checks, sorting, reference updates, and catalog mutation semantics. Add a CAD-independent regression smoke covering corrupted UTF-8 and valid non-ASCII round-trip behavior.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogUtf8Smoke.cs`
- this claim file

## Excluded scope

- No changes to Material Catalog UI/modeless lifecycle, XLSX material usage export, canonical project binding, Family/Instance material assignment, or BricsCAD V25 runtime.
- No change to valid persisted material encoding format.
- No GitHub Actions dispatch.

## Validation plan

- Inject a four-field catalog record whose ID field is valid Base64 for an invalid UTF-8 byte sequence and assert `GetCustom()` throws `InvalidOperationException` rather than accepting replacement characters.
- Upsert and re-read a valid Vietnamese/non-ASCII custom material to confirm valid UTF-8 behavior is preserved.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent Material Catalog commits focus on UI/project lifecycle and stale-write safety. No current claim or recent commit was found for strict UTF-8 decoding of persisted Core catalog metadata; this lane stays inside `ProjectMaterialCatalog` plus a dedicated smoke.

## Completion condition

Current `main` rejects invalid UTF-8 material catalog fields, preserves valid Unicode catalog behavior, includes focused regression coverage, and this claim is marked `COMPLETED`.

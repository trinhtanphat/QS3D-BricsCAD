# Work claim — Material catalog strict UTF-8 decode

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:34:00+07:00`
- Baseline main SHA: `957b46dcb934a1d96046cfbf8376a03fc408413e`
- Priority: evidence-driven remote-safe Core persistence hardening

## Reason

`ProjectMaterialCatalog.Decode()` rejected malformed Base64 syntax but used the replacement-fallback `Encoding.UTF8.GetString`. A syntactically valid Base64 field containing invalid UTF-8 bytes therefore decoded to replacement characters and was accepted as material data instead of being treated as corrupted persisted catalog state. Other persisted target-state codecs in Core already use strict UTF-8 decoding.

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

Recent Material Catalog commits focus on UI/project lifecycle and stale-write safety. No current claim or recent commit was found for strict UTF-8 decoding of persisted Core catalog metadata; this lane stayed inside `ProjectMaterialCatalog` plus a dedicated smoke.

## Completion

- Implementation commits:
  - `a393d31cffec06e25a11da96807d71653f0cde31` — add strict UTF-8 decoding and normalize invalid Base64/UTF-8 catalog fields to `InvalidOperationException`.
  - `0d6874710daf68b1e8b7d981066e7a4cb56afd97` — add corrupted-byte and valid Vietnamese Unicode round-trip regression coverage.
- Final observed `main` before claim close: `0d6874710daf68b1e8b7d981066e7a4cb56afd97`.
- Validation actually performed:
  - re-fetched `ProjectMaterialCatalog.cs` from current `main` and confirmed `UTF8Encoding(false, true)` plus `DecoderFallbackException` handling are present;
  - re-fetched the new smoke and confirmed valid Base64 carrying bytes `C3 28` fails closed while `Vữa tô`, `m²`, and `Hoàn thiện tường` round-trip through the public catalog API;
  - valid persisted Base64/UTF-8 format and catalog mutation paths were otherwise left unchanged;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core persisted-data integrity hardening.

## Completion condition

Satisfied: current `main` rejects invalid UTF-8 material catalog fields, preserves valid Unicode catalog behavior, includes focused regression coverage, and this claim is released as `COMPLETED`.

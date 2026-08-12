# Work claim — Template BQ column canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-template-bq-column-canonicality-20260812-0817`
- Registered: `2026-08-12T08:17:00+07:00`
- Completed: `2026-08-12T08:20:00+07:00`
- Baseline main SHA: `e4842c86e1fb5c64eeb87dc71fc5ad1a1a3115ba`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`
- Integration PR: `#642`
- Main integration commit: `74a57fb1ae6b1458c7663bd961b8bad163c517b8`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` emits visible BQ columns as trimmed, case-insensitive-distinct values sorted with `StringComparer.OrdinalIgnoreCase`. `Load(...)` accepted each `<column name="...">` through `Required(...)`, which trimmed the raw XML and appended it without checking duplicate/order canonicality. A persisted template could therefore contain padded names, duplicate/case-duplicate names, or noncanonical order and be silently rewritten on the next save.

## Implemented scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateBqColumnCanonicalitySmoke.cs`
- this claim file for close-out

## Completed contract

- Persisted `<bqColumns>` names must already be nonblank and unpadded.
- Persisted BQ column names must be case-insensitive unique.
- Persisted BQ columns must already use the ordering emitted by `Serialize(...)`.
- Programmatic profile save behavior remains normalization-friendly: trimming, case-insensitive de-duplication and deterministic ordering happen during serialization before strict re-load verification.
- Family/rule category semantics, layer mappings, template import UI/lifecycle, BricsCAD runtime and release behavior remain unchanged.

## Validation evidence

- PR `#642` was synchronized with moving `main` without force-push and squash-merged to `main` as `74a57fb1ae6b1458c7663bd961b8bad163c517b8`.
- Post-merge readback confirms `Load(...)` routes persisted BQ columns through `ReadCanonicalBqColumns(...)`.
- The helper rejects blank/padded names, case-insensitive duplicates and noncanonical ordering.
- Post-merge readback confirms isolated smoke source covers padded, case-duplicate and reversed persisted lists plus programmatic normalization/canonical round-trip compatibility.
- No GitHub Actions/build/release was dispatched and no local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source was re-read, and exact integration SHA/evidence is recorded above.

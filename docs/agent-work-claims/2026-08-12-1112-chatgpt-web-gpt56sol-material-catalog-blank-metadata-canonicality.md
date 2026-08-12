# Work claim — Material catalog blank metadata canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-blank-metadata-canonicality`
- Registered: `2026-08-12T11:12:00+07:00`
- Baseline main SHA: `e7cb9601c2a535b52af44e8dcd7fb76aae68e2db`
- Priority: P1 — persisted material catalog corruption must not bypass the existing canonical record/Base64/text validation path.

## Confirmed defect

`ProjectMaterialCatalog.ReadCustom(...)` currently returns an empty custom catalog when the metadata value is any `string.IsNullOrWhiteSpace(...)` value. The canonical writer removes `QS3D.MaterialCatalog.v1` entirely when there are no custom materials, while the parser already rejects whitespace/empty records inside a non-empty payload and requires canonical Base64 plus canonical decoded text. A present whitespace-only metadata payload such as spaces or tabs therefore bypasses all persisted-catalog integrity checks and is silently interpreted as canonical absence.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` — top-level whitespace-only persisted payload handling only
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogEmptyRecordSmoke.cs` — focused regression extension only
- this claim file for close-out

## Intended contract

- Preserve missing metadata as an empty custom catalog.
- Preserve the existing empty-string compatibility path as empty unless repository evidence requires otherwise.
- Reject non-empty whitespace-only top-level material catalog metadata instead of treating it as absence.
- Preserve canonical catalog round-trip, record/resource bounds, Base64/UTF-8/decoded-text canonicality, built-in shadow protection, ordering, rename/reference semantics, and all unrelated Material XLSX behavior.

## Excluded scope

- No Material XLSX/UI/export changes.
- No material rename/reference-scope redesign, project persistence changes, BricsCAD runtime/UI work, or unrelated Domain changes.
- No force-push, GitHub Actions dispatch, executable full-smoke/build PASS, or licensed BricsCAD V25/V26 runtime qualification claim.

## Validation plan

- Re-fetch current `main` source and focused smoke after claim registration.
- Narrow only the top-level absence fast path so missing/empty remains empty while non-empty whitespace reaches the existing fail-closed record parser.
- Extend the existing empty-record smoke with space/tab-only top-level metadata cases plus absence/empty compatibility controls.
- Re-read integrated source/test from `main`, record exact SHAs, close this claim, and verify completion ancestry.
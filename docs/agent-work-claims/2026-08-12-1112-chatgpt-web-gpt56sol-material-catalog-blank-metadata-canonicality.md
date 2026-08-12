# Work claim — Material catalog blank metadata canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-blank-metadata-canonicality`
- Registered: `2026-08-12T11:12:00+07:00`
- Baseline main SHA: `e7cb9601c2a535b52af44e8dcd7fb76aae68e2db`
- Priority: P1 — persisted material catalog corruption must not bypass the existing canonical record/Base64/text validation path.

## Confirmed defect

`ProjectMaterialCatalog.ReadCustom(...)` returned an empty custom catalog when the metadata value was any `string.IsNullOrWhiteSpace(...)` value. The canonical writer removes `QS3D.MaterialCatalog.v1` entirely when there are no custom materials, while the parser already rejects whitespace/empty records inside a non-empty payload and requires canonical Base64 plus canonical decoded text. A present whitespace-only metadata payload such as spaces or tabs therefore bypassed all persisted-catalog integrity checks and was silently interpreted as canonical absence.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` — top-level whitespace-only persisted payload handling only
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogEmptyRecordSmoke.cs` — focused regression extension only
- this claim file for close-out

## Implemented contract

- Missing metadata remains an empty custom catalog.
- Exact empty-string metadata preserves the existing compatibility empty-catalog behavior.
- Non-empty whitespace-only top-level material catalog metadata now reaches the existing empty-record integrity guard and fails closed.
- Canonical catalog round-trip, record/resource bounds, Base64/UTF-8/decoded-text canonicality, built-in shadow protection, ordering, rename/reference semantics, and unrelated Material XLSX behavior remain unchanged.

## Integration evidence

- Claim: `0fbfdf18292c6316a3174cb4f494fd3da9f525f9`
- Production fix: `17bda087e4b448a58f8a3ec9217b6fb59a6917c9` (`fix(materials): reject whitespace-only catalog metadata`)
- Focused regression: `f4f582e178b46928f4b8641e4a289659887c0109` (`test(materials): guard blank catalog metadata`)
- Integrated source read-back confirms the top-level fast path uses `string.IsNullOrEmpty(raw)` while the existing per-record `string.IsNullOrWhiteSpace(...)` guard remains fail-closed.
- Integrated smoke read-back covers missing metadata, exact-empty compatibility, spaces-only and tab-only top-level metadata, existing empty-record corruption, and canonical catalog round-trip.
- The smoke retains its existing `ModuleInitializer` registration, so no registration-file change was required.

## Excluded scope / validation boundary

- No Material XLSX/UI/export changes.
- No material rename/reference-scope redesign, project persistence changes, BricsCAD runtime/UI work, or unrelated Domain changes.
- No force-push and no GitHub Actions dispatch.
- No executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed from this remote connector lane; validation here is repository integration/read-back plus focused regression source coverage.
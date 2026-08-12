# Work claim — Material Catalog decoded-text canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-catalog-decoded-text-canonicality-20260812-0945`
- Registered: `2026-08-12T09:45:00+07:00`
- Completed: `2026-08-12T09:48:00+07:00`
- Baseline main SHA observed: `21fd3fd762a3379ec1631f008dd80c47a2e525e6`
- Claim commit: `899e855e05fcc06c32b7298914f0b8456d0a9f70`
- Pull Request: `#711`
- Reviewed head: `7757439188329b5ab0dd36f07e86c1fc4f892aff`
- Merge SHA: `611295eee7f94ab13b1d78ef2e14bbb3d6867317`
- Priority: P1 persisted material-catalog integrity
- Task Key: `CORE-MATERIAL-CATALOG-DECODED-TEXT-CANONICALITY`

## Confirmed defect

`ProjectMaterialCatalog.WriteCustom(...)` serializes `ProjectMaterial` fields only after `ProjectMaterial` has trimmed required/optional text. `ReadCustom(...)` previously decoded canonical Base64 and immediately passed decoded text to the constructor, allowing canonical-Base64 records containing padded Id/Name/Unit/Description text to be silently repaired into writer-valid semantic values.

## Completed implementation

- Preserved existing strict UTF-8 and canonical Base64 validation.
- Added a decoded-text boundary requiring each persisted Id/Name/Unit/Description field to already equal its `Trim()` representation before `ProjectMaterial` construction.
- Exact-empty optional Unit/Description remains valid.
- Canonical Unicode, duplicate detection, built-in shadowing, size/count limits and rename/delete behavior remain unchanged.
- Public Upsert input normalization and persistence format were not changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogDecodedTextCanonicalitySmoke.cs` covers canonical record loading plus canonical-Base64 encodings of padded Id, Name, Unit and Description that must fail specifically at the decoded-text canonicality boundary.

Moving-main comparison before PR creation showed no overlap with `ProjectMaterialCatalog.cs` or the smoke, and the source was re-read unchanged on `main` immediately before the head-locked squash merge.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No licensed BricsCAD V25/V26 runtime PASS is claimed.

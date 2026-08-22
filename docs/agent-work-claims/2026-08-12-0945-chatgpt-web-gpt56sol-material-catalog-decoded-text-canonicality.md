# Work claim — Material Catalog decoded-text canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-decoded-text-canonicality-20260812-0945`
- Registered: `2026-08-12T09:45:00+07:00`
- Baseline main SHA observed: `21fd3fd762a3379ec1631f008dd80c47a2e525e6`
- Priority: P1 persisted material-catalog integrity
- Task Key: `CORE-MATERIAL-CATALOG-DECODED-TEXT-CANONICALITY`

## Confirmed defect

`ProjectMaterialCatalog.WriteCustom(...)` serializes `ProjectMaterial` fields only after `ProjectMaterial` has trimmed required/optional text. `ReadCustom(...)`, however, decodes canonical Base64 and immediately passes decoded text to the same constructor. A tampered but canonical-Base64 record can therefore contain decoded values such as `" MAT-1 "`, `" Material A "`, `" kg "` or `" note "`; the constructor silently trims them and the reader accepts a semantic representation the writer never emits.

The completed material-catalog Base64 canonicality lane rejects alternate Base64 spellings, but it does not reject canonical Base64 whose decoded semantic text is non-canonical.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- one focused Core smoke regression for decoded catalog text canonicality
- this claim file

## Intended contract

- persisted decoded Id/Name/Unit/Description text must equal its Trim() result before construction;
- exact empty optional Unit/Description remains valid;
- canonical decoded text and Unicode behavior remain unchanged;
- existing canonical Base64/UTF-8, duplicate, built-in shadowing, size/count and rename/delete behavior remain unchanged;
- reader fails closed instead of repairing persisted metadata.

## Excluded scope

No changes to material reference rename/delete semantics, public Upsert input normalization, Base64 spelling rules, ProjectState persistence format, CAD/UI/runtime, Actions/build/release.

## Validation plan

Add auto-registered Core smoke coverage for canonical record success plus padded decoded Id, Name, Unit and Description rejection using canonical Base64 encodings. Re-fetch moving `main`, review exact overlap, merge with expected head SHA, and close this claim with immutable evidence.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.

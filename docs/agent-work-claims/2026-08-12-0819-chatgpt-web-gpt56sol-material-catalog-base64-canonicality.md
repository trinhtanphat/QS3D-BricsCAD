# Work claim — material catalog Base64 canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-catalog-base64-canonicality-20260812-0819`
- Registered: `2026-08-12T08:19:00+07:00`
- Baseline main SHA: `b93aaf08119d53a5316b39864816871c6704b6fa`
- Priority: P2 — keep persisted Material Catalog metadata deterministic and fail-closed on alternate Base64 spellings.

## Reserved scope

`ProjectMaterialCatalog.WriteCustom(...)` always emits canonical `Convert.ToBase64String(...)` fields, but `ReadCustom(...)` currently decodes with `Convert.FromBase64String(...)`, which accepts embedded/surrounding whitespace. Directly mutated or externally corrupted catalog metadata can therefore use a non-canonical Base64 spelling and still be treated as valid catalog state. Tighten only the encoded-field representation by requiring decode/re-encode identity.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs`
- `tests/QS3D.Core.SmokeTests/MaterialCatalogBase64CanonicalitySmoke.cs` (new focused module-initializer regression)
- this claim file

## Intended fix

- After strict Base64 decode, require `Convert.ToBase64String(bytes)` to equal the stored field exactly.
- Preserve strict UTF-8 validation, decoded material trimming/length semantics, record count/empty-record rules, built-in shadowing checks, id/name uniqueness, mutation atomicity and writer output.
- Add focused smoke coverage proving canonical writer-form metadata reads while a whitespace-padded Base64 field fails closed.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25 runtime PASS claimed.

# Work claim — Material catalog deterministic ordering

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-order-determinism`
- Registered: `2026-08-12T10:15:00+07:00`
- Baseline main SHA: `ca792208d6c2a665d55f63ea98455bca5d4197e7`
- Priority: P2 — public Core catalog/reference ordering must not depend on process culture.

## Confirmed defect

`ProjectMaterialCatalog` uses `StringComparer.OrdinalIgnoreCase` for material identity/deduplication and persisted custom-material ordering, but `GetAll(...)` and `ReferencedMaterialNames(...)` currently sort names with `StringComparer.CurrentCultureIgnoreCase`. The same project can therefore return a different material/reference order when the process culture changes (for example accent/casing collation differences), making otherwise identical Core output environment-dependent.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` (ordering comparers only)
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogDeterministicOrderingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogDeterministicOrderingRegistration.cs`
- this claim file

## Intended contract

- Material identity semantics remain `OrdinalIgnoreCase`.
- `GetAll(...)` returns a stable culture-independent name order using the same ordinal-ignore-case semantics as identity.
- `ReferencedMaterialNames(...)` returns the same deterministic ordinal-ignore-case ordering.
- No parser, Base64/UTF-8, size bound, built-in shadowing, rename/delete, serialization or freshness behavior changes.

## Excluded scope

- No Material Catalog persistence format changes.
- No UI/native material rendering changes.
- No family/element material-reference mutation changes.
- No reporting/XLSX changes.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation plan

- Verify claim ancestry and re-fetch exact `ProjectMaterialCatalog.cs` blob before source write.
- Change only the two public name-ordering comparers from current-culture to ordinal-ignore-case.
- Add focused module-initializer regression that sets a non-ordinal culture, uses names whose culture sort differs from ordinal sort, and verifies both catalog output and referenced-material output equal ordinal-ignore-case ordering; restore the original culture in `finally`.
- Review exact pushed diff and re-read final source/test from current `main`.
- Close claim with exact SHAs and ancestry verification; no compile/runtime PASS unless actually executed.

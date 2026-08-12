# Work claim — Material catalog deterministic ordering

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-material-order-determinism`
- Registered: `2026-08-12T10:15:00+07:00`
- Completed: `2026-08-12T10:18:00+07:00`
- Baseline main SHA: `ca792208d6c2a665d55f63ea98455bca5d4197e7`
- Claim commit: `d49c23aa1f2b5cf8a1eaca0eb8e805ec102ea6d3`
- Source fix commit: `67a7ca73b0fff9c626bfeba7cebdc4c00a50455f`
- Regression commit: `5eb917c0b2f9b4de74fca149dcbc47cdc68112b6`
- Registration commit: `6d9a5d0cf6a7de806da3d3bb4cd876e1dabf8d1e`
- Priority: P2 — public Core catalog/reference ordering must not depend on process culture.

## Confirmed defect

`ProjectMaterialCatalog` used `StringComparer.OrdinalIgnoreCase` for material identity/deduplication and persisted custom-material ordering, but `GetAll(...)` and `ReferencedMaterialNames(...)` sorted names with `StringComparer.CurrentCultureIgnoreCase`. The same project could therefore return a different material/reference order when process culture changed, making otherwise identical Core output environment-dependent.

## Implemented surfaces

- `src/QS3D.Core/Domain/ProjectMaterialCatalog.cs` (ordering comparers only)
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogDeterministicOrderingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectMaterialCatalogDeterministicOrderingRegistration.cs`
- this claim file

## Implemented contract

- Material identity semantics remain `OrdinalIgnoreCase`.
- `GetAll(...)` now sorts names with `StringComparer.OrdinalIgnoreCase`.
- `ReferencedMaterialNames(...)` now sorts names with `StringComparer.OrdinalIgnoreCase`.
- Parser, Base64/UTF-8, size bounds, built-in shadowing, rename/delete, serialization and freshness behavior are unchanged.

## Excluded scope honored

- No Material Catalog persistence format changes.
- No UI/native material rendering changes.
- No family/element material-reference mutation changes.
- No reporting/XLSX changes.
- No GitHub Actions dispatch and no BricsCAD runtime qualification claim.

## Validation actually performed

- Material file history was inspected directly without relying on rate-limited code-search; its latest pre-claim source change remained the completed decoded-text canonicality fix `611295eee7f94ab13b1d78ef2e14bbb3d6867317`.
- Claim was published before substantive source writes, and the exact source blob was re-fetched after claim publication as `b0a99e9897a5b30a7a600bf8abdd9fd51b5e4685`.
- Source update used that exact blob SHA as a guard.
- Exact source commit diff was reviewed: only two comparer substitutions changed (`2` additions / `2` deletions), both from `CurrentCultureIgnoreCase` to `OrdinalIgnoreCase`.
- Focused smoke temporarily switches `CurrentCulture` to `en-US`, restores it in `finally`, verifies the full catalog sequence equals ordinal-ignore-case ordering, and verifies referenced names `Zebra`, `Äther` are returned in ordinal-ignore-case order independent of culture collation.
- Final smoke and module-initializer registration were read back from current `main`.
- The first registration-file write was blocked by the connector before any file was created; existence was checked (`404`) and a subsequent normal create succeeded as commit `6d9a5d0cf6a7de806da3d3bb4cd876e1dabf8d1e`.
- No local .NET compile/test execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime qualification is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Completion condition

Completed. Material catalog and referenced-material ordering are now deterministic under the same ordinal-ignore-case semantics used for identity, focused regression source is on `main`, and exact implementation/test SHAs are recorded above.

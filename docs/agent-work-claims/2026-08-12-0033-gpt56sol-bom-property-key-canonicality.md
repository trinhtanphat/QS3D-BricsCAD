# Work claim — BOM property key canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bom-property-key-20260812-0033`
- Registered: `2026-08-12T00:33:00+07:00`
- Baseline main SHA observed before registration: `f546bac4c06f263699158288878d36f7b65066c9`
- Priority: P2 source-proven release-integrity regression hardening

## Reserved scope

Fix the Core BOM release guard mismatch where QSDB persistence rejects blank or surrounding-whitespace semantic property keys, while `BomReleaseGuardService` does not currently inspect property-key canonicality. `ProjectElement.Properties` is publicly mutable, and `ProjectQuantityReportBuilder` reads canonical property names such as `MaterialName`, `Mark`, and dimensional inputs by dictionary lookup, so a malformed key such as `" MaterialName "` can be silently treated as missing/default during BQ construction instead of blocking release.

## Reserved surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file

## Intended fix

- Add an Error-level BOM release issue for blank or surrounding-whitespace semantic property keys.
- Keep quantity-key validation, finite-value checks, report grouping, provenance, generated-handle liveness, and exception redaction unchanged.
- Add focused existing-smoke coverage using direct public dictionary mutation with a padded `MaterialName` key.

## Explicit exclusions

- No report-builder behavior changes.
- No project/QSDB persistence or schema changes.
- No generic property edit-policy changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Completion condition

Complete only after claim-first ancestry is verified, source and focused regression are committed to `main`, current blobs are re-read, and this file records exact SHAs and the actual validation boundary.

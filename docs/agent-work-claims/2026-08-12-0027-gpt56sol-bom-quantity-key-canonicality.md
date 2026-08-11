# Work claim — BOM quantity key canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bom-quantity-key-20260812-0027`
- Registered: `2026-08-12T00:27:00+07:00`
- Baseline main SHA observed before registration: `d7eb1dda291328e8c24eb2ec18fba564898120a8`
- Priority: P2 source-proven release-integrity regression hardening

## Reserved scope

Fix the Core BOM release guard mismatch where `QsdbProjectStore` rejects quantity dictionary keys with leading/trailing whitespace, while `BomReleaseGuardService` currently reports only blank quantity keys. `ProjectElement.Quantities` is publicly mutable, so a key such as `" NetConcreteM3 "` can exist in memory; `ProjectQuantityReportBuilder` performs canonical quantity-name lookup and can therefore treat that malformed key as missing/zero while the release guard fails to raise the existing `BOM_QUANTITY_KEY_INVALID` blocker.

## Reserved surfaces

- `src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs`
- `tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs`
- this claim file

## Intended fix

- Reuse the existing `BOM_QUANTITY_KEY_INVALID` error code for any non-canonical quantity key, including surrounding whitespace.
- Keep finite-value, report grouping, provenance, generated-handle liveness, and exception-redaction behavior unchanged.
- Add focused smoke coverage proving a directly mutated padded quantity key blocks BOM release diagnostics.

## Explicit exclusions

- No `ProjectQuantityReportBuilder` behavior changes.
- No QSDB persistence/schema changes.
- No quantity calculation/rule semantics changes.
- No BricsCAD/native/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Completion condition

The claim is complete only after the guard fix and focused regression are committed on `main`, current source/test are re-read after writes, and this file is updated with exact commit SHAs and the actual validation boundary.

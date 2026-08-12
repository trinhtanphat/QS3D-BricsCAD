# Work claim — door XLSX cell text limit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-door-xlsx-cell-text-limit-20260812-0055`
- Registered: `2026-08-12T00:55:00+07:00`
- Baseline main SHA: `94a6e479c386cd6b67c6284ed61f22bb042efe60`
- Integrated main SHA: `9502eccb7d2d710935249ec74b69196d437896f3`
- PR: `#599`
- Priority: evidence-driven remote-safe XLSX integrity hardening during owner-requested `continue all`

## Completed scope

Door/Opening XLSX export now fails closed before filesystem mutation when an emitted inline-string cell would exceed Excel's 32,767-character content limit, including the aggregate Element IDs and Host IDs cells.

## Changes

- Added a 32,767-character preflight for `Floor`, `Category`, `FamilyName`, and `Material` cells.
- Added incremental aggregate-length validation for `ElementIds` and `HostIds`, counting `;` separators without first allocating the oversized joined string.
- Kept the change local to `DoorOpeningXlsxExporter`; shared `XlsxXmlText` and other exporters were not modified.
- Added dedicated module-initializer smoke coverage for exact-limit acceptance and direct/ElementIds/HostIds over-limit rejection before directory creation.

## Validation actually performed

- Reviewed the exact PR #599 patch: only `src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs` and `tests/QS3D.Core.SmokeTests/DoorOpeningXlsxCellTextLimitSmoke.cs` changed.
- Re-read moving `main` immediately before integration; the Door/Opening exporter still had the original pre-claim blob, so no concurrent source overlap was present.
- Confirmed no pull-request workflow runs were associated with exact head `15a5adb0251a258e0a1e964817c4e09623c49598`.
- Squash-merged PR #599 with exact head SHA `15a5adb0251a258e0a1e964817c4e09623c49598` into `main` as `9502eccb7d2d710935249ec74b69196d437896f3`.
- Re-read the merged exporter and dedicated smoke from remote `main` after integration.
- No GitHub Actions were dispatched.
- No local .NET compile/build, licensed BricsCAD V25/Windows runtime, native entity/UI/geometry execution, or `LOCAL_PASS` is claimed from this environment.

## Integration

PR #599 was squash-merged into `main` as `9502eccb7d2d710935249ec74b69196d437896f3` without force-push.

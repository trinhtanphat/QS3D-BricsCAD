# Work claim — rebar XLSX cell text limit

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-cell-text-limit-20260812-0141`
- Registered: `2026-08-12T01:41:00+07:00`
- Baseline main SHA: `66f759b595a214c8698dd61b142dbb1b468610ed`
- Integrated main SHA: `8ac14f198e74387192e902613007e29c57be248a`
- PR: `#610`
- Priority: evidence-driven remote-safe XLSX compatibility hardening during owner-requested `continue all`

## Completed scope

BBS/Rebar XLSX export now fails closed during row preflight when any emitted inline-string data cell exceeds Excel's 32,767-character cell-content limit.

## Changes

- Added a 32,767-character limit to all emitted BBS text fields: Element, Bar Mark, Shape, Notation, Fabrication Status, Standard Code, and Detailing Revision.
- Reused the existing `ValidateRows()` preflight so text-limit failures occur before path resolution, directory creation, temp-package creation, or worksheet serialization.
- Limit failures identify the worksheet row and offending field.
- Preserved existing worksheet row limits, null-row preflight, XML 1.0 sanitization, numeric serialization, and package validation.
- Added dedicated module-initializer smoke coverage for exact-limit acceptance and 32,768-character rejection before filesystem mutation.

## Validation actually performed

- Reviewed PR #610 exact patch: only `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs` and `tests/QS3D.Core.SmokeTests/XlsxRebarCellTextLimitSmoke.cs` changed.
- Re-read moving `main` immediately before publication; the exporter remained at pre-patch blob `471b6378037f0bfbca1eb5ed852a68956a1aab75`, so no concurrent source overlap was present.
- Confirmed no workflow runs were associated with exact PR head `f078339ceb5a531523da44f6c73265dda4154826`.
- Squash-merged PR #610 with exact head SHA `f078339ceb5a531523da44f6c73265dda4154826` into `main` as `8ac14f198e74387192e902613007e29c57be248a`.
- Re-read the merged exporter and dedicated smoke from remote `main` after integration.
- No GitHub Actions were dispatched.
- No local .NET compile/build, licensed BricsCAD V25/Windows runtime, native entity/UI/geometry execution, or `LOCAL_PASS` is claimed from this environment.

## Integration

PR #610 was squash-merged into `main` as `8ac14f198e74387192e902613007e29c57be248a` without force-push.

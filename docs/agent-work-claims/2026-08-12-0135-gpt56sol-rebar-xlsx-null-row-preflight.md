# Work claim — rebar XLSX null-row preflight

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-xlsx-null-row-preflight-20260812-0135`
- Registered: `2026-08-12T01:35:00+07:00`
- Baseline main SHA: `4ccc29908eeff51857ea6bf8553d9e3fbbc0e3fc`
- Integrated main SHA: `1f8b547dc05e886fc3a0e800f7df56cd08a1856b`
- PR: `#607`
- Priority: evidence-driven remote-safe export atomicity hardening during owner-requested `continue all`

## Completed scope

BBS/Rebar XLSX null rows now fail closed during preflight before path resolution, directory creation, temp-package creation, or worksheet serialization.

## Changes

- Extended the existing row-count preflight into `ValidateRows()` so each reviewed row is checked for null before filesystem work begins.
- Null-row failures identify the zero-based invalid row index and the `rows` argument.
- Preserved existing worksheet row limits, XML sanitization, numeric serialization, package validation, and ordinary non-null export behavior.
- Added dedicated module-initializer smoke coverage for existing-destination preservation, no-directory-creation preflight, row-index reporting, and a successful ordinary export.

## Validation actually performed

- Reviewed exact PR #607 patch: only `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs` and `tests/QS3D.Core.SmokeTests/XlsxRebarNullRowPreflightSmoke.cs` changed.
- Re-read moving `main` before publication and after a merge retry; the exporter remained at pre-patch blob `9a81459f6769b072140a2bff299a25632d14060a`, so concurrent base changes did not overlap this source.
- Confirmed no workflow runs were associated with exact PR head `56b74c4f9c09b2d939eb134c15100c7360245e1d`.
- First exact-head merge attempt was rejected because the base branch changed; refreshed `main`, verified no exporter overlap, then retried the same exact head successfully.
- Squash-merged PR #607 into `main` as `1f8b547dc05e886fc3a0e800f7df56cd08a1856b`.
- Re-read the merged exporter and dedicated smoke from remote `main` after integration.
- No GitHub Actions were dispatched.
- No local .NET compile/build, licensed BricsCAD V25/Windows runtime, native entity/UI/geometry execution, or `LOCAL_PASS` is claimed from this environment.

## Integration

PR #607 was squash-merged into `main` as `1f8b547dc05e886fc3a0e800f7df56cd08a1856b` without force-push.

# LOCAL-001 Windows smoke executable acceptance

Status: BLOCKED
Agent: codex-local003-smoke-exe-windows-20260815
Issue: #72
Branch: `agent/local003/local001-smoke-exe-windows-20260815`
Baseline: `adf6f6ca8bc0f91d1cb367831ecd38f18c09a342`
Handoff row: `docs/LOCAL-SHEET-ACCEPTANCE-HANDOFF-2026-08-15.md` row 1

## Reserved scope

- Build the exact committed and pushed candidate's `QS3D.Core.SmokeTests` Release executable with the repository portable .NET SDK workflow.
- Launch `QS3D.Core.SmokeTests.exe` as a normal Windows process rather than relying only on `dotnet run`.
- Capture sanitized stdout/stderr and the native process exit code while monitoring for a `QS3D.Core.SmokeTests.exe - Application Error`, CLR `0xe0434352`, Windows Error Reporting dialog/process, hang or test-owned process residue.
- Exercise an intentional managed failure only if the existing committed harness already exposes a repository-approved injection path. Do not add or weaken source/tests merely to manufacture that path.
- Publish only the exact-SHA bounded acceptance result in this claim, the canonical LOCAL-001 inbox row and the existing Sheet handoff.

Generated logs remain under ignored `artifacts/` and contain no project/drawing/private data.

## Exclusions

- No production, smoke-test, entry-point, project, runner or workflow source changes.
- No BricsCAD, AutoCAD, BLT3D, private/customer drawing, proprietary binary, screenshot, secret or signing operation.
- Any executable failure returns to a non-local source-fix lane with the smallest sanitized reproduction.
- No GitHub Actions dispatch/re-run/cancel, release operation, direct `main` write, force-push or merge.

## Validation plan

1. Publish this claim on the dedicated branch/draft PR and verify local/remote head identity before build or execution.
2. Confirm interactive Windows x64, clean worktree, no pre-existing smoke/WER process and the portable SDK path.
3. Run the focused smoke-entrypoint/failure-containment, strict manual-CI and local-handoff guards.
4. Build `tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj` in Release and verify ProductVersion binds to the exact SHA.
5. Start the built executable with redirected ignored stdout/stderr, monitor its process and relevant error-reporting windows/processes, and require exit code `0`, `ALL PASS`, empty stderr, no popup and no residue.
6. If no approved failure injection exists, record that negative-path execution was not run instead of inventing it.
7. Commit/push only sanitized documentation, rerun documentation/policy guards on that exact pushed evidence head, and stop before merge.

## Current source blocker

- Exact committed/pushed candidate `baff1291962560449f215b730428c25f5eb2ffcc` passed the two focused containment guards, manual-CI policy and local-handoff preflight on interactive Windows x64.
- The portable .NET SDK 8.0.423 Release build failed before executable launch with `CS8602` at `src/QS3D.Core/Cost/DeepCostWorkflows.cs:236`; the nullable compiler does not narrow `tradeCode` through the `string.IsNullOrWhiteSpace(...)` conditional before `tradeCode.Trim()`.
- Non-local source-fix issue #1634 owns the CAD-independent repair. The local worker did not edit Core/tests and did not run the executable, so popup/exit/stdout/stderr acceptance remains `NOT RUN`.
- No `QS3D.Core.SmokeTests`, Windows Error Reporting or BricsCAD process remained, and no GitHub Actions ran. Resume only on a new clean committed/pushed exact descendant after issue #1634 lands.

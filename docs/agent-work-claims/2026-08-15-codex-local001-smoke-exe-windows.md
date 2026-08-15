# LOCAL-001 Windows smoke executable acceptance

Status: COMPLETED
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

## Source blocker resolution and resumed candidate

- Exact committed/pushed candidate `baff1291962560449f215b730428c25f5eb2ffcc` passed the two focused containment guards, manual-CI policy and local-handoff preflight on interactive Windows x64.
- The portable .NET SDK 8.0.423 Release build failed before executable launch with `CS8602` at `src/QS3D.Core/Cost/DeepCostWorkflows.cs:236`; the nullable compiler does not narrow `tradeCode` through the `string.IsNullOrWhiteSpace(...)` conditional before `tradeCode.Trim()`.
- Non-local source-fix issue #1634 produced the reviewed repair. Release-prepared execution parent `1489ec2dcff55e4ca0e0011b9703a7bac6d46e17` contained the equivalent recovered fix through `5781ae666`; the local worker did not edit Core/tests.
- The non-force task-branch merge produced exact committed/pushed candidate `9b25be9df47459f72acecfa96443d94204410f2f`, with matching local/remote heads before build and execution.

## Exact Windows acceptance evidence

- Environment: interactive Windows x64; portable .NET SDK `8.0.423`; zero pre-existing smoke, Windows Error Reporting or BricsCAD processes.
- Focused guards: guarded smoke entry point, smoke failure containment, manual-only CI policy and local-agent handoff all passed on exact candidate `9b25be9df47459f72acecfa96443d94204410f2f`.
- Release build: `QS3D.Core.SmokeTests.csproj` succeeded with `0 warnings / 0 errors`. The apphost ProductVersion was `1.0.0+9b25be9df47459f72acecfa96443d94204410f2f`, SHA-256 `C65D39BAD9BDBB8F61671478EDA47A182C16C1061F110C8E42E0CDD3E2192606`. The loaded Core ProductVersion was `0.1.0-preview.10045+9b25be9df47459f72acecfa96443d94204410f2f`, SHA-256 `82786A401B2315AD18783B92F476D998B55B02690E3A1EE98BC34D1D58D5E377`.
- Direct apphost run: `QS3D.Core.SmokeTests.exe` completed in about `19.63 s` with numeric exit code `0`, 30 non-empty stdout lines, exactly one `ALL PASS`, zero stdout failure tokens and empty stderr. Stdout SHA-256 was `CDC600A760DEB602B346D2DE12310DDBEBC8AA0A842FC1B65EF6CB78089146E7`; empty stderr SHA-256 was `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855`.
- Windows containment: zero matching Application Error/.NET windows, zero WerFault process observations, zero matching Application log events, zero timeout, and zero smoke/WER process residue after the run. A preliminary `Start-Process` launch produced the same passing stdout/stderr hashes but was deliberately excluded because Windows PowerShell 5.1 did not expose a numeric exit-code value; the decisive `System.Diagnostics.Process` run above supplies that evidence.
- Negative-path runtime injection is `NOT_RUN`: no existing repository-approved external managed-failure trigger exists. The two focused static guards passed, but no new injection/test/source code was added merely to manufacture a failure.
- Logs remain under ignored `artifacts/`; Git reports only ignored artifact state. No private data, BricsCAD/AutoCAD/BLT3D, GitHub Actions, release or production/test source edit was involved.

This is bounded `LOCAL_PASS` only for Google Sheet row 1 / the normal Windows smoke-executable Application Error boundary. Overall `LOCAL-001` remains `IN_PROGRESS` for its larger licensed V25 matrix.

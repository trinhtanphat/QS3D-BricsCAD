# LOCAL V25 — native post-commit viewport UI qualification

Status: `LOCAL_ONLY / SOURCE_READY / NO_RESULT`

This matrix qualifies Issue #5107 on one exact BricsCAD V25 artifact. Remote source guards, Core smoke and locked-reference compilation are not `LOCAL_PASS`.

## Bind candidate

Record exact source SHA, built V25 DLL SHA-256, BricsCAD V25 version, Windows version and disposable DWG identity before testing. Restart the matrix if the candidate changes.

## Matrix

| Row | Scenario | Required result |
| --- | --- | --- |
| PC01 | NETLOAD exact candidate | Load succeeds; no startup exception escapes. |
| PC02 | Representative Structural native 3D build | Ownership/project/CAD commit succeeds and viewport refresh remains normal. |
| PC03 | Representative LINE wall native 3D build | Same durable-commit behavior. |
| PC04 | Representative Polyline wall native 3D build | Same durable-commit behavior. |
| PC05 | Representative WallPier profile native 3D build | Same durable-commit behavior. |
| PC06 | Supported safe Regen-failure injection immediately after a successful representative commit | Durable model/CAD result remains committed; command does not roll back or escape. |
| PC07 | Observe PC06 editor output | Stable warning ends with `đã commit; viewport could not refresh.` and contains no exception message/type/path/stack detail. |
| PC08 | Repeat supported Regen-failure injection on a second representative builder | Same non-escaping, non-rollback behavior; no builder-specific raw host detail. |
| PC09 | Restore normal viewport behavior and regenerate | Viewport recovers without rebuilding/duplicating already committed generated geometry. |
| PC10 | Save and cold-reopen after PC06/PC08 | Durable generated geometry and project state remain consistent. |
| PC11 | Repeat normal build after recovery | No stale failure state or duplicate warning behavior. |
| PC12 | Exit BricsCAD | Host exits cleanly with no QS3D-owned process/UI residue attributable to the probe. |

PC06–PC08 must use an existing supported local test/probe path. If no safe failure injection exists, mark those rows `NO_RESULT`; never corrupt BricsCAD installation state, patch proprietary binaries or fabricate evidence.

## Verdict

Overall `LOCAL_PASS` requires executed PASS evidence for PC01–PC12 on one exact candidate. If safe Regen-failure injection is unavailable, overall verdict remains `NO_RESULT / PARTIAL` even when all normal-path rows pass.

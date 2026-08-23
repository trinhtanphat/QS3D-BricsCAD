# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Final-document/ordinary-close source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p03-rerun`

Sanitized handoff carrier: PR #3641

Latest attempted licensed runtime baseline: `de802b6cb523831453dd5f6e2c550c9e7f856ffc`

Current exact licensed rerun target: `PENDING_NEXT_MERGED_SOURCE_SHA`

## Boundary

This local-only P01 cell covers the real V25 lifetime of 13 source-DWG-bound modeless windows plus the active-document-dynamic Domain Hub and Rebar 3D Hub over an A/B/C open, switch and close cycle. It verifies exact window/document registration, one close event, lifetime invalidation/detach, dynamic command dispatch to the active drawing, project isolation and repeat-cycle cleanup. It does not cover the representative bound-window Locate/Refresh/Export/mutation actions or the broader H.1/H.2 matrices.

The run uses only three disposable copies of the repository-public generated sample. Raw scripts, markers, drawing copies, sidecars and managed-object diagnostics remain Git-ignored. No customer/private drawing, raw Handle, ProjectId, ElementId, stack trace, proprietary DLL or machine-specific path is committed.

## 2026-08-23 licensed rerun before the ordinary-close regression

BricsCAD V25.2.10 loaded the exact SourceLink-bound Release x64 candidate at then-current baseline `9af52940bd0c9e65e4e7fba948a62391ea11bc62`, including the earlier #3621 source fix merged by PR #3625. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `A5E5C4C144D97B4DE6774F814AC1DE4370FE0CB37B6DCDE6E00EDA107CEB2F09` and `8F3F8E8FD6410EC332B3BA4B422E2536D7AE3C8B7B96E8B2F516688AE3D9F5BD`. The private probe SHA-256 was `DFEA6722BDD3AB8C03556E2654D083D3AEAFF96996735ED6B585771F2A4EBD15`.

The #3594 behavior passed its licensed boundary. The probe opened and validated all 13 A-bound windows, both dynamic hubs and B-bound Family/BBS windows launched through the real dynamic-hub buttons. All 13 A windows closed exactly once and detached. After C opened, BricsCAD returned a different managed `Document` wrapper for the same live B database; the probe verified the stable native database identity without path matching, and both B windows then closed exactly once and detached when B was destroyed. The C-bound windows and both active-document-dynamic hubs remained alive, while project isolation, repeat-cycle cleanup and a final one-document count all passed.

An intermediate run appeared to stall for 90 seconds because the private runner recognized only BricsCAD's multi-document native save dialog containing `ListBox` control 10029. An exact-PID/owner-HWND snapshot showed that V25 used the single-document `DirectUIHWND` variant with the exact `Yes`/`No`/`Cancel` button set. The ignored runner was corrected to accept either known variant only when it belongs to the exact PID, is owned by the exact disabled host HWND and appears after the bounded shutdown request. No product source changed for that runner correction.

The corrected rerun sent one WM_CLOSE to the exact COM host HWND, discarded the disposable C-copy drawing once and let BricsCAD exit with Process exit code `0` and `graceful_exit=true`. Real teardown then produced one exact-PID Windows Application Error for `bricscad.exe` in `ucrtbase.dll`, exception `0xc0000409`. The sanitized result was `FAIL / CLEANUP_FAILED` with `marker_status=PASS`, `drawing_save_dialogs_discarded=1` and `application_error_event_count=1`. This was not `LOCAL_PASS`.

## 2026-08-23 post-#3632 licensed rerun — ordinary document-close regression

PR #3632 merged the next #3621 source attempt at exact `main@74f2f26e175451d22b64831937abe4ff22c2b435`. The local qualification branch was fast-forwarded to that exact SHA before the unchanged licensed BricsCAD V25.2.10 A/B/C probe.

Source/static gates passed (13/13 focused lifetime guards, V25 Release x64 `0 warnings / 0 errors`, private helper `0 warnings / 0 errors`, Core smoke `ALL PASS`), but licensed runtime failed early at `CLOSE_A / A_BOUND_WINDOW_REMAINED_OPEN`. The probe therefore did not legitimately reach the B/C final marker or host shutdown. Its later `exit=-1` was bounded failure cleanup, not an accepted teardown verdict. Exact-PID Application Error count was zero.

Safety remained PASS: public/user drawing bytes, DemandLoad loader/bytes and `LoadCtrls=2` were preserved; zero BricsCAD processes remained; tracked worktree stayed clean. #3621 was reopened for the source correction. No product source changed in the local qualification lane.

## 2026-08-23 post-#3634 remote source handoff

PR #3634 corrected the source split that #3632 made too coarse. Ordinary multi-document teardown now keeps the proven synchronous dispatcher close path, while final/only-document teardown (or an unsafe/ambiguous document enumeration) defers WPF close until the native callback can unwind. Stable native database identity, fail-closed invalidation, project-affinity behavior and the managed-wrapper-drift fallback remain guarded.

Exact PR head `a2b42f6b9f5f78ace98857a60c83f66f27404b83` passed protected workflow run `32628921279`: `preflight` SUCCESS, all discovered feature source guards SUCCESS, Core build SUCCESS, deterministic smoke SUCCESS, trusted BricsCAD V25 compile-reference validation SUCCESS and V25 plugin build SUCCESS. PR #3634 merged as exact SHA `9a4c281b0a17b0498e6002ec352512675a694e18`; #3621 was closed on that remote evidence while licensed acceptance remained pending.

This is remote/source evidence only. It does not convert #3593 to `LOCAL_PASS` and it does not prove licensed final-host behavior.

## 2026-08-23 post-#3634 licensed current-main rerun — final teardown blocker persists

The unchanged runner was rebuilt and executed on licensed BricsCAD V25.2.10 at exact clean, pushed and current-at-start `main@de802b6cb523831453dd5f6e2c550c9e7f856ffc`. The #3634 source head is represented in that SHA. Thirteen focused lifetime guards passed, the V25 Release x64 and private-helper builds each completed with `0 warnings / 0 errors`, Core smoke reported `ALL PASS`, and no early-quit diagnostic hook was present.

The complete A/B/C functional marker passed. All 13 A-bound windows and both B-bound Family/BBS windows closed exactly once and detached; managed-wrapper drift was observed while native database identity remained stable; the C-bound windows and both dynamic hubs remained live; and project isolation, repeat-cycle cleanup and the final one-document count all passed.

Final-host acceptance still failed. The runner matched and closed the exact host, discarded one disposable single-document save dialog, and observed process exit code `0` with `graceful_exit=true`. The exact PID nevertheless produced one Windows Application Error. The BricsCAD crash report classified it as `ACCESS_VIOLATION (C0000005)` in `brx25.dll`, with `AcRxProtocolReactorManagerImp::newIterator()` as the top named native frame. Metadata therefore remained `FAIL / CLEANUP_FAILED`, with `marker_status=PASS` and `application_error_event_count=1`.

Exact candidate SHA-256 values were plugin `B06C66CB61383ADC12A9F1EBF912EE30AEDEEE784521FF28F66246CA793FA4D2`, Core `D0BB46688DDB17CD4638448BE3CE16D3388A833811E9D2B0E4AA078C9A2060A9`, and private helper `483D42A516F31B7F402C666398264A6541BF7BF6B14389A300CCC23B4A97DBCE`.

Safety passed: the public fixture and protected user drawing remained byte-identical; the fixed DemandLoad loader path/bytes and `LoadCtrls=2` were preserved; exact cleanup left zero BricsCAD processes; and the tracked worktree stayed clean. Raw runtime/crash evidence remains ignored. #3621 is reopened as the existing canonical remote/source blocker, and #3593 remains open. No production source changed in this local-only lane.

## Validation and safety

- Historical licensed V25 Release x64 builds: `0 warnings / 0 errors`.
- Historical private licensed probe builds: `0 warnings / 0 errors`.
- The post-#3634 licensed attempt passed all thirteen focused lifetime guards and the complete A/B/C functional marker before failing final-host crash acceptance.
- Historical `QS3D.Core.SmokeTests` Release build/execution: `0 warnings / 0 errors`, `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing, installed loader bytes/path and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Fail-closed cleanup ended with zero BricsCAD processes and a clean tracked worktree.
- Post-#3634 licensed evidence is `BLOCKED_SOURCE_FIX`, not `LOCAL_PASS`.

## Next exact handoff

Remote/source owner #3621 must first resolve the remaining final-host teardown crash and merge the correction. Then rerun the unchanged bounded #3593 licensed BricsCAD V25.2.10 A/B/C + final-host qualification on a clean, pushed task branch whose HEAD exactly matches the freshly fetched resulting `origin/main` SHA.

A qualifying result must first close all 13 A-bound windows exactly once, preserve the #3594 B wrapper-drift/native-identity behavior, keep C-bound windows and Domain/Rebar dynamic hubs correct, then complete the final exact-host shutdown with exit code `0`, zero exact-PID Application Error events, no retained-Document `get_Name` AccessViolation, zero BricsCAD residue and all drawing/loader/private-state safety checks passing.

Only actual licensed evidence may change this claim to `LOCAL_PASS`. Until then, broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

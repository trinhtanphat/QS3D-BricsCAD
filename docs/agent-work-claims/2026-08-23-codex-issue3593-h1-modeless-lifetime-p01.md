# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `LOCAL_PASS`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Final-document/ordinary-close source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p07-rerun`

Latest attempted licensed runtime baseline: `fec0b81cf4b6949d07652d6d7241167d2627e870`

Current exact licensed rerun target: `COMPLETED_P07_LOCAL_PASS`

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

Exact PR head `a2b42f6b9f5f78ace98857a60c83f66f27404b83` passed protected workflow run `32628921279`: `preflight` SUCCESS, all discovered feature source guards SUCCESS, Core build SUCCESS, deterministic smoke SUCCESS, trusted BricsCAD V25 compile-reference validation SUCCESS and V25 plugin build SUCCESS. PR #3634 merged as exact SHA `9a4c281b0a17b0498e6002ec352512675a694e18`; licensed acceptance remained pending.

This is remote/source evidence only. It does not convert #3593 to `LOCAL_PASS` and it does not prove licensed final-host behavior.

## 2026-08-24 post-#3645 host-owned-teardown licensed rerun

PRs #3644 and #3645 moved final modeless teardown under BricsCAD `BeginQuit` ownership and added a callback-time host-quit barrier. The unchanged runner was rebuilt at exact current-at-start `main@b3212c11ba1dfed04f4a7e1f1e0fd8670e3561a5`, which contained #3645 source head `aef9345d6e5dd38373ab662dc5a1708b733ea389`. Fourteen focused guards, the V25 Release/helper builds and Core smoke passed. One hidden-launcher startup was a safely cleaned `NO_RESULT`; one fresh unchanged retry reached the complete A/B/C functional marker.

A 13/13 and B 2/2 windows closed/detached exactly once, B wrapper drift preserved native database identity, C-bound windows and both dynamic hubs remained alive, and project isolation/repeat-cycle checks passed. Final-host acceptance still failed: process exit was `0` and `graceful_exit=true`, but one exact-PID Application Error accompanied an `ACCESS_VIOLATION (C0000005)` BricsCAD report in `brx25.dll`. Its normalized `AcRxProtocolReactorManagerImp` and concurrent `MilContent_DetachFromHwnd` signatures were identical to the pre-#3644 failure. Safety and zero-process cleanup passed.

## 2026-08-24 post-#3651 native-reactor-quiescence licensed p05 rerun

PR #3651 extended the host-quit boundary so modeless code also avoids native BricsCAD lifecycle unsubscription and document/project access during final teardown. Exact source head `92947101915141bbda2bbc4548e5fb6cae65cc76` merged as exact `main@3d5b77066c30e0f1e7d11065c3ec5feb8f1b87c5`; protected run `32675526681` was green. The fresh p05 carrier began 0 ahead / 0 behind that exact main. The current unchanged private runner and helper were not modified.

Fourteen focused lifetime/dispatch/host-quit guards passed. The V25 Release x64 and private-helper builds each completed with `0 warnings / 0 errors`; Core deterministic smoke returned `ALL PASS`; current production source and current harness contained no early-quit diagnostic hook. Exact candidate SHA-256 values were plugin `AEE662630B525B3366C28B3385C20481DE82E5E0D1ACA32C9F03607586A7F4A9`, Core `26FCD41539C53EA8162B384ED8C96C5273B56421D14DB14998BAD68B24DC8B21`, and private helper `392F7FF19E8564BFD15A464C741F712F1DBFFAC2ECE72C92FC4BB8F9B0641D58`.

The licensed BricsCAD V25.2.10 functional marker again passed completely: A 13/13 and B 2/2 windows closed/detached exactly once; managed-wrapper drift matched by stable native database identity without path identity; C Family/BBS windows and both active-document-dynamic hubs stayed alive; project isolation, repeat cycle and final one-document count passed.

Final-host acceptance failed more directly. The exact host HWND matched, one ordinary close was requested, and one disposable save dialog was discarded, but BricsCAD exited `0xC0000374` (`STATUS_HEAP_CORRUPTION`) with `graceful_exit=false`. Exact-PID Windows evidence contained one Application Error for `ntdll.dll` / `c0000374` plus one WER `APPCRASH`. The BricsCAD-generated report simultaneously remained `ACCESS_VIOLATION (C0000005)` in `brx25.dll`. After ASLR/argument normalization, the three `AcRxProtocolReactorManagerImp`/`AcRxObject` frames and three concurrent `MilContent_DetachFromHwnd` frames matched the post-#3645 report line-for-line. This is `FAIL / QUALIFICATION_FAILED` with `marker_status=PASS`, not `LOCAL_PASS`.

The public fixture and protected user drawing stayed byte-identical, the installed DemandLoad loader path/bytes and `LoadCtrls=2` were restored, exact cleanup left zero BricsCAD processes, and the tracked worktree stayed clean. Raw dump/report/runtime files remain ignored. #3593 and #3621 remain open; the local lane changed no production source.

## 2026-08-24 post-#3654 plugin-global host-quiescence licensed p06 rerun

PR #3654 replaced per-window application-quit subscriptions with one plugin-global modeless host-quiescence coordinator and added an earlier state-only `QuitWillStart` barrier plus race re-checks. Exact source head `3958572813a833d5e8dca945b1841acf955e6849` merged as exact `main@ec4384eb6a12ff6763dfdd19d4e4b84747ab60f3`; protected run `32681341209` was green. The pre-staged p06 carrier was identical to that exact main, and the private runner/helper remained byte-identical to p05.

Sixteen focused lifetime/dispatch/global-quiescence/race guards passed. The V25 Release x64 and private-helper builds each completed with `0 warnings / 0 errors`; Core deterministic smoke returned `ALL PASS`; plugin/Core PDB SourceLink named the exact candidate SHA; current production source and current harness contained no early-quit diagnostic hook. Exact candidate SHA-256 values were plugin `BAF4B6165060DAB7280BB0EEA4F8637F8546053D1BF80099CBEAE1D5367F1C01`, Core `A68A2A030DF2B0E19D9E1AC5125C3973C1392E19653E8D3EA102137343F109DC`, and unchanged private helper `176CFFE8B2435A2B7A2314B133305BAA84E886F7C24A2E9D88A69690A12ED368`.

The licensed BricsCAD V25.2.10 functional marker again passed completely: A 13/13 and B 2/2 windows closed/detached exactly once; managed-wrapper drift matched by stable native database identity without path identity; C Family/BBS windows and both active-document-dynamic hubs stayed alive; project isolation, repeat cycle and final one-document count passed.

Final-host acceptance still failed. The exact host HWND matched, one ordinary close was requested and one disposable save dialog was discarded. The process returned exit code `0` with `graceful_exit=true`, but exact-PID evidence contained one `ucrtbase.dll` / `c0000409` Application Error plus one WER `BEX64`; there were zero Application Hang and zero .NET Runtime events. The BricsCAD-generated report remained `ACCESS_VIOLATION (C0000005)` in `brx25.dll`. The normalized three `AcRxProtocolReactorManagerImp`/`AcRxObject` frames and three concurrent `MilContent_DetachFromHwnd` frames matched p05 line-for-line. This is `FAIL / CLEANUP_FAILED` with `marker_status=PASS`, not `LOCAL_PASS`.

The public fixture and protected user drawing stayed byte-identical, the installed DemandLoad loader path/bytes and `LoadCtrls=2` were restored, exact cleanup left zero BricsCAD processes, and the tracked worktree stayed clean. Raw dump/report/runtime files remain ignored. #3593 and #3621 remain open; the local lane changed no production source and used no manual Actions operation.

## 2026-08-24 post-#3658 centralized native-lifecycle licensed p07 rerun

PR #3658 centralized native document lifecycle ownership in `DocumentBoundNativeLifecycleCoordinator`. Exact source head `4fc992c585e531f6fb3f0dbb3b5a86c7222523f3` merged as exact `main@fec0b81cf4b6949d07652d6d7241167d2627e870`; protected run `32689850036` passed `preflight` and `core`. The p07 qualification carrier, its upstream and freshly fetched `origin/main` were all identical to that SHA with zero ahead/behind before launch.

Nineteen focused lifetime/dispatch/quiescence/native-reactor/result-contract guards passed. The V25 Release x64 and private-helper builds each completed with `0 warnings / 0 errors`; Core deterministic smoke returned `ALL PASS`; plugin/Core PDB SourceLink matched the exact candidate. Exact candidate SHA-256 values were plugin `EBD23D41322F51DBB75A99E829DF7F95176BEF7B0090F6076B2E8A5EDEA67F16` and Core `E677F700577D8A14FC48F5F3751959A80159C62D497A5ED2C53B5F0A75451ED2`.

The first launch produced `NO_RESULT / HARNESS_FAILURE`, not a product verdict: the ignored post-run audit treated a valid zero-row Windows Event Log query as an exception. The bounded private harness condition was corrected so zero rows remain count `0`, verified red/green, and retried once on the same source/binary identity. No production source or tracked test changed.

The licensed BricsCAD V25.2.10 retry passed the complete functional marker: A 13/13 and B 2/2 windows closed/detached exactly once; managed-wrapper drift matched by stable native database identity without path identity; C Family/BBS windows and both active-document-dynamic hubs stayed alive; project isolation, repeat cycle and final one-document count passed.

Final-host acceptance also passed. The exact host matched, one ordinary close was requested, one disposable save dialog was discarded, and BricsCAD exited `0` with `graceful_exit=true`. The exact run and delayed post-run audit found zero BricsCAD Application Error, WER, Application Hang, `.NET Runtime` and AccessViolation evidence. Cleanup left zero BricsCAD/helper processes. The public fixture and protected user drawing stayed byte-identical; the installed DemandLoad loader path/bytes and `LoadCtrls=2` were preserved; private state was restored; the tracked tree stayed clean; raw evidence remained ignored.

The allowlisted p07 manifest passed `scripts/validate-local002-h1-result.py` against the exact SHA and routed `PASS` to `LOCAL_PASS_ELIGIBLE`. This is licensed bounded `LOCAL_PASS` for the #3593 H.1 A/B/C lifetime plus final-host cell. It does not qualify representative bound-window actions, the broader H.1/H.2 matrices or overall LOCAL-002 parity.

## Validation and safety

- Current licensed V25 Release x64 build: `0 warnings / 0 errors`.
- Current private licensed probe build: `0 warnings / 0 errors`.
- The post-#3658 licensed attempt passed all nineteen focused guards, exact SourceLink identity, the complete A/B/C marker and final-host acceptance.
- Current `QS3D.Core.SmokeTests` Release build/execution: `0 warnings / 0 errors`, `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing, installed loader bytes/path and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Fail-closed cleanup ended with zero BricsCAD/helper processes and a clean tracked worktree; raw evidence remained ignored.
- The fail-closed result-contract preflight passed, and the exact-SHA sanitized p07 manifest routed to `LOCAL_PASS_ELIGIBLE`.
- Post-#3658 licensed evidence is bounded `LOCAL_PASS` for #3593.

## Closeout

#3593 may close as completed after publishing the allowlisted `LOCAL_PASS` summary. The H.3 source acceptance in #3621 is satisfied by the same exact-SHA licensed result and may close if no independent residual defect remains. No production source was edited locally, no manual Actions operation was used, and this normal local session does not merge its documentation branch to `main`.

Historical p01-p06 failures remain diagnostic provenance and must not be rerun or reinterpreted. Representative bound-window action coverage, broader H.1/H.2 matrices and overall LOCAL-002 remain `PENDING_LOCAL`.

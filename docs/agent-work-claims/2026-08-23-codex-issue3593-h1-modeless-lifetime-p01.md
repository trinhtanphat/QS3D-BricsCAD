# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Final-document/ordinary-close source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p05-rerun`

Latest attempted licensed runtime baseline: `3d5b77066c30e0f1e7d11065c3ec5feb8f1b87c5`

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

## Validation and safety

- Historical licensed V25 Release x64 builds: `0 warnings / 0 errors`.
- Historical private licensed probe builds: `0 warnings / 0 errors`.
- The post-#3651 licensed attempt passed all fourteen focused lifetime/dispatch/host-quit guards and the complete A/B/C marker before failing final-host crash acceptance.
- Historical `QS3D.Core.SmokeTests` Release build/execution: `0 warnings / 0 errors`, `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing, installed loader bytes/path and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Fail-closed cleanup ended with zero BricsCAD processes and a clean tracked worktree.
- Post-#3651 licensed evidence is `BLOCKED_SOURCE_FIX`, not `LOCAL_PASS`.

## Next exact handoff

Remote/source owner #3621 must reassess the final host native/WPF teardown architecture using the p05 heap-corruption outcome and the unchanged normalized reactor/WPF frames. After a new correction merges, rerun the unchanged bounded #3593 licensed BricsCAD V25.2.10 A/B/C + final-host qualification on a fresh canonical branch whose HEAD exactly matches freshly fetched `origin/main`.

A qualifying result must first close all 13 A-bound windows exactly once, preserve the #3594 B wrapper-drift/native-identity behavior, keep C-bound windows and Domain/Rebar dynamic hubs correct, then complete the final exact-host shutdown with exit code `0`, zero exact-PID Application Error events, no retained-Document `get_Name` AccessViolation, zero BricsCAD residue and all drawing/loader/private-state safety checks passing.

Only actual licensed evidence may change this claim to `LOCAL_PASS`. Until then, broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

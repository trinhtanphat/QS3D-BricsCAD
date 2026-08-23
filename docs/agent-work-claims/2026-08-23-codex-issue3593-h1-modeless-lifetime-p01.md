# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Current shutdown source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p02-rerun`

Exact runtime baseline: `9af52940bd0c9e65e4e7fba948a62391ea11bc62`

## Boundary

This local-only P01 cell covers the real V25 lifetime of 13 source-DWG-bound modeless windows plus the active-document-dynamic Domain Hub and Rebar 3D Hub over an A/B/C open, switch and close cycle. It verifies exact window/document registration, one close event, lifetime invalidation/detach, dynamic command dispatch to the active drawing, project isolation and repeat-cycle cleanup. It does not cover the representative bound-window Locate/Refresh/Export/mutation actions or the broader H.1/H.2 matrices.

The run uses only three disposable copies of the repository-public generated sample. Raw scripts, markers, drawing copies, sidecars and managed-object diagnostics remain Git-ignored. No customer/private drawing, raw Handle, ProjectId, ElementId, stack trace, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed rerun

BricsCAD V25.2.10 loaded the exact SourceLink-bound Release x64 candidate at the clean, pushed and current-at-start baseline above, including the #3621 source fix merged by PR #3625. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `A5E5C4C144D97B4DE6774F814AC1DE4370FE0CB37B6DCDE6E00EDA107CEB2F09` and `8F3F8E8FD6410EC332B3BA4B422E2536D7AE3C8B7B96E8B2F516688AE3D9F5BD`. The private probe SHA-256 was `DFEA6722BDD3AB8C03556E2654D083D3AEAFF96996735ED6B585771F2A4EBD15`.

The #3594 behavior passed its licensed boundary. The probe opened and validated all 13 A-bound windows, both dynamic hubs and B-bound Family/BBS windows launched through the real dynamic-hub buttons. All 13 A windows closed exactly once and detached. After C opened, BricsCAD returned a different managed `Document` wrapper for the same live B database; the probe verified the stable native database identity without path matching, and both B windows then closed exactly once and detached when B was destroyed. The C-bound windows and both active-document-dynamic hubs remained alive, while project isolation, repeat-cycle cleanup and a final one-document count all passed.

An intermediate run appeared to stall for 90 seconds because the private runner recognized only BricsCAD's multi-document native save dialog containing `ListBox` control 10029. An exact-PID/owner-HWND snapshot showed that V25 used the single-document `DirectUIHWND` variant with the exact `Yes`/`No`/`Cancel` button set. The ignored runner was corrected to accept either known variant only when it belongs to the exact PID, is owned by the exact disabled host HWND and appears after the bounded shutdown request. No product source changed for that runner correction.

The corrected rerun sent one WM_CLOSE to the exact COM host HWND, discarded the disposable C-copy drawing once and let BricsCAD exit with Process exit code `0` and `graceful_exit=true`. Real teardown then produced one exact-PID Windows Application Error for `bricscad.exe` in `ucrtbase.dll`, exception `0xc0000409`. The sanitized result is therefore still `FAIL / CLEANUP_FAILED` with `marker_status=PASS`, `drawing_save_dialogs_discarded=1` and `application_error_event_count=1`. Issue #3621 remains open for the production final-host teardown correction; production source was not edited in this local lane.

## Validation and safety

- `QS3D.BricsCAD.V25` Release x64 build: `0 warnings / 0 errors`.
- Private licensed probe build: `0 warnings / 0 errors`.
- Twelve focused document-bound/dynamic/modeless lifetime guards: PASS.
- `QS3D.Core.SmokeTests` Release build: `0 warnings / 0 errors`; execution: `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing remained byte-identical, the installed loader bytes/path and `LoadCtrls=2` were preserved, and no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- The exact-host HWND, exact dialog-owner and exact-PID Event Log guards distinguish the corrected private save-dialog handling from the remaining production teardown crash.
- Fail-closed cleanup still ended with zero BricsCAD processes and a clean tracked worktree.

## Next exact handoff

After the reopened #3621 follow-up lands on an exact current `main` SHA, rerun #3593 unchanged in licensed V25. The #3594 functional boundary is proven, but a qualifying result must exit code `0` after one normal exact-host WM_CLOSE, produce zero exact-PID Application Error events, close all remaining modeless UI safely and pass all drawing/loader/process/private-state checks. Broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

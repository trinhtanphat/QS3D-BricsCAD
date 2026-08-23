# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Current shutdown source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p02-rerun`

Exact runtime baseline: `02a1461fde844e6d17daf1161f9c837670ec3b77`

## Boundary

This local-only P01 cell covers the real V25 lifetime of 13 source-DWG-bound modeless windows plus the active-document-dynamic Domain Hub and Rebar 3D Hub over an A/B/C open, switch and close cycle. It verifies exact window/document registration, one close event, lifetime invalidation/detach, dynamic command dispatch to the active drawing, project isolation and repeat-cycle cleanup. It does not cover the representative bound-window Locate/Refresh/Export/mutation actions or the broader H.1/H.2 matrices.

The run uses only three disposable copies of the repository-public generated sample. Raw scripts, markers, drawing copies, sidecars and managed-object diagnostics remain Git-ignored. No customer/private drawing, raw Handle, ProjectId, ElementId, stack trace, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed rerun

BricsCAD V25.2.10 loaded the exact SourceLink-bound Release x64 candidate at the clean, pushed and current-at-start baseline above, including the #3621 source fix merged by PR #3625. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `DBE0B7780B405B5FE90F637162D7E19EB5CD092A9640179B9B22572C0CCB0E03` and `155D61C5E9AE1725FC4E6A711E68814C1DF3036F178F626719BFE2712A3CD3A9`.

The #3594 behavior passed its licensed boundary. The probe opened and validated all 13 A-bound windows, both dynamic hubs and B-bound Family/BBS windows launched through the real dynamic-hub buttons. All 13 A windows closed exactly once and detached. After C opened, BricsCAD returned a different managed `Document` wrapper for the same live B database; the probe verified the stable native database identity without path matching, and both B windows then closed exactly once and detached when B was destroyed. The C-bound windows and both active-document-dynamic hubs remained alive, while project isolation, repeat-cycle cleanup and a final one-document count all passed.

The #3621 fix eliminated the prior `ucrtbase.dll / 0xc0000409` Application Error, but the run still cannot claim `LOCAL_PASS`. The private runner captured the exact COM `Application.HWND`, verified that it matched `Process.MainWindowHandle` and belonged to the exact BricsCAD PID, then sent WM_CLOSE directly to that host HWND with the C-bound windows and both dynamic hubs still live. BricsCAD did not exit within 90 seconds. The runner-owned cleanup close subsequently ended the process with exit code `-1`; no matching exact-PID Application Error, Application Hang, .NET Runtime or Windows Error Reporting event appeared. The sanitized result is therefore `FAIL / QUALIFICATION_FAILED` with `marker_status=PASS` and `graceful_exit=false`. Issue #3621 is reopened for the remaining source shutdown correction; production source was not edited in this local lane.

## Validation and safety

- `QS3D.BricsCAD.V25` Release x64 build: `0 warnings / 0 errors`.
- Private licensed probe build: `0 warnings / 0 errors`.
- Twelve focused document-bound/dynamic/modeless lifetime guards: PASS.
- `QS3D.Core.SmokeTests` Release build: `0 warnings / 0 errors`; execution: `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing remained byte-identical, the installed loader bytes/path and `LoadCtrls=2` were preserved, and no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- The exact-host HWND and exact-PID guards distinguish the remaining 90-second shutdown stall/exit `-1` from the eliminated native Application Error.
- Fail-closed cleanup still ended with zero BricsCAD processes and a clean tracked worktree.

## Next exact handoff

After the reopened #3621 follow-up lands on an exact current `main` SHA, rerun #3593 unchanged in licensed V25. The #3594 functional boundary and elimination of the original activation crash are proven, but a qualifying result must additionally exit code `0` after one normal exact-host WM_CLOSE, close all remaining modeless UI safely and pass all drawing/loader/process/private-state checks. Broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

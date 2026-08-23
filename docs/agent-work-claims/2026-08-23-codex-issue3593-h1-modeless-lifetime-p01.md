# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Original source-fix issue: #3594

Current shutdown source-fix issue: #3621

Lane-Key: `issue-3593`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p01-rerun`

Exact runtime baseline: `12b5f0d7d8549d8b107a1b921d2bb431f809bf69`

## Boundary

This local-only P01 cell covers the real V25 lifetime of 13 source-DWG-bound modeless windows plus the active-document-dynamic Domain Hub and Rebar 3D Hub over an A/B/C open, switch and close cycle. It verifies exact window/document registration, one close event, lifetime invalidation/detach, dynamic command dispatch to the active drawing, project isolation and repeat-cycle cleanup. It does not cover the representative bound-window Locate/Refresh/Export/mutation actions or the broader H.1/H.2 matrices.

The run uses only three disposable copies of the repository-public generated sample. Raw scripts, markers, drawing copies, sidecars and managed-object diagnostics remain Git-ignored. No customer/private drawing, raw Handle, ProjectId, ElementId, stack trace, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed rerun

BricsCAD V25.2.10 loaded the exact SourceLink-bound Release x64 candidate at the clean, pushed and current baseline above. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `8C4D5E9B3D8B8D1FDB2223708CC48996355130792EF0987AD75A2200836C0F2E` and `9D26044B5DB00A2B6751F601A772BEEC3DD4CAC3FC01973840236E24800FE0B7`.

The #3594 behavior passed its licensed boundary. The probe opened and validated all 13 A-bound windows, both dynamic hubs and B-bound Family/BBS windows launched through the real dynamic-hub buttons. All 13 A windows closed exactly once and detached. After C opened, BricsCAD returned a different managed `Document` wrapper for the same live B database; the probe verified the stable native database identity without path matching, and both B windows then closed exactly once and detached when B was destroyed. The C-bound windows and both active-document-dynamic hubs remained alive, while project isolation, repeat-cycle cleanup and a final one-document count all passed.

The run still cannot claim `LOCAL_PASS`. Normal final BricsCAD main-window shutdown after the complete functional marker produced one exact-PID Windows `Application Error` for `bricscad.exe`, fault module `ucrtbase.dll`, exception `0xc0000409`. The process wrapper reported exit code `0`, so the private runner now also audits the exact PID in Windows Event Log and correctly classified the result as `FAIL / CLEANUP_FAILED`. A separate diagnostic final-document close also exposed an access violation after native document disposal. Issue #3621 owns the remote-safe teardown/activation correction; production source was not edited in this local lane.

## Validation and safety

- `QS3D.BricsCAD.V25` Release x64 build: `0 warnings / 0 errors`.
- Private licensed probe build: `0 warnings / 0 errors`.
- Ten focused document-bound/dynamic/modeless lifetime guards: PASS.
- `QS3D.Core.SmokeTests` Release build: `0 warnings / 0 errors`; execution: `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing remained byte-identical, the installed loader bytes/path and `LoadCtrls=2` were preserved, and no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- The exact-PID Application Error guard recorded the single shutdown fault instead of accepting the misleading process exit code.
- Fail-closed cleanup still ended with zero BricsCAD processes and a clean tracked worktree.

## Next exact handoff

After #3621 lands on an exact current `main` SHA, rerun #3593 unchanged in licensed V25. The functional #3594 boundary is now proven, but a qualifying result must additionally complete normal host shutdown with no exact-PID Application Error, close all remaining modeless UI safely, exit cleanly and pass all drawing/loader/process/private-state checks. Broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

# LOCAL-002 H.1 P01 — bound/dynamic modeless lifetime qualification

Status: `BLOCKED_SOURCE_FIX`

Parent: #72 / LOCAL-002

Licensed qualification issue: #3593

Remote source-fix issue: #3594

Lane-Key: `issue-local002-h1-p01`

Qualification branch: `agent/codex/issue3593-v25-modeless-h1-p01`

Exact runtime baseline: `83714ad2b32ffa5731e92f6928cbec3d892e9c8e`

## Boundary

This local-only P01 cell covers the real V25 lifetime of 13 source-DWG-bound modeless windows plus the active-document-dynamic Domain Hub and Rebar 3D Hub over an A/B/C open, switch and close cycle. It verifies exact window/document registration, one close event, lifetime invalidation/detach, dynamic command dispatch to the active drawing, project isolation and repeat-cycle cleanup. It does not cover the representative bound-window Locate/Refresh/Export/mutation actions or the broader H.1/H.2 matrices.

The run uses only three disposable copies of the repository-public generated sample. Raw scripts, markers, drawing copies, sidecars and managed-object diagnostics remain Git-ignored. No customer/private drawing, raw Handle, ProjectId, ElementId, stack trace, proprietary DLL or machine-specific path is committed.

## 2026-08-23 licensed diagnostic result

BricsCAD V25.2.10 loaded the exact SourceLink-bound Release x64 candidate at the clean, pushed and current baseline above. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `64FDCB04867E00C18F646E35F265C332AC295CB0FF779E425C7691E7B69580F2` and `033A5289F2D7D20FD215B2F6C86DB1B0BDE88AE1273BEF6826830B8C83112615`.

The probe opened and validated all 13 A-bound windows, both dynamic hubs, and B-bound Family/BBS windows launched through the real dynamic-hub buttons. After C opened, BricsCAD returned a different managed `Document` wrapper for the still-open B DWG. The retained B windows and their valid lifetime registrations still referenced the original wrapper. Closing the real B drawing then left the B Family Manager loaded, visible and hosted because `DocumentBoundWindowLifetime.OnDocumentToBeDestroyed` rejected the replacement wrapper through `ReferenceEquals`.

The final sanitized verdict was `CLOSE_B / B_FAMILY_REMAINED_OPEN`. Three diagnostic attempts progressively distinguished ordinary availability, same-DWG wrapper mismatch, and actual stale-window retention. This is a product source defect, not a `LOCAL_PASS`, and production source was not edited in this local lane. Issue #3594 owns the remote-safe correction and deterministic source guards.

## Validation and safety

- `QS3D.BricsCAD.V25` Release x64 build: `0 warnings / 0 errors`.
- Private licensed probe build: `0 warnings / 0 errors`.
- Ten focused document-bound/dynamic/modeless lifetime guards: PASS.
- `QS3D.Core.SmokeTests` Release build: `0 warnings / 0 errors`; execution: `ALL PASS`.
- The public fixture remained byte-identical at SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- The user's drawing remained byte-identical, the installed loader bytes/path and `LoadCtrls=2` were preserved, and no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Every diagnostic cleanup ended with zero BricsCAD processes and a clean tracked worktree.

## Next exact handoff

After #3594 lands on an exact current `main` SHA, rerun #3593 unchanged in licensed V25. A qualifying result must close all 13 A-bound windows and both B-bound windows exactly once through document destruction, prove invalidation/detach, retain both dynamic hubs across A/B closes, keep C-bound windows alive until final shutdown, exit gracefully, and pass all drawing/loader/process/private-state cleanup checks. Broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.

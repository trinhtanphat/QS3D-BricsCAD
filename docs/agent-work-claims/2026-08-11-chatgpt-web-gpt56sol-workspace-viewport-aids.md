# Work claim — Workspace viewport aid toggles

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-viewport-aids`
- Registered: `2026-08-11T21:57:00+07:00`
- Completed: `2026-08-11T22:03:00+07:00`
- Baseline main SHA: `3a507903e14a4f1db2ad4f85ced57f4ce8a47858`
- Priority: P1 screenshot/reference workflow parity

## Implemented

- `bd006d6d67b635f08e40b076ce93d46420b004a6` — added isolated `WorkspacePanel.ViewAids.cs`. It registers a class-level Loaded hook without modifying `WorkspacePanel.xaml`, `WorkspacePanel.xaml.cs` or the completed compact/header surface, locates the existing right-docked footer status row, and idempotently appends native `Vuông góc` / `Bắt điểm` checkboxes.
- `35594b4893b82bd5445490f7b3490444ad32d816` — anchored the static registration field as an intentionally consumed class-registration contract so the view-aid hook remains explicit and warning-safe.
- `Vuông góc` reads/writes native `ORTHOMODE` through `Bricscad.ApplicationServices.Application.GetSystemVariable/SetSystemVariable` with 0/1 state.
- `Bắt điểm` reads/writes native `OSMODE`: off adds bit `16384`, on removes that suppression bit while preserving the user's configured lower snap-mode bits. If the lower bits are zero, QS3D refuses to invent a preset and tells the user to configure native Entity Snap first.
- Loaded and pointer-enter refresh state from BricsCAD so native F8/F3/status-bar changes are reflected when the user returns to the footer.
- No project lookup/create/mutation, QSDB persistence or command-string dispatch was introduced in this lane.
- `2358591c72338971a8630b4a49f20ed22d4de3e8` — added `scripts/preflight-workspace-viewport-aids.py`, guarding system-variable names, idempotent footer injection, OSMODE bit preservation/zero-mode fail-closed behavior, load/pointer refresh, absence of semantic side effects and preservation of the existing Workspace footer plus compact/header contracts.
- `2677804ab415f9fc3e6af4768643daeb240d368a` — narrowed the reservation before implementation so current XAML/compact-shell winners remained audit-only and untouched.

## Source validation

- Re-fetched `WorkspacePanel.ViewAids.cs` from current `main`; the exact native ORTHOMODE/OSMODE implementation, 16384 suppression-bit policy, no-preset fallback and idempotent runtime injection are intact.
- Re-fetched `scripts/preflight-workspace-viewport-aids.py`; the focused gate still requires all of the above plus the existing `Mô hình`, `BQ`, `Kiểm tra` footer handlers and completed compact/header functions.
- Current-main ancestry comparison from `bd006d6d67b635f08e40b076ce93d46420b004a6` reports `behind_by: 0` with that implementation as merge base while preserving ten concurrent commits; no force push/reset was used.
- GitHub exposes no combined status checks for `2358591c72338971a8630b4a49f20ed22d4de3e8`; no GitHub Actions were dispatched.

## API/source basis

- BricsCAD exposes static `Application.GetSystemVariable` / `SetSystemVariable` in BrxMgd.
- BricsCAD documents `ORTHOMODE` as native orthogonal cursor mode and `OSMODE` bit `16384` as the official `Turn off all snaps` flag. This source lane uses those native contracts rather than maintaining duplicate QS3D state.

## LOCAL_ONLY disposition

- Actual checkbox render, F8/F3 interop, pointer-refresh behavior and native cursor/snap behavior in licensed BricsCAD V25 remain part of the existing local Workspace/palette qualification boundary. No duplicate LOCAL inbox item was created.
- No remote native runtime PASS is claimed.

## Completion evidence

The Workspace footer now provides the screenshot-style `Vuông góc` and `Bắt điểm` controls as real BricsCAD drafting-state toggles while preserving the user's Entity Snap configuration and all existing QS3D Workspace behavior.

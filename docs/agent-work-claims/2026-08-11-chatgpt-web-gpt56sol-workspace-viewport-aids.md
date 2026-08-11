# Work claim — Workspace viewport aid toggles

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-viewport-aids`
- Registered: `2026-08-11T21:57:00+07:00`
- Baseline main SHA: `3a507903e14a4f1db2ad4f85ced57f4ce8a47858`
- Priority: P1 screenshot/reference workflow parity

## Goal

Add the two unambiguous screenshot-style drafting aids to the QS3D Workspace footer as real BricsCAD state controls: `Vuông góc` (ORTHOMODE) and `Bắt điểm` (2D entity snaps / OSMODE). Keep the controls bound to native BricsCAD system variables instead of inventing QS3D-only state.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ViewAids.cs` (new isolated partial)
- `scripts/preflight-workspace-viewport-aids.py`
- this claim file
- `WorkspacePanel.xaml`, `WorkspacePanel.xaml.cs` and `WorkspacePanel.CompactShell.cs` are audit-only/no-edit surfaces. The view-aid partial registers its own class-level Loaded hook through a static field initializer and appends controls to the existing footer at runtime, so recent XAML/compact/header winners remain untouched.

## Functional contract

- Footer exposes visible checkboxes `Vuông góc` and `Bắt điểm` next to the existing live viewport status.
- `Vuông góc` reads/writes BricsCAD `ORTHOMODE` through `Bricscad.ApplicationServices.Application.GetSystemVariable/SetSystemVariable`; checked = native value 1, unchecked = 0.
- `Bắt điểm` reads/writes native `OSMODE` without destroying the user's configured snap-mode bits: turning snaps off sets the official 16384 suppression bit; turning snaps on removes 16384 while preserving all configured lower bits.
- If no snap modes are configured (`OSMODE` lower bits = 0), the QS3D checkbox must not silently invent a snap preset; it remains off and reports that the user should configure native entity snaps first.
- Load and pointer-enter refresh the indicators from current BricsCAD state so native F8/F3/status-bar changes are reflected when the user returns to the QS3D footer.
- UI refresh/toggle is CAD-state-only: no QS3D project creation/mutation, no QSDB write, no command-string dispatch, no private drawing assumptions.
- Runtime UI injection must be idempotent: repeated Loaded events do not duplicate controls or handlers.
- Existing Workspace footer buttons (`Mô hình`, `BQ`, `Kiểm tra`) and compact-shell/header behavior remain intact.

## Validation plan

- Re-fetch current `main` and audit the current `WorkspacePanel.xaml` footer immediately before source writes; preserve concurrent winners.
- Add an auto-discovered static preflight requiring exact system-variable names, suppression-bit preserving logic, zero-base-mode fail-closed behavior, class-load/footer injection, idempotence, pointer-enter refresh and continued existing footer actions in XAML.
- Re-fetch final source/ancestry and status. Do not dispatch GitHub Actions.

## Completion condition

The Workspace footer provides real native ORTHO/entity-snap toggles matching the supplied reference's drafting controls without overwriting user snap configuration, with source regression coverage and exact SHAs recorded here.

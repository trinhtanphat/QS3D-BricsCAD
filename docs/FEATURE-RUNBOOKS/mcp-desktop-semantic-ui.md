# MCP semantic desktop UI and bounded window layout

Issue: #4799

## Purpose

This lane adds three explicit desktop tools so ChatGPT can keep BricsCAD visible and inspect action-oriented Windows UI metadata without relying on blind coordinate guesses:

- `desktop_window_set_state` maximizes or restores one exact visible current-session top-level window.
- `desktop_window_move_resize` moves/resizes that exact window while keeping its full rectangle inside the Windows virtual desktop.
- `desktop_ui_tree` reads a bounded Windows UI Automation ControlView tree for that exact window.

The feature does not launch processes, shells, scripts, or arbitrary executables and does not add semantic invoke/click. Existing exact-target mouse fallback remains the mutation surface for clicking.

## Consent and mutation boundary

`desktop_window_set_state` and `desktop_window_move_resize` require both local QS3D desktop consent and `confirmMutation=true`. They run under the existing `McpCadAgentRuntime.Mutation` epoch and re-check the mutation callback before and after the Win32 mutation, then verify the resulting window state/bounds.

`desktop_ui_tree` is read-only but privacy-sensitive. It requires local QS3D desktop consent and `confirmSensitiveRead=true`. The exact target must remain an exact visible current-session top-level window.

Emergency stop, guarded-action accounting, audit callbacks, and the existing desktop-control consent session remain authoritative. The semantic runtime does not implement a parallel stop flag.

## Privacy contract

The UI tree is bounded to `maxDepth <= 8` and `maxNodes <= 200`. Each emitted node contains only depth, control type, a privacy-safe action name when allowlisted, bounds, enabled/offscreen state, and a redaction flag.

Names are allowlisted only for action/navigation-oriented controls such as Button, TabItem, MenuItem, CheckBox, RadioButton, ComboBox, ListItem, TreeItem, Hyperlink, Window, and ToolBar. Edit/Document/password and generic Text names are redacted. The runtime performs no ValuePattern or TextPattern reads and returns no unrestricted text dump.

Window handles use the same non-zero hexadecimal contract as the existing desktop tools. The target is revalidated with Win32 visibility/top-level checks and must belong to the current Windows session.

## Window layout bounds

Move/resize accepts explicit `x`, `y`, `width`, and `height`, with minimum dimensions 160x120. The requested rectangle must fit wholly inside the current virtual desktop. The runtime uses `SetWindowPos` without z-order activation and verifies the resulting rectangle within a small Windows framing tolerance.

Maximize/restore uses the explicit Windows state APIs and verifies the requested maximized state before reporting success.

## Qualification

`python scripts/preflight-mcp-desktop-semantic-ui.py` is the deterministic source guard. It verifies dispatcher registration, mutation/sensitive classification, consent/epoch wiring, UI Automation bounds/privacy rules, Win32 target constraints, project references, and this runbook boundary.

Protected CI must additionally compile the locked BricsCAD V25 plugin references on the exact PR head before merge. This is static/hosted qualification only; it does not claim licensed interactive BricsCAD runtime PASS. A future licensed runtime qualification should exercise maximize, restore, deliberate split-screen move/resize, UIA discovery on native/WPF surfaces, emergency-stop interruption, and privacy redaction on editable/password/document controls.

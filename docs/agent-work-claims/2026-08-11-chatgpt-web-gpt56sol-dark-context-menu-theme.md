# Work claim — Host-independent dark Workspace context-menu chrome

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dark-context-menu-20260811`
- Registered: `2026-08-11T22:00:00+07:00`
- Scope refined: `2026-08-11T22:12:00+07:00`
- Baseline main SHA: `24f623ab2e01a78dc2c9ae7e948a83f42eca468b`
- Refined baseline main SHA: `2617eb4d66bc4db73be605dbcc35879ac341b8c8`
- Priority: continue the owner-requested premium/dark UI/UX hardening after the Workspace overlap fixes. Source review confirms Workspace creates real `ContextMenu` / `MenuItem` controls for Family and selected-object actions, while those popup items still rely on host/system menu templates for highlight chrome.

## Reserved scope

Add a presentation-only dark popup-menu layer for the **existing Workspace Family and selected-object context menus** so their popup, hover/highlight, disabled and separator states cannot fall back to bright BricsCAD/Windows rendering. Preserve every existing menu item, click handler, tag, shortcut and command path.

The implementation is intentionally isolated in a new `WorkspacePanel` presentation partial rather than editing the shared `Theme.xaml` during heavy concurrent theme/window work. It may assign explicit styles/templates only to the already-created Workspace context menus and their leaf items; it must not create commands, modify menu behavior, or introduce a second interaction path.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkContextMenu.cs` — new presentation-only partial
- `scripts/preflight-workspace-dark-context-menu.py` — focused auto-discovered source guard
- this claim file for close-out

## Excluded scope

- No edits to `Theme.xaml` or `scripts/preflight-wpf-theme.py`; premium theme v2 remains authoritative.
- No edits to `WorkspacePanel.xaml`, `WorkspacePanel.xaml.cs`, `WorkspacePanel.QuickDraw.cs`, existing handlers, semantic/project mutation, CAD commands or viewport behavior.
- No edits to `RightPanel*`; the current Xref-scale lane owns its neighboring Right Panel work.
- No Project Browser XML-state, Quantity Settings, export/save-dialog, reporting, updater, rebar, Direct Draw, Ribbon, Start Center or release work.
- No GitHub Actions dispatch.

## Validation plan

- Re-fetch latest `main`, existing Workspace menu construction, current premium theme and active neighboring claims immediately before implementation/integration.
- Keep the new partial idempotent and presentation-only; attach through a class-level loaded hook without adding another command/menu-construction path.
- Style the existing Family/Inspection `ContextMenu` instances with explicit dark background/foreground/border templates; style leaf `MenuItem` highlight/disabled states and menu `Separator` lines without changing headers/click delegates/tags.
- Reapply presentation on menu open so later-added existing workflow items (for example the already-supported `Vẽ Nhanh`) receive the same chrome without duplicate handlers.
- Add a focused static preflight requiring the idempotent hook, two canonical existing menu references, dark templates/highlight/disabled/separator states and forbidding CAD/project/command dispatch or context-menu reconstruction.
- Source-review for no blur/shadow/animation and no business mutation.
- Real BricsCAD V25 visual/HiDPI qualification remains existing `LOCAL-012`; no remote runtime PASS claim.

## Coordination

The prior premium-theme-v2 and Workspace overlap claims are `COMPLETED`. The active Project Browser XML namespace guard is Core-only and non-overlapping. Current Xref scale, export preflight, reporting, quantity, updater/rebar/reference-search lanes are explicitly excluded.

## Implementation readiness

- Source commit: `3a555f2fe58c508d15aa68fbb91f33cf782ba31a`.
- Latest current-main reconciliation commit prepared on the feature branch: `cc93448b628b9eab070949083b6c51aa226ce59b`.
- PR: `#499`.
- The focused preflight is auto-discovered by `scripts/preflight-all.py`; no GitHub Actions were dispatched.

## Completion condition

Workspace dark context-menu chrome and its focused static regression guard are integrated into current `main`, existing menu actions remain untouched, the claim is marked `COMPLETED` with exact implementation/integration SHA(s), and native visual proof remains correctly parked under the existing local UI qualification boundary.

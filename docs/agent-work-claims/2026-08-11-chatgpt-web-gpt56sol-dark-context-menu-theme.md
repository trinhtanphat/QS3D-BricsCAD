# Work claim — Host-independent dark context-menu chrome

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-dark-context-menu-20260811`
- Registered: `2026-08-11T22:00:00+07:00`
- Baseline main SHA: `24f623ab2e01a78dc2c9ae7e948a83f42eca468b`
- Priority: continue the owner-requested premium/dark UI/UX hardening after the Workspace overlap fixes. Source review confirms Workspace creates real `ContextMenu` / `MenuItem` controls for Family and selected-object actions, while the shared premium theme currently provides host-independent chrome for core inputs/tooltips but no shared `ContextMenu` / `MenuItem` template. That leaves popup/highlight visuals exposed to BricsCAD/Windows system-theme rendering.

## Reserved scope

Add a shared presentation-only dark popup-menu contract so plugin context menus cannot fall back to bright host/system menu surfaces or highlight states. Preserve every existing menu item, click handler, tag, shortcut and command path. This is a theme/presentation lane only; it must not change Workspace business behavior or command routing.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Theme.xaml`
- `scripts/preflight-wpf-theme.py`
- `docs/UI-UX-PREMIUM-PLAN.md` only if a focused popup-menu note is useful
- this claim file for close-out

## Excluded scope

- No edits to `WorkspacePanel.xaml.cs`, `WorkspacePanel.QuickDraw.cs`, Workspace handlers, semantic/project mutation, CAD commands or viewport behavior.
- No edits to `RightPanel*`; the current Xref-scale lane owns its neighboring Right Panel work.
- No Quantity Settings, export/save-dialog, reporting, updater, rebar, Direct Draw, Ribbon, Start Center or release work.
- No GitHub Actions dispatch.

## Validation plan

- Re-fetch latest `main`, `Theme.xaml`, the existing theme preflight and active neighboring claims immediately before implementation/integration.
- Keep `Theme.xaml` well-formed and preserve every existing public theme resource key.
- Add host-independent dark `ContextMenu` and `MenuItem` templates with explicit foreground/background/border, hover/highlight, disabled and submenu glyph/arrow states; keep separators readable on dark popup surfaces.
- Extend `preflight-wpf-theme.py` to require the popup-menu templates and reject system-highlight/resource fallbacks that can reintroduce bright menu chrome.
- Source-review for presentation-only behavior and no blur/shadow/animation/CAD/project mutation.
- Real BricsCAD V25 visual/HiDPI qualification remains existing `LOCAL-012`; no remote runtime PASS claim.

## Coordination

The prior premium-theme-v2 and Workspace overlap claims are `COMPLETED`; this lane extends only the shared popup-menu visual contract. Current active Xref scale, export preflight, reporting, quantity, updater/rebar/reference-search lanes are explicitly excluded.

## Completion condition

Dark context-menu/menu-item chrome and its focused static regression guard are integrated into current `main`, existing menu actions remain untouched, the claim is marked `COMPLETED` with exact implementation/integration SHA(s), and native visual proof remains correctly parked under the existing local UI qualification boundary.

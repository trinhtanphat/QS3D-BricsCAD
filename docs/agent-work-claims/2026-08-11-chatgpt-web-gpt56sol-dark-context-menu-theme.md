# Work claim — Host-independent dark Workspace context-menu chrome

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-dark-context-menu-20260811`
- Registered: `2026-08-11T22:00:00+07:00`
- Scope refined: `2026-08-11T22:12:00+07:00`
- Completed: `2026-08-11T22:33:00+07:00`
- Baseline main SHA: `24f623ab2e01a78dc2c9ae7e948a83f42eca468b`
- Refined baseline main SHA: `2617eb4d66bc4db73be605dbcc35879ac341b8c8`

## Delivered scope

The existing Workspace Family and selected-object context menus now receive presentation-only host-independent dark chrome without changing their command/action behavior.

Delivered behavior:

- Styles the already-created `FamilyList.ContextMenu` and `InspectionList.ContextMenu`; no parallel menu/command construction path was added.
- Applies explicit dark popup surface, border and foreground presentation.
- Applies leaf `MenuItem` hover/highlight, selected/submenu-open and disabled visual states through a custom template.
- Applies explicit dark separator rules instead of relying on host/system separator rendering.
- Re-applies leaf/separator presentation on `ContextMenu.Opened`, so existing later-added workflow items such as `Vẽ Nhanh (Ctrl+D)` inherit the same presentation without duplicate click handlers.
- Leaves future submenu headers on their native functional template while recursively styling leaf children, avoiding accidental removal of `PART_Popup` behavior.
- Disables context-menu drop shadow and keeps layout rounding/device-pixel snapping enabled for crisp BricsCAD palette rendering.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DarkContextMenu.cs`
- `scripts/preflight-workspace-dark-context-menu.py`
- this claim file

No edits were made to `Theme.xaml`, `WorkspacePanel.xaml`, `WorkspacePanel.xaml.cs`, `WorkspacePanel.QuickDraw.cs`, Right Panel, Core semantics, CAD commands, selection/project mutation or release surfaces.

## Commits / integration

- source implementation commit: `3a555f2fe58c508d15aa68fbb91f33cf782ba31a`
- final reconciled feature-branch head: `5223746437dad51577a83db6935aed6b0b22020f`
- PR: `#499`
- integrated `main` merge commit: `129a091e7a6ea4fbf4cd3e39acf3fe922e2ffca8`

## Validation evidence

- Re-fetched current Workspace menu construction and confirmed the canonical Family/inspection context menus and their existing click handlers remain in `WorkspacePanel.xaml.cs`.
- Re-fetched the integrated presentation partial and source-reviewed the idempotent class-level Loaded hook, existing menu references, `Opened` refresh path, dark popup/menu/separator templates and presentation-only boundary.
- `scripts/preflight-workspace-dark-context-menu.py` is auto-discovered by the existing `scripts/preflight-all.py` glob contract and guards the canonical menu construction, existing `Vẽ Nhanh` menu item, the dark templates/states and the absence of command/CAD/project mutation paths in the presentation partial.
- The focused preflight file was syntax/source-reviewed in this connector session, but no repository checkout or licensed BricsCAD V25 environment was available to execute the aggregate/local runtime matrix. GitHub Actions were not dispatched.

## Coordination / LOCAL_ONLY boundary

The premium theme v2 and prior Workspace overlap lanes remain completed and authoritative. This lane did not touch active Xref-scale, Project Browser XML, BQ, export, reporting, updater/rebar/reference-search or release work.

Real BricsCAD V25 popup rendering, keyboard navigation, Vietnamese clipping and 100% / 125% / 150% / 200% HiDPI confirmation remain under the existing `LOCAL-012` UI qualification boundary; no remote runtime PASS is claimed.

## Completion

Reservation released. Workspace dark context-menu chrome and its focused regression guard are integrated into `main`, existing menu actions remain untouched, and native visual proof remains correctly parked under the existing local UI qualification boundary.

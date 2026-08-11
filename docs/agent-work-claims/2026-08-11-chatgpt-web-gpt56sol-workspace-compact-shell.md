# Work claim — Workspace compact BLT-style shell polish

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-workspace-compact-shell-20260811-2043`
- Registered: `2026-08-11T20:43:20+07:00`
- Completed: `2026-08-11T21:08:00+07:00`
- Baseline main SHA: `c654f2a2c62d941dac57b0944555b5265fc039f8`
- Claim registration commit: `739927db9c129348992522d7d1a2f3f52e175484`
- Scope-amendment commit: `0358e8e3d3a16776b475379f7ec6af520b8b3454`
- Implementation commit: `9be25bb5a0a8d1b9535487c4f321ebc9c2072a20`
- Integrated main commit: `46a2f6562f230bad39f95dc1ad37abf629ebb607` (PR #457)
- Priority: owner-provided BricsCAD/BLT3D reference shows a denser, clearer modeling workspace; improve the existing QS3D Workspace visual hierarchy and discoverability without duplicating already-active Right Panel/BQ/Project Tools functionality.

## Reserved scope

Polish only the existing BricsCAD-hosted Workspace palette so its left/model/property shell more closely matches the supplied reference: compact working Zone/Floor selectors, clearer model/category hierarchy, denser Family/Type action area, stronger selected-state/section hierarchy, and a compact professional dark layout. Reuse every existing real handler/binding and preserve current semantic selection/property mutation behavior; this lane is presentation/source-contract work, not a new business model or command stack.

The existing XAML already exposes the required functional regions and was changing concurrently. To minimize merge/ownership risk, the implementation adds density/discoverability in a dedicated `WorkspacePanel` partial class that styles only the existing named controls at load time; it does not replace handlers, invent commands, or create a parallel viewport.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml` — contract/read surface retained unchanged by this lane.
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs` — new idempotent presentation-only compact shell layer.
- `scripts/preflight-workspace-compact-shell.py` — new auto-discovered focused source contract.
- `docs/UI-WORKSPACE-COMPACT-SHELL-2026-08-11.md` — focused screenshot mapping, density and qualification note.
- this claim file for close-out.

## Delivered behavior

- Preserves `ZoneCombo`/`FloorCombo`, model tree tags, Family list, Property list/search, room-finishes block, selection inspection and every existing real click/selection handler.
- Compacts top/footer chrome and rebalances the model/Family/property/room/selection panes for 1366×768-class CAD workstations while retaining the existing horizontal fallback.
- Makes model-tree rows denser, gives Family/property/selection inspectors explicit minimum working space, enables preview resize on splitters, and strengthens the runtime section-title hierarchy.
- Surfaces the already-existing `Ctrl+S`, `Ctrl+F`, `Ctrl+B`, `F5`, and Family-list `Delete` shortcuts in tooltips without adding a second behavior path.
- Keeps BricsCAD as the real viewport; no embedded `Viewport3D`, fake canvas, standalone shell, semantic capture path, project mutation service, or CAD-command sender exists in the presentation partial.

## Excluded scope preserved

- No edits to `RightPanel*`, `PaletteCoordinator.cs`, the active right-panel quantity explanation/Xref/layer lane, `QuantitySummaryWindow*`, BQ detail/viewport-reveal, Project Tools, Zone/Family manager implementation, Start Center, Ribbon, `Theme.xaml`, `Commands.cs`, Core semantic/persistence/reporting, Direct Draw/Create Similar, Room Auto, Level/native placement, updater/release/signing or GitHub Actions.
- No licensed BricsCAD V25/WPF runtime PASS claim; existing `LOCAL-012` remains the native Workspace/HiDPI qualification boundary.

## Actual validation performed

- Re-fetched current `main` and confirmed the Workspace XAML had not changed between the audited source baseline and the implementation integration point.
- PR #457 changed exactly three implementation files: the dedicated presentation partial, focused preflight and focused UI note; no neighboring reserved surface was included.
- PR #457 reported mergeable before integration and was merged into `main`; integrated main SHA is `46a2f6562f230bad39f95dc1ad37abf629ebb607`.
- Re-fetched `WorkspacePanel.CompactShell.cs` and `preflight-workspace-compact-shell.py` from `main` after merge and confirmed their expected blob SHAs/content.
- Re-fetched moving `main` at `66c2ccc0c685f6ebc1acbfcf1dcfce5ade30516d`; compare showed the integration commit `46a2f6562f230bad39f95dc1ad37abf629ebb607` remains an ancestor with no Workspace compact-shell file modified by the six later concurrent commits.
- The focused Python gate was source-reviewed but not executed in this GitHub-connector session because there is no repository checkout/runtime attached to the connector. No GitHub Actions workflow was dispatched.
- Native BricsCAD V25/WPF rendering, dock/undock, contrast and 100/125/150/200% DPI remain intentionally unclaimed and must be qualified under `LOCAL-012`.

## Coordination result

The lane integrated without touching Right Panel quantity workspace, BQ detail/viewport reveal, Project Tools readiness, Zone/Family refresh identity, Start Center, Room Auto, Core mutation/schedule provenance, modeless schedule/revision viewers or LOCAL-003 Level placement. The focused UI note is feature-specific and does not replace high-level repository-health documentation.

## Completion

The screenshot-inspired compact Workspace presentation and focused static guard are on `main`, existing handlers/semantic boundaries remain intact, native visual/HiDPI proof is truthfully left to the existing LOCAL_ONLY Workspace qualification, and this claim is closed with the exact implementation/integration SHAs above.

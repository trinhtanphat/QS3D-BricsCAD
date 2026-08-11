# Work claim — Workspace compact BLT-style shell polish

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-compact-shell-20260811-2043`
- Registered: `2026-08-11T20:43:20+07:00`
- Baseline main SHA: `c654f2a2c62d941dac57b0944555b5265fc039f8`
- Priority: owner-provided BricsCAD/BLT3D reference shows a denser, clearer modeling workspace; improve the existing QS3D Workspace visual hierarchy and discoverability without duplicating already-active Right Panel/BQ/Project Tools functionality.

## Reserved scope

Polish only the existing BricsCAD-hosted Workspace palette so its left/model/property shell more closely matches the supplied reference: compact working Zone/Floor selectors, clearer model/category hierarchy, denser Family/Type action area, stronger selected-state/section hierarchy, and a compact professional dark layout. Reuse every existing real handler/binding and preserve current semantic selection/property mutation behavior; this lane is presentation/source-contract work, not a new business model or command stack.

The existing XAML already exposes the required functional regions and is being changed concurrently. To minimize merge/ownership risk, this claim may implement the density/discoverability layer in a dedicated `WorkspacePanel` partial class that styles only the existing named controls at load time; it must not replace handlers, invent commands, or create a parallel viewport.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml` (read/contract surface; edit only if still necessary at integration)
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs` (new dedicated presentation partial)
- `scripts/preflight-workspace-compact-shell.py` (new focused static source contract)
- `docs/UI-WORKSPACE-COMPACT-SHELL-2026-08-11.md` (new focused design/qualification note)
- this claim file for close-out

## Functional contract

- Keep `ZoneCombo`/`FloorCombo`, model tree tags, Family list, Property list, property scope/search, selection review and every current click handler wired to the same existing behavior.
- Do not add decorative buttons or stub actions; every visible actionable control must keep a real existing handler/workflow.
- Preserve explicit property origin/state badges and Family/Instance edit-policy presentation already implemented on `main`.
- Preserve BricsCAD as the real viewport; no embedded/parallel fake CAD canvas and no standalone shell.
- Favor compact 1366×768-friendly density while retaining horizontal fallback and existing minimum-size protections.
- Any code-only presentation helper must be idempotent, source-local to `WorkspacePanel`, and must not send CAD commands or mutate semantic project state.

## Excluded scope

- No edits to `RightPanel*`, `PaletteCoordinator.cs`, or the active right-panel quantity explanation/Xref/layer lane.
- No `QuantitySummaryWindow*`, BQ detail/viewport-reveal, schedule UI, or quantity formulas/provenance.
- No `ProjectToolsWindow*`, Zone/Family manager implementation, Start Center, Ribbon, `Theme.xaml`, `Commands.cs`, Core semantic/persistence/reporting, Direct Draw/Create Similar, Room Auto, Level/native placement, updater/release/signing or GitHub Actions.
- No licensed BricsCAD V25/WPF runtime PASS claim; existing `LOCAL-012` remains the native Workspace/HiDPI qualification boundary.

## Validation plan

- Re-fetch current `main`, the Workspace XAML and active claims immediately before implementation and integration.
- Preserve all existing `x:Name`, click/selection handlers and important model-category tags unless a source-proven defect requires otherwise.
- Add a focused auto-discovered static preflight that checks the compact Zone/Floor/model/Family/property visual contract while guarding the real action bindings and plugin-hosted boundary.
- Validate the dedicated partial as presentation-only: no `SendStringToExecute`, project mutation services, `Viewport3D`, or duplicate command handlers.
- Parse the current Workspace XAML and execute the focused Python preflight in the available source-only environment if practical; otherwise record source inspection only and do not overstate execution.
- Inspect final diff/ancestry on newest `main`; do not dispatch GitHub Actions.

## Coordination

Active neighboring claims reserve Right Panel quantity workspace, BQ detail/viewport reveal, Project Tools readiness, Zone/Family refresh identity, Start Center, Room Auto, Core mutation/schedule provenance, modeless schedule/revision viewers and LOCAL-003 Level placement. This lane intentionally owns only the existing Workspace presentation contract plus its dedicated presentation partial and a new feature-specific preflight/design note; it does not touch those neighboring surfaces. The repository-health/docs lane owns top-level/high-level documentation only; this new focused UI note is feature-specific.

## Completion condition

The screenshot-inspired compact Workspace presentation and focused static guard are pushed on current `main`, existing handlers/semantic boundaries remain intact, native visual/HiDPI proof is truthfully left to the existing LOCAL_ONLY Workspace qualification, and this claim is marked `COMPLETED` with exact implementation SHA(s) and actual validation performed.

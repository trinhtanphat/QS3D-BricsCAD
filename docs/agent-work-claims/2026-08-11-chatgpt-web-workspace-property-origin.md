# Work claim — Workspace property origin/status semantics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-property-origin`
- Registered: `2026-08-11T20:08:00+07:00`
- Baseline main SHA: `6dd428bc8fc157c01d2a4b7ffa89d0d252df95ba`
- Priority: remove a confirmed Property Inspector UX ambiguity where every read-only row is labeled as CAD-derived even when the row is semantic/system/selection metadata

## Reserved scope

Make the Workspace Property Inspector expose an explicit presentation-only row state/origin label instead of inferring every badge from `IsReadOnly`/`CanReset`. Keep Family, inherited Instance, explicit Instance override, CAD/source-derived, system/identity and multi-selection metadata states distinguishable without changing the underlying edit policy or mutation boundaries.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs`
- `src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.MultiSelectionProperties.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.PropertyFiltering.cs` only if search should index the new state label
- `scripts/preflight-workspace-property-palette.py`
- this claim file for close-out

## Excluded scope

- No changes to `SemanticPropertyEditPolicy` semantics, bulk mutation behavior, Direct Draw/Create Similar, Room Auto, Material Catalog, Start Center, modeless viewers, reporting, local qualification, release or CI.
- No new editable property types and no multi-selection relation editing.
- No Ribbon surfaces, including the newly regrouped `RibbonBootstrapper.cs` information architecture.
- No BricsCAD V25/WPF rendering PASS claim; visual qualification remains under existing Workspace LOCAL_ONLY coverage.

## Validation plan

- Re-fetch every expected surface immediately before its write and preserve concurrent changes.
- Add a view-model state/origin presentation contract that emits change notifications when dynamic reset/override state changes.
- Require XAML to bind the badge to the explicit state rather than using `IsReadOnly => CAD / đọc`.
- Extend the existing auto-discovered Workspace property-palette preflight so CAD/source, system/read-only, Family/inherited and Instance override labels cannot collapse back together.
- No GitHub Actions/build/release dispatch.

## Coordination

The Create Similar lane is BLOCKED only on its canonical `LOCAL-008` inbox write and does not reserve Workspace property files. The previous Workspace multi-selection policy claim is completed. The active Start Center claim explicitly excludes `WorkspacePanel*`, and the completed Ribbon information-architecture lane did not modify Workspace surfaces. Current neighboring active claims are outside this Property Inspector presentation lane.

## Completion condition

Explicit property row state/origin labels are merged on current `main`, single- and multi-selection row construction assigns the correct presentation state, XAML/search use it without changing mutation safety, focused static coverage is updated, and this claim is marked `COMPLETED` with exact implementation commits and no false V25 runtime claim.

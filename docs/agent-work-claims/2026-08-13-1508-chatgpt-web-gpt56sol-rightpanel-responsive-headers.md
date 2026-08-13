# Work claim — V25 RightPanel responsive headers

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rightpanel-responsive-headers-20260813`
- Registered: `2026-08-13T15:08:00+07:00`
- Baseline main SHA: `d4660c82768fe51af6d9ae0fb2cd2dd0e0eeb347`
- Priority: User-visible V25 UI redesign follow-up. Current `RightPanel.xaml` still has both section title/action rows implemented as `DockPanel`s whose final child is marked `DockPanel.Dock="Right"`. With default `LastChildFill=True`, that last action stack fills remaining width instead of honoring the right dock, making title/badge/action placement brittle at the 255-DIP compact host minimum.

## Reserved scope

Redesign only the two RightPanel section title rows (`QUẢN LÝ BẢN VẼ` and `QUẢN LÝ LỚP`) into deterministic responsive grids with shrinkable title content and an auto-sized right action cluster. Preserve all Xref/layer commands, context menus, dark-host resources, list semantics and current compact-shell sizing.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- new `scripts/preflight-rightpanel-responsive-headers.py`

## Excluded scope

- completed `RightPanel.DarkHostTheme.cs` / dark selection/context-menu lane
- `RightPanel.CompactShell.cs` row sizing and density
- WorkspacePanel, QuantityInsightPanel, shared `Theme.xaml`, palette persistence
- Xref/layer mutation semantics, keyboard workflow, project/QSDB state
- V26, installer/release, GitHub Actions dispatch or native BricsCAD PASS claims without runtime evidence

## Validation plan

- Require named `DrawingHeaderGrid` and `LayerHeaderGrid`, each with top-level `*` + `Auto` columns.
- Require the title-side accent/title/caption group to remain shrinkable and use no-wrap ellipsis; keep badge/action clusters in column 1.
- Preserve every existing section-header action/binding (`OnClearDrawingSelectionClick`, `OnRefreshClick`, drawing count and layer count bindings) plus the surrounding Xref/layer toolbar commands.
- Re-fetch current `main` after registration and confirm the earlier RightPanel dark-host claim remains `COMPLETED` with no new overlap. Re-fetch exact pushed source/test after implementation; do not dispatch Actions.

## Coordination

The earlier RightPanel dark-host claim is `COMPLETED` and explicitly excluded layout/density behavior. The just-completed Quantity Insight responsive lane is a different XAML surface. LOCAL-004 and Core/local qualification lanes are unrelated.

## Completion condition

Focused RightPanel responsive-header redesign + regression are pushed to current `main`, registration/source are verified, and this claim is marked `COMPLETED` with exact implementation/test SHAs and only validation actually executed.

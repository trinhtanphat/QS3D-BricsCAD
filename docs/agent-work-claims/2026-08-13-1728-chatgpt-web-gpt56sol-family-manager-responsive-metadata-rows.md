# Work claim — V25 Family Manager responsive metadata rows

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-manager-responsive-metadata-rows-20260813`
- Registered: `2026-08-13T17:28:00+07:00`
- Baseline main SHA: `5b92851248cea93ab3fb2753af4907e3cc03ad86`
- Priority: user-visible V25 UI hardening. Current `FamilyManagerWindow.xaml` has two metadata/title rows where the final child is marked `DockPanel.Dock="Right"` under default `LastChildFill=True`: `Instance tham chiếu` / `ReferenceCountText`, and `PROPERTY CỦA FAMILY` / `KEY / VALUE`. In WPF the final child fills instead of honoring the right dock, producing width-dependent alignment.

## Reserved scope

Replace only those two Family Manager metadata/title DockPanels with deterministic two-column responsive grids (`*` + `Auto`). Keep left text shrinkable/no-wrap/ellipsis where appropriate, keep count/status right-aligned in the auto column, and preserve all Family list/property/activation/edit/assignment behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- new `scripts/preflight-family-manager-responsive-metadata-rows.py`
- this claim file

## Excluded scope

- completed Family Manager dark-selection partial/regression
- Family business rules, code-behind handlers, project/QSDB mutations
- shared `Theme.xaml`, list column redesign, overall window size/layout, other UI surfaces
- MTR/MAP/Core/release/runtime/V26/GitHub Actions

## Validation plan

- Require named `FamilyReferenceSummaryGrid` and `FamilyPropertyHeaderGrid`, each with exactly `*` + `Auto` columns.
- Preserve `ReferenceCountText`, Family/property list handlers, create/duplicate/rename/delete/activate/save/remove/assign actions.
- Require right-side count/status text to occupy column 1 and reject the stale right-docked final-child patterns.
- Re-fetch current `main` before source write and exact pushed XAML/preflight after implementation; verify intervening commits for overlap.

## Coordination

The Family Manager dark-selection claim is already `COMPLETED` and explicitly treated `FamilyManagerWindow.xaml` as read-only. Its presentation-only partial remains untouched. Current diagnostic/schedule dark-selection work is on separate windows.

## Completion condition

The narrow responsive metadata-row redesign and focused source regression are on current `main`, exact source/test are read back, and this claim is closed `COMPLETED` with only actually executed validation reported.
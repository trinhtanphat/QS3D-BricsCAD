# Work claim — V25 Family Manager dark selection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-manager-dark-selection-20260813`
- Registered: `2026-08-13T17:10:00+07:00`
- Baseline main SHA: `e86057a1de25d27e2b1ee390c49a147a2a6875bc`
- Priority: Continuing the user-requested dark-host audit. `FamilyManagerWindow.xaml` contains `FamilyList` and `PropertyList` ListViews. Shared `Theme.xaml` sets dark `ListViewItem` selection values but keeps the stock WPF item template, so BricsCAD/WPF active/inactive system highlight resources can still leak bright selection chrome.

## Reserved scope

Make Family Manager ListView selection host-independent by shadowing active/inactive WPF selection background/text resources at the window boundary and directly on `FamilyList` and `PropertyList`. Preserve Family creation/rename/delete/activation/property/assignment handlers and all model/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-family-manager-dark-selection.py` (new focused regression)
- read-only `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- read-only `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- Family Manager commands/business rules/project mutations
- shared `Theme.xaml` redesign and ComboBox template changes
- Workspace/RightPanel/Quantity windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without runtime evidence

## Validation plan

- Require all four active/inactive `SystemColors` selection background/text resource pins.
- Require window, `FamilyList`, and `PropertyList` local resource boundaries.
- Preserve `OnFamilySelectionChanged` and `OnPropertySelectionChanged`; assert presentation partial contains no command/CAD/project mutation path.
- Re-fetch exact pushed source/test and verify ancestry against current `main`.

## Coordination

Quantity Summary dark selection is completed. Current Curtain/runtime/source-gap lanes are unrelated. No recent Family Manager dark-selection reservation was found in commit history.

## Completion condition

Focused fix + regression are pushed to `main`, exact source/ancestry are verified, and this claim is marked `COMPLETED` with exact SHAs and only validation actually executed.

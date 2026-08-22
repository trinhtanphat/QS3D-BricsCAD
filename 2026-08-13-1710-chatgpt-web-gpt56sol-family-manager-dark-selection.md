# Work claim — V25 Family Manager dark selection

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-manager-dark-selection-20260813`
- Registered: `2026-08-13T17:10:00+07:00`
- Completed: `2026-08-13T17:13:00+07:00`
- Baseline main SHA: `e86057a1de25d27e2b1ee390c49a147a2a6875bc`
- Priority: Continuing the user-requested dark-host audit. `FamilyManagerWindow.xaml` contains `FamilyList` and `PropertyList` ListViews while shared `Theme.xaml` retains the stock WPF ListViewItem template.

## Reserved scope

Make Family Manager ListView selection host-independent by shadowing active/inactive WPF selection background/text resources at the window boundary and directly on `FamilyList` and `PropertyList`. Preserve Family creation/rename/delete/activation/property/assignment handlers and all model/project semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.DarkHostTheme.cs`
- `scripts/preflight-family-manager-dark-selection.py`
- read-only `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- read-only `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- Family Manager commands/business rules/project mutations
- shared `Theme.xaml` redesign and ComboBox template changes
- Workspace/RightPanel/Quantity windows, V26, release/installer work
- GitHub Actions dispatch and native BricsCAD PASS claims without runtime evidence

## Result

- Implementation: `8d636fc4a4c4d5e060f6f562475d849ab5c5b846` (`fix(v25): keep Family Manager selection dark`).
  - Shadows active/inactive WPF selection background keys with `BgSelectedBrush`.
  - Shadows active/inactive WPF selection text keys with `TextBrush`.
  - Publishes each resource at `FamilyManagerWindow.Resources` and directly on `FamilyList` / `PropertyList`.
  - Does not alter Family commands, handlers or project/domain state.
- Regression: `2544bee6c129e84fad15b044a3b1508a46f1d2ea` (`test(ui): guard Family Manager dark selection`).

## Validation actually executed

- Re-fetched exact pushed implementation and regression from `main`; all four system selection pins and both ListView local boundaries are present.
- Current `FamilyManagerWindow.xaml` still contains `FamilyList` / `OnFamilySelectionChanged` and `PropertyList` / `OnPropertySelectionChanged` unchanged.
- Current shared Theme still exposes `BgSelectedBrush` and the implicit `ListViewItem` contract.
- `python -m py_compile` for the focused preflight logic — PASS in an isolated connector-derived fixture.
- Focused preflight logic — `PASS: V25 Family Manager dark host-selection contract` in that fixture.
- `compare_commits(2544bee6c129e84fad15b044a3b1508a46f1d2ea, main)` returned `identical` at validation time.
- No GitHub Actions were dispatched. Native BricsCAD V25 visual/runtime qualification was not executed and is not claimed as PASS.

## Coordination

Quantity Summary dark selection is completed. Concurrent Curtain/runtime/source-gap lanes are unrelated and did not touch this scope.

## Completion condition

Satisfied for repository source/regression: focused fix and regression are pushed to `main`, exact source/ancestry were verified, and native visual qualification remains pending a licensed runtime smoke.

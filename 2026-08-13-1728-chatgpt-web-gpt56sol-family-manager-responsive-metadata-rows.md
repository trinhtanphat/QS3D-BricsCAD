# Work claim — V25 Family Manager responsive metadata rows

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-manager-responsive-metadata-rows-20260813`
- Registered: `2026-08-13T17:28:00+07:00`
- Completed: `2026-08-13T17:31:00+07:00`
- Baseline main SHA: `5b92851248cea93ab3fb2753af4907e3cc03ad86`
- Priority: user-visible V25 UI hardening. Source inspection confirmed `FamilyManagerWindow.xaml` had two metadata/title rows whose final children were marked `DockPanel.Dock="Right"` under default `LastChildFill=True`: `Instance tham chiếu` / `ReferenceCountText`, and `PROPERTY CỦA FAMILY` / `KEY / VALUE`. In WPF those final children fill instead of honoring the right dock, producing width-dependent alignment.

## Reserved scope

Replace only those two Family Manager metadata/title DockPanels with deterministic two-column responsive grids (`*` + `Auto`). Keep left text shrinkable/no-wrap/ellipsis where appropriate, keep count/status right-aligned in the auto column, and preserve all Family list/property/activation/edit/assignment behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml`
- `scripts/preflight-family-manager-responsive-metadata-rows.py`
- this claim file

## Excluded scope

- completed Family Manager dark-selection partial/regression
- Family business rules, code-behind handlers, project/QSDB mutations
- shared `Theme.xaml`, list column redesign, overall window size/layout, other UI surfaces
- MTR/MAP/Core/release/runtime/V26/GitHub Actions

## Result

- Implementation: `0fed4369891136bd439c7e20819fba84f7378f0f` (`fix(ui): make Family Manager metadata rows responsive`).
  - Replaced the reference-count DockPanel with named `FamilyReferenceSummaryGrid` and the property-title DockPanel with named `FamilyPropertyHeaderGrid`.
  - Both use deterministic `*` + `Auto` columns; left labels are shrinkable/no-wrap/ellipsis and `ReferenceCountText` / `KEY / VALUE` are right-aligned in the auto column.
  - Family/property ListViews and all existing refresh, activate, create, duplicate, rename, delete, save/remove property and assign handlers remain unchanged.
- Regression: `27d80dfe76166d425b65bdde819cf4f6207c8126` (`test(ui): guard Family Manager responsive metadata rows`).
  - Parses the XAML, validates both named star/auto grids, verifies shrink/right-alignment contracts, preserves handler tokens and rejects both stale right-docked final-child patterns.

## Validation actually executed

- Re-fetched current-main `FamilyManagerWindow.xaml`; both named responsive grids, star/auto columns, `ReferenceCountText`, list handlers and action handlers are present.
- Re-fetched the focused preflight from `main` and reviewed its XML/continuity checks against the pushed XAML.
- `compare_commits(f6a53cff8ca2d112f5bc7250e36d5b04aea515bf, main)` reported the claim commit as merge base with `behind_by=0`; intervening files were this lane plus unrelated schedule dark-selection and Core cost claim work, with no Family Manager overlap.
- The Python preflight was not executed in a repository checkout from this connector environment, so no executable PASS is claimed. No GitHub Actions or licensed native BricsCAD visual smoke was run by this lane.

## Coordination

The completed Family Manager dark-selection partial remains independent and untouched. Concurrent diagnostic/schedule dark-selection work targets separate windows.

## Completion condition

Satisfied for repository source/regression: the narrow responsive metadata-row redesign and focused source regression are on current `main`, exact source/test were read back, and native visual qualification remains explicitly unclaimed pending a licensed local runtime smoke.

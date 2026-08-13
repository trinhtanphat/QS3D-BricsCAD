# Work claim — V25 RightPanel dark host chrome

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rightpanel-dark-host-chrome-20260813`
- Registered: `2026-08-13T14:59:30+07:00`
- Baseline main SHA: `bd429d3ceec1058f984fca068ce54aeb88e391fe`
- Priority: Follow-up UI audit after the Workspace white-selection fix. Current `RightPanel.xaml` still has two `ListView` selection surfaces and two XAML context menus whose `MenuItem` containers rely on stock WPF templates. `Theme.xaml` sets dark foreground/background values but does not own `MenuItem`/`ContextMenu` templates, and the Workspace-specific host guard does not cover RightPanel. A BricsCAD/WPF host can therefore still inject bright active/inactive list selection or menu-highlight chrome in the right palette.

## Reserved scope

Make RightPanel selection and context-menu chrome host-independent without changing Xref/layer actions or selection semantics. Shadow active/inactive WPF selection resources at the RightPanel boundary for `DrawingList` and `LayerList`, and apply presentation-only dark leaf `MenuItem`/separator templates to the two existing context menus.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.DarkHostTheme.cs` (new presentation-only partial)
- `scripts/preflight-rightpanel-dark-host-chrome.py` (new focused regression)
- read-only contract references: `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`, `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- RightPanel commands/handlers, Xref/layer mutation semantics, keyboard workflow
- `RightPanel.CompactShell.cs` sizing/density behavior
- Workspace/QuantityInsight/V26 behavior
- shared Theme redesign or native BricsCAD runtime claims

## Validation plan

- Focused source regression must require all four active/inactive `SystemColors` selection resources at RightPanel scope, direct coverage of both named ListViews, and explicit dark `MenuItem`/separator templates applied to both existing context menus.
- Re-fetch current source and verify no newer overlapping RightPanel claim before implementation.
- Re-fetch pushed source/test and verify commit ancestry. No GitHub Actions dispatch and no native V25 PASS claim without licensed runtime evidence.

## Coordination

The prior Workspace dark-selection and Workspace dark-context-menu claims are completed and explicitly Workspace-only. Recent active/local Curtain/LOCAL-003 and Core cost lanes are unrelated. No current recent RightPanel dark-host claim was found in commit/claim history.

## Completion condition

The bounded RightPanel dark-host fix and regression are pushed to current `main`, remote ancestry/source are verified, and this claim is marked `COMPLETED` with exact SHAs and only actually executed validation reported.

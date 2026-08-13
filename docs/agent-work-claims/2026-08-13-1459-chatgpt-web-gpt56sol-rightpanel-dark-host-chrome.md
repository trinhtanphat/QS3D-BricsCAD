# Work claim — V25 RightPanel dark host chrome

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rightpanel-dark-host-chrome-20260813`
- Registered: `2026-08-13T14:59:30+07:00`
- Completed: `2026-08-13T15:05:10+07:00`
- Baseline main SHA: `bd429d3ceec1058f984fca068ce54aeb88e391fe`
- Priority: Follow-up UI audit after the Workspace white-selection fix. Current `RightPanel.xaml` still had two `ListView` selection surfaces and two XAML context menus whose `MenuItem` containers relied on stock WPF templates. `Theme.xaml` set dark foreground/background values but did not own `MenuItem`/`ContextMenu` templates, and the Workspace-specific host guard did not cover RightPanel.

## Reserved scope

Make RightPanel selection and context-menu chrome host-independent without changing Xref/layer actions or selection semantics. Shadow active/inactive WPF selection resources at the RightPanel boundary for `DrawingList` and `LayerList`, and apply presentation-only dark leaf `MenuItem`/separator templates to the two existing context menus.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RightPanel.DarkHostTheme.cs`
- `scripts/preflight-rightpanel-dark-host-chrome.py`
- read-only contract references: `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`, `src/QS3D.BricsCAD.V25/UI/Theme.xaml`

## Excluded scope

- RightPanel commands/handlers, Xref/layer mutation semantics, keyboard workflow
- `RightPanel.CompactShell.cs` sizing/density behavior
- Workspace/QuantityInsight/V26 behavior
- shared Theme redesign or native BricsCAD runtime claims

## Result

- Implementation: `649d52971e49b4b2ab479003149d35b787de74be` (`fix(v25): harden RightPanel dark host chrome`).
  - Shadows active/inactive `SystemColors` selection background/text keys at the RightPanel boundary and directly on `DrawingList`/`LayerList`.
  - Applies QS3D-owned dark leaf `MenuItem` and separator templates to both existing RightPanel context menus, with dark hover/selected/disabled states and host drop shadow disabled.
  - Leaves every existing Xref/layer command handler and selection path unchanged.
- Regression: `9725c4a4f96ede2d577e603b80ef6a9b1fb5b77f` (`test(ui): guard RightPanel dark host chrome`).

## Validation actually executed

- Re-fetched current-main `RightPanel.DarkHostTheme.cs` and the focused regression; all four active/inactive selection resource pins, both ListView pins, both context-menu applications, owned MenuItem template and separator template are present.
- Re-fetched current `RightPanel.xaml`; it still contains exactly the two named ListViews/context-menu surfaces targeted by the guard and no command surface was removed.
- `compare_commits(9725c4a4f96ede2d577e603b80ef6a9b1fb5b77f, main)` returned `status=ahead`, `behind_by=0`, merge-base equal to the regression commit; the only newer file at that check was an unrelated LOCAL-004 claim.
- A fresh full V25 build/native visual smoke was not executed from this connector environment, so no native BricsCAD PASS is claimed.
- No GitHub Actions were dispatched by this lane.

## Coordination

The prior Workspace dark-selection and Workspace dark-context-menu claims are completed and explicitly Workspace-only. The concurrent LOCAL-004 source-reconcile runtime claim is unrelated and did not touch this lane's files.

## Completion condition

Satisfied for repository source/regression: the bounded RightPanel dark-host fix and regression are pushed to `main`, current source/ancestry were verified, and native BricsCAD visual qualification remains unclaimed pending licensed runtime evidence.

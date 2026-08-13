# Work claim — V25 Reference Search responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-reference-search-responsive-footer-20260813`
- Registered: `2026-08-13T17:35:00+07:00`
- Baseline main SHA: `db57e99904359ff500fa535ef101daaad8362fab`
- Priority: user-visible V25 UI hardening. Current `ReferenceSearchWindow.xaml` footer uses `StatusText` followed by a final `TextBlock DockPanel.Dock="Right"` while `LastChildFill` remains at its default. The gate label can therefore fill the remaining row instead of reliably occupying the right edge.

## Reserved scope

Replace only the Reference Search footer DockPanel with a deterministic responsive grid: status indicator in an auto column, shrinkable/wrapping `StatusText` in `*`, and `DOCUMENT-BOUND • HTTPS • SAFESEARCH` in a right-aligned auto column. Preserve all guarded web-launch semantics, query controls, search category/quick-query handlers and HTTPS/safe-search wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml`
- new `scripts/preflight-reference-search-responsive-footer.py`
- this claim file

## Excluded scope

- URL/query validation, effective-query bounds, browser process launch, code-behind
- shared `Theme.xaml`, Start Center or other windows
- project/QSDB/Core/release/V26/GitHub Actions
- no native visual/runtime PASS claim without execution

## Validation plan

- Require named `ReferenceSearchStatusGrid` with `Auto` + `*` + `Auto` columns.
- Preserve `StatusText`, status indicator, footer safe-launch wording, `QueryBox`, `TechnicalContextCheck`, Enter handler, all six category tags and all six quick-query buttons.
- Reject the stale footer right-docked final child.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening commits for overlap.

## Coordination

Existing Reference Search claims cover construction-search behavior and effective query bounds and are completed. Recent search found no responsive/dark-host lane for this window. The concurrent V25 dark-selection coverage gate is regression-only and explicitly excludes layout/responsiveness, so it does not overlap this XAML-only lane.

## Completion condition

The narrow responsive-footer redesign and focused source regression are on current `main`, exact source/test are read back, and this claim is closed `COMPLETED` with only actually executed validation reported.
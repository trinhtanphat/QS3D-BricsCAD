# Work claim — V25 Zone Manager responsive footer

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-manager-responsive-footer-20260813`
- Registered: `2026-08-13T18:10:00+07:00`
- Baseline main SHA: `b0ce99896d51f63df1e32a2aaea91d5777230dee`
- Priority: P1 user-visible V25 UI reliability. `ZoneManagerWindow` still uses a default footer `DockPanel` for the success indicator, wrapping `StatusText`, and final `SEMANTIC SCOPE ONLY • NO CAD MOVE` boundary label. The status text has no explicit flexible column and the final child can consume the remaining row at narrow host widths.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml`
- `scripts/preflight-zone-manager-responsive-footer.py` (new focused source regression)
- this claim file

## Intended change

Replace only the Zone Manager footer with a named `Auto` / `*` / `Auto` grid: preserve success indicator in column 0, keep `StatusText` wrapping/shrinkable in column 1, and pin the semantic-scope boundary label right/no-wrap in column 2. Preserve zone list/editor commands, IDs/names, semantic-only behavior, and handlers.

## Excluded scope

- zone CRUD/domain logic, CAD movement, selection semantics, code-behind
- list/theme/dark-selection behavior, header/body redesign
- other windows, V26, GitHub Actions, native runtime qualification

## Validation plan

- Add a focused offline XAML preflight requiring named `ZoneManagerStatusGrid`, exact `Auto`/`*`/`Auto` widths, preserved status/boundary label, zone CRUD handler sentinels, and rejection of stale right-docked boundary label.
- Re-fetch exact pushed XAML/regression and inspect production diff.
- Verify ancestry against moving `main` before closeout.
- Source/static validation only; no native BricsCAD V25 runtime PASS will be claimed.

## Coordination

Recent commit search found no Zone Manager responsive-footer lane. Current Model Health/Room Finish/Rebar Schedule/NETLOAD/closed-polyline work and completed Material/Wall/Curtain lanes are distinct scopes.

## Completion condition

The narrow footer fix and focused regression are on current `main`, exact source/test and ancestry are verified, and this claim is marked `COMPLETED` with actual validation boundaries recorded.

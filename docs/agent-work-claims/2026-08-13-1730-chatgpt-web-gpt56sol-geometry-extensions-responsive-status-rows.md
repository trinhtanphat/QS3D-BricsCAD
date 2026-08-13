# Work claim — V25 Geometry Extensions responsive status rows

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-geometry-extensions-responsive-status-rows-20260813`
- Registered: `2026-08-13T17:30:00+07:00`
- Baseline main SHA: `7de2bbd6484c9344441ebb45ddc666b5ae5199c9`
- Priority: user-visible V25 UI hardening. Current `GeometryExtensionsWindow.xaml` has two final-child right-docking rows under default `DockPanel.LastChildFill=True`: the `REBAR HEALTH` / `FAIL-CLOSED` card header and the footer `StatusText` / `PREVIEW / FINGERPRINT / HEALTH GATES`. Their final right-docked text can fill the remaining row instead of honoring right alignment, making layout width-dependent.

## Reserved scope

Replace only those two Geometry Extensions presentation rows with deterministic responsive grids. The Rebar Health header uses `*` + `Auto`; the footer preserves the status indicator then uses a shrinkable status column plus an auto right gate label. Preserve every geometry/rebar command Tag, `OnCommandClick` binding, gate wording and release-warning semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml`
- new `scripts/preflight-geometry-extensions-responsive-status-rows.py`
- this claim file

## Excluded scope

- geometry/rebar algorithms, command routing/code-behind, review/runtime gate semantics
- generated ownership/health behavior, Core/project/QSDB state
- shared `Theme.xaml`, other windows, installer/release/V26/GitHub Actions
- no native BricsCAD visual/runtime PASS claim without licensed execution

## Validation plan

- Require named `RebarHealthHeaderGrid` with exactly `*` + `Auto` columns and a right-aligned `FAIL-CLOSED` status.
- Require named `GeometryStatusGrid` with indicator + shrinkable `*` status + auto gate label; preserve `StatusText` and footer wording.
- Preserve all current command Tags and `Click="OnCommandClick"` surfaces.
- Reject both stale final-child `DockPanel.Dock="Right"` patterns.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening commits for overlap.

## Coordination

Recent commit/code search found no Geometry Extensions responsive/dark-host lane. Historical Geometry Extensions work is from the initial review-panel feature and its gate documentation, not this presentation defect. Current Start Center and other window dark-host lanes are unrelated.

## Completion condition

The narrow responsive-status redesign and focused source regression are pushed to current `main`, exact source/test are read back, and this claim is closed `COMPLETED` with only actually executed validation reported.
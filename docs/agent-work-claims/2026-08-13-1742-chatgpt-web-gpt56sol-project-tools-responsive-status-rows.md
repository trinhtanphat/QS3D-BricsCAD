# Work claim — V25 Project Tools responsive status rows

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-tools-responsive-status-rows-20260813`
- Registered: `2026-08-13T17:42:00+07:00`
- Baseline main SHA: `559c5f2ea955f839e502f5f8b9f527a4275649b3`
- Priority: user-visible V25 UI hardening. Source inspection shows three final-child right-docking rows under default `DockPanel.LastChildFill=True`: `PROJECT SNAPSHOT` / `LIVE • READ-ONLY`, the readiness text / `ReadinessBadgeText`, and the footer `StatusText` / `PROJECT-SAFE • READ-ONLY SNAPSHOT • DWG CONTEXT LOCK`. The final right-docked child can fill the remaining row instead of occupying a bounded right edge, making alignment and shrink behavior width-dependent.

## Reserved scope

Replace only those three Project Tools presentation rows with deterministic responsive grids. Snapshot and readiness rows use shrinkable `*` content plus `Auto` right status/badge; the footer uses indicator + shrinkable `*` status + auto right gate label. Preserve every command Tag/handler, all readiness/snapshot bindings, read-only semantics and status wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml`
- new `scripts/preflight-project-tools-responsive-status-rows.py`
- this claim file

## Excluded scope

- Project Tools code-behind, command dispatch, project creation/save/reload semantics
- interchange/source-reconcile/domain/Core/QSDB behavior
- shared Theme, other windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `ProjectSnapshotHeaderGrid` with `*` + `Auto` and right-aligned `LIVE • READ-ONLY`.
- Require named `ProjectReadinessHeaderGrid` with `*` + `Auto`, shrinkable readiness content and `ReadinessBadgeText` in the auto column.
- Require named `ProjectToolsStatusGrid` with `Auto` + `*` + `Auto`, preserving `StatusText`, indicator and footer gate wording.
- Preserve the existing Project Tools command Tags/`OnCommandClick` wiring and the read-only readiness/snapshot named controls.
- Reject the three stale final-child right-docked patterns.
- Re-fetch current `main` before source write and exact pushed XAML/regression after implementation; inspect intervening changes for overlap.

## Coordination

Recent commit search found historical Project Tools readiness/command work but no responsive-status lane. Current Source Reconcile/Curtain/runtime work is outside this XAML-only scope.

## Completion condition

The narrow responsive redesign and focused source regression are on current `main`, exact source/test are read back, ancestry is checked, and this claim is closed `COMPLETED` with only actually executed validation reported.
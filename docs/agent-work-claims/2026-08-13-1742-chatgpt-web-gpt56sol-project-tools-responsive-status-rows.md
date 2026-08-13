# Work claim — V25 Project Tools responsive status rows

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-tools-responsive-status-rows-20260813`
- Registered: `2026-08-13T17:42:00+07:00`
- Completed: `2026-08-13T17:47:00+07:00`
- Baseline main SHA: `559c5f2ea955f839e502f5f8b9f527a4275649b3`
- Priority: user-visible V25 UI hardening. Source inspection confirmed three final-child right-docking rows under default `DockPanel.LastChildFill=True`: `PROJECT SNAPSHOT` / `LIVE • READ-ONLY`, the readiness text / `ReadinessBadgeText`, and the footer `StatusText` / `PROJECT-SAFE • READ-ONLY SNAPSHOT • DWG CONTEXT LOCK`. The final right-docked child could fill the remaining row instead of occupying a bounded right edge, making alignment and shrink behavior width-dependent.

## Reserved scope

Replace only those three Project Tools presentation rows with deterministic responsive grids. Snapshot and readiness rows use shrinkable `*` content plus `Auto` right status/badge; the footer uses indicator + shrinkable `*` status + auto right gate label. Preserve every command Tag/handler, all readiness/snapshot bindings, read-only semantics and status wording.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml`
- `scripts/preflight-project-tools-responsive-status-rows.py`
- this claim file

## Excluded scope

- Project Tools code-behind, command dispatch, project creation/save/reload semantics
- interchange/source-reconcile/domain/Core/QSDB behavior
- shared Theme, other windows, V26/release/GitHub Actions/native runtime claims

## Result

- Implementation: `6111c2dfe1c7a1ec2aad68141b359ddd1dea7d29` (`fix(ui): make Project Tools status rows responsive`).
  - Replaced the snapshot DockPanel with named `ProjectSnapshotHeaderGrid` using `*` + `Auto`; title is shrinkable/no-wrap/ellipsis and `LIVE • READ-ONLY` remains right-aligned/no-wrap.
  - Replaced the readiness DockPanel with named `ProjectReadinessHeaderGrid` using `*` + `Auto`; `ReadinessText` remains named/wrapping in a shrinkable content column and `ReadinessBadgeText` remains named in a bounded right badge.
  - Replaced the footer DockPanel with named `ProjectToolsStatusGrid` using `Auto` + `*` + `Auto`; status indicator, `StatusText`, and the project-safe gate wording are preserved.
  - Existing Project Tools command Tags and `OnCommandClick` wiring were left unchanged.
- Regression: `e87b6b1c8483bb4bd67411caa58fccaed5792aa0` (`test(ui): guard Project Tools responsive status rows`).
  - Parses XAML; validates all three named grid/column contracts; checks readiness/snapshot/footer alignment and wrapping; verifies the full current command Tag set and `OnCommandClick`; requires the read-only named snapshot/readiness controls; rejects all three stale right-docked patterns.

## Validation actually executed

- Re-fetched current-main `ProjectToolsWindow.xaml`; `ProjectSnapshotHeaderGrid`, `ProjectReadinessHeaderGrid` and `ProjectToolsStatusGrid` are present with the intended `*`/`Auto` and `Auto`/`*`/`Auto` column contracts, and the read-only status/badge/footer text remains intact.
- Re-fetched the focused preflight from current `main` and reviewed its XML/command continuity checks against the pushed XAML.
- `compare_commits(998c6d0041011608133b24be128ed855d2c7386c, main)` reported the registration commit as merge base with `behind_by=0`. Intervening files were the expected Project Tools XAML/preflight plus unrelated Source Reconcile/runtime diagnostics work; no competing Project Tools XAML edit was present at that check.
- The Python preflight was not executed in a repository checkout from this connector environment, so no executable PASS is claimed. No GitHub Actions or licensed BricsCAD V25 visual/runtime smoke was run by this lane.

## Coordination

Historical Project Tools readiness/command work remained non-overlapping. Concurrent Source Reconcile/runtime diagnostics changes were outside this XAML-only scope and were not modified.

## Completion condition

Satisfied for repository source/regression: the narrow responsive redesign and focused source regression are on current `main`, exact source/test were read back, ancestry was checked, and native visual qualification remains explicitly unclaimed pending licensed local runtime evidence.
# Work claim — V25 Rebar 3D Hub responsive status rows

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar3d-hub-responsive-status-rows-20260813`
- Registered: `2026-08-13T17:40:00+07:00`
- Baseline main SHA: `cc74b798174cc89bee644473b3730aaa4a53d1b0`
- Priority: user-visible V25 UI hardening. Current `Rebar3DHubWindow.xaml` has two final-child right-docking rows under default `DockPanel.LastChildFill=True`: the `HEALTH` / `FAIL-CLOSED` card header and the footer `StatusText` / `EXPLICIT REBAR INPUTS • NATIVE 3D`. Their final status labels can fill the remaining row rather than reliably occupying the right edge.

## Reserved scope

Replace only those two Rebar 3D Hub presentation rows with deterministic responsive grids. The Health header uses `*` + `Auto`; the footer uses status indicator + shrinkable `*` status + auto right workflow label. Preserve every rebar command Tag, `OnCommandClick` binding, fail-closed wording and explicit-input semantics.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml`
- new `scripts/preflight-rebar3d-hub-responsive-status-rows.py`
- this claim file

## Excluded scope

- rebar planners/builders/health logic, command routing/code-behind
- mesh/BBS/generated ownership semantics, project/QSDB/Core
- shared Theme, other UI windows, V26/release/GitHub Actions/native runtime claims

## Validation plan

- Require named `RebarHubHealthHeaderGrid` with `*` + `Auto` and right-aligned `FAIL-CLOSED`.
- Require named `RebarHubStatusGrid` with `Auto` + `*` + `Auto`, preserving `StatusText`, warning indicator and `EXPLICIT REBAR INPUTS • NATIVE 3D`.
- Preserve every current command Tag and handler count.
- Reject both stale final-child right-docked patterns.
- Re-fetch `main` before source write and exact pushed source/regression after implementation; inspect intervening commits for overlap.

## Coordination

Recent commit search found no Rebar 3D Hub responsive lane. The concurrent repository-wide dark-selection coverage claim is regression-only and excludes layout/responsiveness; this window has no collection-selection surface involved in the change.

## Completion condition

The narrow responsive-status redesign and focused source regression are on current `main`, exact source/test are read back, and this claim is closed `COMPLETED` with only actually executed validation reported.
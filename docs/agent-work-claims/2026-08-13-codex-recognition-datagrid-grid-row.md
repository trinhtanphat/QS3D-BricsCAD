# Work claim — V25 Recognition DataGrid/Grid row compile fix

- Status: `COMPLETED`
- Agent: `codex-remote-recognition-datagrid-row-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T16:03:00+07:00`
- Scope expanded: `2026-08-13T16:08:00+07:00` after the same PR `#1008` audit exposed four aggregate presentation-contract failures
- Gate refinement registered: `2026-08-13T16:11:00+07:00` after focused rerun proved the developer warning retained both safety clauses but the existing gate compared obsolete punctuation
- Completed: `2026-08-13T16:13:00+07:00`
- Baseline main SHA: `9446d962fb31b3541110b934c88919d5a73e7a76`
- Priority: current-main V25 compilation is blocked after UI integration PR `#1008` because the Recognition XAML-generated `DataGrid Grid` member shadows the WPF `Grid` type at two attached-row getter call sites.

## Reserved scope

Qualify the two Recognition compact-shell attached-row getter calls so they bind to `System.Windows.Controls.Grid.GetRow` despite the generated `DataGrid` member named `Grid`. Add a focused static regression for this exact type/member shadowing boundary while preserving the responsive header/footer behavior.

Also restore only the four canonical Quantity Settings presentation strings changed by PR `#1008`: the two existing rule-create button labels and the two developer-section headings. The underlying 11 persisted controls, handlers and responsive redesign remain unchanged; canonical gates prove the user-facing rule/developer contracts rather than obsolete structure.

Refine the existing developer-layout preflight so its warning check requires both durable safety clauses while allowing the newer `CẢNH BÁO` prefix/punctuation. Do not rewrite the semantically equivalent current warning copy merely to satisfy an obsolete exact-string assertion.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.CompactShell.cs`
- `scripts/preflight-recognition-compact-shell-grid-row.py`
- read-only contract reference: `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml` — four text-only presentation restorations
- `scripts/preflight-quantity-settings-developer-layout.py` — warning assertion only
- read-only focused gates: `preflight-quantity-category-rule-create-ui.py`, `preflight-quantity-rule-create-ui.py`, `preflight-quantity-settings-future-schema-ui.py`
- this claim file

## Excluded scope

- Recognition engine, candidate/review/capture behavior, XAML layout redesign or command handlers
- Quantity Settings layout, controls, handlers, bindings, persistence, warning meaning, Quantity Summary, Quantity Insight, Workspace, RightPanel or shared theme behavior beyond the four named text restorations and punctuation-tolerant warning assertion
- Source Reconcile/`LOCAL-004`, Core semantics, V26, private/customer drawings, BricsCAD runtime, packaging/release/signing/installer or GitHub Actions

## Validation plan

- Run the focused member-shadowing regression, the four affected Quantity Settings gates and the aggregate feature preflight auto-discovery runner.
- Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` in `Release|x64` against installed V25 managed references without launching BricsCAD.
- Re-fetch current `origin/main` before every commit/merge, reconcile intervening work and verify the final diff contains only the reserved source/gate/claim surfaces.

## Coordination

PR `#1008` is merged and introduced both regressions; its older UI-polish claim is closed. The Quantity Insight contrast lane completed on a different file as PR `#1012`. Current open PR inventory is empty, and no `ACTIVE`/`BLOCKED` claim references the Recognition compact-shell or Quantity Settings XAML surfaces. The active Source Reconcile lane is unrelated and explicitly excluded.

## Result

- Claim-only PR `#1011` merged as `c8f3f5d067ad006fbca9fc6d2b450c4c2a06fb9d`.
- Claim expansion PR `#1013` merged as `72fa00cef4776fbc93da2b8d5c806d774166284d`; gate-refinement claim PR `#1014` merged as `acfb8195527a4efddd2767dde07eafa09a6acb90`.
- Implementation commit `5f280066ad03cb4e2230b65b8efada80e3c64e37` merged by PR `#1015` as `980f0d6c8024c836788bb0cbc66ac42aa733dcbb`.
- Both Recognition row lookups now explicitly call `System.Windows.Controls.Grid.GetRow`, so the XAML-generated `DataGrid Grid` member cannot shadow the WPF attached-property owner.
- PR `#1008` responsive Quantity Settings layout remains intact. Only the four canonical rule/developer labels were restored; the warning gate now checks both safety clauses without requiring obsolete punctuation.

## Validation actually executed

- Exact clean `main` SHA `980f0d6c8024c836788bb0cbc66ac42aa733dcbb` passed the new Recognition collision gate and all four affected Quantity Settings gates.
- The aggregate auto-discovery runner passed all `732` feature preflight gates on that exact SHA.
- Installed-reference `QS3D.BricsCAD.V25` `Release|x64` build succeeded with `0 warnings / 0 errors` against `C:\Program Files\Bricsys\BricsCAD V25 en_US`; BricsCAD was not launched.
- `git diff --check` passed. The implementation PR changed only the two reserved UI source files and two reserved static gates; Source Reconcile/`LOCAL-004` remained untouched.
- No GitHub Actions were dispatched, re-run or cancelled. No native BricsCAD runtime PASS is claimed.

## Completion condition

Satisfied: the claim-first two-call qualification, focused guard, four text-only contract restorations and punctuation-tolerant warning assertion are merged normally to current `main`; exact-main V25 compilation and all focused/aggregate gates pass; exact SHAs and truthful non-runtime/non-CI evidence are recorded.

# Work claim — V25 Recognition DataGrid/Grid row compile fix

- Status: `ACTIVE`
- Agent: `codex-remote-recognition-datagrid-row-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T16:03:00+07:00`
- Scope expanded: `2026-08-13T16:08:00+07:00` after the same PR `#1008` audit exposed four aggregate presentation-contract failures
- Gate refinement registered: `2026-08-13T16:11:00+07:00` after focused rerun proved the developer warning retained both safety clauses but the existing gate compared obsolete punctuation
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

## Completion condition

The claim-first two-call qualification, focused guard and four text-only contract restorations are merged normally to current `main`, installed-reference V25 compilation and all focused/aggregate gates pass, and this claim is marked `COMPLETED` with exact merge SHAs and truthful non-runtime/non-CI evidence.

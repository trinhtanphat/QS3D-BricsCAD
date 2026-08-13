# Work claim — V25 Recognition DataGrid/Grid row compile fix

- Status: `ACTIVE`
- Agent: `codex-remote-recognition-datagrid-row-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T16:03:00+07:00`
- Baseline main SHA: `9446d962fb31b3541110b934c88919d5a73e7a76`
- Priority: current-main V25 compilation is blocked after UI integration PR `#1008` because the Recognition XAML-generated `DataGrid Grid` member shadows the WPF `Grid` type at two attached-row getter call sites.

## Reserved scope

Qualify the two Recognition compact-shell attached-row getter calls so they bind to `System.Windows.Controls.Grid.GetRow` despite the generated `DataGrid` member named `Grid`. Add a focused static regression for this exact type/member shadowing boundary while preserving the responsive header/footer behavior.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.CompactShell.cs`
- `scripts/preflight-recognition-compact-shell-grid-row.py`
- read-only contract reference: `src/QS3D.BricsCAD.V25/UI/RecognitionWindow.xaml`
- this claim file

## Excluded scope

- Recognition engine, candidate/review/capture behavior, XAML layout redesign or command handlers
- Quantity Summary, Quantity Insight, Workspace, RightPanel or shared theme behavior
- Source Reconcile/`LOCAL-004`, Core semantics, V26, private/customer drawings, BricsCAD runtime, packaging/release/signing/installer or GitHub Actions

## Validation plan

- Run the focused member-shadowing regression and the aggregate feature preflight auto-discovery runner.
- Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` in `Release|x64` against installed V25 managed references without launching BricsCAD.
- Re-fetch current `origin/main` before every commit/merge, reconcile intervening work and verify the final diff contains only the reserved source/gate/claim surfaces.

## Coordination

PR `#1008` is merged and introduced the compact-shell file; the older UI-polish claim is closed. Current open PR inventory is empty, and no `ACTIVE`/`BLOCKED` claim references this compact-shell surface. The active Source Reconcile and Quantity Insight lanes are unrelated and explicitly excluded.

## Completion condition

The claim-first two-call qualification and focused guard are merged normally to current `main`, installed-reference V25 compilation and focused/aggregate gates pass, and this claim is marked `COMPLETED` with exact merge SHAs and truthful non-runtime/non-CI evidence.

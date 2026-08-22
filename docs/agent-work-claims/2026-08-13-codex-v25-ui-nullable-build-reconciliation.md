# Work claim — V25 UI nullable build reconciliation

- Status: `COMPLETED`
- Agent: `codex-v25-ui-nullable-build-reconciliation-20260813` (`/root`)
- Registered: `2026-08-13T11:05:00+07:00`
- Baseline main SHA: `2d3736ea53429890b6784d8309cf82a41b1ec051`
- Priority: P0 — restore the strict installed-reference V25 Release build without changing UI behavior or error-redaction policy.

## Confirmed compile blockers

- `QuantitySummaryWindow.xaml.cs` has seven `catch (Exception ex)` handlers after the completed stable-message redaction work stopped reading `ex`; warnings-as-errors produces CS0168.
- `WorkspacePanel.FooterContext.cs` uses `string.IsNullOrWhiteSpace(value) ? ... : value.Trim()` against net48 reference annotations that do not prove `value` non-null; warnings-as-errors produces CS8602.
- A diagnostic build with warnings-as-errors disabled compiles the same current source successfully, confirming there is no independent API/compiler error behind these eight diagnostics.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs`
- `scripts/preflight-quantity-summary-callback-error-containment.py` only if the existing exact catch token requires truthful alignment
- `scripts/preflight-workspace-footer-context.py` only if the existing null-normalization token requires truthful alignment
- this claim for closeout

The older still-active Workspace footer reservation is released in the same claim-only documentation PR because the newer `2026-08-12-1331` claim completed and superseded the exact file/behavior. No other active or blocked claim or open PR owns the two source files.

## Intended change

- Replace only the seven unused exception variable declarations with `catch (Exception)`; preserve catch boundaries, rollback/presentation state and all stable localized messages.
- Normalize the optional footer name through an explicit non-null local/guard before `Trim()`; preserve `—` for null/blank and exact presentation-only read behavior.
- Do not suppress nullable analysis, disable warnings-as-errors or edit project build settings.

## Validation and exclusions

- Run the two focused gates, UI/modeless/export gates touching the same surfaces, installed-reference V25 `Release|x64` strict build, aggregate preflight and `git diff --check`.
- No quantity calculations, reporting identity, XLSX, CAD locate, Workspace mutation, XAML/layout, Core model, BricsCAD runtime, private fixture, GitHub Actions, release or package work.

## Completion condition

The claim-only reservation is merged before source edits; strict V25 Release builds with zero warnings/errors; focused/aggregate gates pass; implementation and claim closeout are merged normally with exact SHAs and no force-push or Actions.

## Completion evidence

- Claim PR `#953` merged as `8418fdaf5005b1911269e2a418771d650905eaf1`. Implementation commit `03bd4cbeaa8d6e2e400a5922674841d062119408` merged by PR `#954` as `461a64eaec5e81d7a87bc375baa4e37252939d9c`.
- Installed-reference V25 `Release|x64` build passed with zero warnings and zero errors.
- Quantity callback containment, BQ export/modeless, Workspace footer/current-project/compact shell, HiDPI/premium UI and all 716 aggregate feature gates passed; `git diff --check` passed.
- UI behavior, stable localized messages and presentation-only footer semantics were preserved. No GitHub Actions, BricsCAD runtime, private fixture, release or package operation occurred.

# Work claim — BQ Summary/Detail preflight reconciliation

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + BricsCAD V25 agent)
- Registered: `2026-08-11T20:42:50+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: restore adapter build and aggregate source validation after several already-landed UI/Ribbon refactors without changing product behavior.

## Reserved scope

Reconcile four stale BQ gates with the already-landed `QuantitySummaryWindow` refresh-helper and footer contracts. Preserve the stronger Save-confirmation, current-project, mode-aware refresh, filtering and export ordering.

After the neighboring Right quantity, Project readiness and Ribbon claims reached `COMPLETED`, also fix the nullable adapter compile error and two stale exact-token gates left by those delivered commits. This remains integration-only follow-up to source already present on `main`.

## Expected surfaces

- `scripts/preflight-bq-export-freshness.py`
- `scripts/preflight-modeless-review-windows.py`
- `scripts/preflight-schedule-arithmetic.py`
- `scripts/preflight-ui-premium-layout.py`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs` — nullable-flow compile fix only
- `scripts/preflight-project-maintenance-actions.py`
- `scripts/preflight-ribbon-augmenter-panel-targets.py`
- this claim file for close-out

## Excluded scope

- No changes to `QuantitySummaryWindow.xaml`, `QuantitySummaryWindow.xaml.cs`, `Commands.cs`, Core reporting, quantity formulas, CAD locate behavior or user-visible feature behavior.
- No takeover of the active BQ detail/viewport, quantity-description, modeless-project-identity or Core schedule-reporting feature lanes.
- No redesign of Right quantity, Project Tools, Ribbon or Start Center; only the proven nullable compile repair and stale post-merge gate reconciliation are owned.
- No BricsCAD/private-DWG qualification, GitHub Actions, release, signing or packaging work.

## Validation plan

- Run the four focused gates and `scripts/preflight-all.py`.
- Run Core Release smoke and the BricsCAD V25 x64 Release build to detect integration regressions from current `main`.
- Run `git diff --check` and verify the final pushed commit against current `origin/main`.

## Coordination

The active BQ/modeless claims continue to own product source and user-facing feature behavior. The Right quantity, Project readiness and Ribbon reconciliation claims are now `COMPLETED`, releasing their delivered surfaces for this bounded integration repair. This lane owns only the compiler/gate fallout; it does not reinterpret their product contract.

## Completion condition

All seven focused gates, Core smoke, adapter V25 build and aggregate preflight pass on current `main`; the only product-source edit is the behavior-preserving nullable compile repair; the claim is marked `COMPLETED` with pushed commit and validation evidence.

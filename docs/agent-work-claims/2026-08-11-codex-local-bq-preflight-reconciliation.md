# Work claim — BQ Summary/Detail preflight reconciliation

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + BricsCAD V25 agent)
- Registered: `2026-08-11T20:42:50+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: restore aggregate source validation after several already-landed UI/Ribbon refactors without changing product behavior.

## Reserved scope

Reconcile four stale BQ gates with the already-landed `QuantitySummaryWindow` refresh-helper and footer contracts. Preserve the stronger Save-confirmation, current-project, mode-aware refresh, filtering and export ordering.

After the neighboring Project readiness and Ribbon claims reached `COMPLETED`, also reconcile two stale exact-token gates left by those delivered commits. This remains integration-only follow-up to source already present on `main`.

## Expected surfaces

- `scripts/preflight-bq-export-freshness.py`
- `scripts/preflight-modeless-review-windows.py`
- `scripts/preflight-schedule-arithmetic.py`
- `scripts/preflight-ui-premium-layout.py`
- `scripts/preflight-project-maintenance-actions.py`
- `scripts/preflight-ribbon-augmenter-panel-targets.py`
- this claim file for close-out

## Excluded scope

- No changes to `QuantitySummaryWindow.xaml`, `QuantitySummaryWindow.xaml.cs`, `Commands.cs`, Core reporting, quantity formulas, CAD locate behavior or user-visible feature behavior.
- No takeover of the active BQ detail/viewport, quantity-description, modeless-project-identity or Core schedule-reporting feature lanes.
- No redesign of Right quantity, Project Tools, Ribbon or Start Center; only stale post-merge gate reconciliation is owned.
- `QuantityInsightPanel.xaml.cs` and its compile/affinity hardening remain owned by the active Quantity Insight affinity claim.
- No BricsCAD/private-DWG qualification, GitHub Actions, release, signing or packaging work.

## Validation plan

- Run the four focused gates and `scripts/preflight-all.py`.
- Run Core Release smoke and the BricsCAD V25 x64 Release build to detect integration regressions from current `main`.
- Run `git diff --check` and verify the final pushed commit against current `origin/main`.

## Coordination

The active BQ/modeless and Quantity Insight affinity claims continue to own product source and user-facing feature behavior. The Project readiness and Ribbon reconciliation claims are now `COMPLETED`, releasing their gate fallout for this bounded integration repair. This lane does not reinterpret their product contract.

## Completion condition

All six focused gates, Core smoke and aggregate preflight pass on current `main`; no product source is changed; the claim is marked `COMPLETED` with pushed commit and validation evidence. Adapter V25 build is re-run when the separately owned Quantity Insight compile blocker is resolved.

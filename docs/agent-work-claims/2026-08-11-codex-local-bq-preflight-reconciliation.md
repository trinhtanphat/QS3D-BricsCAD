# Work claim — BQ Summary/Detail preflight reconciliation

- Status: `ACTIVE`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + BricsCAD V25 agent)
- Registered: `2026-08-11T20:42:50+07:00`
- Baseline main SHA: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`
- Priority: restore aggregate source validation after the already-landed BQ Summary/Detail and viewport-reveal refactor without changing product behavior.

## Reserved scope

Reconcile four stale feature gates with the already-landed `QuantitySummaryWindow` refresh-helper and footer contracts. Preserve the stronger Save-confirmation, current-project, mode-aware refresh, filtering and export ordering. This is integration-only follow-up to commits already present on `main`.

## Expected surfaces

- `scripts/preflight-bq-export-freshness.py`
- `scripts/preflight-modeless-review-windows.py`
- `scripts/preflight-schedule-arithmetic.py`
- `scripts/preflight-ui-premium-layout.py`
- this claim file for close-out

## Excluded scope

- No changes to `QuantitySummaryWindow.xaml`, `QuantitySummaryWindow.xaml.cs`, `Commands.cs`, Core reporting, quantity formulas, CAD locate behavior or any other product source.
- No takeover of the active BQ detail/viewport, quantity-description, modeless-project-identity or Core schedule-reporting feature lanes.
- No BricsCAD/private-DWG qualification, GitHub Actions, release, signing or packaging work.

## Validation plan

- Run the four focused gates and `scripts/preflight-all.py`.
- Run Core Release smoke and the BricsCAD V25 x64 Release build to detect integration regressions from current `main`.
- Run `git diff --check` and verify the final pushed commit against current `origin/main`.

## Coordination

The active BQ/modeless claims continue to own product source and user-facing feature behavior. This narrow lane owns only post-merge reconciliation of stale exact-token assertions after commits `3675084a` and `2c367e4d` landed without corresponding updates to these four gates.

## Completion condition

All four focused gates and the aggregate preflight pass on current `main`; no product source is changed; the claim is marked `COMPLETED` with pushed commit and validation evidence.

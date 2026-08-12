# Work claim — release #31 quantity locate validation-failure pre-clear reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-quantity-locate-preclear`
- Registered: `2026-08-12T10:42:00+07:00`
- Baseline main SHA: `79b0ef83ba160a04092b27774d64f76fc654edd7`
- Priority: release #31 reports `preflight-quantity-locate-validation-failure-clear.py` failing after BQ Follow3D became parity behavior for Summary and Detail modes.

## Reserved scope

Reconcile only `scripts/preflight-quantity-locate-validation-failure-clear.py`. Preserve Quantity Summary/Insight production source unchanged.

## Canonical evidence

- Summary selection pre-clear now triggers whenever `AutoRevealCheck` is enabled and a row is newly selected, regardless of Summary/Detail mode.
- Summary double-click pre-clear now runs when `AutoRevealCheck` is disabled, regardless of Summary/Detail mode.
- Explicit Locate, active-DWG affinity, class-handler ordering, empty-selection clear, canonical locate validation/selection/zoom and Insight behavior remain unchanged.
- The gate still requires obsolete `_detailMode` predicates.

## Excluded scope

No production UI changes, no broad selection clearing, no wrong-DWG clearing, and no unrelated #31 work.

## Completion condition

The gate tracks current Follow3D parity triggers while preserving pre-clear safety and canonical locate checks, is pushed to `main`, and this claim is closed with exact evidence.
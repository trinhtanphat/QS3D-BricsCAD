# Work claim — release #31 premium UI BQ Follow3D preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-ui-premium-quantity-follow3d`
- Registered: `2026-08-12T10:39:00+07:00`
- Baseline main SHA: `4e49bedf178f560b6fa97a3713a28f1cced3cf8c`
- Priority: release #31 still reports `scripts/preflight-ui-premium-layout.py` failing because Quantity Summary's footer/workflow copy changed to the current Bám 3D interaction while the gate retains an obsolete Detail/Summary sentence.

## Reserved scope

Reconcile only the Quantity Summary assertions in `scripts/preflight-ui-premium-layout.py`. Preserve XAML and code-behind unchanged.

## Canonical evidence

- `QuantitySummaryWindow.xaml` retains `BQ REVIEW`, Floor/Search/Category/Grid/Totals controls, column visibility handlers and export action.
- Follow3D is represented by `x:Name="AutoRevealCheck" Content="Bám 3D"`, `SelectionChanged="OnQuantityGridSelectionChanged"`, and `MouseDoubleClick="OnQuantityGridDoubleClick"`.
- Current footer explicitly documents `BÁM 3D: CLICK → LOCATE • TẮT BÁM 3D: DOUBLE-CLICK / ĐỊNH VỊ • EXPORT XLSX`.
- The gate still pins the obsolete `DETAIL: CLICK → 3D • SUMMARY: DOUBLE-CLICK → LOCATE • EXPORT XLSX` literal.

## Excluded scope

No XAML/code-behind changes, no UI redesign, and no weakening of Follow3D, explicit locate, export, theme or XAML well-formed checks.

## Completion condition

The premium gate tracks the current Follow3D structure/copy and is closed with exact implementation evidence.
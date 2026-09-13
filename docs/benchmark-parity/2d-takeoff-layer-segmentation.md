# 2D Takeoff layer segmentation

## Problem

`TakeoffQuantityEvidence2D` already records both `Zone` and `Layer`, but the inventory/estimate workflow previously grouped drawing evidence only by classification, zone, and unit. Two markups on different drawing layers could therefore collapse into one inventory row even though their evidence retained distinct layer provenance.

For CostX/Autodesk-style 2D takeoff this loses an important drawing organization dimension and makes layer-based review, filtering, and evidence reconciliation less reliable.

## Behavior

`AutodeskTakeoffWorkflow.BuildInventoryAndEstimate` now preserves layer segmentation for drawing evidence:

- grouping uses Classification + Zone + Layer + Unit;
- `TakeoffWorkflowLine` exposes the resulting `Layer`;
- deterministic ordering includes Layer between Zone and Unit;
- evidence on the same layer still aggregates normally;
- BIM quantities remain compatible and use an empty layer because `IfcQtoItem` has no drawing-layer dimension.

The formula and rate delegates keep their existing signatures. Formula evaluation still operates per grouped inventory row, and rate lookup still uses classification + unit.

## Compatibility

This is additive at the public model boundary. The existing seven-argument `TakeoffWorkflowLine` constructor remains available and initializes `Layer` to an empty string. A new overload accepts the layer explicitly.

Existing callers that never inspect layers continue to compile unchanged. Existing drawing evidence that uses an empty layer retains the previous aggregation behavior. Distinct non-empty layers now intentionally produce separate inventory rows.

## Workflow impact

The layer dimension now survives the path:

Drawing markup → calibrated quantity evidence → Package → Classification → Quantity grouping → Formula → Inventory → Estimate.

Splitting rows by layer does not change the total estimate when formula/rate functions are linear identity/rate functions; it prevents unrelated drawing-layer evidence from being merged and improves downstream traceability.
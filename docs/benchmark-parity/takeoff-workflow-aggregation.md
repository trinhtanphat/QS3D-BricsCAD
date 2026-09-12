# 2D Takeoff workflow aggregation

This note records the numeric and compatibility contract for the P0/P1 workflow that joins calibrated drawing evidence with BIM quantities before Formula → Inventory → Estimate publication.

## Workflow boundary

`AutodeskTakeoffWorkflow.BuildInventoryAndEstimate` accepts detached `TakeoffQuantityEvidence2D` rows from PDF/image takeoff plus `IfcQtoItem` rows from BIM. Rows are grouped by case-insensitive `Classification + Zone + Unit`. Drawing `Zone` and BIM `Storey` intentionally share the workflow location dimension used by the existing inventory/estimate surface.

The workflow validates every source row before publishing any result. Null rows are rejected explicitly; classification, location, unit and quantity continue through the existing model normalization/finite-value guards. Formula outputs and rate-provider outputs remain finite-value checked.

## Numeric stability

Measured quantity aggregation uses compensated summation rather than ordinary `Enumerable.Sum`. This matters for CostX/Autodesk-style packages that can combine very large BIM quantities with small calibrated drawing corrections. For example, `1e16 + 1 + 1` must publish `10000000000000002`, not silently lose both small evidence contributions before the formula stage.

The compensated measured value is the value supplied to the formula delegate, stored on `TakeoffWorkflowLine.MeasuredQuantity`, and used to derive the downstream formula quantity and estimate. Non-finite aggregate results fail closed.

## Determinism

Published rows are ordered case-insensitively by Classification, then Zone, then Unit. The Unit tie-break is required because one classification/location can legitimately contain separate length, area and count lines; enumeration order from source collections must not leak into package UX or snapshot comparisons.

## Compatibility

No public method signature, grouping key, drawing/BIM location mapping, formula callback contract, rate-provider contract, or estimate formula is changed. Existing callers receive the same rows for ordinary magnitudes. The intentional behavior changes are:

- small quantities are no longer lost when mixed with much larger values;
- null source rows fail with `ArgumentException` instead of an incidental null-reference failure;
- same classification/location rows with different units have deterministic unit ordering.

This is managed Core behavior and does not claim licensed BricsCAD runtime qualification.
# 2D Takeoff sheet evidence affinity

## Scope

`TakeoffSheetResult2D` is the provenance boundary between calibrated 2D measurement evidence and revision/package workflows. A result is valid only when every evidence row belongs to the exact logical sheet generation represented by the result.

## Admission contract

Construction now fails closed when an evidence row is null, belongs to another `SheetId`, carries a different sheet `Revision`, references a different `SourceReference`, or repeats a `MarkupId` already present in the same result. Sheet and revision identity remain case-insensitive; source references remain ordinal because they identify the concrete PDF/image source artifact.

Engine-produced results are unchanged: `CalibratedTakeoffEngine2D` already emits evidence using the admitted sheet id, revision, and source reference. Empty evidence collections remain valid for sheets with no markups.

## Why this matters

Before this guard, callers could manually combine a current `DrawingSheet2D` with stale or cross-sheet `TakeoffQuantityEvidence2D`. Revision overlay/compare and downstream package/inventory code would then receive individually well-formed rows with inconsistent provenance. The new boundary prevents stale drawing generations from entering Drawing → Package → Classification → Quantity → Formula → Inventory → Estimate.

## Compatibility

No public type, constructor signature, measurement unit, calibration rule, classification, zone/layer behavior, or revision comparison output changed for valid inputs. Callers that manually construct `TakeoffSheetResult2D` must now keep each evidence row aligned with the result sheet's `Id`, `Revision`, and `SourceReference`, and keep markup ids unique within the sheet result.

## Validation

`Qs2DTakeoffWorkflowSmoke` covers null evidence, cross-sheet evidence, stale revision, stale source, duplicate markup evidence, normal calibrated extraction, revision delta behavior, and downstream Drawing+BIM inventory/estimate aggregation.
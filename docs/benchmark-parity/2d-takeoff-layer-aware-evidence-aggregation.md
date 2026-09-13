# 2D takeoff layer-aware evidence aggregation

## Purpose

CostX/Autodesk-style 2D takeoff must preserve drawing-layer provenance through quantity aggregation. The primary AutodeskTakeoffWorkflow already separates rows by classification, zone, layer, and unit; the evidence aggregation API now follows the same boundary so downstream quantity, formula, inventory, estimate, revision, and snapshot consumers do not silently recombine measurements from different drawing layers.

## Behavior

`TakeoffQuantityAggregator2D.Aggregate` groups evidence by:

- Classification
- Zone
- Layer
- Unit

Evidence identity remains `SheetId + Revision + MarkupId`, compensated summation is unchanged, and `EvidenceCount` / `SheetCount` retain their previous meanings. Results are ordered deterministically by classification, zone, layer, and unit.

## Compatibility

The existing five-argument `TakeoffEvidenceAggregate2D` constructor is retained. It maps legacy callers to an empty layer. Existing evidence created without a logical drawing layer continues to aggregate exactly as before because empty-layer records remain in the same group.

Consumers that display or serialize aggregate identity should add the new `Layer` field. Consumers that intentionally ignore drawing layers can explicitly coalesce layer-aware aggregates after receiving them, rather than losing provenance inside the core aggregator.

## Validation

`Qs2DQuantityAggregationLayerSmoke` runs automatically with the smoke-test assembly and verifies that distinct layers remain distinct, quantities and evidence counts are preserved, multi-sheet evidence retains sheet counts, and legacy empty-layer construction remains compatible.

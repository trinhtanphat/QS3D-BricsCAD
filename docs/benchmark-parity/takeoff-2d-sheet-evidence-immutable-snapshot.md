# 2D Takeoff sheet evidence immutable snapshot

## Problem

`TakeoffSheetResult2D` is a provenance boundary between calibrated drawing markup and downstream revision/package/inventory/estimate workflows. Validation already rejected cross-sheet, stale-revision, stale-source, null, and duplicate-markup evidence at construction time, but retaining the caller's `IReadOnlyList` reference still allowed a mutable backing `List<T>` to change after validation.

## Behavior

The constructor now copies validated evidence into a `ReadOnlyCollection<TakeoffQuantityEvidence2D>`. Published result evidence therefore represents the exact validated sheet generation and cannot be changed by mutating the caller's collection later.

Order is preserved. Existing engine-produced results remain compatible. No schema, classification, formula, rate, zone/layer, or revision-compare semantics change for valid data.

## Compatibility and audit

Callers that intentionally mutated an input list after constructing `TakeoffSheetResult2D` must now construct a new result after the mutation so provenance is validated again. This is intentional fail-closed behavior and prevents post-validation evidence substitution from entering Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate.

`scripts/preflight-takeoff-evidence-snapshot.py` guards both the immutable snapshot and the existing provenance checks.

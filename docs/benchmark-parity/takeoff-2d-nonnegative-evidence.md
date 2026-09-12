# 2D Takeoff nonnegative evidence contract

## Benchmark scope

This hardening applies to CostX/Autodesk-style measured 2D takeoff evidence before it enters revision compare, package, classification, formula, inventory, and estimate workflows.

`TakeoffQuantityEvidence2D.Quantity` represents an observed count, length, or area quantity. Source evidence is therefore admitted only when the value is finite and nonnegative. A negative measured quantity now fails closed at construction with `ArgumentOutOfRangeException`.

## Compatibility

- `0` remains valid for imported or normalized zero-valued evidence.
- Positive finite quantities remain unchanged.
- `NaN` and infinities continue to fail through the existing finite-number guard.
- Signed revision differences remain supported by `RevisionMarkupDelta2D.QuantityDelta`; a decrease from 12 to 7 still produces `-5`.
- Importers, serializers, and external adapters that previously emitted negative source evidence must normalize the source model or reject it before creating `TakeoffQuantityEvidence2D`. Do not encode removals or credits as negative source takeoff evidence; represent revision decreases through previous/current evidence and revision delta semantics.

## Workflow effect

The change closes a provenance-boundary bypass where manually materialized evidence could inject a negative quantity into:

`Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`

Engine-generated count/length/area markup remains compatible because calibrated extraction already produces positive source quantities. Zones, layers, source handles, revision/source affinity, duplicate-markup protection, immutable sheet evidence snapshots, compensated aggregation, formula evaluation, and estimate calculation are otherwise unchanged.

## Validation

`TakeoffQuantityEvidenceNonNegativeSmoke` is auto-discovered through a module initializer and covers negative rejection, zero/positive compatibility, and preservation of signed negative revision deltas.

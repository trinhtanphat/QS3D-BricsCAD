# Commercial aggregate precision

Lane-Key: `issue-4840`
Runtime: `NOT_APPLICABLE`

## Purpose

Tender evaluated totals and progress gross-certified totals are bounded non-negative commercial aggregates. They must preserve every contribution whenever the complete exact aggregate is representable as `decimal`; a transient pairwise precision loss is not a valid reason to reject a representable final total.

## Deterministic regression

The canonical high-dynamic-range sequence is:

`10000000000000000000000000000m + 0.5m + 0.5m = 10000000000000000000000000001m`

The product canonicalizes tender requirements and progress contract item codes, so A/B/C fixtures force the large contribution before the two half-unit contributions independent of caller enumeration order. Both surfaces must return the exact recovered total rather than throwing on the first half-unit.

Controls also cover ordinary totals, progress retention semantics, preserved line/result cardinality, caller-order independence, and fail-closed behavior when the final exact aggregate itself exceeds the `decimal` representable range.

## Production boundary

Reuse the bounded exact non-negative decimal accumulator in `CostDecimalMath`; do not introduce a second cost arithmetic model. Tender line cost and progress certified line value multiplication remain guarded by the existing precision-preserving multiplication helper. The complete set of admitted non-negative line values is accumulated exactly before conversion back to `decimal`.

Tender missing-item handling, completeness/ranking/tie-break rules, currency validation and canonical snapshots remain unchanged. Progress quantity clipping, rejected/remaining quantities, retention calculation, deterministic item ordering and traversal Count-integrity guards remain unchanged.

## Validation

Run the auto-discovered commercial aggregate source guard, deterministic Core smoke suite, Core build, and repository-required protected `preflight + core` checks. No licensed BricsCAD runtime or private DWG is required or claimed for this deterministic Core/commercial correctness lane.

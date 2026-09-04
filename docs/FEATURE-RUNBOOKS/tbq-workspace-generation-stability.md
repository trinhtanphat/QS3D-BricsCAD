# TBQ workspace generation stability

## Scope

`TbqProjectWorkspaceState` publishes commercial bill-item and build-up-rate snapshots. Countable inputs are authoritative-count sources, so cardinality stability alone is insufficient: a same-count source must not be able to replace, reorder, or mutate semantic content between admission and publication.

Runtime classification: `NOT_APPLICABLE / REMOTE_SAFE`. This contract is deterministic Core/commercial correctness and does not require licensed BricsCAD runtime evidence.

## Failure mode

Before issue #5562, `SnapshotBillItems` and `SnapshotBuildUpRates` rebounded known Count around their first traversal but immediately sorted/published that first snapshot. A source could keep Count unchanged while exposing a different semantic generation on a later enumeration. The top-level workspace therefore had a weaker contract than downstream hardened rate-reference/library and analysis inputs.

## Required contract

For an authoritative-count bill-item source, retain the admitted ordered snapshot and replay once before sorting/publication. Compare exact `ItemCode`, `Description`, `Unit`, `TradeCode`, `Quantity`, `UnitRate`, and `RateCode`. Replacement, reorder, null, content drift, cardinality drift, or Count drift must fail closed.

For an authoritative-count build-up source, replay once and compare exact `RateCode` plus `UnitRate`, the complete current semantic state of `BuildUpRateSnapshot`.

Unknown-count streaming inputs remain single-pass and are never replayed.

## Deterministic evidence

`scripts/preflight-tbq-workspace-generation-stability.py` is auto-discovered by shared CI. It locks replay-before-publication, authoritative-count-only invocation, ordered semantic equality for every published bill/build-up field, and Count fences around replay traversal observations.

An initial dedicated Core smoke was intentionally removed after Reservation v2 proved that its path was inside the earlier active C01 reservation #5550 for the entire `tests/QS3D.Core.SmokeTests/` prefix. C02 does not override or rename around that ownership boundary. Hosted `core` still exercises the existing repository smoke suite against the changed production source, while the carrier-owned focused preflight provides the deterministic regression contract for this specific generation-stability behavior.

Acceptance requires fresh exact-head shared/protected `preflight` and `core` success on the current PR candidate, latest-main reconciliation under repository policy, protected merge, and exact-main verification.

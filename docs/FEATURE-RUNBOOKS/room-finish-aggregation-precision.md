# Room Finish aggregation precision

Lane-Key: issue-4646
Runtime: NOT_APPLICABLE

## Contract

Room Finish schedule grouping may combine finite non-negative quantities with very different magnitudes. The builder must preserve small contributions when their eventual binary64 aggregate is representable instead of rejecting the first temporarily swallowed pairwise addend.

Each group keeps isolated compensated state for `LengthM`, `AreaM2`, and `PrimaryQuantity`. Contributions are validated before accumulation. Publication occurs only after the group has been fully traversed and each compensated total is finalized.

The stronger aggregation contract does **not** weaken the historical precision boundary: if the final compensated total itself cannot represent a material compensation (for example `2^53 + 1`), publication still fails closed. NaN, infinity, negative quantities, accumulator overflow, checked Count overflow, category/Family inconsistency, provenance and grouping semantics remain unchanged.

## Deterministic acceptance

`tests/QS3D.Core.SmokeTests/RoomFinishAggregationPrecisionSmoke.cs` proves:

- `1e16 + 1 + 1` publishes the representable `10000000000000002` aggregate;
- the same aggregate remains correct when small values sort before the large value;
- ordinary grouping, Count, category/Family/unit and provenance remain stable;
- a final unrepresentable `2^53 + 1` total remains fail-closed;
- invalid non-finite input remains fail-closed.

`scripts/preflight-room-finish-aggregation-precision.py` is auto-discovered by the aggregate feature-guard runner and pins compensated accumulation plus strict final representability.

## Validation

Run the focused source guard, Core Release build and deterministic smoke suite. For integration, require exact-head automatic branch CI, refresh/reconcile current protected `main`, then protected PR `preflight` + `core` success before expected-head merge and exact-main verification.

No licensed BricsCAD execution or `LOCAL_PASS` claim applies to this Core-only reporting package.

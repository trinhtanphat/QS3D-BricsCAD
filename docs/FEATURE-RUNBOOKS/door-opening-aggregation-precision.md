# Door / Opening aggregation precision

Lane-Key: `issue-4655`

## Contract

`DoorOpeningScheduleBuilder.Build` must preserve finite non-negative `OpeningAreaM2` contributions that are individually smaller than the running binary64 spacing when their eventual grouped total is representable. Group arithmetic therefore uses isolated compensated state rather than fail-fast pairwise assignment to the published row.

The compensation is not permission to silently round away commercial quantity. Finalization remains fail-closed when a non-zero compensation cannot be represented at the resulting magnitude, including the historical `2^53 + 1` boundary. Sum/compensation overflow and non-finite or negative inputs also remain failures.

All non-arithmetic schedule semantics remain unchanged: checked element `Count`, deterministic grouping/order, width/height/sill/thickness/material projection, Family/category validation, canonical host validation and `HostCount`, `ElementIds`, `HostIds`, and `SourceHandles` provenance.

## Deterministic regression

`DoorOpeningAggregationPrecisionSmoke` self-registers in the Core smoke executable and covers:

- large-first `1e16 + 1 + 1 -> 10000000000000002`;
- small-first ordering of the same values;
- ordinary grouping, host and provenance behavior;
- final-unrepresentable `2^53 + 1` refusal;
- non-finite stored area refusal.

Focused source contract:

```text
python scripts/preflight-door-opening-aggregation-precision.py
```

Full repository acceptance still requires the normal Shared CI `preflight` and `core` jobs on the exact current candidate before merge. Licensed BricsCAD runtime is not applicable to this deterministic Core reporting package.

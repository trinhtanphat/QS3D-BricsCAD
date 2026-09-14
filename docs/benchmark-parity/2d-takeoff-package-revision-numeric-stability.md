# 2D takeoff package revision numeric stability

## Contract

Sheet and package revision quantity deltas are signed evidence aggregates. They must use the same finite compensated accumulator as canonical Quantity reporting rather than a private numeric implementation.

The critical high-dynamic regression is the signed sequence `1e16, 1, 2, -1e-16`. A stale local compensation algorithm returns `10000000000000002`, while the canonical accumulator preserves the nearest representable result `10000000000000004`.

## Required behavior

- sheet and package aggregations share `QuantityReportMath.FiniteAccumulator`;
- non-finite inputs fail closed before publication;
- accumulator overflow fails closed;
- signed zero is canonicalized by the shared accumulator;
- Added/Removed/Changed/Unchanged evidence counts are unchanged by the numeric fix;
- deterministic sheet ordering and revision provenance remain independent from aggregation mechanics.

# Cost evidence known-Count no-overread

## Scope

This contract covers the two bounded cost-evidence materializers in Core:

- `RateBook` rate-item ingestion (maximum 10,000), and
- `FrozenEstimateProjection.Create` estimate-line projection (maximum 10,000).

A trusted collection `Count` is cardinality evidence. Traversal must not observe `Current` for an N+1 item after admitting Count=N, and pure streaming inputs must not observe the first item beyond the configured ceiling.

## Required ordering

For both materializers:

1. inspect supported Count surfaces before traversal and reject negative, conflicting, or oversized evidence;
2. use an explicit enumerator;
3. after each successful `MoveNext()`, evaluate the known-Count overrun guard and streaming ceiling before reading `Current`;
4. preserve existing null, duplicate, scope/identity, deterministic sorting and projection semantics;
5. reject under-yield against an admitted known Count;
6. re-read supported Count surfaces after exact traversal and fail closed if the known Count changed.

## Deterministic evidence

`CostEvidenceKnownCountNoOverreadSmoke` uses adversarial counted and streaming enumerables that separately record `MoveNext`, `Current`, and Count reads. It proves known-count overrun/no-overread, streaming ceiling/no-overread and honest counted controls on both cost evidence surfaces.

The auto-discovered source guard is `scripts/preflight-cost-evidence-known-count-no-overread.py`; it pins the required `MoveNext -> cardinality/ceiling -> Current` ordering and post-traversal Count rebinds.

## Validation

Run the feature guard and deterministic Core smoke suite. Protected Shared CI must pass current-candidate `preflight` and `core` before merge. This is deterministic Core-only evidence and does not imply licensed BricsCAD or `LOCAL_PASS` evidence.

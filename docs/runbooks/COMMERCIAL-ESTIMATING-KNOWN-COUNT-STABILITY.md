# Commercial estimating known-Count stability

## Scope

This runbook validates the bounded collection materializers that feed commercial estimating:

- `EstimatingPortfolio` estimating lines (maximum 10,000),
- `BulkRateAssignmentRequest.LineIds` (maximum 10,000), and
- `BulkRateAssignmentRequest.UnitRates` (maximum 256).

These boundaries are semantic evidence boundaries. When an input exposes a trusted collection `Count`, traversal must neither observe nor retain a `Current` beyond that admitted count, and the Count surfaces must remain stable through exact traversal.

## Required invariants

1. Read all supported Count surfaces before traversal; reject negative, conflicting, or oversized evidence before enumeration.
2. Use explicit enumerators. After each `MoveNext()` succeeds, apply the known-Count overrun guard and the streaming ceiling before reading `Current`.
3. Reject under-yield when traversal completes before an admitted known Count.
4. Re-read all supported Count surfaces after exact traversal and reject post-traversal Count drift.
5. Preserve the existing 10,000-line and 256-unit-rate ceilings for pure streaming inputs, rejecting the overflow item before reading `Current`.
6. Preserve null, duplicate line id, duplicate unit assignment, token/provenance validation, and deterministic portfolio ordering.

## Deterministic evidence

`CommercialEstimatingKnownCountStabilitySmoke` uses adversarial enumerables that count `MoveNext`, `Current`, Count reads, and enumeration starts. It covers known-count overrun/no-overread, under-yield, post-traversal drift, conflicting Count surfaces, pure streaming overflow for selected ids and unit rates, and honest counted controls.

The auto-discovered source guard is:

```text
scripts/preflight-commercial-estimating-known-count-stability.py
```

It locks the ordering of the no-overread guards relative to `Current`, post-traversal Count rebinds, and required smoke evidence.

## Validation

Run the feature guard and deterministic Core smoke suite. Protected Shared CI must pass both `preflight` and `core` on the current candidate before merge. This is a Core-only contract and does not create or imply licensed BricsCAD `LOCAL_PASS` evidence.

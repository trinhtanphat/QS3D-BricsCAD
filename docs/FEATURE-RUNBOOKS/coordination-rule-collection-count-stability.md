# Coordination rule collection Count stability

## Scope

This runbook qualifies the repository-safe Core boundary for `CoordinationRuleCollectionContract.MaterializeBounded<T>`. It does not require BricsCAD, a private DWG, or licensed-host evidence.

## Defect boundary

The materializer admits deterministic cardinality from `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection` before traversing caller-controlled input. Cardinality is part of the integrity contract: a counted source must not be accepted if that evidence changes while the source is being enumerated.

The hardened contract therefore:

1. reads all supported Count surfaces before enumeration;
2. rejects negative, conflicting, or over-limit pre-traversal evidence fail-closed;
3. rejects the first observed item beyond an admitted deterministic Count without retaining it;
4. rejects under-yield when exact traversal completes below the admitted Count;
5. re-reads supported Count surfaces after traversal and rejects drift, negative values, or conflicts before returning the snapshot;
6. retains the independent 10,000-observation ceiling for inputs with no deterministic Count surface.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/CoordinationRuleCollectionCountStabilitySmoke.cs` is auto-executed through its module initializer and covers:

- generic `ICollection<T>` Count drift;
- `IReadOnlyCollection<T>` Count drift;
- non-generic `ICollection` Count drift;
- negative post-traversal Count;
- conflicting post-traversal interface Counts;
- known-count under-yield and overrun;
- stable counted input;
- pure streaming input.

`scripts/preflight-coordination-rule-collection-count-stability.py` pins source ordering so Count is bound before traversal, rebound after exact traversal, and checked before the materialized snapshot is returned.

## Repository-safe validation

Run the normal shared branch/PR CI. The candidate is merge-eligible only when current protected `preflight` and `core` contexts are terminal `SUCCESS`, the branch is reconciled to current `main`, and the expected-head SHA still matches.

No licensed BricsCAD `LOCAL_PASS` is implied by this package.

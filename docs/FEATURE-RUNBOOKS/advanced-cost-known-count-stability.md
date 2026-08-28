# Advanced cost deterministic Count stability

Issue: #4365  
Lane-Key: `issue-4365`  
Runtime: `NOT_APPLICABLE` — Core cost-domain correctness only.

## Defect boundary

Advanced-cost collection snapshots already bind deterministic Count evidence from generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` before traversal. They also reject first over-yield before validating the unexpected item and reject final observed-cardinality mismatch. The missing invariant is post-traversal stability: caller-controlled enumeration may mutate one or more Count surfaces while still yielding exactly the initially advertised cardinality.

## Required two-phase contract

For every advanced-cost consumer that admitted deterministic Count evidence:

1. bind all available deterministic Count surfaces before enumeration;
2. reject negative, conflicting, or over-limit metadata before traversal;
3. retain first-overrun-before-item-validation behavior and the independent 10,000-entry streaming ceiling;
4. after a successful traversal, first retain the existing initial Count-versus-observed-cardinality check;
5. before sorting, result construction, or publication, re-read every deterministic Count surface from the original enumerable;
6. reject newly negative, conflicting, over-limit, or changed Count evidence;
7. accept stable honest multi-interface sources and pure streaming `IEnumerable<T>` sources with no deterministic Count contract.

The shared helper is `AdvancedCostCollectionContract.RequireKnownCountStableAfterTraversal`. `AdvancedCostManagement.cs` and `DeepCostWorkflows.cs` must route every consumer of `AdvancedCostCollectionContract.TryGetKnownCount` through that post-traversal helper.

## Deterministic regression

`AdvancedCostKnownCountStabilitySmoke` exercises a cardinality-matching source whose Count changes only after enumeration, a multi-interface source that becomes conflicting only after traversal, a non-generic Count drift case, an honest stable multi-interface control, and a pure streaming control.

`preflight-advanced-cost-known-count-stability.py` is auto-discovered by the aggregate feature guard and locks the shared helper plus consumer wiring across both cost-domain source files.

## Acceptance

The source package is complete only when exact-head branch CI and the current protected PR candidate both report `preflight` and `core` SUCCESS. Merge uses the repository's expected-head protected PR path, followed by exact-main verification. No licensed BricsCAD runtime claim applies.

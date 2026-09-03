# Advanced cost deterministic Count stability

Issues: #4365, #4792  
Lane-Key: `issue-4792` for the traversal-wide follow-up  
Runtime: `NOT_APPLICABLE` — Core cost-domain correctness only.

## Defect boundary

Advanced-cost collection snapshots bind deterministic Count evidence from generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` before traversal. #4365 added first-over-yield protection plus post-traversal Count rebinding, but `AdvancedCostManagement.cs` still crossed caller-controlled `MoveNext()` boundaries without rereading the admitted Count before and after each move. A counted source could therefore expose transient growth, shrink, negative, or conflicting Count evidence at the successful-move boundary and restore the admitted Count before final validation.

## Required traversal-wide contract

For every advanced-cost consumer that admitted deterministic Count evidence:

1. bind all available deterministic Count surfaces before enumeration;
2. reject negative, conflicting, or over-limit metadata before traversal;
3. immediately before each caller-controlled `MoveNext()`, re-read every admitted Count surface and require exact equality with admission;
4. after every successful `MoveNext()`, re-read Count again **before** cap/overrun checks and before `Current`;
5. retain first-overrun-before-item-validation behavior and the independent 10,000-entry streaming ceiling;
6. after traversal, retain the initial Count-versus-observed-cardinality check and post-traversal Count rebind before sorting, result construction, or publication;
7. accept stable honest multi-interface sources and pure streaming `IEnumerable<T>` sources with no deterministic Count contract.

`AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal` owns the pre/post-`MoveNext` rebound. `RequireKnownCountStableAfterTraversal` retains final observed-cardinality validation and delegates to the same Count reread logic.

The traversal-wide contract applies to all shared-contract materializers in `AdvancedCostManagement.cs`: rate build-up components, historical records, tender quote lines, tender requirements, tender bids, progress contract items, and progress claim lines. Existing `DeepCostWorkflows.cs` post-traversal coverage from #4365 remains pinned and is not weakened by #4792.

## Deterministic regression

`AdvancedCostKnownCountStabilitySmoke` keeps the historical final-state regressions: post-enumeration drift, multi-interface conflict, non-generic drift, honest stable multi-interface controls, and pure streaming controls.

`AdvancedCostTransientCountSmoke` adds move-boundary hostile collections whose next Count observation changes after a successful `MoveNext()`. Growth, shrink, and negative Count cases must be rejected with zero `Current` reads, while stable counted and streaming controls continue to succeed.

`preflight-advanced-cost-known-count-stability.py` is auto-discovered by the aggregate feature guard. It retains #4365 assertions across both cost-domain source files and additionally pins `rebind -> MoveNext -> rebind -> Current` ordering for every `AdvancedCostManagement.cs` materializer covered by #4792.

## Acceptance

The source package is complete only when exact-head branch CI and the current protected PR candidate both report `preflight` and `core` SUCCESS. Merge uses the repository's expected-head protected PR path, followed by exact-main verification. No licensed BricsCAD runtime claim applies.

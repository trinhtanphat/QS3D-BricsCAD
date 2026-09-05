# Advanced cost deterministic Count stability

Issues: #4365, #4792, #5686  
Current semantic-generation follow-up: `issue-5686`  
Runtime: `NOT_APPLICABLE` — Core cost-domain correctness only.

## Defect boundary

Advanced-cost collection snapshots bind deterministic Count evidence from generic `ICollection<T>`, `IReadOnlyCollection<T>`, and non-generic `ICollection` before traversal. #4365 added first-over-yield protection plus post-traversal Count rebinding, and #4792 fenced every caller-controlled `MoveNext()` boundary. That protects cardinality metadata, but several `AdvancedCostManagement.cs` consumers still published the first semantic generation without proving that a stable-count source returned the same business data when replayed. A hostile or mutable source could therefore keep Count unchanged while replacing historical records, tender data, or progress-claim data across enumerator generations.

## Required traversal-wide contract

For every advanced-cost consumer that admitted deterministic Count evidence:

1. bind all available deterministic Count surfaces before enumeration;
2. reject negative, conflicting, or over-limit metadata before traversal;
3. immediately before each caller-controlled `MoveNext()`, re-read every admitted Count surface and require exact equality with admission;
4. after every successful `MoveNext()`, re-read Count again **before** cap/overrun checks and before `Current`;
5. retain first-overrun-before-item-validation behavior and the independent 10,000-entry streaming ceiling;
6. after traversal, retain the initial Count-versus-observed-cardinality check and post-traversal Count rebind before sorting, result construction, or publication;
7. for materializers covered by #5686, replay a known-count source before publication/evaluation and compare the complete immutable business state in admitted order; reject any semantic generation drift even when Count remains stable;
8. keep pure streaming `IEnumerable<T>` inputs single-pass because they did not advertise a deterministic Count contract;
9. accept stable honest multi-interface and stable known-count sources without requiring object-reference identity.

`AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal` owns the pre/post-`MoveNext` rebound. `RequireKnownCountStableAfterTraversal` retains final observed-cardinality validation. `RequireStableKnownGeneration` owns the #5686 semantic generation replay and is a no-op for sources without an admitted Count.

The #5686 replay applies to historical records, tender quote lines, tender requirements, tender bids, progress contract items, and progress claim lines. Rate build-up components already had equivalent replay protection, while `DeepCostWorkflows.cs` has its own generation-stability fences; those existing contracts are not weakened.

## Deterministic regression

`AdvancedCostKnownCountStabilitySmoke` retains the historical Count-drift regressions and now adds stable-Count generation-switch sources for all six #5686 boundaries. Each source reports the same Count but returns different semantic state from its second enumerator; publication/evaluation must fail with the semantic generation replay diagnostic. Stable known-count controls remain accepted.

The same smoke also uses a single-pass streaming source to prove the new helper does not replay unknown-count inputs. `AdvancedCostTransientCountSmoke` continues to pin successful-move Count changes before `Current`.

`preflight-advanced-cost-known-count-stability.py` is auto-discovered by the aggregate feature guard. It retains the earlier Count fencing assertions and additionally requires the shared semantic replay helper, all six semantic-state comparators/consumers, the generation-switch regressions, and the streaming single-pass control.

## Acceptance

This package is REMOTE_SAFE. Source acceptance requires fresh exact-head Shared CI and the current protected PR candidate to report `preflight` and `core` SUCCESS, followed by expected-head merge and exact protected-main verification. No licensed BricsCAD runtime or `LOCAL_PASS` claim applies.

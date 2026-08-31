# Commercial collection Count stability and Current no-overread

## Scope

This runbook qualifies deterministic Core integrity for caller-controlled Commercial collections. It covers `CommercialAuditLog.AppendBatch` and the reusable `CommercialGuard.Snapshot<T>` path used by commercial provenance snapshots. No BricsCAD host, private DWG, or licensed runtime is required.

## Defect boundary

Both materializers can receive enumerable objects that also expose deterministic Count through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`. Pre-traversal Count is admission evidence, not a hint. Prior hardening added exact-cardinality, post-traversal Count stability, explicit enumeration, and overrun-before-Current ordering. The remaining gap was the successful `Current` boundary itself: after reading `enumerator.Current`, each materializer began null/identity validation or snapshot accumulation before rebinding the admitted Count. A caller-controlled `Current` getter could therefore change Count and have semantic acceptance begin before the next traversal-edge check observed the drift.

The hardened contract therefore:

1. binds all supported Count surfaces before traversal;
2. rejects negative/conflicting/over-limit admission evidence before processing items;
3. uses explicit enumeration and orders each admitted step as `Count -> MoveNext -> Count -> admitted-Count overrun guard -> independent capacity guard -> Current -> Count -> semantic acceptance`;
4. rejects the first item beyond an admitted deterministic Count without observing its `Current` value;
5. after every successful `Current`, rebinds the exact admitted Count before null checks, duplicate-state mutation, snapshot accumulation, or any other returned-item acceptance;
6. rejects under-yield after exact traversal;
7. rebinds supported Count surfaces after traversal and rejects drift, negative values or interface conflicts before publication/return;
8. keeps audit batch mutation atomic on Count overrun, Current-induced drift, and post-traversal evidence failure;
9. preserves independent streaming ceilings when the source exposes no deterministic Count surface.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/CommercialCountStabilitySmoke.cs` preserves the historical regression matrix for generic/read-only/non-generic post-traversal drift, negative/conflicting evidence, under-yield/overrun atomicity, stable counted inputs, and pure streaming controls.

`tests/QS3D.Core.SmokeTests/CommercialCountNoOverreadSmoke.cs` independently counts `MoveNext` and `Current` observations. It proves both shared paths for Count=1/yield=2 and Count=0/yield=1: the boundary `MoveNext` is observed, but `Current` N+1 is never read.

`tests/QS3D.Core.SmokeTests/CommercialCurrentCountAcceptanceSmoke.cs` uses hostile counted enumerators whose first valid `Current` access exposes a one-observation Count drift while returning a null item. The required failure is the Count-stability `InvalidOperationException` before ordinary null/item acceptance, for both audit batches and revision snapshots. The audit regression also proves no partial event publication, and stable counted controls remain accepted.

`scripts/preflight-commercial-count-stability.py` pins explicit-enumerator ordering in both production paths, including the post-Current Count rebound, rejects caller-controlled `foreach`, preserves final Count rebinding before `_events.AddRange`/immutable return, and requires all three regression suites.

## Repository-safe validation

Run normal Shared Branch and Integration CI on the exact branch head and again on the protected PR candidate. Merge only when current-main freshness is satisfied and both required `preflight` and `core` contexts are terminal `SUCCESS` for the exact candidate.

This package does not claim licensed BricsCAD runtime, private-DWG evidence, or `LOCAL_PASS`.

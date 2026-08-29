# Commercial collection Count stability and Current no-overread

## Scope

This runbook qualifies deterministic Core integrity for caller-controlled Commercial collections. It covers `CommercialAuditLog.AppendBatch` and the reusable `CommercialGuard.Snapshot<T>` path used by commercial provenance snapshots. No BricsCAD host, private DWG, or licensed runtime is required.

## Defect boundary

Both materializers can receive enumerable objects that also expose deterministic Count through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`. Pre-traversal Count is admission evidence, not a hint. Prior #4437 added exact-cardinality and post-traversal Count stability, but the materializers still used C# `foreach`; `foreach` evaluates `IEnumerator.Current` before entering the loop body. A Count=N source could therefore expose caller-controlled Current N+1 before the body-level overrun guard rejected it.

The hardened contract therefore:

1. binds all supported Count surfaces before traversal;
2. rejects negative/conflicting/over-limit admission evidence before processing items;
3. uses explicit enumeration and orders each step as `MoveNext -> admitted-Count overrun guard -> independent capacity guard -> Current`;
4. rejects the first item beyond an admitted deterministic Count without observing its `Current` value;
5. rejects under-yield after exact traversal;
6. rebinds supported Count surfaces after traversal and rejects drift, negative values or interface conflicts before publication/return;
7. keeps audit batch mutation atomic on Count overrun and post-traversal evidence failure;
8. preserves independent streaming ceilings when the source exposes no deterministic Count surface.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/CommercialCountStabilitySmoke.cs` preserves the #4437 regression matrix for generic/read-only/non-generic post-traversal drift, negative/conflicting evidence, under-yield/overrun atomicity, stable counted inputs, and pure streaming controls.

`tests/QS3D.Core.SmokeTests/CommercialCountNoOverreadSmoke.cs` auto-runs through a module initializer and independently counts `MoveNext` and `Current` observations. It proves both shared paths for Count=1/yield=2 and Count=0/yield=1: the boundary `MoveNext` is observed, but `Current` N+1 is never read. The instrumented source is configured to throw if an unadmitted `Current` is accessed, making a reintroduction of `foreach` deterministic.

`scripts/preflight-commercial-count-stability.py` pins explicit-enumerator ordering in both production paths, rejects caller-controlled `foreach`, preserves post-traversal Count rebinding before `_events.AddRange`/immutable return, and requires both regression suites.

## Repository-safe validation

Run normal Shared Branch and Integration CI on the exact branch head and again on the protected PR candidate. Merge only when current-main freshness is satisfied and both required `preflight` and `core` contexts are terminal `SUCCESS` for the exact candidate.

This package does not claim licensed BricsCAD runtime, private-DWG evidence, or `LOCAL_PASS`.

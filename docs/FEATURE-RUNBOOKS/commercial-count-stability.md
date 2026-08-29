# Commercial collection Count stability

## Scope

This runbook qualifies deterministic Core integrity for caller-controlled Commercial collections. It covers `CommercialAuditLog.AppendBatch` and the reusable `CommercialGuard.Snapshot<T>` path used by commercial provenance snapshots. No BricsCAD host, private DWG, or licensed runtime is required.

## Defect boundary

Both materializers can receive enumerable objects that also expose deterministic Count through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`. Pre-traversal Count is admission evidence, not a hint: accepting an object after that evidence changes during enumeration would publish or return a snapshot built from stale cardinality provenance.

The hardened contract therefore:

1. binds all supported Count surfaces before traversal;
2. rejects negative/conflicting/over-limit admission evidence before processing items;
3. rejects the first observed item beyond an admitted deterministic Count before retaining it;
4. rejects under-yield after exact traversal;
5. rebinds supported Count surfaces after traversal and rejects drift, negative values or interface conflicts before publication/return;
6. keeps audit batch mutation atomic when post-traversal evidence is invalid;
7. preserves independent streaming ceilings when the source exposes no deterministic Count surface.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/CommercialCountStabilitySmoke.cs` auto-runs through a module initializer and covers:

- generic, read-only, and non-generic audit-batch Count drift;
- negative and conflicting post-traversal audit Count evidence;
- audit under-yield and overrun with atomic preservation of the existing log;
- generic, read-only, non-generic, negative, and conflicting Count drift for `CommercialAuditRecord.SourceRevisions`, exercising the shared snapshot contract;
- stable counted inputs;
- pure streaming controls.

`scripts/preflight-commercial-count-stability.py` pins the required ordering so post-traversal Count validation occurs before `_events.AddRange` and before immutable snapshot return.

## Repository-safe validation

Run normal Shared Branch and Integration CI on the exact branch head and again on the protected PR candidate. Merge only when current-main freshness is satisfied and both required `preflight` and `core` contexts are terminal `SUCCESS` for the exact candidate.

This package does not claim licensed BricsCAD runtime, private-DWG evidence, or `LOCAL_PASS`.

# Revision snapshot persistence cardinality qualification

## Scope

Issue #5482 closes the gap between revision snapshot capture and public persistence admission. `RevisionSnapshotDetacher` already bounded snapshot element and nested collection cardinality at 100,000 entries; `RevisionSnapshotStore.Save` historically accepted caller-constructed snapshots without that bound, and `Load` could return parsed state without explicitly applying the same supported cardinality invariant.

This package is deterministic Core persistence/integrity work. It does not require or claim licensed BricsCAD runtime evidence.

## Required invariant

- One production definition owns the 100,000-element and 100,000-entry-per-nested-collection limits.
- Capture retains its existing fail-closed mutation/count behavior.
- `RevisionSnapshotStore.Save` applies shared cardinality admission before path resolution, temporary-file creation, replacement checks, serialization, or any primary/backup publication side effect.
- `RevisionSnapshotStore.Load` applies the same supported cardinality before returning parsed state.
- Persistence-bound cardinality failures are `InvalidDataException` so existing backup-fallback recovery classification remains intact.
- Existing canonical identity, XML-character, UTC timestamp, canonical category/number, duplicate identity, finite quantity, 64 MiB file bound, backup fallback, and atomic replacement contracts remain unchanged.

## Deterministic regression

`tests/QS3D.Core.SmokeTests/RevisionSnapshotCardinalitySmoke.cs` validates public behavior:

1. exactly 100,000 nested properties save and load successfully;
2. 100,001 nested properties fail before primary/backup publication;
3. 100,001 elements fail before primary publication;
4. a structurally valid revision file containing 100,001 nested properties fails closed on `Load`.

`scripts/preflight-revision-snapshot-cardinality.py` is auto-discovered by aggregate preflight and locks shared production constants, Save/Load admission ordering, public-behavior smoke presence, and mutation controls for removed Save/Load admission.

## Validation

For the exact candidate SHA:

```text
python scripts/preflight-revision-snapshot-cardinality.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Shared CI must then report protected `preflight` and `core` terminal `SUCCESS` on the current PR candidate. Reconcile with latest protected `main` non-force if strict freshness requires it, then merge only through the protected PR path with the exact expected head SHA.

## Failure interpretation

Any over-limit state accepted by Save/Load, any cardinality check moved after publication/return, any divergence between capture and persistence limits, or any regression in existing persistence validation is a release blocker for this carrier. Do not increase the limit or remove the guard merely to satisfy CI.

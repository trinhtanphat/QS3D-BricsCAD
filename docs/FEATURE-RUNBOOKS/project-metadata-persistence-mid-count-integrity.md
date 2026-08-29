# Project metadata persistence mid-traversal Count integrity

Issue: #4627  
Lane-Key: `issue-4627`

## Purpose

`ProjectMetadataDictionary.ReplacePersistenceState(...)` consumes persistence-owned metadata from caller-controlled enumerable state and publishes it atomically only after the complete replacement has been validated. Known collection cardinality is part of that admission contract, not merely a final diagnostic.

## Defect boundary

The existing persistence path already bound generic `ICollection<T>`, `IReadOnlyCollection<T>` and non-generic `ICollection` Count evidence at admission, rejected Count overrun before `Current`, required exact counted cardinality after traversal, and rebound Count before publication. It still used `while (enumerator.MoveNext())`, so a counted source could change its admitted Count after a prior `Current` and receive another caller-controlled `MoveNext` before the drift was noticed. A `MoveNext` implementation could also mutate Count and expose the corresponding `Current` before final rebound.

That is an integrity gap because hostile input receives additional advancement or value observation after its cardinality contract is already invalid.

## Production contract

For counted persistence inputs:

1. Bind all supported known Count surfaces before traversal and keep the existing negative, conflicting and 10,000-entry admission failures.
2. Revalidate the admitted known Count immediately **before every `MoveNext`**.
3. After successful `MoveNext`, revalidate Count again **after successful `MoveNext`** and **before `Current`**.
4. Preserve the admitted-Count overrun and independent 10,000-entry bound before reading `Current`.
5. Preserve null-key, case-insensitive duplicate-key, value normalization and reserved-metadata validation semantics.
6. Revalidate Count after traversal, require exact counted cardinality, and retain the final independent Count rebound before publication.
7. Publish only after all validation succeeds, so every Count-integrity failure remains atomic with respect to the previously stored metadata dictionary.

Pure streaming `IEnumerable<KeyValuePair<string,string>>` inputs remain supported because they expose no deterministic Count contract.

## Deterministic regression

`ProjectMetadataPersistenceMidCountIntegritySmoke` uses hostile enumerable implementations to prove:

- Count drift triggered by the first `Current` is rejected before the next `MoveNext`;
- Count drift triggered inside a successful `MoveNext` is rejected before the corresponding `Current`;
- a cross-interface Count conflict appearing after `Current` is rejected before further advancement;
- a negative Count appearing after `Current` is rejected before further advancement;
- all failure cases leave the seeded metadata dictionary unchanged;
- stable generic/read-only/non-generic Count evidence remains accepted;
- pure streaming input remains accepted.

The focused source guard pins the exact `Count stability -> MoveNext -> Count stability -> Count/cap guards -> Current` ordering. The pre-existing persistence Count-stability guard is reconciled to the explicit loop while retaining its post-traversal rebound and atomicity requirements.

## Runtime boundary

Licensed BricsCAD runtime is not applicable. This package is deterministic Core persistence integrity and must not be reported as `LOCAL_PASS`.

# Duplicate-detection options snapshot integrity

## Scope

This Core-only contract protects both `DuplicateDetectionService.Detect` overloads from caller-controlled mutation of a mutable `DuplicateDetectionOptions` object while the input enumerable is being materialized.

## Invariant

The operation must capture an immutable policy snapshot before invoking caller-controlled `GetEnumerator`, `MoveNext`, or `Current`. Classification after materialization must consume only that admitted snapshot. The original options object may be changed by the caller after admission without changing the operation already in progress.

The snapshot includes:

- `CoordinateToleranceM`;
- `RequireSameDisciplineForGeometry`;
- `RequireSameCategoryForGeometry`;
- `EnableSemanticIdentity`.

The captured tolerance is validated for finite, non-negative input before enumeration starts. Invalid initial tolerance therefore fails before any caller enumeration side effect.

## Preserved contracts

This correction does not change the existing duplicate-detection input traversal contract. Candidate and element overloads retain known-Count conflict/negative/limit checks, pre/post-`MoveNext` Count rebounds, post-`Current` Count rebound, the 500-element input limit, the 10,000-result limit, duplicate element-id rejection, deterministic pair ordering, and pure-streaming support.

Default options remain equivalent to a newly constructed `DuplicateDetectionOptions`. Stable explicit options keep existing exact, near, classification, and semantic-identity behavior.

## Deterministic regression

`DuplicateDetectionOptionsSnapshotSmoke` uses a pure-streaming enumerable that mutates the original options object on its first `MoveNext`. It proves that traversal cannot widen tolerance, enable semantic identity, or relax category matching after policy admission. It also proves invalid initial tolerance fails before `GetEnumerator` and that stable options remain accepted.

## Runtime boundary

No BricsCAD/native runtime is required. This is deterministic `QS3D.Core` coordination correctness and is eligible for remote CI evidence. It must not be represented as licensed BricsCAD `LOCAL_PASS` evidence.

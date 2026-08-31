# Project metadata persistence known-Count no-overread

## Scope

This runbook covers the deterministic Core persistence boundary in `ProjectMetadataDictionary.ReplacePersistenceState`, where caller-controlled metadata entries are snapshotted before atomic replacement of project metadata.

Runtime classification: `NOT_APPLICABLE`. No licensed BricsCAD host or private DWG is required for this Core integrity contract.

## Correctness contract

When the input exposes deterministic Count evidence through generic `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`, all available Count surfaces must be non-negative, mutually consistent, and no greater than the 10,000-entry persistence limit before traversal begins.

Traversal must be explicit. After every successful `MoveNext()` the implementation must:

1. reject an entry beyond the admitted deterministic Count;
2. reject traversal beyond the independent 10,000-entry safety ceiling;
3. only then observe `IEnumerator.Current`;
4. validate key/null/duplicate semantics and retain the entry in the isolated snapshot.

This ordering matters because C# `foreach` evaluates `IEnumerator.Current` before entering the loop body. A loop-body Count guard therefore cannot guarantee that caller-controlled N+1 `Current` was never observed.

After normal enumeration ends, a deterministic Count input must have yielded exactly the admitted cardinality. Supported Count evidence must then be rebound and remain present, valid, mutually consistent, and equal to the initially admitted Count before the isolated snapshot is validated and published atomically.

## Deterministic acceptance

Run the registered Core smoke suite and the auto-discovered source guard:

- `tests/QS3D.Core.SmokeTests/ProjectMetadataKnownCountOverrunSmoke.cs`
- `scripts/preflight-project-metadata-known-count-overrun.py`

The adversarial smoke independently records `MoveNext` and `Current` access. For Count=1/yield=2 it must perform two successful traversal advances but read `Current` only once; an unexpected second `Current` is configured to throw so the Count-mismatch contract must win before caller-controlled access. Null-key and duplicate-key N+1 controls preserve failure precedence, while a stable multi-interface counted input remains accepted.

## Non-goals

This package does not change reserved metadata codecs, metadata dirty-state semantics, XML validation, persistence formats, project versioning, BricsCAD adapter behavior, or licensed runtime qualification.

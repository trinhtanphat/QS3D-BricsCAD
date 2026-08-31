# Regeneration work profile known-Count stability

Lane-Key: `issue-4426`

`RegenerationWorkProfile` materializes caller-supplied target ids, work items, and category summaries into immutable lists. When a source exposes deterministic `Count` metadata through `ICollection<T>`, `IReadOnlyCollection<T>`, or non-generic `ICollection`, the profile must bind that evidence before traversal and re-bind it after traversal.

The contract is fail-closed: negative, conflicting, oversized, changed, or traversal-mismatched deterministic Count evidence is rejected before profile publication. The first item beyond an admitted known Count is rejected before null/semantic processing. Pure streaming `IEnumerable<T>` inputs retain the independent project-element cardinality bound.

Deterministic coverage lives in `RegenerationWorkProfileCollectionBoundSmoke` and `RegenerationWorkProfileCountStabilitySmoke`. The source guard is `scripts/preflight-regeneration-work-profile-count-stability.py`.

Runtime classification: `NOT_APPLICABLE`; this is Core-only collection-integrity behavior and does not require licensed BricsCAD execution.

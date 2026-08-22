# Plan — Semantic View definition filter bounds

## Goal

Preserve `SemanticViewDefinition` defensive snapshots while enforcing the planner's existing 100,000 include/exclude filter-id capacity during lazy constructor input enumeration.

## Existing contract

- `SemanticViewPlanner.MaxFilterIds` limits each include/exclude list to 100,000 ids.
- Include/exclude values are later normalized and validated for required/canonical identity, duplicates and overlap.
- Definition collections are defensive read-only snapshots.
- Categories have no separate cardinality contract and are out of scope.

## Defect

The public constructor currently uses unrestricted `new List<string>(IEnumerable<string>)` for include/exclude sources. A huge or non-terminating lazy source can therefore be consumed without bound before `NormalizeIds()` reaches the existing 100,000-item limit.

## Implementation

1. Reuse the planner's existing `MaxFilterIds` value from the definition constructor instead of duplicating a new policy constant.
2. Materialize include/exclude sources one pass at a time, rejecting when the 100,001st item is observed and never requesting item 100,002.
3. Return read-only snapshots exactly as before.
4. Preserve downstream id validation, duplicate detection, include/exclude overlap checks, Floor/Zone reference behavior and rendering semantics.

## Regression

- Lazy include source yields exactly 100,001 items and throws a sentinel if 100,002 is requested.
- Lazy exclude source does the same.
- Expected failure is the existing `Semantic view supports at most 100000 ...` capacity message, not the sentinel.
- A bounded source-mutation case confirms the definition remains a defensive snapshot.

## Static guard

Require bounded snapshot helper use for both include/exclude inputs, shared use of `MaxFilterIds`, guard-before-add ordering, read-only return, and absence of the two legacy unrestricted constructor materializations.

## Validation boundary

GitHub Actions remain manual-only and are not dispatched. Remote evidence is source/diff/static-contract review plus committed deterministic smoke/preflight coverage. No BricsCAD V25/V26 runtime PASS is claimed.

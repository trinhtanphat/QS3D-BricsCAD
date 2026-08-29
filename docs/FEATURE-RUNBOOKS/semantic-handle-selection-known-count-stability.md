# Semantic handle selection known-Count stability

## Scope

This runbook qualifies the deterministic Core integrity contract for caller-controlled `IEnumerable<string>` inputs passed to `SemanticHandleOwnershipResolver.Resolve`.

The contract is source/Core-only. It does not require licensed BricsCAD runtime evidence and must not be reported as `LOCAL_PASS`.

## Risk

A counted enumerable is caller-controlled code. A source can advertise a deterministic `Count`, execute arbitrary code during enumeration, and expose a different Count afterward. Accepting the selected handles without revalidating that evidence can let semantic ownership resolution proceed from an internally inconsistent input snapshot.

## Required behavior

`MaterializeSelectedHandles` must:

1. Observe every supported deterministic Count surface (`ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection`) before traversal.
2. Reject negative, conflicting, or greater-than-10,000 admission evidence without enumerating input items.
3. Keep the independent 10,000-entry streaming bound for sources without deterministic Count evidence.
4. When a Count was admitted, reject the first item beyond that Count before normalizing or retaining it.
5. Reject under-yield when completed traversal cardinality differs from the admitted Count.
6. Re-read every supported deterministic Count surface after traversal and reject negative, conflicting, disappearing, or changed evidence before the selected-handle set can escape to ownership resolution.
7. Preserve stable counted inputs, pure streaming inputs, canonical handle normalization, `ProjectState.ChangeVersion` freshness, and semantic element ownership freshness.

## Deterministic regression matrix

`SemanticHandleSelectionKnownCountStabilitySmoke` is auto-registered through a module initializer and covers:

- generic `ICollection<string>` Count drift after exact traversal;
- `IReadOnlyCollection<string>` Count drift;
- non-generic `ICollection` Count drift;
- negative post-traversal Count evidence;
- conflicting post-traversal Count surfaces;
- over-yield versus an advertised Count, rejected before the extra item is processed;
- under-yield versus an advertised Count;
- stable counted selection resolving the expected owner;
- pure streaming selection preserving existing supported behavior.

The auto-discovered `preflight-semantic-handle-selection-known-count-stability.py` pins the production ordering: admission evidence, exact traversal/cardinality checks, post-traversal Count rebinding, then return/public ownership resolution.

## Validation

Required hosted evidence for a merge candidate is fresh exact-head protected Shared CI with both `preflight` and `core` terminal `SUCCESS`. Core must include deterministic smoke execution and the repository-required builds. If protected main advances before merge, reconcile non-force, preserve the reserved four-path task boundary, and obtain fresh exact-head evidence.

Do not weaken the 10,000 cap, skip post-traversal rebinding, convert hostile-input failures into accepted behavior, or substitute licensed runtime claims for deterministic Core evidence.

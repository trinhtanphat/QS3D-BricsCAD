# Semantic handle selection known-Count stability

## Scope

This runbook qualifies the deterministic Core integrity contract for caller-controlled `IEnumerable<string>` inputs passed to `SemanticHandleOwnershipResolver.Resolve`.

The contract is source/Core-only. It does not require licensed BricsCAD runtime evidence and must not be reported as `LOCAL_PASS`.

## Risk

A counted enumerable is caller-controlled code. A source can advertise a deterministic `Count`, execute arbitrary code in `MoveNext` or `Current`, transiently expose a different Count, and restore the admitted value before traversal completes.

The historical post-traversal contract from #4501 rejected persistent Count drift, but a C# `foreach` reads `IEnumerator.Current` before entering the loop body. An advertised Count=N source could therefore expose Current N+1 before the existing overrun guard, and MoveNext-induced transient growth/shrink/negative/conflicting Count evidence could be hidden if Current restored the original value.

## Required behavior

`MaterializeSelectedHandles` must:

1. Observe every supported deterministic Count surface (`ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection`) before traversal.
2. Reject negative, conflicting, or greater-than-10,000 admission evidence without enumerating input items.
3. Use explicit enumerator traversal rather than `foreach` whenever deterministic Count evidence was admitted by the shared materialization path.
4. Rebind the exact admitted Count evidence immediately before `MoveNext` and after every successful `MoveNext`, before reading `Current`.
5. Reject the first successful MoveNext beyond an advertised Count before the extra `Current` can be observed.
6. Retain the independent 10,000-entry streaming bound for sources without deterministic Count evidence.
7. Rebind Count after each observed Current so Current-induced drift cannot escape unnoticed.
8. Reject under-yield when completed traversal cardinality differs from the admitted Count.
9. Re-read every supported deterministic Count surface after traversal and reject negative, conflicting, disappearing, or changed evidence before the selected-handle set can escape to ownership resolution.
10. Preserve stable counted inputs, stable multi-interface counted inputs, pure streaming inputs, blank-selection behavior, canonical handle normalization/deduplication, `ProjectState.ChangeVersion` freshness, and semantic element ownership freshness.

## Deterministic regression matrix

`SemanticHandleSelectionKnownCountStabilitySmoke` is auto-registered through a module initializer and covers:

- generic `ICollection<string>` Count drift;
- `IReadOnlyCollection<string>` Count drift;
- non-generic `ICollection` Count drift;
- negative Count evidence;
- conflicting Count surfaces;
- advertised Count=1 with two successful `MoveNext` calls, proving only one `Current` read;
- MoveNext-induced transient growth, shrink, negative and cross-interface conflict, each proving rejection after one MoveNext and before any Current read;
- under-yield versus an advertised Count;
- stable counted selection resolving the expected owner;
- stable multi-interface counted selection resolving with exactly one Current read;
- pure streaming selection preserving existing supported behavior.

The auto-discovered `preflight-semantic-handle-selection-known-count-stability.py` pins the production ordering: admission evidence, Count rebound, `MoveNext`, Count rebound, overrun/cap admission, `Current`, rebound, exact cardinality/post-traversal validation, then return/public ownership resolution. It explicitly rejects regression to the historical `foreach` source shape.

## Validation

Required hosted evidence for a merge candidate is fresh exact-head protected Shared CI with both `preflight` and `core` terminal `SUCCESS`. Core must include deterministic smoke execution and the repository-required builds. If protected main advances before merge, reconcile non-force, preserve the reserved four-path task boundary, and obtain fresh exact-head evidence.

Do not weaken the 10,000 cap, skip traversal-time or post-traversal rebinding, allow an overrun Current read, convert hostile-input failures into accepted behavior, or substitute licensed runtime claims for deterministic Core evidence.

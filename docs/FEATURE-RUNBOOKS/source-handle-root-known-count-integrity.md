# SourceHandleResolver root known-Count integrity

Issue: #4521  
Lane-Key: `issue-4521`  
Ownership-Key: `core.services.source-handle-root-known-count-integrity`

## Problem

`SourceHandleResolver.Resolve` accepts caller-controlled `IEnumerable<string>` root semantic element IDs. The historical materializer only used generic/read-only `Count` as an oversized-input hint and then traversed with `foreach`. That left deterministic collection metadata non-authoritative: a source advertising `Count=N` could yield N+1 entries, an under-yield could pass, generic/read-only/non-generic Count surfaces could conflict, and Count could change after traversal. Because C# `foreach` obtains `IEnumerator.Current` before the loop body executes, an N+1 element could also be observed before a body-level guard rejected the overrun.

This matters before Locate ownership resolution because caller-controlled root IDs must be one stable generation. A mixed traversal/metadata generation must fail closed rather than resolve handles from stale or hostile input.

## Contract

`MaterializeRootElementIds` now:

1. observes supported `ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection` Count surfaces;
2. rejects negative, conflicting, or greater-than-10,000 deterministic Count evidence before enumeration;
3. enumerates explicitly as successful `MoveNext` -> known-Count/10,000 capacity guards -> `Current`;
4. preserves canonical root-ID validation for entries inside the admitted cardinality;
5. rejects exact under-yield after traversal;
6. re-reads deterministic Count evidence after traversal and rejects negative, conflicting, missing, or changed evidence before returning roots;
7. preserves pure streaming input behavior and the independent 10,000-entry hard cap.

The existing `ProjectState.ChangeVersion` and element-instance ownership freshness checks remain after materialization, so Count integrity complements rather than replaces project-generation integrity.

## Deterministic regression coverage

`SourceHandleRootKnownCountIntegritySmoke` self-registers through `ModuleInitializer` and covers:

- N+1 overrun rejection after the unexpected `MoveNext` but before its `Current` is observed;
- exact under-yield;
- generic, read-only, and non-generic post-traversal Count drift;
- negative and conflicting admission evidence before enumeration;
- negative and conflicting post-traversal evidence;
- stable multi-interface Count evidence;
- canonical-ID validation precedence inside admitted cardinality;
- pure streaming input compatibility.

`scripts/preflight-source-handle-root-known-count-integrity.py` is auto-discovered by aggregate feature preflight. It pins the explicit traversal ordering, Count-surface coverage, post-traversal rebind, independent cap, canonical-ID validation, and smoke registration. Reintroducing outer `foreach` over the caller-controlled root enumerable is explicitly rejected.

## Validation boundary

This package is repository-safe Core behavior. Required remote acceptance is exact-head Shared CI plus protected PR `preflight` and `core` on a candidate reconciled with current protected `main`. No licensed BricsCAD, private DWG, UI automation, or `LOCAL_PASS` evidence is required or claimed.

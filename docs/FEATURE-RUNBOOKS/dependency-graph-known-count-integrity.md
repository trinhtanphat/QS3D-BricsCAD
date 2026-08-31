# Dependency graph known-Count observation integrity

Lane-Key: `issue-4520`

## Purpose

`DependencyGraph.Rebuild` and `TopologicalDirtyOrder` consume caller-controlled semantic-element enumerables. Historical issue #4301 placed known-Count guards before semantic processing in each loop body, but C# `foreach` still evaluated `IEnumerator.Current` before that body. The same boundaries also retained only the initial Count snapshot after traversal.

This lane closes those remaining deterministic integrity gaps without changing dependency semantics.

## Contract

For rebuild and ordering inputs:

1. Snapshot every supported `ICollection<ProjectElement>`, `IReadOnlyCollection<ProjectElement>` and non-generic `ICollection` Count surface before enumeration.
2. Reject negative, conflicting and >10,000 Count evidence before requesting an enumerator.
3. Traverse caller input as `MoveNext -> admitted Count guard -> independent 10,000 guard -> Current` so the first item beyond a trustworthy Count never exposes `IEnumerator.Current`.
4. Preserve exact under-yield rejection after traversal.
5. Rebind all supported Count surfaces after exact traversal and reject negative, conflicting, changed or source-set-drifted evidence before graph publication or ordering evaluation.
6. Preserve pure-streaming behavior, dependency validation, duplicate/missing dependency behavior, rebuild atomicity/version freshness and deterministic topological ordering.

## Deterministic coverage

`DependencyGraphKnownCountIntegritySmoke` proves:

- rebuild and ordering do not read N+1 `Current` for Count=N;
- a rejected rebuild preserves the previously committed graph;
- post-traversal Count drift does not publish staged rebuild state;
- rebound negative and conflicting Count evidence fails closed;
- stable multi-interface Count sources remain accepted;
- pure streaming inputs remain accepted.

The existing #4301 `DependencyGraphKnownCountContractSmoke` remains active and continues to lock initial malformed Count rejection, under-yield, exact 10,000 boundaries, duplicate IDs and streaming-limit semantics.

`preflight-dependency-graph-known-count-integrity.py` is auto-discovered and pins explicit traversal ordering, Count rebind, smoke registration and this runbook.

## Runtime boundary

This is deterministic Core state/order correctness. Runtime is `NOT_APPLICABLE`; hosted CI must not be described as licensed BricsCAD `LOCAL_PASS`.

## Landing

Require exact-head Shared branch CI, collision-clean latest-main reconciliation if necessary, one canonical PR, protected current-candidate `preflight` + `core` SUCCESS, expected-head merge, and exact protected-main parent/source verification.

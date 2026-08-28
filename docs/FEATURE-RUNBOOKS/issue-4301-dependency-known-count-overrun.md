# Issue #4301 — Dependency graph known-Count overrun ordering

Status: `SOURCE_FIX_ACTIVE`

Lane-Key: `issue-4301`

Canonical owner: independent QS3D schedule worker `C02`

Runtime: `NOT_APPLICABLE` — deterministic Core dependency/state integrity.

## Problem

`DependencyGraph.Rebuild(...)` and `TopologicalDirtyOrder(...)` already bind supported generic, read-only and non-generic `Count` metadata before caller-controlled enumeration. They reject malformed known Counts and final Count/traversal mismatch, and they independently cap pure streaming inputs at 10,000 elements.

Before this lane, a source that advertised Count N but yielded item N+1 was allowed to semantically process that unexpected item before final mismatch rejection. In `Rebuild`, the unexpected item could reach null/duplicate/dependency validation and mutate staging dictionaries. In dirty ordering it could reach null validation/materialization. A malformed unexpected payload could therefore mask the stronger collection-integrity failure.

## Hardened contract

Both public dependency collection boundaries now reject the first yielded element beyond a trustworthy known Count before processing that element:

1. `Rebuild` checks capacity before increment, null/ID/duplicate/dependency validation or next-graph staging.
2. `TopologicalDirtyOrder` checks capacity before the independent streaming ceiling, null validation or materialization.
3. Existing final observed-count validation remains authoritative for under-traversal.
4. Existing negative, oversized and conflicting known-Count rejection remains pre-enumeration.
5. Inputs with no supported known Count retain the independent 10,000-element streaming ceiling.

## Atomicity and preserved semantics

A known-count overrun during `Rebuild` never publishes the staged next graph, so the previously committed graph remains intact. Valid dependency validation, missing-source checks, rebuild-version freshness, duplicate semantic-ID behavior, deterministic topological ordering and exact 10,000-element acceptance are unchanged.

## Deterministic regression

`DependencyGraphKnownCountContractSmoke` now proves:

- rebuild overrun wins before an unexpected null element and preserves the prior graph;
- rebuild overrun wins before duplicate-ID validation on the first unexpected element;
- dirty-order overrun wins before unexpected null-element validation;
- a dishonest counted source reporting Count 1 stops at its second yielded item rather than running to the global bound;
- pure streaming/no-known-Count sources still stop at raw element 10,001;
- historical negative/oversized/conflicting Count, under-traversal, valid counted, duplicate-ID and exact-bound controls remain active.

`scripts/preflight-dependency-known-count-overrun.py` is auto-discovered and locks the guard placement, threshold and regression controls.

## Landing

Repository-safe landing requires automatic exact-head branch CI, latest-main reconciliation on the same canonical carrier when necessary, one PR carrying `Lane-Key: issue-4301`, protected current-candidate `preflight` + `core` terminal SUCCESS, strict mergeability/freshness, expected-head merge, and exact resulting `main` verification.

No licensed BricsCAD host, private DWG, package runtime or `LOCAL_PASS` evidence applies to this Core-only fix.

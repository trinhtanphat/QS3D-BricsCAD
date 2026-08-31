# Dependency impact root known-count stability

Lane-Key: `issue-4397`
Owner: C02
Runtime: `NOT_APPLICABLE` (deterministic Core/service contract)

## Defect boundary

`DependencyImpactPlanner.Plan` accepts caller-owned `IEnumerable<string>` roots. Before this lane, `CanonicalRoots` bound available `ICollection<string>`, `IReadOnlyCollection<string>`, and non-generic `ICollection` Count values before traversal and enforced negative/conflicting/oversized metadata plus early-overrun and under-yield. It did not bind those deterministic Count surfaces again after caller-controlled enumeration.

A source could therefore advertise Count `N`, yield exactly `N` canonical roots, mutate one or more Count surfaces while completing enumeration, and still publish a dependency-impact plan using stale collection metadata.

## Required invariant

For a source exposing deterministic Count metadata:

1. bind every supported Count surface before enumeration;
2. reject negative, conflicting, or project-oversized Count before traversal;
3. reject the first item beyond the bound Count before processing that item;
4. reject under-yield after traversal;
5. re-bind every supported Count surface after traversal and reject negative, conflicting, added/removed, or changed Count evidence before root sorting/plan publication;
6. preserve pure streaming `IEnumerable<string>` behavior when no deterministic Count surface exists.

The planner must not reinterpret a post-traversal Count change as a new valid source shape. The caller must provide a stable source and recompute.

## Deterministic verification

`DependencyImpactPlannerKnownCountSmoke` covers:

- pre-enumeration oversized, negative, and conflicting Count rejection;
- under-yield and early over-yield;
- post-traversal Count change;
- post-traversal interface conflict;
- post-traversal negative Count;
- honest multi-interface counted input;
- pure streaming input.

The auto-discovered guard `scripts/preflight-dependency-impact-known-count-stability.py` locks the two-phase binding order so the post-traversal validation cannot silently move after root publication.

## Acceptance

Repository-safe acceptance is deterministic smoke + discovered feature guard + normal protected `preflight`/`core` CI. Licensed BricsCAD runtime evidence is not applicable and must not be claimed for this lane.

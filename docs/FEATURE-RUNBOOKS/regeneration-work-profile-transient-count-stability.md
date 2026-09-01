# Regeneration work profile transient Count stability

Lane-Key: `issue-4589`

## Boundary

`RegenerationWorkProfile` is a public immutable Core DTO. Its target-id, work-item and category inputs may be caller-controlled `IEnumerable<T>` values that also expose deterministic collection Count metadata.

Count evidence is a traversal contract, not merely an admission/final-cardinality hint. Once a supported Count surface is observed, the same evidence must remain stable while the collection is consumed.

## Current-main defect reproduced

Before this carrier, `MaterializeBounded<T>` sampled supported Count surfaces at admission and after traversal. It rejected an N+1 item before `Current`, but it did not re-read Count around each caller-controlled `MoveNext`. A collection could therefore report Count=N at admission, change Count transiently after a successful `MoveNext`, return to N before terminal rebound, and still publish a profile.

## Required ordering

For every traversal step:

1. rebind all supported Count surfaces before calling `MoveNext`;
2. after a successful `MoveNext`, rebind them again before reading `Current`;
3. enforce admitted Count and project-element ceilings before `Current`;
4. validate the item and append it only after those checks;
5. preserve under-yield rejection and perform a final Count rebound before immutable publication.

Any negative, conflicting, oversized or changed Count observed at a traversal boundary fails closed. Pure streaming inputs retain the independent project-element ceiling.

## Deterministic regression

`RegenerationWorkProfileKnownCountStabilitySmoke` includes hostile collections whose Count grows, shrinks, or becomes negative immediately after the first successful `MoveNext`. The target, work-item and category boundaries must reject without reading `Current`. Existing overrun, under-yield, post-traversal drift, streaming and honest-counted controls remain active.

`scripts/preflight-regeneration-work-profile-transient-count-stability.py` is auto-discovered and pins the source ordering plus the registered smoke evidence.

## Runtime boundary

Core-only deterministic collection integrity. Licensed BricsCAD runtime is `NOT_APPLICABLE`; no `LOCAL_PASS` is required or claimed.

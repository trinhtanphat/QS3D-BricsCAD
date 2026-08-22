# QS3D Grid intersection ownership contract

Updated: 2026-08-10 (UTC+7)

## Purpose

A Grid intersection is not owned by one Grid. It is defined by a **pair of semantic Grid IDs**, and one pair may have up to two finite points for LINE/ARC or ARC/ARC geometry.

`GridIntersectionIdentityPlanner` provides the CAD-independent identity layer needed before native intersection symbols can be created safely. It does not create markers and does not change project schema.

## Canonical pair

Both semantic Grid IDs are trimmed, bounded to 128 characters and canonicalized with invariant uppercase because project element identity is case-insensitive. The two canonical IDs are sorted ordinally, so `(A,B)` and `(B,A)` produce the same pair.

The full `PairKey` is length-prefixed:

`<len:first>:<FIRST>|<len:second>:<SECOND>`

Length prefixes keep the canonical pair collision-free even when IDs contain punctuation or delimiter characters.

## Compact native tokens

The full pair key can exceed practical native metadata string budgets, so Core also derives a compact SHA-256 token:

- pair token: `GIP1:<64 lowercase hex chars>`;
- owner token: `GIX1:<same 64 hex chars>:<occurrence>`.

Core detects an in-batch pair-token hash collision and fails closed instead of accepting ambiguous ownership. Native adapters should additionally persist/verify the two semantic Grid IDs when they materialize pair-owned output; the compact token is an identity key, not permission to skip pair validation.

The current occurrence is **occurrence 0/1** only. The Core finite intersection planner cannot legitimately produce more than two distinct finite points for one supported curve pair, so a third point fails closed.

## Deterministic occurrence assignment

Identity assignment does not trust input ordering. Results are grouped by canonical pair, pair groups are sorted by `PairKey`, and points inside each pair are sorted by `(X,Y)` before occurrence is assigned.

This complements the existing deterministic geometry planner, which already sorts multi-point LINE/ARC roots and ARC/ARC points. Reversing the Grid input pair, reversing the intersection list or changing ID casing therefore does not change `PairToken`/`OwnerToken` for the same reviewed geometry.

Coordinates are deliberately **not** embedded in `OwnerToken`. If Grid geometry moves while the same semantic pair remains, a future native replacement operation should be able to find and replace the old occurrence owner rather than treating the moved marker as an unrelated owner.

Near-duplicate points inside the requested tolerance fail closed because occurrence ownership would be ambiguous.

## No schema/category inflation

This slice **does not add `ElementCategory.GridIntersection`** and does not add another semantic Grid store. An intersection is a derived relation between two existing Grid elements, not a quantity-bearing model element.

`GridIntersectionIdentityPlanner` therefore has no dependency on `ProjectState`, `ProjectElement`, BricsCAD `ObjectId`, CAD Handle or generated-geometry APIs.

## Future pair-owned native marker contract

A local/native marker implementation should treat the following as one ownership tuple:

1. project identity;
2. canonical first Grid ID;
3. canonical second Grid ID;
4. `PairToken`;
5. occurrence index;
6. `OwnerToken`;
7. generated CAD handle(s) and ownership version.

Replacement must verify the full tuple before erasing a previous live marker. Delete/untrack of either Grid must not silently transfer the pair-owned marker to the surviving Grid. Health should report missing members, stale geometry, duplicate owner tokens and live-XData ownership mismatch rather than reclaiming foreign CAD.

How those pair records are persisted and how pair-owned XData is encoded must be reviewed as a separate source slice before native creation. Existing single-`ProjectElement` generated ownership must not be reused by pretending one of the two Grid elements is the sole owner.

## Runtime boundary

Core identity, smoke coverage and static preflight are `REMOTE_DONE` source work. Native marker creation, replacement, Undo/Redo and real DWG/XData behavior remain `LOCAL_ONLY` until the pair-owned adapter source exists and an exact-SHA licensed BricsCAD V25 matrix is run.

The future local matrix should include at least:

- reversed Grid pair selection produces the same owner token;
- two-point ARC/ARC pair retains distinct occurrence 0/1 owners;
- moved geometry replaces the same pair occurrence owner rather than leaking an old marker;
- deletion/untrack of either Grid leaves no silently reassigned ownership;
- foreign/corrupt XData is refused, not erased;
- save/reopen, Undo/Redo and multi-DWG isolation.

Do not create native Grid intersection markers by assigning them arbitrarily to one Grid element. The pair-owned identity contract exists specifically to prevent that asymmetric lifecycle.

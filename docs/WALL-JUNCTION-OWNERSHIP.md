# QS3D physical wall-junction ownership contract

Updated: 2026-08-11 (UTC+7)

## Purpose

QS3D already has deterministic wall-junction analysis and source snap/reconcile behavior. The remaining physical L/T/X problem is different: a junction infill/composite is derived from **multiple semantic walls**, so assigning one native solid to one arbitrary wall would make replacement, invalidation and opening-host behavior ambiguous.

`WallJunctionOwnershipPlanner` adds the CAD-independent ownership/rebuild plan for that boundary. It deliberately does not create `Solid3d`, execute booleans or mutate a DWG.

## Inputs

Each `WallJunctionOwnerContext` binds one analyzed source segment to:

- one semantic Wall element ID;
- one project ID;
- one drawing fingerprint;
- Bottom/Top elevation in metres;
- wall thickness in metres.

The planner consumes existing `WallJunction` results plus those bindings. `End` and `Straight` nodes do not produce multi-owner physical output. A corner formed by multiple source segments that all belong to the **same semantic wall** is also skipped rather than inventing a second ownership domain.

The batch fails closed when:

- a source segment has no semantic owner mapping;
- a source mapping is duplicated;
- one semantic wall has conflicting profile/vertical data across its source segments;
- owner mappings span different projects or drawings;
- a physical junction has inconsistent kind/ray-count data;
- two or more owner walls have no positive shared vertical overlap;
- two physical nodes for the same owner group are duplicate/near-duplicate within the requested tolerance.

## Canonical multi-owner identity

The planner canonicalizes project, drawing and semantic owner IDs case-insensitively and sorts owner Wall IDs. A junction owner group key contains:

1. project identity;
2. drawing fingerprint;
3. the complete sorted set of semantic Wall owner IDs.

It intentionally does **not** contain current point, thickness, junction kind or ray count. Those are rebuild inputs, not long-lived ownership identity.

Core derives three compact SHA-256 tokens:

- group token `WJP1:<hash>` — the canonical multi-owner wall set in one project/drawing;
- native owner token `WJX1:<hash>:<occurrence>` — the specific derived physical output owner;
- input fingerprint `WJF1:<hash>` — current geometry/profile/dependency inputs that must invalidate stale output when they change.

For one owner set with multiple physical nodes, occurrence is assigned deterministically by sorted junction point, then kind/ray count. Input enumeration order therefore cannot change owner identity. Near-duplicate nodes fail closed because native replacement ownership would be ambiguous.

Coordinates are not embedded in `WJX1:`. If a junction moves but remains the same occurrence for the same semantic owner set, a future adapter can locate and replace the previously owned output instead of leaking a new native object. The point, kind, ray count, vertical overlap, source-to-wall bindings and wall profiles are included in `WJF1:` so the old materialization becomes stale.

## Dependency and vertical contract

Every `WallJunctionOwnershipPlan` exposes:

- `OwnerWallIds` — all semantic walls that own the derived relation;
- `SourceSegmentIds` — all authoritative source segments participating in that node;
- `BottomM` / `TopM` — the intersection of owner vertical ranges;
- `MinThicknessM` / `MaxThicknessM` — bounded profile context for native planning;
- `OwnerToken` and `InputFingerprint` — identity versus rebuild state.

The shared vertical overlap is deliberately conservative. If owner vertical ranges do not overlap, Core refuses a physical junction rather than allowing a native boolean to create geometry that cannot be reconciled semantically.

## Native adapter contract

A future/local V25 materializer should treat the complete junction plan as one dedicated ownership record. It must **not** reuse a single wall's `GeneratedSolidHandle` or claim that one participating wall solely owns the junction.

Before destructive replacement, the adapter should verify at minimum:

- active project and drawing match the plan;
- all `OwnerWallIds` still resolve to the expected semantic walls;
- the persisted `WJX1:` owner token is unique;
- the persisted `WJF1:` fingerprint matches the current plan when deciding whether output is current;
- every live native handle still carries matching dedicated junction ownership metadata;
- foreign/ambiguous/corrupt native objects are refused rather than erased.

When any owner source/profile/elevation changes, the planner fingerprint changes and the junction output must be invalidated/rebuilt. Deleting/untracking any participating wall must never transfer the junction to another wall silently.

Door/Opening hosting remains on the original semantic wall. Junction-owned infill/composite output is a derived dependency and must not steal or rewrite Door/Opening host ownership.

## Native geometry boundary

Core does not define the final Solid3d boolean recipe. The V25 adapter still has to decide and qualify whether the safest materialization is dedicated infill, trimmed/composite output, or another explicitly owned native representation. Whatever representation is chosen must preserve the multi-owner token/dependency contract above.

The local implementation must cover:

- L/T/X and bounded Multi nodes;
- 2/3/4+ semantic owners;
- mixed wall thicknesses;
- compatible and incompatible vertical ranges;
- source/profile/elevation changes followed by invalidation/rebuild;
- removal of one owner;
- Door/Opening host retention;
- foreign/corrupt ownership refusal;
- save/reopen, Undo/Redo and multi-DWG isolation.

## Status

`WallJunctionOwnershipPlanner`, smoke coverage and the static preflight are `REMOTE_DONE` source work. Actual native `Solid3d` creation/boolean/replacement and exact-SHA evidence remain `LOCAL_ONLY` under `docs/LOCAL-AGENT-INBOX.md` item `LOCAL-007`.

This contract reduces the remaining issue from “invent safe multi-owner ownership while coding native geometry” to “materialize and qualify a pre-defined ownership/dependency plan in BricsCAD V25.”

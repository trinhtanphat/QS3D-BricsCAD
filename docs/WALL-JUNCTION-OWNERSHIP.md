# QS3D physical wall-junction ownership contract

Updated: 2026-08-23 (UTC+7)

## Purpose

QS3D already has deterministic wall-junction analysis and source snap/reconcile behavior. The physical L/T/X problem is different: a junction infill/composite is derived from **multiple semantic walls**, so assigning one native solid to one arbitrary wall would make replacement, invalidation and opening-host behavior ambiguous.

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

Coordinates are not embedded in `WJX1:`. If a junction moves but remains the same occurrence for the same semantic owner set, the adapter can locate and replace the previously owned output instead of leaking a new native object. The point, kind, ray count, vertical overlap, source-to-wall bindings and wall profiles are included in `WJF1:` so the old materialization becomes stale.

## Dependency and vertical contract

Every `WallJunctionOwnershipPlan` exposes:

- `OwnerWallIds` — all semantic walls that own the derived relation;
- `SourceSegmentIds` — all authoritative source segments participating in that node;
- `BottomM` / `TopM` — the intersection of owner vertical ranges;
- `MinThicknessM` / `MaxThicknessM` — bounded profile context for native planning;
- `OwnerToken` and `InputFingerprint` — identity versus rebuild state.

The shared vertical overlap is deliberately conservative. If owner vertical ranges do not overlap, Core refuses a physical junction rather than allowing a native boolean to create geometry that cannot be reconciled semantically.

## Native adapter contract

The V25 source adapter exposes `QS3DWALLJUNCTION3D` and treats all occurrences sharing one `GroupToken` as one dedicated ownership/replacement record. It does **not** reuse a wall's `GeneratedSolidHandle`, mutate semantic wall ownership, or claim that one participating wall solely owns the junction.

Every created `Solid3d` carries strict versioned XData under RegApp `QS3D_WALL_JUNCTION`. The marker persists hashed project/drawing identities, the exact `WJP1:` / `WJX1:` / `WJF1:` values, kind and occurrence, point/profile/vertical values, and the complete sorted owner/source identity sets. The raw project, drawing, owner and source identifiers are not copied into the DWG marker.

Before destructive replacement, the adapter verifies:

- active project and drawing match the plan;
- all `OwnerWallIds` still resolve to the expected semantic walls;
- the persisted `WJX1:` owner token is unique;
- the persisted `WJF1:` fingerprint matches the current plan when deciding whether output is current;
- every live native handle still carries matching dedicated junction ownership metadata;
- foreign/ambiguous/corrupt native objects are refused rather than erased.

The command reads and validates every existing junction marker before mutation. A non-current group is erased and recreated as a **whole group** in one CAD transaction; partial occurrence replacement is not permitted. Exact-current groups are retained idempotently. Owner add/remove produces a new owner-set group identity, and a selected live owner plus deleted/missing peers scopes cleanup of the retiring group without transferring ownership.

PICKFIRST/interactive selection chooses the semantic owner scope, not a partial topology snapshot. Before planning, the materializer expands to every live eligible semantic wall source on the same plan-elevation scope, then keeps junction nodes incident to the selected owner IDs. An omitted third/fourth wall at a T/X/Multi node therefore still participates in the complete owner group, while sources on other Z planes are planned separately. An unsupported selected source still fails before mutation.

`GeneratedDependentGeometryInvalidator`, `WallSolidBuilder` and `PolylineWallSolidBuilder` invalidate the complete dependent group inside the same native transaction whenever a participating wall is reconciled or rebuilt. `GeneratedNativeSourceGuard` rejects `QS3D_WALL_JUNCTION` entities from source capture even if their marker is malformed or the sidecar is unavailable. Read-only junction health is aggregated into Health All and Release Check, and the dedicated `QS3DWALLJUNCTIONHEALTH` command reports ownership, live-object and stale-fingerprint failures without repairing them.

When any owner source/profile/elevation changes, the planner fingerprint changes and the junction output must be invalidated/rebuilt. Deleting/untracking any participating wall must never transfer the junction to another wall silently.

Door/Opening hosting remains on the original semantic wall. Junction-owned infill/composite output is a derived dependency and must not steal or rewrite Door/Opening host ownership.

## Native geometry boundary

Core remains CAD-independent and does not define a `Solid3d` recipe. The V25 materializer uses one dedicated vertical cylindrical core per physical occurrence:

- center: the planned junction point;
- bottom/height: the positive shared owner overlap `BottomM .. TopM`;
- radius: `MinThicknessM / 2`;
- native construction: `Solid3d.CreateFrustum(height, radius, radius, radius)`;
- layer: the first deterministic participating source layer.

The rotationally symmetric core consumes only explicit ownership-plan fields. It never calls Boolean union/subtraction, never consumes/cuts/reassigns a semantic wall host, and never changes Door/Opening host ownership. Its marker and geometry are appended in the same CAD transaction. The project identity/change-version and sidecar backing-store observation are rechecked before commit; the command does not write junction handles into an arbitrary semantic wall or advance project/audit state.

Licensed local qualification must cover:

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

Licensed native qualification for this boundary is classified `LOCAL_ONLY`; source-only/static evidence cannot be promoted to a runtime pass. The bounded result below comes from the required local host.

`LOCAL_PASS / BOUNDED_P03_PHYSICAL` was recorded on exact clean pushed source candidate `42fd555d9bfad695b2d1a4a82b67ac8de1d98f79` in licensed BricsCAD V25.2.10 x64. The exact-source runtime exercised 61 private allowlisted markers across topology, ownership-failure and persistence sessions. L/T/X/Multi geometry, two occurrences for one owner group, 2/3/4/5 owners, mixed thickness/elevation, whole-group rebuild and cleanup, source/profile/member changes, host retention, missing-output Health and repair, corrupt/foreign/duplicate refusal, Undo/Redo, save/cold-reopen and two independent DWGs all passed.

The V25 `Release|x64` build and nine focused Wall Junction/Health gates passed. Aggregate source preflight passed 989 of 990 discovered gates; its sole failure is the unchanged LOCAL-008 Plan-to-3D evidence row, outside this branch. Raw runtime evidence remains Git-ignored. See `docs/agent-work-claims/2026-08-23-codex-issue3603-wall-junction-physical-p03.md` for the sanitized bounded result.

This closes the physical-output P03 cell only. Parent LOCAL-007 remains open because Wall Snap P02 still awaits the remote #3600/#3601 corrections and an exact-SHA licensed rerun.

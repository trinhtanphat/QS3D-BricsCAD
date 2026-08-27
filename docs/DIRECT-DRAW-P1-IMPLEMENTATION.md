# QS3D Direct Draw P1 — implementation handoff

Updated: 2026-08-25 (UTC+7).

## Product boundary

QS3D remains a **BricsCAD V25 x64 .NET plugin**, not a standalone CAD application. BricsCAD owns the active DWG, Model Space, selection/editor lifecycle and native 2D/3D database. Direct Draw creates ordinary source geometry inside the active BricsCAD DWG and attaches QS3D semantic/project state to that source.

This extends the P0 authoring flow in `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md` without changing the canonical product decision in `docs/PRODUCT-BOUNDARY.md`.

Door/Opening Direct Draw is now implemented as a separate guarded extension documented in `docs/DIRECT-DRAW-OPENINGS.md`; the physical boolean step remains intentionally explicit.

## Source-implemented P1 native commands

### `QS3DDRAWGLASSWALL` — Vách Kính

- Requires Model Space.
- Accepts 2+ plan-view points; two points create a LINE and longer paths create an open POLYLINE.
- Prompts thickness, height and `BottomOffsetM`; the bottom offset is **relative to the source Z**, matching the existing native wall contract.
- Uses the compatible active GlassWall Family when available. An explicitly configured non-finite/invalid Family numeric fails closed instead of being silently replaced by a fallback.
- Captures `GlassWall` semantic state, then reuses canonical `QS3DBUILD3D` for the backing native wall solid.
- Curtain perimeter/mullion/transom frames remain owned by `QS3DCURTAIN3D` / Curtain Hub and their dedicated source/runtime contracts; Direct Draw does not duplicate that frame engine.

### `QS3DDRAWWALLPIER` — Trụ Tường

- Requires Model Space.
- Accepts **two or more plan-view points**. Two points create the legacy LINE source; longer paths create an **open POLYLINE** source.
- Prompts thickness, height and source-relative bottom offset with the same fail-closed Family numeric rule.
- Captures `WallPier` semantic state and reuses guarded canonical `QS3DBUILD3D`. The existing `PolylineWallSolidBuilder` / `WallPierPathProfilePlanner` path is authoritative for multi-segment Rectangular/Chamfered footprint planning; Direct Draw does not add a second corner/profile engine.
- The open path remains subject to the canonical planner's finite-point, non-zero-segment, turn/corner and profile guards. Unsupported or ambiguous paths fail closed in the existing build lifecycle and the Direct Draw wrapper removes its operation-owned source/generated CAD before restoring project state.
- `QS3DDRAWWALLPIERADV` uses the same path contract; Advanced differs only by prompting explicit instance parameters after path capture.

### `QS3DDRAWSTRUCTWALL` — Vách BTCT

- Requires Model Space.
- Uses a two-point LINE source and the shared 5 mm plan-view tolerance.
- Prompts thickness, height and source-relative bottom offset.
- Captures `StructuralWall` semantic state and delegates native creation to canonical `QS3DBUILD3D` / `StructuralSolidBuilder` behavior.

### `QS3DDRAWFOUNDATION` — Móng

- Requires Model Space.
- Accepts 3+ coplanar points and creates a closed POLYLINE source.
- Prompts thickness and source-relative bottom offset.
- Captures `Foundation` semantic state and delegates native extrusion to canonical `QS3DBUILD3D` / `StructuralSolidBuilder` behavior.

## Shared native P1 safety contract

P1 deliberately reuses existing QS3D infrastructure instead of adding another geometry engine:

1. create one real source entity in the active DWG;
2. capture exactly one semantic element with `SemanticCaptureService`;
3. apply explicit instance dimensions/offsets through `ProjectElement.SetProperty()` so Properties/Quantity/Geometry dirty flags and generated-stale invalidation stay on the canonical Core path;
4. immediately re-check that the DWG which started the command is still the active document before delegating to `QS3DBUILD3D`;
5. run canonical `QS3DBUILD3D` for complete-source, Model-Space, category, semantic-regeneration and native geometry checks;
6. require a non-empty `GeneratedSolidHandle` and verify that handle is still live;
7. select the generated result while keeping source CAD as authoritative editable geometry.

LINE/POLYLINE authoring uses the same finite/unit-aware plan-view rules as P0. Persisted POLYLINE elevation and X/Y vertices are finite-checked before they enter the DWG database.

`QS3DBUILD3D` intentionally reports many user-facing failures instead of throwing them outward. Therefore the P1 wrapper performs the explicit live-generated-handle check; a reported-but-unbuilt result becomes a P1 failure and enters outer rollback rather than being treated as success.

Before source creation, P1 snapshots full project state. On failure it gathers generated owner handles from the newly-created semantic element plus matching QS3D XData, then performs rollback in this order:

1. erase the operation-created source CAD;
2. require matching QS3D project/element/category XData before erasing generated CAD;
3. commit CAD cleanup and verify neither source nor generated handles remain live;
4. restore the project snapshot;
5. clear implied selection best-effort.

The project snapshot is deliberately restored **after** CAD cleanup so ownership information remains available while destructive erase decisions are made. P1 must never erase generated CAD solely because a textual persisted Handle collides with another object.

Palette refresh, result selection, editor regen/status and other UI synchronization happen only **after** the CAD/project operation has succeeded. UI synchronization is best-effort; a Palette/UI failure after a valid native commit must not roll back otherwise-correct CAD and project state.

## Door / Opening Direct Draw extension

### `QS3DDRAWDOOR`

- Requires Model Space.
- Picks two plan-view edge points and creates a real LINE source; its plan length is authoritative `WidthM` after unit conversion.
- Prompts/inherits positive `HeightM`, non-negative sill/bottom offset and non-negative `BooleanClearanceM`.
- Explicit malformed/non-finite/negative Family configuration fails closed rather than being silently masked by a fallback.
- Writes instance values through `ProjectElement.SetProperty()`.
- Captures exactly one Door and performs deterministic semantic regeneration.
- Revalidates active-DWG affinity, selects only the newly-created Door source, then reuses the established `QS3DAUTOLINKHOSTS` logic.
- Requires `HostWallId` and performs a second deterministic regeneration after Auto Host. No-host or ambiguous-host placement rolls back source/project state rather than leaving an orphan Door.

### `QS3DDRAWOPENING`

- Uses the same guarded lifecycle for `WallOpening`.
- Its source LINE remains the authoritative DWG provenance; no fake semantic-only handle is introduced.

Auto Host itself now has atomic apply-batch hardening in current source; Direct Draw still retains its own outer single-authoring snapshot/cleanup boundary so nested command-surface behavior cannot make an incomplete new Door/Opening look successful.

### Physical boolean remains explicit

Door/Opening Direct Draw intentionally completes **source + semantic + verified Auto Host**, not an implicit global physical cut.

The existing `QS3DCUTOPENINGS` path can process linked openings grouped by host. Automatically invoking that broader mutation from one Direct Draw operation could also cut unrelated pending openings. The safe current workflow is:

```text
QS3DDRAWDOOR / QS3DDRAWOPENING
-> review Host / dimensions / sill / clearance
-> explicitly run QS3DCUTOPENINGS when physical host mutation is intended
```

A future one-shot cut requires an explicit-target opening-subset API plus proven rollback of host Solid3d mutation. Do not queue or call the current global cut path from Door/Opening Direct Draw.

See `docs/DIRECT-DRAW-OPENINGS.md` for the detailed contract and runtime checklist.

## Discoverability

The BricsCAD-hosted `TẠO MỚI` Ribbon tab and Full Domain Hub expose the current Direct Draw authoring set:

- `QS3DDRAWWALL`
- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWBEAM`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWCOLUMN`
- `QS3DDRAWSLAB`
- `QS3DDRAWFOUNDATION`
- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`

Legacy Capture/Bóc chọn and `QS3DBUILD3D` remain supported for drawings that already contain source geometry. Legacy `QS3DDOOR`, `QS3DOPENING`, Auto/Manual Host and physical cut commands also remain available.

## Static guards

- `scripts/preflight-direct-draw.py` continues to own the current P0 and shared `QS3DBUILD3D` hardening contract.
- `scripts/preflight-direct-draw-p1.py` guards the four native P1 commands, command uniqueness, Ribbon/Hub discoverability, Model-Space/unit-aware behavior, source-relative offsets, fail-closed Family numerics, canonical `SetProperty` writes, active-DWG revalidation, live-generated verification, ownership/XData-aware rollback ordering, finite persisted paths, **WallPier two-point LINE + multi-segment open-POLYLINE routing**, and non-destructive post-commit UI synchronization.
- `scripts/preflight-direct-draw-openings.py` guards Door/Opening command uniqueness, Model-Space/unit-aware source creation, canonical `SetProperty` writes, fail-closed Family numerics, active-DWG checks, selection-scoped Auto Host, post-link regeneration, exact source cleanup before project restore, non-destructive UI sync, Ribbon/Hub wiring and the prohibition on implicit global physical cutting.
- `scripts/preflight-auto-host-atomic.py` guards project-atomic application/regeneration of planned Auto Host links while preserving non-mutating ambiguity/unmatched review semantics.
- `scripts/preflight-all.py` auto-discovers the feature gates.

GitHub Actions remain manual-only except for the repository-authorized automatic shared CI events. A `continue all` source/docs request does not authorize manual workflow dispatch.

## Runtime qualification still required

Source implementation is not BricsCAD runtime proof. Before these commands are called production-ready, the exact source SHA still needs licensed interactive BricsCAD V25 x64 testing for:

1. compile against the exact V25 managed assemblies;
2. NETLOAD/DemandLoad registration;
3. Ribbon/Domain Hub invocation;
4. cancel-at-each-prompt behavior with no orphan CAD/project state;
5. source → semantic → native solid success for all four native P1 categories;
6. GlassWall backing host followed by Curtain-frame workflow;
7. WallPier **two-point LINE and representative 3-/4-point open-POLYLINE Rectangular/Chamfered paths**, including invalid/ambiguous turn rejection and rollback with no orphan source/generated CAD;
8. StructuralWall near-planar LINE tolerance and source-relative offset behavior;
9. Foundation closed-POLYLINE extrusion, drawing-unit behavior and save/reopen;
10. forced native-build failure followed by verified source/generated CAD cleanup and project rollback;
11. forced Palette/UI synchronization failure after a successful native commit, verifying that valid CAD/project state is preserved;
12. Door/Opening width correctness in millimeter and meter drawings;
13. Door/Opening valid-host, no-host and ambiguous-host behavior across Floor/Zone/elevation/gap gates;
14. Door/Opening height/sill/bottom-offset/clearance persistence and schedule/export behavior;
15. explicit `QS3DCUTOPENINGS` after Direct Draw, including host fingerprint/rebuild behavior;
16. World UCS and representative rotated UCS behavior;
17. a private copy of owner-provided `MB MONG.dwg` without committing the drawing;
18. multi-DWG active-document switching guard, Unicode/HiDPI and representative private-DWG regression.

Until those gates are executed, the precise status is **source-implemented / statically guarded, runtime qualification pending**.

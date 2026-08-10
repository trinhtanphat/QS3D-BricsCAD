# QS3D Direct Draw P1 — implementation handoff

Updated: 2026-08-10 (UTC+7).

## Product boundary

QS3D remains a **BricsCAD V25 x64 .NET plugin**, not a standalone CAD application. BricsCAD owns the active DWG, Model Space, selection/editor lifecycle and native 2D/3D database. Direct Draw creates ordinary source geometry inside the active BricsCAD DWG and attaches QS3D semantic/project state to that source.

This extends the P0 authoring flow in `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md` without changing the canonical product decision in `docs/PRODUCT-BOUNDARY.md`.

## Source-implemented P1 commands

### `QS3DDRAWGLASSWALL` — Vách Kính

- Requires Model Space.
- Accepts 2+ plan-view points; two points create a LINE and longer paths create an open POLYLINE.
- Prompts thickness, height and `BottomOffsetM`; the bottom offset is **relative to the source Z**, matching the existing native wall contract.
- Uses the compatible active GlassWall Family when available. An explicitly configured non-finite/invalid Family numeric fails closed instead of being silently replaced by a fallback.
- Captures `GlassWall` semantic state, then reuses canonical `QS3DBUILD3D` for the backing native wall solid.
- Curtain perimeter/mullion/transom frames remain a dedicated Curtain workflow (`QS3DCURTAIN3D` / Curtain Hub), including the path-frame implementation for supported open POLYLINE hosts.

### `QS3DDRAWWALLPIER` — Trụ Tường

- Requires Model Space.
- Accepts 2+ plan-view points as LINE/open POLYLINE source.
- Prompts thickness, height and source-relative bottom offset with the same fail-closed Family numeric rule.
- Captures `WallPier` semantic state and reuses the guarded `QS3DBUILD3D` wall compatibility path.
- Does not broaden the specialized WallPier profile contract beyond the current native source/build support. Freeform/specialized open-POLYLINE profile parity remains separate product/runtime work.

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

### `QS3DDRAWDOOR` / `QS3DDRAWOPENING` — Cửa / Lỗ Mở Vách

Door/Opening now has an explicit host-aware Direct Draw contract rather than guessing the full physical-cut workflow:

- Requires Model Space and exactly two plan-view edge points; the resulting real LINE source owns the authoritative plan `WidthM`.
- Prompts/inherits `HeightM`, sill/bottom offset and `BooleanClearanceM`; configured invalid Family values fail closed.
- Captures exactly one `Door` or `WallOpening` semantic element and persists instance parameters through `ProjectElement.SetProperty()`.
- Performs deterministic semantic regeneration before Auto Host.
- Re-checks that the DWG which started the command is still active immediately before calling the selection-scoped `QS3DAUTOLINKHOSTS` implementation.
- Requires a unique resulting `HostWallId`, then regenerates again so host-dependent quantities/relations must succeed before commit.
- On failure, erases the **exact operation-created source ObjectId** first, verifies the source is no longer live, then restores the project snapshot. It refuses textual-handle fallback when the exact ObjectId is unavailable.
- Palette/selection/Regen/status synchronization happens after the semantic/host-link operation and is best-effort; a UI-only failure does not roll back valid source + semantic + host relation state.
- **Physical boolean cutting remains explicit.** Direct Draw does not call or queue global `QS3DCUTOPENINGS`; the user can run the guarded cut workflow separately when a physical host subtraction is desired.

This separation intentionally keeps semantic authoring/host relation atomic without pretending that global straight/curved boolean-cut scope is part of a single targeted Door/Opening transaction.

## Shared safety contract

P1 deliberately reuses existing QS3D infrastructure instead of adding another geometry engine:

1. create one real source entity in the active DWG;
2. capture exactly one semantic element with `SemanticCaptureService`;
3. apply explicit instance dimensions/offsets through `ProjectElement.SetProperty()` so Properties/Quantity/Geometry dirty flags and generated-stale invalidation stay on the canonical Core path;
4. immediately re-check that the DWG which started the command is still the active document before delegating to nested command-surface workflows such as `QS3DBUILD3D` or Auto Host;
5. use canonical existing builders/linkers instead of creating a second geometry/host engine;
6. verify the required native output or host relation after the nested command returns because command-surface handlers may intentionally catch/report their own errors;
7. keep source CAD as authoritative editable geometry.

LINE/POLYLINE authoring uses finite/unit-aware plan-view rules. Persisted P1 POLYLINE elevation and X/Y vertices are finite-checked before they enter the DWG database.

### Native-solid P1 rollback

GlassWall/WallPier/StructuralWall/Foundation snapshot full project state before source creation. On failure they gather generated owner handles from the newly-created semantic element plus matching QS3D XData, then:

1. erase the operation-created source CAD;
2. require matching QS3D project/element/category XData before erasing generated CAD;
3. commit CAD cleanup and verify neither source nor generated handles remain live;
4. restore the project snapshot;
5. clear implied selection best-effort.

The project snapshot is deliberately restored **after** CAD cleanup so ownership information remains available while destructive erase decisions are made. P1 must never erase generated CAD solely because a textual persisted Handle collides with another object.

### Door/Opening rollback

Door/Opening does not create generated native output during Direct Draw because physical cutting is separate. On failure it retains the exact newly-created source `ObjectId`, erases that object, verifies its Handle is no longer live, and only then restores the project snapshot. It does not resolve an arbitrary textual Handle as a fallback destructive target.

### Post-commit UI

Palette refresh, result/source selection, editor Regen/status and other UI synchronization happen only **after** the CAD/project operation has succeeded. UI synchronization is best-effort; a Palette/UI failure after a valid commit must not roll back otherwise-correct CAD and project state.

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

Legacy Capture/Bóc chọn and `QS3DBUILD3D` remain supported for drawings that already contain source geometry. Auto/Manual Host and straight/curved physical opening-cut workflows remain available independently.

## Static guards

- `scripts/preflight-direct-draw.py` owns the current P0 and shared `QS3DBUILD3D` hardening contract.
- `scripts/preflight-direct-draw-p1.py` guards GlassWall/WallPier/StructuralWall/Foundation authoring, Model-Space/unit-aware behavior, canonical `SetProperty` writes, active-DWG revalidation, live-generated verification, ownership/XData-aware rollback ordering, finite persisted paths and non-destructive post-commit UI synchronization.
- `scripts/preflight-direct-draw-openings.py` guards Door/Opening Family rules, authoritative width/source semantics, active-DWG revalidation before Auto Host, unique host verification, canonical property writes, exact-source rollback-before-project-restore, non-destructive post-commit UI finalization and the boundary that physical cutting is never invoked automatically.
- `scripts/preflight-all.py` auto-discovers all three gates.

GitHub Actions remain manual-only. A `continue all` source/docs request does not authorize workflow dispatch.

## Runtime qualification still required

Source implementation is not BricsCAD runtime proof. Before these commands are called production-ready, the exact source SHA still needs licensed interactive BricsCAD V25 x64 testing for:

1. compile against the exact V25 managed assemblies;
2. NETLOAD/DemandLoad registration;
3. Ribbon/Domain Hub invocation;
4. cancel-at-each-prompt behavior with no orphan CAD/project state;
5. source → semantic → native solid success for GlassWall/WallPier/StructuralWall/Foundation;
6. GlassWall backing host followed by Curtain path/frame workflow;
7. WallPier LINE/open-POLYLINE compatibility and specialized-profile boundary;
8. StructuralWall near-planar LINE tolerance and source-relative offset behavior;
9. Foundation closed-POLYLINE extrusion, drawing-unit behavior and save/reopen;
10. Door/Opening width/sill/clearance authoring and unique Auto Host matching across representative Floor/Zone/elevation cases;
11. ambiguous/unmatched Door/Opening host rollback with no orphan source/semantic state;
12. explicit physical `QS3DCUTOPENINGS` after a successful Door/Opening authoring flow, including straight and separately supported curved-host cases;
13. forced native-build failure followed by verified source/generated CAD cleanup and project rollback;
14. forced Palette/UI synchronization failure after successful native/host commit, verifying valid CAD/project state is preserved;
15. multi-DWG active-document switching guard, Unicode/HiDPI and representative private-DWG regression.

Until those gates are executed, the precise status is **source-implemented / statically guarded, runtime qualification pending**.

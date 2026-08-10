# QS3D Direct Draw P1 subset — implementation handoff

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
- Does **not** claim completion of Curtain perimeter/mullion/transom frames. Use `QS3DCURTAIN3D` / Curtain Hub for that workflow.

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

## Shared safety contract

P1 deliberately reuses existing QS3D infrastructure instead of adding another geometry engine:

1. create one real source entity in the active DWG;
2. capture exactly one semantic element with `SemanticCaptureService`;
3. apply explicit instance dimensions/offsets chosen by the user;
4. run canonical `QS3DBUILD3D` for complete-source, Model-Space, category, semantic-regeneration and native geometry checks;
5. require a non-empty `GeneratedSolidHandle` and verify that handle is still live;
6. select the generated result while keeping source CAD as authoritative editable geometry.

`QS3DBUILD3D` intentionally reports many user-facing failures instead of throwing them outward. Therefore the P1 wrapper performs the explicit live-generated-handle check; a reported-but-unbuilt result becomes a P1 failure and enters outer rollback rather than being treated as success.

Before source creation, P1 snapshots full project state and the pre-existing generated-owner set. On failure it discovers newly tagged generated output by project/element/category, restores project state, erases only operation-owned source/new output, and verifies requested rollback handles are no longer live. It must never erase unrelated user CAD merely because persisted textual Handles collide.

## Why Door / Opening Direct Draw is not included

`QS3DDRAWOPENING` and `QS3DDRAWDOOR` are intentionally **not** added in this P1 subset. Door/Opening authoring has extra decisions that should not be guessed:

- semantic host compatibility and automatic vs manual host selection;
- Floor/Zone/elevation ambiguity;
- opening dimensions and sill rules;
- whether physical boolean cutting is requested;
- straight vs curved host support;
- rollback when host relation succeeds but physical cutting cannot commit.

Until that contract is explicit, the existing capture + Auto Host/Manual Host + guarded cut commands remain the correct workflow.

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

Legacy Capture/Bóc chọn and `QS3DBUILD3D` remain supported for drawings that already contain source geometry.

## Static guards

- `scripts/preflight-direct-draw.py` continues to own the current P0 and shared `QS3DBUILD3D` hardening contract.
- `scripts/preflight-direct-draw-p1.py` guards the four P1 commands, command uniqueness, Ribbon/Hub discoverability, Model-Space/unit-aware behavior, source-relative offsets, fail-closed Family numerics, live-generated verification, ownership-aware rollback and the requirement to reuse canonical `QS3DBUILD3D`.
- `scripts/preflight-all.py` auto-discovers both gates.

GitHub Actions remain manual-only. A `continue all` source/docs request does not authorize workflow dispatch.

## Runtime qualification still required

Source implementation is not BricsCAD runtime proof. Before these commands are called production-ready, the exact source SHA still needs licensed interactive BricsCAD V25 x64 testing for:

1. compile against the exact V25 managed assemblies;
2. NETLOAD/DemandLoad registration;
3. Ribbon/Domain Hub invocation;
4. cancel-at-each-prompt behavior with no orphan CAD/project state;
5. source → semantic → native solid success for all four P1 categories;
6. GlassWall backing host followed by Curtain-frame workflow;
7. WallPier LINE/open-POLYLINE compatibility and specialized-profile boundary;
8. StructuralWall near-planar LINE tolerance and source-relative offset behavior;
9. Foundation closed-POLYLINE extrusion, drawing-unit behavior and save/reopen;
10. forced native-build failure followed by verified project/CAD rollback;
11. multi-DWG, Unicode/HiDPI and representative private-DWG regression.

Until those gates are executed, the precise status is **source-implemented / statically guarded, runtime qualification pending**.

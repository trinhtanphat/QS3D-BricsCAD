# QS3D Direct Draw P1 — implementation note

Updated: 2026-08-10 (UTC+7)

## Status

This note records the **source-implemented** Direct Draw P1 expansion on top of the hardened P0 authoring architecture.

It does not replace `docs/DIRECT-DRAW-WORKFLOW.md`; that document remains the product requirement and design boundary.

Implemented P1 commands:

- `QS3DDRAWGLASSWALL` — direct `GlassWall` authoring;
- `QS3DDRAWWALLPIER` — direct `WallPier` authoring;
- `QS3DDRAWSTRUCTWALL` — direct `StructuralWall` authoring;
- `QS3DDRAWFOUNDATION` — direct `Foundation` authoring.

P0 remains available unchanged:

- `QS3DDRAWWALL`;
- `QS3DDRAWBEAM`;
- `QS3DDRAWCOLUMN`;
- `QS3DDRAWSLAB`.

Existing capture commands remain supported for pre-existing CAD geometry.

## Shared authoring contract

P1 deliberately reuses the same `DirectDrawCommands.ExecuteDirect` orchestration and current P0 hardening:

1. require Model Space;
2. acquire guarded plan-view points with the unit-aware 5 mm vertical tolerance;
3. resolve the active compatible Family (or current category Family) and fail closed when an explicitly stored numeric Family value is invalid;
4. prompt/inherit the key instance dimensions and bottom offset before source creation;
5. create a real DWG source entity;
6. capture through `SemanticCaptureService`;
7. regenerate semantic/rule state before native CAD mutation;
8. reuse the existing native builder for the category;
9. preserve generated ownership/stale metadata and select the generated host when available;
10. on failure, restore `ProjectStateSnapshot` and remove the Direct Draw source/new owned output.

The rollback path keeps ownership-XData recovery through `GeneratedGeometryService.FindMatchingOwnedHandles`, so a generated solid that committed before project metadata completed can still be discovered for cleanup.

## Category-specific behavior

### GlassWall

`QS3DDRAWGLASSWALL` acquires an open plan path and prompts/inherits `ThicknessM`, `HeightM` and `BottomOffsetM`.

- two points create a `LINE` source;
- three or more points create an open `POLYLINE` source;
- native host geometry is generated through the current `WallSolidBuilder` / `PolylineWallSolidBuilder` contract;
- this command does not claim that every GlassWall path automatically receives the full Curtain frame overlay. Current Curtain authoring remains a separate guarded workflow.

### WallPier

`QS3DDRAWWALLPIER` prompts/inherits `ThicknessM`, `HeightM` and `BottomOffsetM`, but always creates an **open POLYLINE**, including the two-point case.

This is intentional: the generated solid must reach `PolylineWallSolidBuilder` and `WallPierPathProfilePlanner` so current `Rectangular` / `Chamfered` path-profile semantics and derived metadata are preserved. The P1 command must not silently downgrade a two-point WallPier to the generic LINE box path. Profile mode/chamfer remain inherited semantic/Family configuration rather than being guessed by Direct Draw.

### StructuralWall

`QS3DDRAWSTRUCTWALL` acquires exactly two plan-view points, prompts/inherits `ThicknessM`, `HeightM` and `BottomOffsetM`, and creates a real `LINE` source. Native output is delegated to `StructuralSolidBuilder`, including its current near-horizontal/fail-closed geometry guards.

### Foundation

`QS3DDRAWFOUNDATION` acquires a closed planar boundary with at least three vertices, prompts/inherits `ThicknessM` and `BottomOffsetM`, and creates a closed `POLYLINE` source. Native output is delegated to `StructuralSolidBuilder` using the same guarded footprint-mass path as existing captured Foundation geometry.

This Direct Draw command does not broaden Foundation rebar mesh support: `QS3DFOUNDATIONREBAR3D` still has its own rectangular-mesh adapter contract.

## UI

The `TẠO MỚI` Ribbon and Domain Hub expose all eight current Direct Draw commands. Capture/Bóc chọn and `QS3DBUILD3D` remain visible as compatibility workflows.

## Explicitly not implemented in this P1 batch

`QS3DDRAWOPENING` and `QS3DDRAWDOOR` are intentionally not added here.

Door/Opening authoring requires a host-aware commit contract covering semantic host resolution, supported physical-cut behavior, ambiguity handling and rollback of both opening source and host mutation. Adding a command that merely creates an unhosted rectangle would not satisfy the product requirement.

## Validation boundary

`scripts/preflight-direct-draw.py` contains static regression contracts for command uniqueness, current P0/P1 prompt/Family guards, builder reuse, authoring UI, rollback ordering, Model-Space/source resolution and the WallPier open-POLYLINE invariant.

Current wording must remain precise:

**source-implemented / static-regression-source-present is not the same as BricsCAD V25 runtime-verified.**

Before describing Direct Draw P1 as runtime complete, validate the exact current SHA on licensed BricsCAD V25 x64 with at minimum:

- create GlassWall, WallPier, StructuralWall and Foundation from an ordinary DWG;
- ESC/cancel and invalid-geometry rollback;
- active compatible Family inheritance and invalid-Family fail-closed behavior;
- native generated ownership and Health All;
- save/reopen and rebuild;
- WallPier rectangular/chamfered profile behavior;
- Foundation quantity/rebar compatibility;
- Ribbon/Domain Hub command invocation and selection sync.

GitHub Actions remain manual-only under `CI_POLICY.md`; this source implementation does not authorize workflow dispatch.

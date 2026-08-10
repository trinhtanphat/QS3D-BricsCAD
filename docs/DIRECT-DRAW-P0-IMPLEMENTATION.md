# QS3D Direct Draw P0 — implementation handoff

Updated: 2026-08-10 (UTC+7)

## Scope

This document records the first source implementation of the owner-required Direct Draw workflow defined in `docs/DIRECT-DRAW-WORKFLOW.md`.

QS3D remains a **BricsCAD V25 x64 .NET plugin**. Direct Draw operates inside the native BricsCAD editor/DWG database and does not introduce a standalone CAD engine.

## Implemented commands

The current P0 source adds:

- `QS3DDRAWWALL` — pick two or more plan-view points; two points create a LINE source, more points create an open POLYLINE source; capture as `ArchitecturalWall`; build owned native wall 3D immediately.
- `QS3DDRAWBEAM` — pick two points; create a LINE source; capture as `Beam`; build the existing native Beam prism immediately.
- `QS3DDRAWCOLUMN` — pick a column center; inherit Width/Depth from the active/first compatible Column Family when available (fallback 0.4 m), allow the user to confirm/edit dimensions, create a centered rectangular closed POLYLINE source, capture as `Column`, then build native 3D immediately.
- `QS3DDRAWSLAB` — pick at least three coplanar plan-view points and press Enter to finish; create a closed POLYLINE source; capture as `Slab`; build native 3D immediately.

The commands are implemented in:

- `src/QS3D.BricsCAD.V25/DirectDrawCommands.cs`

## Architecture

Direct Draw is intentionally a thin orchestration layer. It does **not** duplicate the existing geometry engines.

Current flow:

```text
BricsCAD Editor point acquisition
-> create a real LINE/POLYLINE source in the active DWG
-> set that source as implied selection
-> SemanticCaptureService.Capture(...)
-> existing Family/starter-Family + semantic/project contract
-> existing WallSolidBuilder / PolylineWallSolidBuilder / StructuralSolidBuilder
-> generated ownership + quantity/state updates
-> deterministic semantic regeneration
-> select the source + switch/show 3D view
```

This keeps Direct Draw and legacy capture workflows converged on the same semantic/native model.

## Transaction and rollback contract

Direct Draw creates persistent CAD source geometry before semantic capture because the existing model uses real DWG Handles as source provenance. Therefore the command owns an outer rollback boundary around the complete authoring operation.

Before creating source CAD it captures:

- `ProjectStateSnapshot` of the current project;
- the project-wide set of existing generated-owner handles.

If capture, native generation or semantic regeneration fails:

1. collect the just-created source Handle plus generated handles that did not exist before the operation;
2. restore the project snapshot;
3. erase only the failed operation's new source/generated CAD;
4. clear implied selection;
5. preserve/report rollback errors instead of silently hiding them.

Existing generated geometry is not intentionally erased by this cleanup path. The established native builders retain their own fail-closed generated ownership replacement checks.

## Reused product invariants

P0 deliberately reuses:

- `SemanticCaptureService` for category-safe active Family/starter Family behavior;
- `ProjectStateSnapshot` for project rollback;
- `GeneratedHandleOwnershipPolicy` for project-wide generated-handle classification;
- `WallSolidBuilder` for two-point/LINE ArchitecturalWall;
- `PolylineWallSolidBuilder` for open-POLYLINE ArchitecturalWall;
- `StructuralSolidBuilder` for Beam/Column/Slab;
- `GeneratedGeometryService` inside the established builders;
- current quantity, dirty/stale, health, save/reopen and semantic selection contracts downstream of normal capture.

Do not replace these with a Direct-Draw-specific parallel model or geometry engine.

## Static regression gate

`scripts/preflight-direct-draw.py` guards the P0 architecture. It checks, among other things:

- all four commands exist exactly once;
- Direct Draw still enters through `SemanticCaptureService`;
- an outer `ProjectStateSnapshot` rollback remains present;
- failure cleanup remains present;
- established wall/structural builders are reused;
- Direct Draw does not directly introduce its own `CreateBox`/`CreateExtrudedSolid` native geometry implementation;
- the existing builder dependencies retain generated replacement contracts.

`preflight-all.py` auto-discovers `preflight-*.py`, so this gate participates the next time the owner explicitly authorizes a source/preflight run. Adding this file does **not** authorize GitHub Actions.

## Sample DWG reference used for this implementation

The owner supplied a local/private sample named `MB MONG.dwg` for reference during this task.

The sample was used only as a local format/modeling reference. It was **not committed to Git** and must remain private unless the owner explicitly approves a sanitized repository-owned fixture.

The available local inspection identified it as a modern DWG in the AutoCAD 2018/2019/2020 file family and exposed BricsCAD/ODA/ACIS-related markers. Because the current execution environment does not provide a licensed interactive BricsCAD V25 session or a trustworthy full DWG semantic extractor, no exact layer/entity/solid inventory is claimed from that inspection. Agents must not invent details that were not runtime-verified.

Product implication retained from the sample: Direct Draw must create real BricsCAD-owned source/native geometry with stable DWG Handle provenance, not decorative preview-only objects or metadata-only stand-ins.

## Current boundary

This is **source implementation**, not current-SHA BricsCAD V25 runtime proof.

Before describing P0 as runtime-verified, a local agent with licensed interactive BricsCAD V25 must validate the exact source SHA for:

1. `NETLOAD`/DemandLoad and unique command registration;
2. `QS3DDRAWWALL` with both 2-point LINE and multi-point open POLYLINE paths;
3. `QS3DDRAWBEAM` two-point creation;
4. `QS3DDRAWCOLUMN` center + dimensions + rectangular footprint;
5. `QS3DDRAWSLAB` closed polygon creation;
6. source + semantic + generated ownership after each command;
7. `QS3DHEALTHALL` / ownership checks on valid objects;
8. save/reopen and `QS3DREGEN`;
9. selection/workspace synchronization;
10. ESC/cancel and forced native-build failure cleanup;
11. representative testing against a copy of the owner-provided `MB MONG.dwg` without committing that drawing;
12. Unicode/HiDPI and screenshots of the real BricsCAD runtime.

## Next implementation priorities

After P0 runtime qualification, continue with the remaining requirements from `docs/DIRECT-DRAW-WORKFLOW.md`:

- persistent/transient live preview where it can remain ownership-neutral;
- fast repeated authoring using the same active Family;
- primary Ribbon/Hub buttons for Tường / Dầm / Cột / Sàn while keeping Capture/Bóc chọn separate;
- P1 candidates: GlassWall, WallPier, StructuralWall, Foundation, Opening and Door;
- richer parameter/elevation/offset editing without weakening current category/geometry guards.

Do not claim production readiness until the exact release SHA passes the repository's licensed BricsCAD V25 runtime gate.
# QS3D Direct Draw P0 — implementation handoff

Updated: 2026-08-10 (UTC+7)

## Scope and current status

This document records the current source implementation of the owner-required Direct Draw workflow defined in `docs/DIRECT-DRAW-WORKFLOW.md`.

QS3D remains a **BricsCAD V25 x64 .NET plugin**. Direct Draw operates inside the native BricsCAD editor/DWG database and does not introduce a standalone CAD engine.

Current status is **source-implemented / static-regression-source-present**, with Ribbon + Domain Hub discoverability and BLT-style parameter prompts wired on `main`. This is **not** a claim that the exact current SHA has passed licensed BricsCAD V25 interactive runtime qualification.

## Implemented commands

The current P0 source includes:

- `QS3DDRAWWALL` — pick two or more plan-view points; two points create a LINE source, more points create an open POLYLINE source; prompt/inherit `ThicknessM`, `HeightM` and `BottomOffsetM`; capture as `ArchitecturalWall`; build owned native wall 3D immediately.
- `QS3DDRAWBEAM` — pick two plan-view points; create a LINE source; prompt/inherit `WidthM`, `HeightM` and `BottomOffsetM`; capture as `Beam`; build the existing native Beam prism immediately.
- `QS3DDRAWCOLUMN` — pick a column center; prompt/inherit `WidthM`, `DepthM`, `HeightM` and `BottomOffsetM`; create a centered rectangular closed POLYLINE source; capture as `Column`; build native 3D immediately.
- `QS3DDRAWSLAB` — pick at least three coplanar plan-view points and press Enter to finish; prompt/inherit `ThicknessM` and `BottomOffsetM`; create a closed POLYLINE source; capture as `Slab`; build native 3D immediately.

The command implementation is in:

- `src/QS3D.BricsCAD.V25/DirectDrawCommands.cs`

The four P0 actions are also exposed in the QS3D **TẠO MỚI** Ribbon workflow and in the Full Domain Hub. Existing Capture/Bóc chọn actions remain separate for converting CAD that already exists.

## Current authoring guards

P0 deliberately fails closed instead of silently changing user geometry:

- Direct Draw currently runs only when the active drawing space is **Model Space**. PaperSpace/Layout invocation is rejected before source creation.
- picked Wall/Beam/Slab path points must remain plan-view within **5 mm vertical tolerance**, evaluated after converting drawing units to meters rather than using a raw drawing-unit epsilon;
- existing wall/structural native builders retain their own source-type, planarity, finite-number and geometry guards;
- unsupported/mixed `QS3DBUILD3D` batches are rejected before the first native builder commit;
- active/compatible Family defaults are reused, while prompted P0 values become instance properties for the new semantic element.

Rotated/non-world UCS behavior is intentionally left in the licensed runtime qualification gate rather than guessed in source without a real BricsCAD V25 session.

## Architecture

Direct Draw is intentionally a thin orchestration layer. It does **not** duplicate the existing geometry engines.

Current flow:

```text
BricsCAD Editor point acquisition
-> create a real LINE/POLYLINE source in Model Space
-> set that source as implied selection
-> SemanticCaptureService.Capture(...)
-> existing Family/starter-Family + semantic/project contract
-> apply prompted instance dimensions/offset
-> deterministic semantic regeneration before native mutation
-> existing WallSolidBuilder / PolylineWallSolidBuilder / StructuralSolidBuilder
-> generated ownership + quantity/state updates
-> select generated host when available
-> switch/show 3D view
```

This keeps Direct Draw and legacy capture workflows converged on the same semantic/native model. `QS3DWALL`, `QS3DBEAM`, `QS3DCOLUMN`, `QS3DSLAB` and `QS3DBUILD3D` remain supported for pre-existing CAD.

## Transaction and rollback contract

Direct Draw creates persistent CAD source geometry before semantic capture because the existing model uses real DWG Handles as source provenance. Therefore the command owns an outer rollback boundary around the complete authoring operation.

Before creating source CAD it captures:

- `ProjectStateSnapshot` of the current project;
- the project-wide set of existing generated-owner handles.

The command performs semantic regeneration **before** calling a native builder so dependency/rule failures happen before Solid3d mutation whenever possible.

If capture, semantic regeneration or native generation fails:

1. collect the just-created source Handle plus generated handles newly visible in project ownership;
2. when the semantic element id is known, scan Model Space for QS3D XData ownership matching that exact project/element/category via `GeneratedGeometryService.FindMatchingOwnedHandles(...)`; this also finds tagged native output that may have committed before project handle metadata was written;
3. restore the project snapshot;
4. erase only the failed operation's source/new owned output;
5. verify requested cleanup handles are no longer live instead of swallowing per-entity erase failures;
6. clear implied selection;
7. preserve/report ownership-discovery, project-restore and CAD-cleanup errors together with the original operation error.

Existing generated geometry that predates the Direct Draw operation remains protected through the pre-operation generated-handle snapshot and the established generated ownership checks.

## Reused product invariants

P0 deliberately reuses:

- `SemanticCaptureService` for category-safe active Family/starter Family behavior;
- `ProjectStateSnapshot` for project rollback;
- `GeneratedHandleOwnershipPolicy` for project-wide generated-handle classification;
- `GeneratedGeometryService` XData ownership for native generated-host provenance and orphan discovery;
- `WallSolidBuilder` for two-point/LINE ArchitecturalWall;
- `PolylineWallSolidBuilder` for open-POLYLINE ArchitecturalWall;
- `StructuralSolidBuilder` for Beam/Column/Slab;
- current quantity, dirty/stale, health, save/reopen and semantic selection contracts downstream of normal capture;
- the shared semantic-source resolver used by rebuild flows so selecting semantic/generated output can resolve back to live source geometry where supported.

Do not replace these with a Direct-Draw-specific parallel model or geometry engine.

## Static regression gate

`scripts/preflight-direct-draw.py` guards the P0 architecture. The current contract checks include, among other things:

- all four Direct Draw commands and legacy capture/build commands remain uniquely registered;
- Direct Draw still enters through `SemanticCaptureService` and performs semantic regeneration before native mutation;
- all four P0 entry points retain the Model Space guard;
- the planarity threshold is unit-aware and remains 5 mm;
- established wall/structural builders are reused rather than duplicating `CreateBox`/`CreateExtrudedSolid` inside Direct Draw;
- generated XData ownership discovery remains wired into failed-operation cleanup;
- project restore occurs before final CAD cleanup;
- CAD cleanup does not swallow erase failures and verifies requested handles are no longer live;
- Ribbon and Domain Hub keep the P0 creation actions visible;
- `QS3DBUILD3D` rejects mixed atomicity hazards before native commit.

`preflight-all.py` auto-discovers `preflight-*.py`. Repository policy remains manual-only: source/docs work and `continue all` do **not** authorize GitHub Actions or release workflows.

## Sample DWG reference used for this implementation

The owner supplied a local/private sample named `MB MONG.dwg` for reference during this task.

The sample is a **private runtime/reference fixture**. It was not committed to Git and must remain private unless the owner explicitly approves a sanitized repository-owned fixture.

The available local inspection identified it as a modern DWG in the AutoCAD 2018/2019/2020 file family and exposed BricsCAD/ODA/ACIS-related markers. Because the current execution environment does not provide a licensed interactive BricsCAD V25 session or a trustworthy full DWG semantic extractor, no exact layer/entity/solid inventory is claimed from that inspection. Agents must not invent details that were not runtime-verified.

Product implication retained from the sample: Direct Draw must create real BricsCAD-owned source/native geometry with stable DWG Handle provenance, not decorative preview-only objects or metadata-only stand-ins.

## Current validation boundary

The current source could not be freshly cloned/executed in the available container because that environment could not resolve `github.com`. GitHub Actions were intentionally **not** dispatched. Therefore static regression source is present, but this handoff does not claim a freshly executed green preflight/build for the latest `main`.

Before describing P0 as runtime-verified, a local agent with licensed interactive BricsCAD V25 must validate the exact source SHA for:

1. Release/x64 compile against the exact installed V25 managed assemblies, then `NETLOAD`/DemandLoad and unique command registration;
2. `QS3DDRAWWALL` with 2-point LINE and multi-point open POLYLINE paths, including prompted thickness/height/bottom offset;
3. `QS3DDRAWBEAM` with prompted width/height/bottom offset;
4. `QS3DDRAWCOLUMN` center + width/depth/height/bottom offset + rectangular footprint;
5. `QS3DDRAWSLAB` closed polygon + thickness/bottom offset;
6. Model Space success and PaperSpace/Layout fail-closed behavior;
7. drawing units such as millimeter and meter, plus the 5 mm planarity boundary;
8. World UCS and representative rotated UCS behavior so point acquisition/source creation is proven rather than inferred;
9. source + semantic + generated ownership after each command;
10. selection/workspace synchronization, including generated-host selection and rebuild back to the live semantic source;
11. `QS3DHEALTHALL`, ownership checks, BQ/quantities and supported rebar downstream workflows;
12. save/reopen and `QS3DREGEN` / `QS3DBUILD3D` rebuild;
13. ESC/cancel and forced semantic/native-build failure cleanup, including proof that no tagged orphan Solid3d remains;
14. representative testing against a copy of the owner-provided `MB MONG.dwg` without committing that drawing;
15. Unicode/HiDPI and screenshots of the real BricsCAD runtime.

## Next implementation priorities

Ribbon/Domain Hub exposure and the primary P0 dimension/elevation prompts are already implemented. After P0 runtime qualification, remaining product priorities are:

- persistent/transient live preview where it can remain ownership-neutral;
- fast repeated authoring using the same active Family and previous command parameters;
- more polished dynamic input/property-pane coordination without weakening cancellation/atomicity;
- P1 candidates: GlassWall, WallPier, StructuralWall, Foundation, Opening and Door;
- broader native geometry authoring only where existing guarded builders/Core planners can support it safely.

Do not claim production readiness until the exact release SHA passes the repository's licensed BricsCAD V25 runtime gate.

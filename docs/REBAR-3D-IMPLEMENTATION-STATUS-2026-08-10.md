# QS3D Rebar 3D implementation status — 2026-08-10

This is the current source-level handoff for generated reinforcement geometry. It supplements the broader project implementation status without claiming licensed BricsCAD V25 runtime verification.

## Current command matrix

| Workflow | Command | Semantic source | Generated handle family |
|---|---|---|---|
| Column longitudinal bars | `QS3DREBAR3D` | Column + rectangular closed POLYLINE | `GeneratedRebarHandles` |
| Column ties | `QS3DREBARTIES3D` | Column + rectangular closed POLYLINE | `GeneratedTieRebarHandles` |
| Beam longitudinal bars | `QS3DBEAMREBAR3D` | Beam + nearly-horizontal LINE | `GeneratedRebarHandles` |
| Beam stirrups | `QS3DREBARSTIRRUP3D` | Beam + nearly-horizontal LINE | `GeneratedBeamStirrupHandles` |
| Slab rectangular X/Y mesh | `QS3DSLABREBAR3D` | Slab + rectangular closed POLYLINE | `GeneratedRebarHandles` |
| Structural-wall H/V mesh | `QS3DWALLREBAR3D` | StructuralWall + nearly-horizontal LINE | `GeneratedRebarHandles` |
| BBS shape bars | `QS3DREBAR3DSHAPE` | supported semantic host + BBS shape properties | `GeneratedShapeRebarHandles` |
| Rebar workspace | `QS3DREBARHUB` | modeless UI | n/a |
| Full model health | `QS3DHEALTHALL` | project + live CAD handles | n/a |
| Combined rebar health | `QS3DREBARHEALTHALL` | generated reinforcement | n/a |
| Rebar mode/category health | `QS3DREBARMODEHEALTH` | generic generated rebar metadata | n/a |

Do not reintroduce the obsolete/unregistered names `QS3DBEAMSTIRRUP3D` or `QS3DBEAMSTIRRUPHEALTH`. Current beam stirrup commands are `QS3DREBARSTIRRUP3D` and `QS3DREBARSTIRRUPHEALTH`.

## Deterministic Core planners

Implemented Core planning now includes:

- `RectangularRebarLayoutPlanner` — column perimeter layout;
- `BeamLongitudinalRebarPlanner`;
- `LinearRebarLayoutPlanner` — common count/spacing distribution;
- `BeamStirrupLayoutPlanner`;
- `RebarShapePathBuilder` — BBS shape path validation;
- `RectangularSlabMeshPlanner` — X/Y mesh, bottom/top/both, cover/layer stacking;
- `RectangularWallMeshPlanner` — horizontal/vertical mesh, near/far/both, cover/layer stacking.

Core planners validate finite values, cover, count/spacing ambiguity, usable geometry and bounded bar counts before CAD mutation.

## Generic rebar slot modes

`GeneratedRebarHandles` is intentionally reused for generated longitudinal/mesh workflows where one semantic element owns one generic rebar output set at a time.

Recognized modes:

- `ColumnVerticalBars` → `Column`;
- `BeamLongitudinalBars` → `Beam`;
- `SlabMeshXY` → `Slab`;
- `StructuralWallMesh` → `StructuralWall`.

Slab/StructuralWall builders refuse to replace a generic handle slot carrying another mode. `GeneratedRebarModeHealthService` validates mode/category and mode-specific metadata.

The first native Slab and StructuralWall mesh adapters require the two directions to share one diameter so they can use the established generic ownership/health contract. Core mesh planning already supports different directional diameters.

## Cross-set destructive ownership

Generated geometry replacement must reserve all active ownership domains before erasing anything:

- semantic `SourceHandles`;
- `GeneratedSolidHandle`;
- `PhysicalOpeningCutSolidHandle`;
- `GeneratedRebarHandles`;
- `GeneratedShapeRebarHandles`;
- `GeneratedTieRebarHandles`;
- `GeneratedBeamStirrupHandles`.

A live handle must belong to the exact element/property key requesting deletion and must still resolve to a `Solid3d`. Collision or a live non-solid target is a hard failure.

## Generated stale lifecycle

`ProjectElement` tracks stale snapshots independently for:

1. generated host solid;
2. generic longitudinal/mesh rebar;
3. BBS shape rebar;
4. column ties;
5. beam stirrups.

Geometry/Properties/Relations changes mark current outputs stale. Quantity-only dirty state does not. Replacing/removing the snapshotted handles resolves the relevant stale family; health uses the stale APIs rather than raw `Dirty != None`.

`GeneratedDependentGeometryInvalidator` removes host/dependent generated outputs after source geometry changes, including column ties and beam stirrups, and clears stale metadata after successful invalidation metadata commit.

## Health commands

- `QS3DHEALTHALL` — broad semantic/generated/rebar review and generated-handle Locate.
- `QS3DREBARHEALTHALL` — combined reinforcement health.
- `QS3DREBARHEALTH` — generic longitudinal/shape compatibility path.
- `QS3DREBARTIEHEALTH` — column ties.
- `QS3DREBARSTIRRUPHEALTH` — beam stirrups.
- `QS3DREBARSHAPEHEALTH` — BBS shape solids.
- `QS3DREBARMODEHEALTH` — generic mode/category/metadata validation.

## UI

`QS3DREBARHUB` opens a dedicated modeless Rebar 3D Hub that exposes current Column, Beam, Slab, StructuralWall and BBS-shape workflows plus health commands. It is intentionally independent from the heavily shared main Domain Hub/Ribbon so concurrent UI work cannot make advanced reinforcement unavailable.

`scripts/preflight-command-wiring.py` checks that QS3D XAML/Ribbon command references resolve to exactly one `CommandMethod` owner. The Rebar Hub has its own XML/command and BricsCAD `Application` binding gates.

## Static gates

`scripts/preflight-all.py` automatically discovers feature gates. Current reinforcement-related source gates include the planners/builders, ownership, stale lifecycle, Health All, command wiring, Rebar Hub, Beam rebar/stirrups, Slab mesh, StructuralWall mesh and target-framework compatibility.

Both GitHub workflows remain `workflow_dispatch` only. Adding source or gates is not authorization to run GitHub Actions.

## Runtime-gated / not claimed complete

Still requires actual licensed BricsCAD V25/private-DWG evidence:

- newest full plugin compile against the installed V25 managed assemblies;
- NETLOAD/DemandLoad after the newest reinforcement batches;
- `Solid3d` boolean/union behavior for ties/stirrups/shapes on real drawings;
- large-model performance and redraw/undo behavior;
- visual review of generated mesh placement, section boxes and isolate/focus workflows;
- engineering-specific hooks, bend radii, laps, anchors, wall-opening reinforcement and irregular slab mesh clipping;
- code/design compliance calculations. QS3D generated rebar geometry is currently a deterministic semantic/takeoff/review representation, not an automatic structural-design engine.

# QS3D command reference

Updated for the current source baseline on 2026-08-10. Commands that create or mutate native BricsCAD geometry remain subject to the licensed V25 runtime gate.

## Workspace and project

- `QS3D` — open the docked QS3D workspace.
- `QS3DHIDE` — hide QS3D palettes.
- `QS3DDOMAIN` — open the Full Domain Hub.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — project persistence and deterministic regeneration.
- `QS3DINSPECT` — inspect current/prompted CAD selection and synchronize the workspace review/property pane.
- `QS3DHEALTH` — run Model Health.
- `QS3DRUNTIMEPROBE` — inspect V25 runtime availability/identity.

## BLT-style property workflow

The workspace property pane has two scopes:

- **Family / Type** — edits the selected Family defaults. Existing elements that still equal the previous Family value inherit the change; true instance overrides are preserved.
- **Đối tượng / Instance** — automatically selected when exactly one semantic element matches the CAD selection. Edits affect only that element. The `↺` action resets an override back to the current Family value.

Common boolean fields use a checkbox, mode/material/classification-like fields use editable choices, and other fields use text/numeric editors with finite-value validation. Selection matching uses semantic reference handles, so auto-room boundary provenance can participate without duplicating ownership handles.

## Semantic capture

### Room / finishes

- `QS3DROOM` — capture selected CAD source as Room.
- `QS3DROOMAUTO` — discover bounded room faces from selected planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges are tessellated deterministically; SPLINE is sampled by bounded chord length before the Core engine splits intersections/T-junctions, snaps endpoints, removes dangling bridges and calculates room area/perimeter.
- ARC/POLYLINE plan-view orientation is validated and LINE/ARC/POLYLINE/SPLINE sampled elevations must remain within `RoomBoundaryToleranceM`.
- Room Auto metadata: `RoomBoundaryToleranceM` (default `0.005`), `RoomBoundaryMinimumAreaM2` (default `0.5`), `RoomBoundaryArcSagittaM` (default `0.002`), `RoomBoundarySplineChordM` (default `0.02`). SPLINE sampling is capped.
- Room Auto lifecycle is non-destructive: stable source provenance reuses rooms when possible; topology split/merge can mark superseded rooms `Stale` instead of deleting them.
- `QS3DFINISH` — generate/synchronize room finish semantics.

### Tường KT / Cửa

- `QS3DWALL` — capture Tường Gạch / ArchitecturalWall.
- `QS3DGLASSWALL` — capture Vách Kính / GlassWall.
- `QS3DWALLPIER` — capture Trụ Tường / WallPier.
- `QS3DWALLJUNCTIONS` — analyze selected LINE/open-POLYLINE wall centerlines and classify L/T/X/Straight/End/Multi junction nodes. The report also includes a reviewable endpoint snap plan.
- `QS3DWALLSNAPPREVIEW` — calculate endpoint cleanup for **QS3D semantic wall source** LINE/open straight POLYLINE only. It records a SHA-256 preview signature; curved/bulged polylines are review-only and are not moved automatically.
- `QS3DWALLSNAPAPPLY` — apply the previously previewed endpoint moves only when the current selection/geometry/tolerances still hash to the same preview signature. Changed source geometry requires previewing again. Affected semantic owners are marked `Geometry|Quantity` dirty after mutation.
- Wall snap metadata: `WallJunctionToleranceM` (default `0.005`), `WallJunctionPlanarityToleranceM` (defaults to junction tolerance), `WallJunctionSnapEpsilonM` (small movement/no-op epsilon).
- All three Tường KT variants use the guarded `QS3DBUILD3D` LINE/open-POLYLINE centerline pipeline. Polyline bulges are tessellated before wall-footprint generation.
- `QS3DOPENING` — capture Lỗ Mở Vách.
- `QS3DDOOR` — capture Cửa Đi.
- `QS3DAUTOLINKHOSTS` — safely match selected Door/Opening semantics to the nearest compatible wall host. Matching uses wall surface gap rather than centerline distance alone, groups tessellated segments by semantic host, rejects near-tie ambiguity, respects Floor/Zone when assigned, and applies an independent source-elevation gate before linking. It does **not** silently run the physical boolean cut.
- Auto Host metadata: `AutoHostMaxGapM` (default `0.25`), `AutoHostAmbiguityM` (default `0.02`), `AutoHostElevationToleranceM` (default `0.25`), plus `WallArcSagittaM` for curved polyline centerline matching.
- `QS3DLINKHOST` — manually link selected Door/Opening semantics to a selected wall host; this remains the explicit override workflow.
- `QS3DCUTOPENINGS` — physically subtract linked Door/Opening cutters from supported generated wall solids. LINE hosts are supported for ArchitecturalWall, GlassWall, WallPier and StructuralWall. Straight non-bulged POLYLINE hosts are also supported when the opening safely projects to one segment and does not cross a corner/junction. Curved/bulged polyline cuts are rejected rather than approximated silently.
- The physical-cut fingerprint includes live host/opening placement and dimensions; changed geometry on an already-cut generated solid requires rebuilding the host before re-cutting.

### Structure / earthwork

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff from current CAD selection.

## Native 3D

- `QS3DBUILD3D` — create/update generated `Solid3d` for supported selected semantic elements.
- Tường Gạch / Vách Kính / Trụ Tường: LINE or open POLYLINE centerline; bulges are tessellated and guarded miter/bevel footprint logic is used.
- The current Vách Kính/Trụ Tường path is a generic Tường KT extrusion, not a full curtain-wall/pier authoring system.
- LINE source: beam, structural wall, railing.
- Closed POLYLINE source: slab, column, foundation, stair footprint mass, earthwork footprint mass.
- Earthwork is extruded downward by `DepthM`.

## Recognition, quantity and rebar

- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` — deterministic recognition + review/auto-accept.
<<<<<<< HEAD
- `QS3DBQ` — quantity summary, filtering/grouping/Locate/XLSX.
=======
- `QS3DB4D` — scan every entity in the active Current Space; read curve/closed-area/Region/Hatch/Solid3d metrics, auto-apply only high-confidence recognition and leave the rest in the review window.
- `QS3DBQ`, `QS3DED2` — quantity summary and XLSX workflow. Exported rows include stable QS3D Element IDs and hexadecimal CAD handles.
- `QS3DEXCELLOCATE` — choose an `.xlsx` file and row, resolve exported QS3D handles or legacy BLT hidden `$<decimal handle>` tokens, then select and zoom the live CAD entities.
>>>>>>> 1f2557c (feat: add B4D scan and Excel handle round-trip)
- `QS3DBBSVIEW` — BBS review/Locate window.
- `QS3DBBS` — BBS XLSX export.
- `QS3DBBSCSV` — UTF-8 CSV export with spreadsheet formula-injection guards.
- `QS3DREBAR3D` — guarded rectangular-column longitudinal-bar `Solid3d` generation.
- `QS3DREBARHEALTH` — verify generated column-bar ownership/handle/count state.
- `QS3DREBAR3DSHAPE` — generate supported BBS-shape-driven 3D bars. Source supports straight and configured L/U/Z/custom leg/turn paths; shape metadata and total cutting length are validated before geometry mutation.
- `QS3DREBARSHAPEHEALTH` — verify generated shape-bar ownership/handle state.

See [`ADVANCED-GEOMETRY.md`](ADVANCED-GEOMETRY.md) for shape metadata and current geometric limits.

## Review / viewport

- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT` — transient selected-object highlighting.
- `QS3DFOCUS` — focus/zoom the current selection with review emphasis.
- `QS3DISOLATE`, `QS3DUNISOLATE` — temporarily isolate selected objects and restore hidden objects.
- `QS3DSECTIONBOX` — launch BricsCAD's native `BIMSECTION` **Detail** workflow to create an interactive detail-section volume/box. Existing implied selection is highlighted as visual context only. This command requires an edition/runtime that provides `BIMSECTION`; source presence is not treated as proof that the current BricsCAD license exposes the BIM command.
- `QS3DSECTIONPLANE` — launch native `SECTIONPLANE` for a standard interactive section entity when a full BIM Detail volume is not desired.
- `QS3DCLIPDISPLAY` — launch native `CLIPDISPLAY` to toggle clipping for a selected/prompted section entity.
- Native command wrappers deliberately use English command/option names with the `_` localization prefix (`_BIMSECTION _Detail`, `_SECTIONPLANE`, `_CLIPDISPLAY`) rather than hard-coding localized command text.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport controls.
- `QS3DLOCATE` — locate a semantic element by ID/reference handles.
- `QS3DUNTRACK`, `QS3DUNTRACKFINISH` — remove semantic tracking without deleting source CAD.

## Revision

- `QS3DREVBASE`, `QS3DREVDIFF` — revision baseline/delta workflow.
<<<<<<< HEAD
=======
- `QS3DLOCATE` also follows semantic dependencies, so a generated room-finish element resolves back to its room geometry.
- `QS3DUNTRACKFINISH` removes only finish semantics resolved from the selected room/source geometry and never erases CAD entities.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport commands.
- `QS3DLOCATE` — locate a semantic element by ID.
- `QS3DUNTRACK`, `QS3DUNTRACKFINISH` — remove QS3D semantic tracking without deleting the source CAD object.
>>>>>>> 1f2557c (feat: add B4D scan and Excel handle round-trip)

## UI entry points

The main palette, Ribbon and Full Domain Hub expose the major product flows consistently: Room Auto, Tường KT, Giao tường + review-gated snap cleanup, Auto/Manual Door-Opening host linking, physical cuts, Build 3D, Focus/Isolate/Section Box, BQ/BBS, column rebar, shape rebar, health checks and revision tools. The goal is to minimize command-line memorization while preserving explicit commands for power users and test harnesses.

## Packaging and autoload

- `scripts/package-v25.ps1` creates the V25 release ZIP, excludes proprietary BricsCAD assemblies, generates `COMMANDS.txt` from current QS3D `CommandMethod` declarations, records metadata and SHA-256 hashes.
- The package includes DemandLoad install/uninstall helpers with hash verification and optional Authenticode enforcement.
- DemandLoad/NETLOAD remain part of the licensed V25 runtime gate; source presence is not treated as runtime verification.

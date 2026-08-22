# QS3D command reference

Updated for the current source baseline on 2026-08-10. Commands that create native BricsCAD geometry remain subject to the licensed V25 runtime gate described below.

## Workspace and project

- `QS3D` — open the docked QS3D workspace.
- `QS3DHIDE` — hide QS3D palettes.
- `QS3DDOMAIN` — open the Full Domain Hub.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — project persistence and deterministic regeneration.
- `QS3DINSPECT` — inspect the current/prompted CAD selection and synchronize the workspace review pane.
- `QS3DHEALTH` — run Model Health.
- `QS3DRUNTIMEPROBE` — inspect V25 runtime availability/identity.

## Semantic capture

### Room / finishes

- `QS3DROOM` — capture selected CAD source as Room.
- `QS3DROOMAUTO` — discover bounded room faces from selected planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges are tessellated deterministically; SPLINE is sampled by bounded chord length before the Core engine splits intersections/T-junctions, snaps endpoints, removes dangling bridges and calculates room area/perimeter.
- ARC/POLYLINE plan-view orientation is validated and LINE/ARC/POLYLINE/SPLINE sampled elevations must remain within `RoomBoundaryToleranceM`, preventing mixed-Z geometry from being flattened silently.
- Room Auto project metadata: `RoomBoundaryToleranceM` (default `0.005`), `RoomBoundaryMinimumAreaM2` (default `0.5`), `RoomBoundaryArcSagittaM` (default `0.002`), `RoomBoundarySplineChordM` (default `0.02`). SPLINE sampling is capped to prevent unbounded segment generation.
- Room Auto lifecycle: a changed boundary with the same normalized source-handle set reuses the existing Room record; a topology split/merge marks superseded auto Rooms `Stale` instead of deleting them. Stale Rooms and direct dependents are excluded from BQ while remaining in `.qsdb` for audit/recovery.
- `QS3DFINISH` — generate/synchronize room finish semantics. For an auto Room, select the full boundary source set so a shared wall does not accidentally target both adjacent rooms.

### Tường KT / Cửa

- `QS3DWALL` — capture Tường Gạch / ArchitecturalWall semantics.
- `QS3DGLASSWALL` — capture Vách Kính / GlassWall semantics and create safe starter Family properties if no GlassWall family exists yet.
- `QS3DWALLPIER` — capture Trụ Tường / WallPier semantics and create safe starter Family properties if no WallPier family exists yet.
- All three Tường KT variants use the guarded `QS3DBUILD3D` LINE/open-POLYLINE centerline pipeline. Polyline bulges are tessellated before the deterministic wall-footprint engine constructs the extrusion profile.
- `QS3DOPENING` — capture Lỗ Mở Vách.
- `QS3DDOOR` — capture Cửa Đi.
- `QS3DLINKHOST` — link a selected Door/Opening semantic element to a selected wall host.
- `QS3DCUTOPENINGS` — physically subtract linked Door/Opening cutters from compatible generated **LINE-host** wall solids: ArchitecturalWall/Tường Gạch, GlassWall/Vách Kính, WallPier/Trụ Tường and StructuralWall/Vách BTCT. The idempotence fingerprint includes live host/opening geometry and dimensions; if geometry changes on the same already-cut solid, rebuild the host 3D first and then cut again.

### Structure / earthwork

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff from the current CAD selection.

## Native 3D

- `QS3DBUILD3D` — create/update generated `Solid3d` for supported selected semantic elements.
- Tường Gạch / Vách Kính / Trụ Tường: LINE or open POLYLINE centerline. Polyline bulges are tessellated; the Core wall-footprint engine handles deterministic miter joins with guarded bevel fallback.
- The current Vách Kính/Trụ Tường geometry path intentionally reuses the generic Tường KT centerline extrusion. Dedicated curtain-wall framing/panel systems or specialized pier profiles/material display behavior remain later product/runtime work.
- LINE source: beam, structural wall, railing.
- Closed POLYLINE source: slab, column, foundation, stair footprint mass, earthwork footprint mass.
- Earthwork is extruded downward by `DepthM`.
- Native 3D commands remain release-gated until tested by NETLOAD on licensed BricsCAD V25.

## Recognition, quantity and rebar

- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` — deterministic recognition + review/auto-accept.
- `QS3DB4D` — scan every entity in the active Current Space; read curve/closed-area/Region/Hatch/Solid3d metrics, auto-apply only high-confidence recognition and leave the rest in the review window.
- `QS3DBQ`, `QS3DED2` — quantity summary and XLSX workflow. Exported rows include stable QS3D Element IDs and hexadecimal CAD handles.
- `QS3DEXCELLOCATE` — choose an `.xlsx` file and row, resolve exported QS3D handles or legacy BLT hidden `$<decimal handle>` tokens, then select and zoom the live CAD entities.
- `QS3DBBSVIEW` — BBS review/Locate window.
- `QS3DBBS` — BBS XLSX export.
- `QS3DBBSCSV` — UTF-8 CSV export with spreadsheet formula-injection guards.
- `QS3DREBAR3D` — guarded rectangular column longitudinal-rebar `Solid3d` generation from a Column semantic element with compatible rectangle source + rebar notation. Current source path is intentionally narrow; general beam/slab/wall/stirrup/shape authoring is not claimed complete.

## Revision and viewport

- `QS3DREVBASE`, `QS3DREVDIFF` — revision baseline/delta workflow.
- `QS3DLOCATE` also follows semantic dependencies, so a generated room-finish element resolves back to its room geometry.
- `QS3DUNTRACKFINISH` removes only finish semantics resolved from the selected room/source geometry and never erases CAD entities.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport commands.
- `QS3DLOCATE` — locate a semantic element by ID.
- `QS3DUNTRACK`, `QS3DUNTRACKFINISH` — remove QS3D semantic tracking without deleting the source CAD object.

## BLT-style UI workflow

The primary palette now supports a command-light workflow:

1. select a model group in the left semantic tree;
2. select/create a Family/Type;
3. select CAD entities in BricsCAD;
4. press **Bóc chọn** to dispatch the category-appropriate semantic command;
5. edit grouped Vietnamese properties;
6. use **Vẽ 3D** where a native adapter exists;
7. review/Locate quantities through BQ/BBS/Model Health.

The Ribbon and Full Domain Hub expose the same key Tường KT, Cửa/Lỗ, physical-cut and rebar workflows so features are not hidden behind command-line knowledge.

## Packaging and autoload

- `scripts/package-v25.ps1` creates the V25 release ZIP, excludes proprietary BricsCAD assemblies, generates `COMMANDS.txt` from current QS3D `CommandMethod` declarations, records package metadata and SHA-256 hashes.
- The package includes `install-v25-autoload.ps1` and `uninstall-v25-autoload.ps1` for per-user BricsCAD V25 Registry DemandLoad installation/removal, with hash verification and optional Authenticode enforcement.
- DemandLoad/NETLOAD remain part of the licensed V25 runtime gate; source presence is not treated as runtime verification.

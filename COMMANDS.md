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
- `QS3DROOMAUTO` — discover bounded room faces from selected LINE/POLYLINE networks. Straight segments and polyline bulges are converted to metric planar segments; bulges are tessellated deterministically before the Core engine splits intersections/T-junctions, snaps endpoints, removes dangling bridges and calculates room area/perimeter.
- Room Auto project metadata: `RoomBoundaryToleranceM` (default `0.005`), `RoomBoundaryMinimumAreaM2` (default `0.5`), `RoomBoundaryArcSagittaM` (default `0.002`).
- Room Auto lifecycle: a changed boundary with the same normalized source-handle set reuses the existing Room record; a topology split/merge marks superseded auto Rooms `Stale` instead of deleting them. Stale Rooms and direct dependents are excluded from BQ while remaining in `.qsdb` for audit/recovery.
- `QS3DFINISH` — generate/synchronize room finish semantics. For an auto Room, select the full boundary source set so a shared wall does not accidentally target both adjacent rooms.

### Tường KT / Cửa

- `QS3DWALL` — capture Tường Gạch / ArchitecturalWall semantics. The current 3D builder supports LINE and open plan-view POLYLINE centerlines.
- `QS3DGLASSWALL` — explicit Vách Kính semantic capture with safe default family properties if no GlassWall family exists yet.
- `QS3DWALLPIER` — explicit Trụ Tường semantic capture with safe default family properties if no WallPier family exists yet.
- `QS3DOPENING` — capture Lỗ Mở Vách.
- `QS3DDOOR` — capture Cửa Đi.
- `QS3DLINKHOST` — link a selected Door/Opening semantic element to a selected wall host.
- `QS3DCUTOPENINGS` — physically subtract linked Door/Opening cutters from supported generated LINE-host wall solids. The idempotence fingerprint includes live host/opening geometry and dimensions; if geometry changes on the same already-cut solid, rebuild the host 3D first and then cut again.

### Structure / earthwork

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff from the current CAD selection.

## Native 3D

- `QS3DBUILD3D` — create/update generated `Solid3d` for supported selected semantic elements.
- ArchitecturalWall/Tường Gạch: LINE or open POLYLINE centerline. Polyline bulges are tessellated; the Core wall-footprint engine handles deterministic miter joins with guarded bevel fallback.
- LINE source: beam, structural wall, railing.
- Closed POLYLINE source: slab, column, foundation, stair footprint mass, earthwork footprint mass.
- Earthwork is extruded downward by `DepthM`.
- Vách Kính and Trụ Tường currently have explicit semantic capture/UI workflow; their dedicated production-grade native 3D profiles remain a later runtime/product completion item.
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

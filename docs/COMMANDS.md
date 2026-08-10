# QS3D command reference

## Workspace and project

- `QS3D` — open the docked QS3D workspace.
- `QS3DDOMAIN` — open the Full Domain Hub.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — project persistence and deterministic regeneration.
- `QS3DHEALTH` — run Model Health.

## Semantic capture

- `QS3DROOM`, `QS3DWALL`, `QS3DOPENING`, `QS3DDOOR`.
- `QS3DROOMAUTO` — discover bounded room faces from selected LINE/POLYLINE networks. Straight segments and polyline bulges are converted to metric planar segments; bulges are tessellated deterministically before the Core engine splits intersections/T-junctions, snaps endpoints, removes dangling bridges and calculates room area/perimeter.
- Room Auto project metadata: `RoomBoundaryToleranceM` (default `0.005`), `RoomBoundaryMinimumAreaM2` (default `0.5`), `RoomBoundaryArcSagittaM` (default `0.002`).
- Room Auto lifecycle: a changed boundary with the same normalized source-handle set reuses the existing Room record; a topology split/merge marks superseded auto Rooms `Stale` instead of deleting them. Stale Rooms and direct dependents are excluded from BQ while remaining in `.qsdb` for audit/recovery.
- `QS3DFINISH` can resolve Room Auto through boundary provenance. For an auto Room, select the full boundary source set so a shared wall does not accidentally target both adjacent rooms; existing room finishes are synchronized when the Room Auto geometry is updated.
- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DFINISH` — generate room finish semantics.
- `QS3DLINKHOST` — link Door/Opening to a wall host.

## Native 3D

- `QS3DBUILD3D` — create/update generated `Solid3d` for supported selected semantic elements.
- LINE source: architectural wall, beam, structural wall, railing.
- Closed POLYLINE source: slab, column, foundation, stair footprint mass, earthwork footprint mass.
- Earthwork is extruded downward by `DepthM`.
- Native 3D commands remain release-gated until tested by NETLOAD on licensed BricsCAD V25.

## Recognition, quantity and rebar

- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` — deterministic recognition + review/auto-accept.
- `QS3DBQ` — quantity summary and XLSX workflow.
- `QS3DBBSVIEW` — BBS review/locate window.
- `QS3DBBS` — BBS XLSX export.
- `QS3DBBSCSV` — UTF-8 CSV export with spreadsheet formula-injection guards.

## Revision and viewport

- `QS3DREVBASE`, `QS3DREVDIFF` — revision baseline/delta workflow.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport commands.

## Packaging and autoload

- `scripts/package-v25.ps1` creates the V25 release ZIP, excludes proprietary BricsCAD assemblies, generates `COMMANDS.txt` from current QS3D `CommandMethod` declarations, records package metadata and SHA-256 hashes.
- The package includes `install-v25-autoload.ps1` and `uninstall-v25-autoload.ps1` for per-user BricsCAD V25 Registry DemandLoad installation/removal, with hash verification and optional Authenticode enforcement.
- DemandLoad/NETLOAD remain part of the licensed V25 runtime gate; source presence is not treated as runtime verification.

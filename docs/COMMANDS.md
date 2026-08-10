# QS3D command reference

## Workspace and project

- `QS3D` — open the docked QS3D workspace.
- `QS3DDOMAIN` — open the Full Domain Hub.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — project persistence and deterministic regeneration.
- `QS3DHEALTH` — run Model Health.

## Semantic capture

- `QS3DROOM`, `QS3DWALL`, `QS3DOPENING`, `QS3DDOOR`.
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

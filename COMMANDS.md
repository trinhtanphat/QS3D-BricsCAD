# QS3D commands

## Workspace / diagnostics
`QS3D`, `QS3DHIDE`, `QS3DRIBBON`, `QS3DINSPECT`, `QS3DHEALTH`, `QS3DLOCATE`, `QS3DREFRESH`, `QS3DRESETUI`, `QS3DSAFEMODE`, `QS3DABOUT`

## Project / quantity
`QS3DSAVE`, `QS3DRELOAD`, `QS3DBQ`, `QS3DTAKEOFF`, `QS3DREGEN`

## Architecture
`QS3DROOM`, `QS3DWALL`, `QS3DOPENING`, `QS3DDOOR`, `QS3DLINKHOST`, `QS3DFINISH`

## Structure
`QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DEARTHWORK`

Beam and structural wall use selected LINE geometry for source-level 3D generation. Slab, column and foundation use selected closed polylines for source-level extrusion. These geometry paths remain runtime-gated until a real BricsCAD V25 integration runner/session executes them.

## Rebar
`QS3DREBAR`, `QS3DBBS`

## Recognition
`QS3DRECOGNIZE` opens the review queue. `QS3DRECOGNIZEAUTO` auto-applies only high-confidence candidates with sufficient top-two margin and still opens the review UI.

## Revisions
`QS3DREVBASE` stores a persistent `.qsrev` baseline next to the project. `QS3DREVDIFF` compares current per-element quantities to that baseline.

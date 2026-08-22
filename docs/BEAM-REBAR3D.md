# Beam longitudinal rebar 3D

`QS3DBEAMREBAR3D` creates deterministic longitudinal reinforcing-bar `Solid3d` geometry for Beam semantic elements whose CAD source is a LINE.

## Inputs

- Beam semantic element with `WidthM` and `HeightM` from the instance or Family.
- `RebarNotation` remains the BBS/source notation and supplies the bar diameter when unambiguous.
- `RebarCoverM` defaults to `0.04` m when not defined.
- `RebarBeamEndCoverM` defaults to `RebarCoverM`.
- Optional `RebarBeamDiameterMm` overrides the geometry diameter when `RebarNotation` contains multiple diameters.
- `RebarBeamTopCount` and `RebarBeamBottomCount` define explicit placement counts. If both are omitted, only one even count-style notation with at least four bars can be split equally between top and bottom layers.

## Geometry rules

- `BeamLongitudinalRebarPlanner` reuses `LinearRebarLayoutPlanner` for horizontal bar-center distribution.
- Cover is measured to the outside of the bar, so center clearance is `cover + diameter / 2`.
- Top/bottom layers must fit inside the Beam height and may not overlap.
- Bar-center spacing may not be smaller than one bar diameter.
- End cover must leave a positive usable longitudinal bar length.
- The Beam source LINE is treated as the section centerline, matching the Beam solid convention.
- The V25 adapter requires a near-horizontal source: the current planarity tolerance is `|ΔZ| <= 0.005 m` after drawing-unit conversion. A source outside this envelope is rejected instead of silently flattening geometry.

## Ownership, stale state and regeneration

Generated bars are tracked through `GeneratedRebarHandles` with `GeneratedRebarMode=BeamLongitudinalBars`. Re-running safely replaces previous generated longitudinal-bar solids only when ownership still matches and tracked handles resolve to the expected generated `Solid3d`; an unexpected live object type aborts instead of erasing unknown CAD data.

Semantic/source edits participate in the generated-geometry stale lifecycle. A stale snapshot must be regenerated before release rather than treated as current geometry. Shape rebar, ties, Beam stirrups, slab/wall/foundation mesh remain separate generated-handle namespaces so one workflow cannot erase another workflow's geometry.

## Validation and runtime boundary

`BeamRebarRegressionSmoke` covers Core behavior and is explicitly registered through `BeamRebarSmokeRegistration`. `scripts/preflight-beam-rebar.py`, the repository-wide smoke-registration guard and aggregate `scripts/preflight-all.py` protect the source contract.

This source-level coverage is not a substitute for BricsCAD runtime proof. V25 plugin compilation, `NETLOAD`, selection behavior and placement in a real DWG remain release gates on an authorized BricsCAD V25 Windows runner.
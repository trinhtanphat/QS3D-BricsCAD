# Beam longitudinal rebar 3D

`QS3DBEAMREBAR3D` creates deterministic longitudinal reinforcing-bar `Solid3d` geometry for Beam semantic elements whose CAD source is a horizontal LINE.

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
- The Beam source LINE is treated as the section centerline, matching the existing Beam `Solid3d` convention.
- The V25 adapter currently accepts horizontal XY Beam LINE sources only.

## Ownership and regeneration

Generated bars are tracked through `GeneratedRebarHandles` and `GeneratedRebarMode=BeamLongitudinalBars`. Re-running the command safely replaces the previous generated longitudinal-bar solids only when tracked handles still resolve to `Solid3d`; an unexpected live object type aborts instead of erasing unknown CAD data.

Shape rebar remains separate under the existing shape-rebar workflow and its own `GeneratedShapeRebarHandles`. This Beam path deliberately does not fake stirrups, hooks or bends.

## Validation status

The Core layout planner has deterministic regression coverage and `scripts/preflight-beam-rebar.py` checks the source contract. `.github/workflows/beam-rebar.yml` is manual-only. Actual plugin compile, `NETLOAD` and Beam rebar placement inside a real BricsCAD V25 drawing remain runtime gates and must not be claimed from source review alone.

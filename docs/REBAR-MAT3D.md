# QS3D Slab / Foundation Rebar Mat 3D

`QS3DREBARMAT3D` creates deterministic orthogonal reinforcement mats for selected QS3D `Slab` or `Foundation` semantics.

## Supported source geometry

The current native V25 adapter intentionally supports a conservative host shape:

- one selected semantic source per element;
- closed 4-vertex `POLYLINE` rectangle;
- plan-view XY normal;
- no bulge/curved edges;
- `Slab` or `Foundation` category only.

Arbitrary polygons are rejected instead of silently extending bars outside the host footprint. The Core planner is CAD-independent and can be reused by a future clipped-polygon adapter.

## Properties

Direction notations use the existing rebar notation parser and must be one spacing group such as `D12@200`:

- `RebarMatXNotation` — bars running along local X; falls back to `RebarMatNotation`, then `RebarNotation`.
- `RebarMatYNotation` — bars running along local Y; same fallback chain.
- `RebarMatFaces` — `Bottom`, `Top`, or `Both`; default `Bottom`.
- `RebarCoverM` — concrete cover to bar surface; default `0.025` m.
- `ThicknessM` — host thickness, inherited from Slab/Foundation family/element.
- `BottomOffsetM` — same host reference offset used by native structural solid generation.

## Geometry convention

`OrthogonalRebarMatPlanner` uses `LinearRebarLayoutPlanner` for both station directions. X bars are placed closest to each enabled concrete face and Y bars are placed immediately inward. Their center-plane separation equals the sum of radii so crossing cylinders do not occupy the same elevation. Top and bottom stacks are rejected if the host thickness cannot contain them safely.

The planner also rejects center spacing smaller than one bar diameter and caps a plan at 10,000 bars. The V25 builder applies tighter mutation limits of 1,200 bars per element and 4,000 bars per batch before destructive replacement.

## Ownership and lifecycle

Generated bars are stored separately from Column/Beam longitudinal, ties, stirrups and BBS-shape bars:

- `GeneratedRebarMatHandles`
- `GeneratedRebarMatCount`
- `GeneratedRebarMatXNotation`
- `GeneratedRebarMatYNotation`
- `GeneratedRebarMatFaces`
- `GeneratedRebarMatXActualSpacingM`
- `GeneratedRebarMatYActualSpacingM`
- `GeneratedRebarMatMode`

`GeneratedRebarOwnershipGuard` reserves the mat handle set before erase/rebuild. Rebuilding host geometry through the dependent-geometry invalidator erases owned mat solids and clears their metadata rather than leaving orphan geometry.

## Health

- `QS3DREBARMATHEALTH` checks handle validity, ownership, live `Solid3d` presence, count metadata, allowed category/faces and dirty/stale state.
- `QS3DREBARHEALTHALL` includes mat health together with Column/Beam longitudinal, BBS shape, Column ties and Beam stirrups.

Native geometry remains runtime-gated until the current exact `main` is compiled and exercised with licensed BricsCAD V25 and representative private DWG samples.

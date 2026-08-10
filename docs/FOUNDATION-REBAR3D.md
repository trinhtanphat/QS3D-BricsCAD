# QS3D Foundation Rebar 3D

`QS3DFOUNDATIONREBAR3D` creates a guarded orthogonal X/Y reinforcement mesh for selected QS3D `Foundation` semantics.

## Design reuse

Foundation Mesh deliberately reuses `RectangularSlabMeshPlanner` instead of introducing another mesh math engine. Slab and Foundation therefore share deterministic count/spacing, cover, face-order and overlap checks while keeping separate CAD ownership metadata and commands.

## Supported source geometry

The current V25 adapter accepts one selected semantic Foundation source per element and requires a closed 4-vertex rectangular `POLYLINE` in the XY plane. Rotated rectangles are supported. Bulged or arbitrary polygon footprints are rejected rather than extending straight bars outside the host.

## Properties

Use `QS3DREBARSETUP` with a selected Foundation or set these properties through the normal Family/Instance property workflow:

- `RebarFoundationXNotation` — X-direction bars, one count/spacing group such as `D16@200` or `12D16`.
- `RebarFoundationYNotation` — Y-direction bars.
- `RebarFoundationCoverM` — concrete cover to bar surface; setup default `0.05` m.
- `RebarFoundationFaces` — `Bottom`, `Top`, or `Both`; default `Bottom`.
- `RebarFoundationXClosestToFace` — controls X/Y ordering through the thickness.
- `ThicknessM` and `BottomOffsetM` — inherited from the Foundation host convention.

## Generated ownership

Foundation bars are tracked separately from Slab/Wall mesh and Column/Beam reinforcement:

- `GeneratedFoundationMeshHandles`
- `GeneratedFoundationMeshCount`
- `GeneratedFoundationMeshXDiameterMm`
- `GeneratedFoundationMeshYDiameterMm`
- `GeneratedFoundationMeshCoverM`
- `GeneratedFoundationMeshXActualSpacingM`
- `GeneratedFoundationMeshYActualSpacingM`
- `GeneratedFoundationMeshFaces`
- `GeneratedFoundationMeshMode=FoundationMeshXY`

`GeneratedRebarOwnershipGuard` reserves the Foundation set before destructive replacement. Host geometry rebuilds erase owned Foundation bars and clear the metadata through `GeneratedDependentGeometryInvalidator`; cross-key ownership health also includes the Foundation set.

## Health and UX

- `QS3DFOUNDATIONREBARHEALTH` validates handles, live solids, count, numeric metadata, mode/category and stale state.
- `QS3DREBARHEALTHALL` and `QS3DHEALTHALL` include Foundation Mesh health.
- `QS3DREBARHUB` / `QS3DREBARBUILDSELECTED` dispatch selected Foundation semantics to `QS3DFOUNDATIONREBAR3D`.
- `QS3DREBARSETUP` supports Slab, StructuralWall and Foundation using one setup surface.

Native Solid3d behavior remains runtime-gated until the exact integrated head is compiled and tested inside licensed BricsCAD V25 with representative private DWG files.

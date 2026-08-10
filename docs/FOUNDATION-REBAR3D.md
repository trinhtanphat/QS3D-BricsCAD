# QS3D Foundation Rebar 3D

`QS3DFOUNDATIONREBAR3D` creates a guarded orthogonal X/Y reinforcement mesh for selected QS3D `Foundation` semantics.

## Design reuse

Foundation Mesh deliberately reuses `RectangularSlabMeshPlanner` instead of introducing another mesh math engine. Slab and Foundation therefore share deterministic count/spacing, cover, face-order and overlap checks while keeping separate CAD ownership metadata, stale lifecycle and commands.

## Supported source geometry

The current V25 adapter accepts one selected semantic Foundation source per element and requires a closed 4-vertex rectangular `POLYLINE` in the XY plane. Rotated rectangles are supported. Bulged or arbitrary polygon footprints are rejected rather than extending straight bars outside the host. Duplicate semantic ownership of one selected Foundation source is rejected before geometry mutation.

## Properties

Use `QS3DREBARMESHSETUP` with a selected Foundation or set these properties through the normal Family/Instance property workflow:

- `RebarFoundationXNotation` — X-direction bars, one count/spacing group such as `D16@200` or `12D16`.
- `RebarFoundationYNotation` — Y-direction bars. X and Y may use independent diameter, count or spacing.
- `RebarFoundationCoverM` — concrete cover to bar surface; setup default `0.05` m.
- `RebarFoundationFaces` — `Bottom`, `Top`, or `Both`; default `Bottom`.
- `RebarFoundationXClosestToFace` — controls X/Y ordering through the thickness.
- `ThicknessM` and `BottomOffsetM` — inherited from the Foundation host convention.

The setup UI only validates explicit user input. It does not calculate or recommend reinforcement design.

## Generated ownership and stale lifecycle

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

`GeneratedRebarOwnershipGuard` reserves the Foundation set before destructive replacement. Host geometry rebuilds erase owned Foundation bars and clear metadata through `GeneratedDependentGeometryInvalidator`; cross-family ownership health includes the Foundation set.

Semantic/source edits snapshot the current Foundation handles as stale. A successful Foundation mesh rebuild writes the replacement metadata and clears only the Foundation stale state after the CAD transaction has committed.

## Health and UX

- `QS3DFOUNDATIONREBARHEALTH` validates handles, live solids, count, numeric metadata, mode/category and stale snapshot state.
- `QS3DREBARHEALTHALL` and `QS3DHEALTHALL` include Foundation Mesh health and Locate the generated Foundation solids for Foundation-specific issues.
- `QS3DREBARHUB`, Ribbon and Full Domain Hub expose Foundation Mesh beside Slab/Wall mesh.
- `QS3DREBARMESHSETUP` supports Slab, StructuralWall and Foundation on one setup surface.

Native `Solid3d` behavior remains runtime-gated until the exact integrated head is compiled and tested inside licensed BricsCAD V25 with representative private DWG files.

# QS3D Foundation Rebar 3D

`QS3DFOUNDATIONREBAR3D` creates a guarded orthogonal X/Y reinforcement mesh for selected QS3D `Foundation` semantics.

## Design reuse

Foundation Mesh deliberately reuses the existing slab mesh planning engines instead of introducing another rebar math implementation:

- guarded rectangular footprints use `RectangularSlabMeshPlanner`;
- guarded non-rectangular straight simple polygons use `PolygonalSlabMeshPlanner` / `PolygonScanlineClipper`.

Slab and Foundation therefore share deterministic count/spacing, cover, face-order, clipping and overlap checks while keeping separate CAD ownership metadata, stale lifecycle and commands.

## Supported source geometry

The current V25 adapter accepts one selected semantic Foundation source per element and requires a closed straight-segment plan-view `POLYLINE` in the XY plane with at least three vertices. Duplicate semantic ownership of one selected Foundation source is rejected before geometry mutation.

Two footprint modes are source-implemented:

- `RectangleLocalXY` — a true four-vertex rectangle preserves the legacy local axes derived from its first two edges. Rotated rectangles therefore keep the same X/Y reinforcement orientation as before the polygon extension.
- `PolygonGlobalXY` — any other closed straight simple polygon is clipped by `PolygonalSlabMeshPlanner` in drawing/global X/Y. Concave scanlines may produce multiple physical bar segments while remaining inside the guarded footprint/cover envelope.

The polygon path does **not** claim automatic local-axis inference. Bulged/curved edges, holes/islands, multiple outer loops and arbitrary 3D/non-XY footprints remain unsupported and fail closed instead of extending straight bars outside the Foundation host.

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
- `GeneratedFoundationMeshFootprintMode=RectangleLocalXY|PolygonGlobalXY`

`GeneratedRebarOwnershipGuard` reserves the Foundation set before destructive replacement. Host geometry rebuilds erase owned Foundation bars and clear metadata through `GeneratedDependentGeometryInvalidator`; cross-family ownership health includes the Foundation set.

Semantic/source edits snapshot the current Foundation handles as stale. During a successful Foundation mesh rebuild, generated handles/count/spacing/faces/footprint mode, audit and revision state are written while the CAD transaction is still rollback-capable; `ClearGeneratedFoundationMeshStale()` is part of that same pre-commit semantic phase. If the operation fails before CAD commit, the CAD transaction aborts and the deep project snapshot is restored.

## Health and UX

- `QS3DFOUNDATIONREBARHEALTH` validates handles, live solids, count, numeric metadata, mode/category, `GeneratedFoundationMeshFootprintMode` and stale snapshot state.
- `QS3DREBARHEALTHALL` and `QS3DHEALTHALL` include Foundation Mesh health and Locate the generated Foundation solids for Foundation-specific issues.
- `QS3DREBARHUB`, Ribbon and Full Domain Hub expose Foundation Mesh beside Slab/Wall mesh.
- `QS3DREBARMESHSETUP` supports Slab, StructuralWall and Foundation on one setup surface.

## Runtime boundary

This change is source-implemented/statically guarded only until exact-SHA licensed BricsCAD V25 qualification exists. The local matrix must include at least:

1. axis-aligned and rotated rectangular Foundations and verify `RectangleLocalXY` preserves legacy orientation;
2. convex and concave straight polygon Foundations and verify `PolygonGlobalXY` bars remain inside the host after cover/bar-radius clearance;
3. Bottom, Top and Both faces with independent X/Y diameter/count/spacing;
4. replacement, ownership conflict, stale/rebuild, save/reopen and health behavior;
5. millimetre and metre drawings;
6. malformed/self-intersecting/bulged/non-XY polygons and verify fail-closed behavior with no partial replacement;
7. batch-limit failure before destructive replacement/allocation.

Native `Solid3d` behavior remains runtime-gated until the exact integrated head is compiled and tested inside licensed BricsCAD V25 with representative private DWG files. Curved/bulged boundaries, holes/islands/multiple outer loops and arbitrary local-axis inference remain separate product work.

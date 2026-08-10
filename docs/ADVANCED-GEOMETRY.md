# Advanced geometry and review workflows

This batch complements the existing Auto Room, wall-footprint, opening-boolean, TKT variants and column-rebar work already present in QS3D.

## Shape-driven rebar 3D

`QS3DREBAR3DSHAPE` consumes the deterministic BBS produced by `ProjectRebarScheduleBuilder`.

- Straight shape `00` can be built directly from BBS cutting length.
- L/`11`, U/`21` and Z/`31` require `RebarShapeLegsM`, so physical geometry is not guessed.
- Custom segmented shapes use `RebarShapeLegsM` plus `RebarShapeTurnsDeg`.
- Sum of shape legs must match BBS cutting length.
- `RebarCoverM` controls placement cover.
- The V25 adapter limits bars per element and per batch before creating `Solid3d` geometry.
- Adjacent shape cylinders overlap slightly and are united to avoid disconnected segmented bars.
- Shape-generated handles use `GeneratedShapeRebarHandles`, so this path can coexist with the existing column/perimeter rebar builder.

Exact rounded bend/hook radii remain a BricsCAD V25/private-DWG runtime validation item; source does not invent code-specific bend geometry without dimensions.

## Transient model review

- `QS3DHIGHLIGHT` — highlight current or prompted selection.
- `QS3DUNHIGHLIGHT` — remove QS3D transient highlight.
- `QS3DFOCUS` — highlight and zoom selected objects.
- `QS3DISOLATE` — isolate selected objects with BricsCAD object isolation.
- `QS3DUNISOLATE` — restore isolated objects.

Prompted selections are promoted to implied selection so chained BricsCAD/QS3D commands continue on the same object set.

## Validation boundary

`python scripts/preflight-advanced-geometry.py` is a static/source gate. It checks command uniqueness, required shape/review markers, geometry caps and smoke coverage. It is not evidence of BricsCAD V25 compilation, NETLOAD, boolean behavior or UI rendering.

GitHub Actions remain manual-only according to repository policy. Real V25 validation still requires the licensed Windows self-hosted runner.

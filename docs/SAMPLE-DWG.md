# Supplied DWG regression sample (not committed)

Local review targets supplied by the project owner include a private villa practice DWG and `DGKL.xlsx`. Their absolute local paths and bytes are deliberately not stored in the repository.

The drawing is treated as a private regression fixture. The repository only stores the expected test workflow, not the drawing bytes.

Runtime checks planned against this sample:

1. Open DWG in licensed BricsCAD V25.
2. `NETLOAD` QS3D plugin.
3. `QS3D` palettes render without covering the native viewport.
4. `QS3DB4D` scans Current Space and leaves ambiguous categories in Recognition review.
5. Selection → `QS3DINSPECT` reads handle/type/layer/curve length and closed-polyline area.
6. `QS3DBQ`/`QS3DED2` exports `.xlsx` with QS3D Element IDs and CAD handles.
7. `QS3DEXCELLOCATE` reads an export row and selects/zooms the same entities; separately verify the private legacy workbook decimal-handle conversion in read-only mode.
8. Repeat after switching drawings to catch stale document references.

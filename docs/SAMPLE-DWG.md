# Supplied DWG regression sample (not committed)

Local review target supplied by the project owner:

- File: `260808.SHOP XAY TUONG_NHA NOI TRU.dwg`
- Size: approximately 22 MiB
- DWG signature: `AC1021`
- Format family: AutoCAD 2007/2008/2009 DWG
- Embedded producer metadata contains AutoCAD 2021 and BricsCAD/Open Design Alliance markers.

The drawing is treated as a private regression fixture. The repository only stores the expected test workflow, not the drawing bytes.

Runtime checks planned against this sample:

1. Open DWG in licensed BricsCAD V25.
2. `NETLOAD` QS3D plugin.
3. `QS3D` palettes render without covering the native viewport.
4. Selection → `QS3DINSPECT` reads handle/type/layer/curve length and closed-polyline area.
5. `QS3DBQ` groups the current selection and exports `.xlsx`.
6. Reopen exported workbook in Excel/LibreOffice and verify headers/numeric values.
7. Repeat after switching drawings to catch stale document references.

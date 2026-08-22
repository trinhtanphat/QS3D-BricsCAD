# Synthetic sample and private regression boundary

<<<<<<< HEAD
The repository now ships a self-created fixture under `samples/generated/`. It includes a metric DWG/DXF pair, QSDB semantic sidecar, Excel Handle lookup workbook and reusable QS3D template. The geometry was generated in an installed CAD core runtime, the DXF was exported from the DWG, and both drawing files passed an audit with zero reported errors. Qualification inside licensed BricsCAD V25 remains a separate release gate.

No byte from the owner-supplied villa drawing, `DGKL.xlsx`, BLT/BLT3D folders or BricsCAD runtime DLLs is copied into Git or the Release. Those files remain optional, read-only, local regression inputs.
=======
Local review targets supplied by the project owner include a private villa practice DWG and `DGKL.xlsx`. Their absolute local paths and bytes are deliberately not stored in the repository.
>>>>>>> 645b39943 (feat: add B4D scan and Excel handle round-trip)

## Public synthetic workflow

1. Open `samples/generated/QS3D-Sample.dwg`, or open the DXF and save it as a same-name DWG.
2. Load the packaged plugin using DemandLoad or `NETLOAD`.
3. Run `QS3D`, `QS3DRELOAD`, `QS3DHEALTHALL` and `QS3DBQ`.
4. Run `QS3DB4D` on Current Space and review every ambiguous recognition result before accepting it.
5. Run `QS3DED2`, choose `Selection`, active `Floor`, active `Zone` or `All`, and save a fresh workbook with `CHI_TIET`, `TONG_HOP` and the actual drawing fingerprint.
6. Run `QS3DEXCELLOCATE` against a `CHI_TIET` row in that newly exported workbook. Verify that Element ID, Handle and drawing fingerprint resolve completely before selection changes. The supplied generic sample workbook has a blank fingerprint and is reference data only; do not use it as a modern QS3D round-trip proof.
7. Import `QS3D-Architecture.qstemplate` with `QS3DTEMPLATEIMPORT` in a disposable copy and review the planned Family/rule/mapping changes before accepting.

<<<<<<< HEAD
## Optional private runtime checks

Private local fixtures can still be used to compare behavior, but must not be modified or committed:

1. Open the private DWG in licensed BricsCAD V25.
2. `NETLOAD` the release plugin.
3. Verify palettes and Ribbon do not cover the native viewport.
4. Run `QS3DB4D` in review mode; never auto-accept uncertain categories.
5. Export a new workbook with `QS3DBQ` or the scoped two-sheet `QS3DED2`; do not overwrite the supplied workbook.
6. Test modern Handle lookup only against the newly exported copy. Explicit no-fingerprint confirmation is reserved for recognizable legacy BLT `$decimal` Handle rows.
7. Switch between two drawings to catch stale document references.

Passing source tests does not replace this licensed BricsCAD runtime qualification.
=======
1. Open DWG in licensed BricsCAD V25.
2. `NETLOAD` QS3D plugin.
3. `QS3D` palettes render without covering the native viewport.
4. `QS3DB4D` scans Current Space and leaves ambiguous categories in Recognition review.
5. Selection → `QS3DINSPECT` reads handle/type/layer/curve length and closed-polyline area.
6. `QS3DBQ`/`QS3DED2` exports `.xlsx` with QS3D Element IDs and CAD handles.
7. `QS3DEXCELLOCATE` reads an export row and selects/zooms the same entities; separately verify the private legacy workbook decimal-handle conversion in read-only mode.
8. Repeat after switching drawings to catch stale document references.
>>>>>>> 645b39943 (feat: add B4D scan and Excel handle round-trip)

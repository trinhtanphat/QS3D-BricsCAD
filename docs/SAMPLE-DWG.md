# Synthetic sample and private regression boundary

The repository now ships a self-created fixture under `samples/generated/`. It includes a metric DWG/DXF pair, QSDB semantic sidecar, Excel Handle lookup workbook and reusable QS3D template. The geometry was generated in an installed CAD core runtime, the DXF was exported from the DWG, and both drawing files passed an audit with zero reported errors. Qualification inside licensed BricsCAD V25 remains a separate release gate.

No byte from the owner-supplied villa drawing, `DGKL.xlsx`, BLT/BLT3D folders or BricsCAD runtime DLLs is copied into Git or the Release. Those files remain optional, read-only, local regression inputs.

## Public synthetic workflow

1. Open `samples/generated/QS3D-Sample.dwg`, or open the DXF and save it as a same-name DWG.
2. Load the packaged plugin using DemandLoad or `NETLOAD`.
3. Run `QS3D`, `QS3DRELOAD`, `QS3DHEALTHALL` and `QS3DBQ`.
4. Run `QS3DB4D` on Current Space and review every ambiguous recognition result before accepting it.
5. Run `QS3DED2` to create a fresh workbook with the actual drawing fingerprint.
6. Run `QS3DEXCELLOCATE` against the shipped workbook and verify rows 2–11. The shipped workbook deliberately has a blank fingerprint and therefore requires explicit `YES` confirmation.
7. Import `QS3D-Architecture.qstemplate` with `QS3DTEMPLATEIMPORT` in a disposable copy and review the planned Family/rule/mapping changes before accepting.

## Optional private runtime checks

Private local fixtures can still be used to compare behavior, but must not be modified or committed:

1. Open the private DWG in licensed BricsCAD V25.
2. `NETLOAD` the release plugin.
3. Verify palettes and Ribbon do not cover the native viewport.
4. Run `QS3DB4D` in review mode; never auto-accept uncertain categories.
5. Export a new workbook with `QS3DBQ`/`QS3DED2`; do not overwrite the supplied workbook.
6. Test Handle lookup only against the newly exported copy or after explicit confirmation of a legacy no-fingerprint row.
7. Switch between two drawings to catch stale document references.

Passing source tests does not replace this licensed BricsCAD runtime qualification.

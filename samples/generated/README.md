# QS3D synthetic sample pack

Everything in this folder is generated specifically for QS3D and contains no BLT source, private project data, customer drawing, personal workbook, or BricsCAD-owned binary.

Files:

- `QS3D-Sample.dxf`: metric ASCII DXF with walls, slab, columns, beam, glass wall, door and room boundary. Entity handles are stable and intentionally match the workbook and QSDB fixture.
- `QS3D-Sample.dwg`: the source synthetic drawing generated in an installed CAD core runtime. The DXF was exported from this DWG; both files passed an audit with zero reported errors.
- `QS3D-Sample.qsdb`: editable semantic sidecar with blank drawing fingerprint. On first open, QS3D adopts the actual drawing identity; run `QS3DSAVE` to persist it.
- `QS3D-Quantity-Template.xlsx`: first-sheet quantity/Handle lookup sample compatible with `QS3DEXCELLOCATE`; its blank fingerprint intentionally triggers the confirmation guard.
- `QS3D-Architecture.qstemplate`: Family, quantity-rule, layer-mapping and visible-column template for `QS3DTEMPLATEIMPORT`.

Quick local check:

1. Open `QS3D-Sample.dwg` when available, otherwise open the DXF and save it as DWG beside the sidecar.
2. Load the release plugin with `NETLOAD` or install its DemandLoad package.
3. Run `QS3D`, then `QS3DRELOAD`, `QS3DHEALTHALL` and `QS3DBQ`.
4. Run `QS3DEXCELLOCATE`, choose the workbook and enter a detail row such as `2`. Because the template fingerprint is blank, explicitly type `YES` after checking that the active drawing is this sample.
5. Use `QS3DTEMPLATEIMPORT` to test the `.qstemplate` independently.

The fixed Handle mapping is test data, not a promise that handles survive arbitrary copy/paste, purge, WBLOCK or third-party conversion operations.

# QS3D v0.1.0-preview.1

This prerelease packages the first public QS3D preview for BricsCAD V25 x64 together with repository-owned sample data.

## Product form

This is a **BricsCAD plugin package**, not a standalone desktop application. Start BricsCAD V25 and load/register QS3D through DemandLoad or `NETLOAD`. The ZIP intentionally contains the plugin/support DLLs and release helpers; a `QS3D.exe` is not expected. See `docs/PRODUCT-BOUNDARY.md` for the canonical product boundary.

## Included

- QS3D's own `QS3D.BricsCAD.V25.dll` plugin and `QS3D.Core.dll` support library.
- DemandLoad installer, uninstaller and guarded HTTPS updater scripts.
- Synthetic DWG/DXF, QSDB sidecar, quantity/Handle Excel workbook and architecture template under `Samples/`.
- B4D recognition, ED2 Excel round-trip lookup, quantity/reporting, templates, revisions and domain workflows represented by the command inventory in `COMMANDS.txt`.

## Verification

- Release build completes with zero compiler warnings and errors.
- Core smoke suite and the aggregate source/preflight gates pass before packaging.
- The synthetic DWG and exported DXF were audited with zero reported drawing errors.
- `SHA256SUMS.txt` covers every packaged payload, including nested sample files.
- The package excludes private BLT/project inputs and BricsCAD-owned runtime assemblies.

## Preview limits

- The DLLs are an unsigned development build. Use them only in a controlled test environment; production deployment should use an Authenticode-signed build from an approved publisher.
- Native Solid3d, palette/ribbon layout and DemandLoad behavior still need interactive qualification in licensed BricsCAD V25.
- The sample workbook has a deliberately blank drawing fingerprint. `QS3DEXCELLOCATE` therefore requires explicit confirmation before locating its Handle rows.

See `Samples/README.md` in the ZIP for the safe sample workflow.

# BricsCAD V25 install / DemandLoad

## What is being installed

This installs a **BricsCAD V25 plugin**, not a standalone desktop application. There is intentionally no `QS3D.exe` in the current release target. Users start BricsCAD V25; QS3D is then loaded as `QS3D.BricsCAD.V25.dll` through DemandLoad or `NETLOAD` and exposes its commands, Ribbon, palettes and modeless windows inside BricsCAD.

`QS3D.Core.dll` is a supporting library, not an independent CAD executable. See `docs/PRODUCT-BOUNDARY.md`.

QS3D supports two source-level loading paths for BricsCAD V25 x64.

## Recommended release package

Run `scripts/package-v25.ps1` after the V25 plugin has been compiled against the exact installed BricsCAD V25 managed assemblies. The package contains only QS3D assemblies and release helpers; `BrxMgd.dll`, `TD_Mgd.dll` and other BricsCAD-owned runtime assemblies are deliberately excluded.

The package generates:

- `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll`;
- `COMMANDS.txt`, generated from source `CommandMethod` declarations;
- `PACKAGE-METADATA.json` including command count and Authenticode status;
- `SHA256SUMS.txt` covering every shipped payload file;
- `install-v25-autoload.ps1` and `uninstall-v25-autoload.ps1`;
- a ZIP suitable for release evidence/artifact storage.

A standalone `QS3D.exe` is not expected from this packaging flow.

## DemandLoad registration

`install-v25-autoload.ps1` uses the BricsCAD V25 per-user DemandLoad registry model under `HKCU\Software\Bricsys\BricsCAD\<VersionKey>\<LanguageKey>\Applications\QS3D`.

The default is `OnCommand` (`LoadCtrls=4`). `OnStartup` (`LoadCtrls=2`) is optional. The installer registers the package command list under the `Commands` child key, so BricsCAD can load the QS3D .NET module on demand.

The installer:

- requires BricsCAD to be closed;
- verifies `SHA256SUMS.txt` before copying anything;
- optionally requires a valid Authenticode signature with `-RequireSigned`;
- refuses to overwrite an existing QS3D registration unless `-Force` is explicitly supplied;
- uses `SupportsShouldProcess`, so PowerShell `-WhatIf` / confirmation semantics remain available;
- never changes BricsCAD security variables or weakens trusted-path policy.

If enterprise security rejects an unsigned assembly, ship a signed build or use an administrator-approved trusted location. Do not lower BricsCAD security settings as an installer workaround.

## Uninstall

`uninstall-v25-autoload.ps1` removes only the QS3D DemandLoad application keys for matching V25 targets. By default it removes files only from the QS3D folder below LocalAppData; deleting a custom path requires explicit `-Force`.

## Runtime qualification

Source packaging and registry wiring are not equivalent to runtime qualification. Release evidence still requires the self-hosted `windows/x64/bricscad-v25` runner to compile the adapter against the installed V25 DLLs, execute NETLOAD/DemandLoad, exercise the Ribbon/palettes/commands and capture screenshots.

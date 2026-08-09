# BricsCAD V25 self-hosted runner contract

Gate C intentionally uses a Windows self-hosted runner because the QS3D V25 plugin references managed assemblies from an installed BricsCAD V25 environment and those vendor assemblies must not be committed to this public repository.

## Required runner

- Windows x64.
- GitHub Actions self-hosted runner registered for this repository or an allowed runner group.
- labels: `self-hosted`, `windows`, `x64`, `bricscad-v25`.
- BricsCAD V25 installed and licensed for the required integration/runtime work.
- `BrxMgd.dll` and `TD_Mgd.dll` present in the installed BricsCAD directory.
- network access required by GitHub Actions checkout/setup actions.

## Repository configuration

Create repository variable:

`BRICSCAD_V25_DIR`

Its value is the absolute installation directory containing `BrxMgd.dll` and `TD_Mgd.dll`.

Do **not** create secrets or repository files containing copies of those DLLs.

## Runner software

The workflow currently installs/selects .NET 8 for building the netstandard Core tests. The machine must also be able to target .NET Framework 4.8 for `QS3D.BricsCAD.V25` (`net48/x64`). Python 3.12 is selected by the workflow for preflight.

## Validation order

The integration workflow performs:

1. checkout;
2. source preflight;
3. Core Release build;
4. Core smoke tests;
5. validate `BRICSCAD_V25_DIR` and V25 managed references;
6. build `QS3D.BricsCAD.V25` Release x64;
7. upload only QS3D assemblies as the build artifact.

A successful Gate C build still does **not** prove runtime behavior. Gate D must NETLOAD that artifact in BricsCAD V25 and run the documented interactive regression suite.

## Current status

The probe run `31341184031` remained queued with no assigned runner. Bring a matching runner online before retrying Gate C.

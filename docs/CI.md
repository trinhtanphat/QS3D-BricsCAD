# CI strategy

## Why two workflows

The core engine must compile and test without BricsCAD.
The V25 plugin needs the proprietary assemblies shipped with a licensed BricsCAD V25 installation.

## Hosted CI

`.github/workflows/ci.yml`

- manual dispatch
- Windows hosted runner
- preflight
- compile `QS3D.Core`
- run package-free smoke tests

No BricsCAD installation is required.

## V25 integration CI

`.github/workflows/bricscad-v25.yml`

Runner labels:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

Repository variable:

`BRICSCAD_V25_DIR`

Example value:

`C:\Program Files\Bricsys\BricsCAD V25 en_US`

The runner must have a valid BricsCAD V25 installation.

## Release gate later

1. preflight
2. core build
3. core smoke tests
4. V25 adapter compile
5. scripted BricsCAD load smoke test
6. manual DWG regression set
7. package/sign
8. release

Do not upload BricsCAD DLLs as source-controlled artifacts.

# Local BricsCAD V26 qualification

Status: `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE` until a licensed interactive BricsCAD V26 workstation or dedicated self-hosted runner is available.

## Why V26 is a separate gate

BricsCAD V26 hosts managed plugins on .NET 8 instead of the .NET Framework 4.8 lane used by BricsCAD V25. QS3D therefore emits a distinct `QS3D.BricsCAD.V26.dll` from `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`, targeting `net8.0-windows` and resolving `BrxMgd.dll` / `TD_Mgd.dll` from the installed V26 directory only.

The V25 project remains `net48`. Passing source/static checks or the Core smoke suite does **not** prove V26 runtime compatibility.

## Prerequisites

- Windows x64 interactive desktop.
- Licensed BricsCAD V26 x64.
- .NET 8 Windows Desktop Runtime x64 / compatible .NET 8 SDK.
- Python 3 and .NET SDK available for repository preflights/build.
- Clean checkout at the exact candidate SHA.
- No proprietary BricsCAD DLLs, customer DWGs, signing keys or private runtime paths committed to Git.

Set the host directory explicitly; do not point V26 builds at a V25 installation:

```powershell
$env:BRICSCAD_V26_DIR = 'C:\Program Files\Bricsys\BricsCAD V26 en_US'
```

If the installed locale/path differs, use that licensed V26 installation directory instead.

## Source/build gate

```powershell
python scripts/preflight-ci-manual-only.py
python scripts/preflight.py
python scripts/preflight-bricscad-v26.py
python scripts/preflight-all.py

dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Expected plugin:

```text
src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll
```

## Licensed runtime gate

Run from a dedicated V26 desktop with all existing BricsCAD processes closed:

```powershell
.\scripts\test-bricscad-v26-runtime.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -PluginDll .\src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll
```

The gate must fail closed if the configured `bricscad.exe` is not major version 26, if the plugin is not the V26 assembly, if the host is not x64, or if `QS3DRUNTIMEPROBE` does not report Ribbon and palette readiness.

## Interactive matrix before V26 can be called qualified

Record sanitized evidence tied to the exact SHA for all of the following:

- `NETLOAD` of `QS3D.BricsCAD.V26.dll` into the current installed V26 build with no loader/type exceptions.
- Core QS3D command registration plus Ribbon, Workspace palette and Right Panel startup.
- Representative native 2D/3D authoring, semantic capture, quantity/reporting and generated-geometry commands against repository-owned/sanitized drawings.
- Save/reopen and cold-cache `.qsdb` continuity.
- Two-DWG switching with modeless WPF surfaces to prove document ownership/isolation remains correct under the .NET 8 host.
- WPF theme/resources, dialogs, DPI scaling and shutdown/reopen behavior.
- Real V26 host shutdown after runtime probe with no orphaned BricsCAD process.

## Update/install boundary

V26 intentionally does **not** reuse the V25 one-click updater. `QS3DUPDATE` in the V26 assembly is a fail-safe informational command until a V26-specific signed package/manifest/install lane is implemented and qualified. Never install `QS3D-BricsCAD-V25.update.json` or a V25 plugin payload into V26.

The V25 installer/update/release tooling remains independently owned and must not be weakened while V26 support is introduced. A future V26 installer/update batch must preserve the existing hash, Authenticode, atomic staging/rollback and host-major identity protections, then receive its own LOCAL_ONLY clean-machine proof.

## Evidence required

Record only sanitized evidence:

- exact QS3D commit SHA;
- BricsCAD V26 file/product version and x64 identity;
- installed .NET 8 Windows Desktop Runtime version;
- SHA-256 of the exact `QS3D.BricsCAD.V26.dll` tested;
- build/runtime gate PASS/FAIL summaries;
- interactive matrix results and any sanitized failure category;
- confirmation that no proprietary DLL, customer drawing/path, ProjectId, handle, signing secret or raw private artifact was published.

Until all required runtime evidence exists, report V26 as **source/build compatibility work with runtime qualification pending**, not production-ready.
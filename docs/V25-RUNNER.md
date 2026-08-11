# BricsCAD V25 self-hosted runner contract

The V25 integration/runtime gate intentionally uses a Windows self-hosted runner because the QS3D plugin references managed assemblies from an installed BricsCAD V25 environment. Vendor installers, licensing data, `BrxMgd.dll`, and `TD_Mgd.dll` must not be committed to this public repository.

## Required runner

- Windows x64.
- GitHub Actions self-hosted runner registered for this repository or an allowed runner group.
- Labels: `self-hosted`, `windows`, `x64`, `bricscad-v25`.
- BricsCAD V25 installed and licensed for integration/runtime work.
- `bricscad.exe`, `BrxMgd.dll`, and `TD_Mgd.dll` present in the configured BricsCAD directory.
- .NET Framework 4.8 targeting support for `QS3D.BricsCAD.V25` (`net48/x64`).
- Network access required by GitHub Actions checkout/setup actions.
- For the real UI screenshot gate, the runner must run in an **interactive logged-in Windows desktop session**, not as a Windows service/session 0.

## Install/setup

Download the official BricsCAD V25 x64 MSI with the licensed Bricsys account used for this runner and keep that MSI outside the repository. If the MSI is already cached on the runner, an elevated PowerShell can use:

```powershell
.\scripts\install-bricscad-v25.ps1 -MsiPath "D:\Installers\BricsCAD-V25.x.xx-x-en_US(x64).msi"
```

The helper verifies SHA-256, requires a valid Authenticode signature from a Bricsys signer by default, and then performs a quiet MSI installation. When a trusted download source publishes an expected SHA-256, pass it explicitly with `-ExpectedSha256` for an additional integrity check. `-AllowUntrustedPublisher` exists only for an intentional offline/certificate-chain exception and should not be used to bypass an unknown installer. Licensing is intentionally not automated or stored by this repository.

After installation:

1. log into the dedicated Windows runner account;
2. launch BricsCAD V25 once interactively;
3. activate a valid V25 license/trial for that account;
4. finish the first-launch interface/workspace dialogs;
5. close BricsCAD before starting the GitHub runtime gate;
6. start the GitHub Actions self-hosted runner interactively in that same desktop session.

## Repository configuration

Create repository variable:

`BRICSCAD_V25_DIR`

Its value is the absolute installation directory containing `bricscad.exe`, `BrxMgd.dll`, and `TD_Mgd.dll`.

Optional repository variable:

`BRICSCAD_V25_PROFILE`

Leave it empty to use the runner account's current initialized BricsCAD profile. Set it only when a known pre-initialized profile should be used for the runtime screenshot.

Do **not** create repository files containing vendor DLLs, installers, or license material.

## Validation order

`.github/workflows/bricscad-v25.yml` performs:

1. checkout;
2. source preflight;
3. Core Release build;
4. Core smoke tests;
5. validate `BRICSCAD_V25_DIR`, `bricscad.exe`, and managed V25 references;
6. build `QS3D.BricsCAD.V25` Release x64;
7. start real BricsCAD V25 and run a startup `.scr` file;
8. execute `NETLOAD` against the newly built `QS3D.BricsCAD.V25.dll`;
9. execute the in-host `QS3DRUNTIMEPROBE` command;
10. require a `status=PASS` marker proving the expected plugin DLL is loaded in 64-bit BricsCAD and both QS3D ribbon and palette initialization succeeded;
11. capture the BricsCAD HWND directly with `PrintWindow` into `bricscad-v25-qs3d.png`;
12. upload the plugin DLLs and the complete runtime-evidence directory.

The runtime evidence artifact contains, when successful:

- `runtime-result.txt` — proves the expected plugin command executed inside BricsCAD and records the runtime invariants;
- `runtime-metadata.json` — host/plugin version, SHA-256, runner and timing metadata;
- `runtime.scr` — exact NETLOAD command script used for the test;
- `bricscad-v25-qs3d.png` — screenshot of the real BricsCAD V25 window with QS3D requested visible.

The runtime helper must capture only the target BricsCAD HWND. It must not capture a desktop rectangle as a fallback, because an unrelated overlapping application could leak private UI into the qualification artifact. If `PrintWindow` cannot capture the host, the screenshot gate fails closed. `runtime-metadata.json` records `screenshot_capture=PrintWindow(hwnd)` for a successful capture.

For a separately installed exact package, the same helper supports `-DemandLoadOnly -SkipScreenshot`. That mode omits the explicit load command, invokes `QS3DRUNTIMEPROBE` directly, and still requires the in-host assembly path to match the registered installed loader supplied through `-PluginDll`. Metadata records `load_mode=DemandLoad`; the normal exact-build probe records `load_mode=NETLOAD`.

A build-only success is not a runtime success. Gate D is PASS only when the in-host marker and screenshot are both produced and the marker validates the expected DLL, x64 host, ribbon, and palette state.

## Current status

The historical probe run `31341184031` remained queued with no assigned `bricscad-v25` runner. The repository now has the full NETLOAD/runtime/screenshot harness, but a matching licensed interactive Windows runner still has to be online before this gate can actually execute.

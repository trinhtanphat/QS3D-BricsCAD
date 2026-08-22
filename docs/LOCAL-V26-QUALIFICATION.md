# Local BricsCAD V26 qualification

Status: `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE` until a licensed interactive BricsCAD V26 workstation or dedicated self-hosted runner is available.

## Why V26 is a separate gate

BricsCAD V26 hosts managed plugins on .NET 8 instead of the .NET Framework 4.8 lane used by BricsCAD V25. QS3D therefore emits a distinct `QS3D.BricsCAD.V26.dll` from `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`, targeting `net8.0-windows` and resolving `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` from the installed V26 directory only. `TD_MgdBrep.dll` is required because the shared quantity explanation/preview code uses the native BREP API.

The V25 project remains `net48`. Passing source/static checks or the Core smoke suite does **not** prove V26 runtime compatibility.

The V26 project keeps nullable annotations from the linked adapter source while retaining the established shared-source flow-warning context; all other repository warnings-as-errors remain active. It also emits `QS3D.BricsCAD.V26.runtimeconfig.json` with the explicit .NET 8 Windows Desktop framework contract required by the shared WPF/WinForms adapter. Update preferences use the V26-specific registry path, and unsigned direct preview download remains disabled for V26 so the Update Center fails closed to the manual release page while the signed-manifest path remains separate.

## Prerequisites

- Windows x64 interactive desktop.
- Licensed BricsCAD V26 x64.
- The licensed V26 installation must contain the co-located `BrxMgd.dll`, `TD_Mgd.dll`, and `TD_MgdBrep.dll` host assemblies.
- .NET 8 Windows Desktop Runtime x64 / compatible .NET 8 SDK.
- Python 3 and .NET SDK available for repository preflights/build.
- Clean checkout at the exact candidate SHA.
- For signed release/update qualification: the real code-signing certificate/private key must be available through the approved local certificate store; it must never be committed.
- No proprietary BricsCAD DLLs, customer DWGs, signing keys or private runtime paths committed to Git.

Set the host directory explicitly; do not point V26 builds at a V25 installation:

```powershell
$env:BRICSCAD_V26_DIR = 'C:\Program Files\Bricsys\BricsCAD V26 en_US'
```

If the installed locale/path differs, use that licensed V26 installation directory instead.

Before starting BricsCAD, verify that `dotnet --list-runtimes` reports both `Microsoft.NETCore.App 8.x` and `Microsoft.WindowsDesktop.App 8.x` for x64. A machine with BricsCAD V26 but no discoverable .NET 8 Windows Desktop runtime can enter `NETLOAD` without ever reaching the plugin initializer; that host prerequisite failure is not a plugin runtime PASS.

## Source/build gate

```powershell
python scripts/preflight-ci-manual-only.py
python scripts/preflight.py
python scripts/preflight-bricscad-v26.py
python scripts/preflight-v26-package-release.py
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

### Bounded native Slab P02 gate

Issue `#3576` owns one representative production native-edit cell under `LOCAL-017`. After the exact V26 Release build, run:

```powershell
.\scripts\test-bricscad-v26-native-polyline-edit.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -PluginDll .\src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll `
  -FixtureDwg .\samples\generated\QS3D-Sample.dwg `
  -Profile Default `
  -ArtifactDir <outside-repository-empty-directory> `
  -ConfirmDisposableCopies
```

The shared runner must reject a V25/V26 host or plugin-major mismatch. If the process environment sets `DOTNET_ROOT`, it must point to a complete .NET 8 host/runtime containing `dotnet.exe`, an 8.x `hostfxr.dll` and an 8.x `coreclr.dll`; an invalid override must be refused before artifact creation or BricsCAD launch.

This bounded gate uses the repository-generated disposable fixture and the existing production Slab probe. It drives one real top-level closed-POLYLINE crossing-window `STRETCH`, verifies pre-sync generated isolation, production source reconcile/metric and quantity refresh, generated invalidation/rebuild, scoped Health, save/sidecar persistence and a fresh-process cold reopen. The gate was `PENDING_LOCAL` until the exact evidence below passed; its bounded PASS cannot close the broader #80 or #1462 matrix.

Bounded P02 evidence is `LOCAL_PASS` on clean pushed exact SHA `54b7fce6127208085817f20dd0781b580a18e4bd`. The V26 `Release|x64` build completed with zero warnings/errors; ProductVersion was `0.1.0-preview.10081`, plugin SHA-256 was `BA0A0758BD2440D50455FDD9A36D992EE14869800B1700396CE5580F53E1882D`, and the portable plugin/Core PDB SourceLink records named the exact tested SHA. Licensed BricsCAD V26.2.07 / CLR 8.0.29 passed the generic x64/native-runtime identity control. The two-process P02 run then passed every sanitized production marker, including vertex STRETCH, pre-sync isolation, metric/quantity reconcile, invalidation/rebuild, native bounds, scoped Health, save/sidecar persistence and fresh-process cold reopen. The repository fixture hash remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; drawing, script and private state were restored with zero BricsCAD processes left. This remains a bounded synthetic Slab cell only; the broader native/private-DWG and release matrix is still pending.

## Package/install gate

After the exact V26 Release build passes source/build checks:

```powershell
.\scripts\package-v26.ps1
```

The package must contain at minimum:

- `QS3D.BricsCAD.V26.dll`;
- `QS3D.BricsCAD.V26.runtimeconfig.json`;
- `QS3D.Core.dll`;
- `install-v26-autoload.ps1`;
- `uninstall-v26-autoload.ps1`;
- `update-v26.ps1`;
- `PACKAGE-METADATA.json` with `product=QS3D`, `target=BricsCAD V26 x64`, `framework=net8.0-windows`;
- `SHA256SUMS.txt` covering every package payload except itself.

It must **not** contain `BrxMgd.dll`, `TD_Mgd.dll`, `TD_MgdBrep.dll`, a V25 plugin DLL, a V25 manifest or a V25 update package.

Clean-machine install proof must use the generated V26 installer and verify that DemandLoad registration is created only under matching BricsCAD `V26*` registry keys. Existing foreign/custom directories must remain fail-closed; `-Force` may not bypass package ownership identity.

Uninstall proof must verify canonical package identity before recursive removal and must exercise registry/filesystem rollback behavior on an injected failure.

## Signing/finalization gate

For a production candidate, sign the exact package executable payload with the approved code-signing certificate and timestamp server, then verify it:

```powershell
$payload = @(
  '.\dist\QS3D-BricsCAD-V26\QS3D.BricsCAD.V26.dll',
  '.\dist\QS3D-BricsCAD-V26\QS3D.Core.dll',
  '.\dist\QS3D-BricsCAD-V26\install-v26-autoload.ps1',
  '.\dist\QS3D-BricsCAD-V26\uninstall-v26-autoload.ps1',
  '.\dist\QS3D-BricsCAD-V26\update-v26.ps1'
)

.\scripts\sign-v26.ps1 `
  -Path $payload `
  -CertificateThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT `
  -TimestampServer $env:QS3D_TIMESTAMP_SERVER `
  -Confirm:$false

.\scripts\verify-v26-signatures.ps1 `
  -Path $payload `
  -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT

.\scripts\finalize-v26-signed-package.ps1 `
  -ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT `
  -Confirm:$false
```

Finalization must re-read both signed managed DLL identities, re-bind signed metadata, regenerate package hashes and rebuild the ZIP only after signature/identity checks pass.

## Signed update / one-click gate

V26 has a separate update channel:

- manifest asset: `QS3D-BricsCAD-V26.update.json`;
- package asset: `QS3D-BricsCAD-V26.zip`;
- installed updater: `update-v26.ps1`;
- mutex namespace: `Global\QS3D-BricsCAD-V26-Update-<Windows SID>`.

The V26 release client ignores repository releases that do not contain the exact V26 manifest asset **before** selecting the latest version. V25 does the same with its V25 manifest asset, preventing cross-major release-channel selection.

Generate a signed V26 manifest only after package signing/finalization:

```powershell
.\scripts\new-v26-update-manifest.ps1 `
  -PackageUri 'https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<TAG>/QS3D-BricsCAD-V26.zip' `
  -ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT `
  -OutputPath '.\dist\QS3D-BricsCAD-V26.update.json' `
  -Confirm:$false
```

`QS3DUPDATE` is now wired to the V26 Update Center source-side. One-click eligibility remains fail-closed unless the **currently running V26 DLL itself is Authenticode-valid**, the selected release belongs to the V26 manifest channel, manifest target/version/URL/hash/signer checks pass, and signed `update-v26.ps1` exists beside the installed plugin. The detached worker must wait for BricsCAD to close normally, keep the V26 update mutex reservation, verify updater signer again, run only the V26 updater, and reopen BricsCAD after success or recovery failure handling.

Do not call one-click update qualified until this exact flow is exercised against an actually signed V26 GitHub release asset set.

## Interactive matrix before V26 can be called qualified

Record sanitized evidence tied to the exact SHA for all of the following:

- `NETLOAD` of `QS3D.BricsCAD.V26.dll` into the current installed V26 build with no loader/type exceptions.
- Core QS3D command registration plus Ribbon, Workspace palette and Right Panel startup.
- `QS3DUPDATE` opens the V26 Update Center without seeing V25-only releases as V26 updates.
- Representative native 2D/3D authoring, semantic capture, quantity/reporting and generated-geometry commands against repository-owned/sanitized drawings.
- Save/reopen and cold-cache `.qsdb` continuity.
- Two-DWG switching with modeless WPF surfaces to prove document ownership/isolation remains correct under the .NET 8 host.
- WPF theme/resources, dialogs, DPI scaling and shutdown/reopen behavior.
- Clean-machine V26 install, update, rollback/cancel and uninstall.
- Real V26 host shutdown after runtime/update probes with no orphaned BricsCAD process.

## Manual release workflow

`.github/workflows/release-v26.yml` is intentionally `workflow_dispatch`-only. Stable publication requires explicit `confirm_release=RELEASE`, `run_runtime=true` and `sign_package=true`. The workflow must be dispatched from `main`, bind the release tag to source/package version, validate the exact V26 host, run source/Core/V26 gates, sign/finalize the package, run the exact signed payload through the V26 runtime gate, generate the V26-only manifest/checksum and publish only the expected V26 assets.

Remote source work must **not** dispatch or claim PASS for this workflow unless the real self-hosted V26/signing environment actually executes it.

## Evidence required

Record only sanitized evidence:

- exact QS3D commit SHA;
- BricsCAD V26 file/product version and x64 identity;
- installed .NET 8 Windows Desktop Runtime version;
- SHA-256 of the exact `QS3D.BricsCAD.V26.dll` and final V26 ZIP tested;
- signer thumbprint/public certificate identity and timestamp result, never the private key;
- build/runtime/package/install/update/uninstall gate PASS/FAIL summaries;
- interactive matrix results and any sanitized failure category;
- confirmation that no proprietary DLL, customer drawing/path, ProjectId, handle, signing secret or raw private artifact was published.

Until all required runtime/signing/install/update evidence exists, report V26 as **source/build/package/update implementation complete with local qualification pending**, not production-ready.

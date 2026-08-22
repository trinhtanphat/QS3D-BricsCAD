# Manual BricsCAD V26 build and release runbook

Updated 2026-08-12.

## Scope and truth boundary

This runbook covers the **QS3D BricsCAD V26 x64 plugin**. V26 is a separate managed-host lane targeting `net8.0-windows`; it does not replace the existing V25 `net48` lane and must never consume V25 package/update assets.

Source/static checks can prove repository contracts. A production-ready V26 claim additionally requires the exact candidate SHA to pass licensed BricsCAD V26 runtime, signed-package, clean-machine install/update/uninstall and representative-DWG qualification. See `docs/LOCAL-V26-QUALIFICATION.md`.

## Required machine

Use Windows x64 with:

- licensed BricsCAD V26 x64;
- .NET 8 SDK and Microsoft Windows Desktop Runtime 8.x x64;
- Python 3;
- an interactive desktop session for BricsCAD runtime validation;
- a code-signing certificate with private key and Code Signing EKU when signing a release.

Set:

```powershell
$env:BRICSCAD_V26_DIR = 'C:\Program Files\Bricsys\BricsCAD V26 en_US'
```

Use the actual licensed installation path if the locale/path differs. `bricscad.exe` must report file major version 26, and the directory must contain the matching host-owned `BrxMgd.dll`, `TD_Mgd.dll` and `TD_MgdBrep.dll`. The V26 exact-face quantity source requires the BREP compile reference; every host reference uses `Private=false` and stays outside release packages.

## Source/build gate

```powershell
python scripts/preflight-ci-manual-only.py
python scripts/preflight.py
python scripts/preflight-bricscad-v26.py
python scripts/preflight-v26-package-release.py
python scripts/preflight-all.py

dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build QS3D.V26.sln -c Release
```

The V26 adapter output is:

```text
src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll
```

`QS3D.sln` remains the V25-oriented solution. Use `QS3D.V26.sln` for V26 development/build so a V25 workstation is not forced to resolve V26 host references and vice versa.

## Runtime gate

With all existing BricsCAD processes closed:

```powershell
.\scripts\test-bricscad-v26-runtime.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -PluginDll .\src\QS3D.BricsCAD.V26\bin\x64\Release\net8.0-windows\QS3D.BricsCAD.V26.dll
```

The gate validates the V26 host major, x64 process, exact V26 plugin assembly and `QS3DRUNTIMEPROBE` Ribbon/palette readiness. Passing this probe is necessary but not sufficient for production qualification; use the full matrix in `docs/LOCAL-V26-QUALIFICATION.md`.

## Build package

```powershell
.\scripts\package-v26.ps1
```

The packager:

- reads `net8.0-windows` V26 Release output;
- requires `QS3D.BricsCAD.V26.dll` plus `QS3D.Core.dll` identity parity;
- derives standalone `install-v26-autoload.ps1`, `uninstall-v26-autoload.ps1` and `update-v26.ps1` from the current hardened V25 templates through the guarded V25→V26 transformer;
- emits `PACKAGE-METADATA.json` with `target = BricsCAD V26 x64`;
- creates `COMMANDS.txt`, full `SHA256SUMS.txt` and `QS3D-BricsCAD-V26.zip`;
- rejects proprietary BricsCAD managed DLLs from the package.

The generated installer/updater payloads must contain no V25/v25 token after transformation.

## Sign and finalize

Set the expected signing inputs outside the repository. Never commit a private key or certificate export.

```powershell
$thumbprint = '<40-hex-thumbprint>'
$timestamp = 'https://<trusted-timestamp-server>'

$payload = @(
  'dist\QS3D-BricsCAD-V26\QS3D.BricsCAD.V26.dll',
  'dist\QS3D-BricsCAD-V26\QS3D.Core.dll',
  'dist\QS3D-BricsCAD-V26\install-v26-autoload.ps1',
  'dist\QS3D-BricsCAD-V26\uninstall-v26-autoload.ps1',
  'dist\QS3D-BricsCAD-V26\update-v26.ps1'
)

.\scripts\sign-v26.ps1 -Path $payload -CertificateThumbprint $thumbprint -TimestampServer $timestamp -Confirm:$false
.\scripts\verify-v26-signatures.ps1 -Path $payload -ExpectedThumbprint $thumbprint
.\scripts\finalize-v26-signed-package.ps1 -ExpectedSignerThumbprint $thumbprint -Confirm:$false
```

Finalization must verify signatures and re-bind `QS3D / BricsCAD V26 x64` metadata to both signed managed DLLs **before** regenerating hashes and ZIP bytes.

## Create the V26 update manifest

For release tag `vX.Y.Z...`:

```powershell
$packageUri = 'https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/vX.Y.Z/QS3D-BricsCAD-V26.zip'
.\scripts\new-v26-update-manifest.ps1 `
  -PackageUri $packageUri `
  -ExpectedSignerThumbprint $thumbprint `
  -OutputPath .\dist\QS3D-BricsCAD-V26.update.json `
  -Confirm:$false
```

V26 one-click update accepts only the V26 manifest/package channel. Release discovery is host-major isolated before latest-version selection: V25 requires `QS3D-BricsCAD-V25.update.json`; V26 requires `QS3D-BricsCAD-V26.update.json`.

## Manual install/update validation

Before production publication, exercise on a disposable/clean V26 user profile:

1. first install through generated `install-v26-autoload.ps1`;
2. DemandLoad/`NETLOAD` and `QS3DRUNTIMEPROBE`;
3. `QS3DUPDATE` against a correctly signed V26 release;
4. rejection of a V25 manifest/ZIP and a relabelled/tampered V26 package;
5. upgrade over a known previous canonical QS3D V26 install;
6. rollback after an intentionally induced replacement failure;
7. uninstall and uninstall rollback/identity checks;
8. verification that foreign/custom directories cannot be replaced or recursively removed merely with `-Force`;
9. representative-DWG authoring, quantity/reporting, save/reopen, two-DWG modeless UI and generated-geometry checks.

Do not report these as PASS until they were actually executed against the exact candidate SHA/package.

## Owner-approved GitHub release

The V26 release workflow is `.github/workflows/release-v26.yml`. It is `workflow_dispatch`-only and its publish job requires `confirm_release=RELEASE`.

Stable releases require:

- `run_runtime=true`;
- `sign_package=true`;
- V26 licensed self-hosted runner labels: `self-hosted`, `windows`, `x64`, `bricscad-v26`;
- `BRICSCAD_V26_DIR` and optional `BRICSCAD_V26_PROFILE`;
- signing thumbprint/timestamp variables.

The workflow builds Core + V26, packages, signs/verifies/finalizes when requested, runs the exact signed V26 runtime gate for stable releases, creates the V26-only signed update manifest/checksum, and creates the GitHub Release as a draft. Before that draft can be published, the workflow must resolve the remote release tag (including annotated-tag dereference when applicable) and prove it targets the exact qualified `GITHUB_SHA`; verify the remote uploaded asset set exactly matches the expected V26 files; re-download every uploaded asset through the GitHub asset API and require both its byte size and SHA-256 to match the qualified local artifact; then resolve the remote tag again and require it still targets the same `GITHUB_SHA`. Any mismatch leaves the release as a draft.

Editing or committing this workflow does **not** authorize dispatch. Run it only when the repository owner explicitly requests a build/release.

## Required release assets

Signed V26 release:

```text
QS3D-BricsCAD-V26.zip
QS3D-BricsCAD-V26.zip.sha256
QS3D-BricsCAD-V26.update.json
```

V25 assets are a separate channel and must not appear in the V26 release lane.

## Never do this

- Never point `BRICSCAD_V26_DIR` at V25.
- Never load `QS3D.BricsCAD.V25.dll` as the V26 release payload.
- Never use `QS3D-BricsCAD-V25.update.json` or `QS3D-BricsCAD-V25.zip` for V26.
- Never package `BrxMgd.dll`, `TD_Mgd.dll`, `TD_MgdBrep.dll` or other proprietary BricsCAD runtime assemblies.
- Never publish a stable release without exact signed-payload V26 runtime evidence.
- Never claim signing, clean-machine install/update or runtime PASS from source review alone.

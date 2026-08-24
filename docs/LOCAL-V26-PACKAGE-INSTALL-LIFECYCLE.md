# Local V26 package install/uninstall lifecycle

Status: `PENDING_LOCAL` / `DO_NOT_RETRY_REMOTE`

Carrier: #3792  
Lane-Key: `issue-v26-package-install-lifecycle`  
Parent release qualification: #1462 / `docs/LOCAL-V26-QUALIFICATION.md`

This handoff qualifies only the clean-machine BricsCAD V26 package install/uninstall lifecycle. The remote/source lane prepares and statically guards the production package, generated installer/uninstaller and one-command local runner. It does **not** claim licensed BricsCAD runtime, production signing, update-channel, SECURELOAD or customer/private-DWG acceptance.

## Source defect closed before local pickup

The V26 installer is generated from the hardened V25 installer. V26 additionally requires `QS3D.BricsCAD.V26.runtimeconfig.json` beside the managed plugin because the adapter targets `net8.0-windows`. The V25 payload template does not have that file, so a pure V25→V26 token transform omitted it from the installed payload even though `package-v26.ps1` packaged and hashed it.

The source handoff therefore requires `scripts/new-v26-script-from-v25.ps1` to add exactly that V26-only runtimeconfig payload entry and fail closed if the installer payload anchor changes. The runner must verify the runtimeconfig is packaged, hash-covered, installed byte-for-byte from the package and targets `Microsoft.WindowsDesktop.App`.

## Local command

Start from a clean checkout at the exact pushed candidate SHA. Close every BricsCAD process and use a disposable V26 user profile/registry target that has no existing QS3D V26 DemandLoad registration.

```powershell
.\scripts\test-v26-package-install-lifecycle.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -VersionKey '<exact initialized V26 registry version key>' `
  -LanguageKey '<exact initialized language key, for example en_US>' `
  -ExpectedSourceSha (git rev-parse HEAD) `
  -ArtifactDir '.\artifacts\local-v26-package-install-lifecycle\run' `
  -ConfirmDisposableInstall
```

The command intentionally performs no `NETLOAD`, BricsCAD launch or licensed command execution. It uses the installed host only to bind the V26 build to exact licensed V26 managed references and to reject a non-V26 host directory.

## Fail-closed prerequisites

The runner refuses before owned install mutation when any of these conditions is true:

- source HEAD is not the supplied 40-hex SHA;
- the working tree is dirty, including untracked files;
- the OS is not Windows;
- a BricsCAD process is running;
- `bricscad.exe`, `BrxMgd.dll`, `TD_Mgd.dll` or `TD_MgdBrep.dll` is missing from the supplied host directory;
- host major is not 26;
- `VersionKey` is not V26 or `LanguageKey` is malformed;
- the selected V26 profile already contains a QS3D DemandLoad registration;
- the disposable install directory escapes `%LOCALAPPDATA%\QS3D\Qualification`;
- Release build/package generation, package identity or exact hash coverage fails.

The production installer/uninstaller are invoked without `-Force`; qualification may not bypass their package-ownership safeguards.

## What PASS proves

On one exact source SHA the runner must prove all of the following:

1. V26 `Release|x64` builds against the supplied V26 host directory.
2. `package-v26.ps1` produces `BricsCAD V26 x64` / `net8.0-windows` metadata, ZIP and exact `SHA256SUMS.txt` coverage.
3. `QS3D.BricsCAD.V26.runtimeconfig.json` is package-hash-covered.
4. The generated production V26 installer creates only the selected V26 DemandLoad registration and preserves unrelated V25 QS3D registration state.
5. DemandLoad `Loader`, `LoadCtrls=4`, description and `QS3D` command identity match the disposable installed V26 payload.
6. The installed payload is the exact canonical installer file set; every installed package-managed file has the same SHA-256 as its packaged source.
7. The installed runtimeconfig exists and targets `Microsoft.WindowsDesktop.App`.
8. The generated production V26 uninstaller removes the selected V26 registration and installed payload.
9. Unrelated V25 QS3D registration state and the qualification sentinel remain unchanged through install/uninstall.
10. Owned disposable registration/files/sentinel are removed in final cleanup.

## Sanitized evidence

Keep `v26-package-install-lifecycle.json` under ignored `artifacts/`. It may contain only exact source SHA, product version, package SHA-256, host major and bounded boolean results. It must not contain raw registry paths, install paths, private DLL paths, user identity, ProjectId/Handle, customer drawing names, proprietary BricsCAD binaries, signing secrets or unsanitized exceptions.

A successful source/static/CI run is **not** `LOCAL_PASS`. Only the local Windows/V26 execution above can promote this bounded package lifecycle cell. Failure should be returned as sanitized exact-SHA evidence; production/source defects then reopen or continue a source-fix carrier before local retry.

## Scope left pending

Even after this bounded install/uninstall cell passes, #1462 remains responsible for the broader V26 matrix, including actual DemandLoad/NETLOAD behavior, signed package finalization, clean update, rollback/cancel, SECURELOAD/trust, native command behavior, interactive UI/DPI, private/customer-like acceptance where authorized, and release publication policy.

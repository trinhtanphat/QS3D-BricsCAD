# Local V26 package install/uninstall lifecycle

Status: `LOCAL_PASS / BOUNDED`

Carrier: #3792  
Evidence carrier: #3916  
Canonical status closeout: #3922  
Lane-Key: `issue-v26-package-install-lifecycle`  
Parent release qualification: #1462 / `docs/LOCAL-V26-QUALIFICATION.md`

This handoff qualifies only the clean-machine BricsCAD V26 package install/uninstall lifecycle. The remote/source lane prepares and statically guards the production package, generated installer/uninstaller and one-command local runner. The bounded licensed Windows/V26 execution has now passed at one exact source SHA. It does **not** claim production signing, update-channel, SECURELOAD, customer/private-DWG acceptance, interactive native commands or UI/DPI coverage.

## Accepted bounded licensed evidence

The unchanged production lifecycle runner passed on exact clean runtime source `e90c6aba7ef7bf903042d42dd991f9e7112fe659` in licensed BricsCAD V26.2.07 x64 using canonical profile `V26x64/en_US`.

- ProductVersion: `0.1.0-preview.10081`.
- Generated package SHA-256: `60F5239611B13F424BAE49922E5D34ADF3FC12C3064BF7506FE06CD27B8B3F7C`.
- V26 `Release|x64` build: `0 warnings / 0 errors`.
- Runner: unchanged `scripts/test-v26-package-install-lifecycle.ps1 -ConfirmDisposableInstall`.
- Package identity/hash/runtimeconfig, V26-only disposable registration, installed payload/hash parity, uninstall removal, V25-registration preservation, unrelated-sentinel preservation and cleanup all passed.
- The runtimeconfig check accepted the real .NET 8 `runtimeOptions.frameworks` array and required `Microsoft.WindowsDesktop.App`; it did not fall back to the obsolete singular `runtimeOptions.framework` shape.
- Independent post-run readback found zero BricsCAD processes, no qualification V26 registration/install directory/sentinel residue, and unchanged unrelated V25 loader state.

Sanitized evidence is preserved by PR #3916 and `docs/agent-work-claims/2026-08-25-codex-issue3878-v26-package-install-local-pass.md`.

This result is historical exact-SHA evidence. A newer `main` SHA must not be called runtime-tested merely because it contains this claim. A future material change to the package/install lifecycle may require a separate current-candidate requalification; that does not erase this accepted bounded PASS at `e90c6aba7ef7bf903042d42dd991f9e7112fe659`.

## Source defect closed before local pickup

The V26 installer is generated from the hardened V25 installer. V26 additionally requires `QS3D.BricsCAD.V26.runtimeconfig.json` beside the managed plugin because the adapter targets `net8.0-windows`. The V25 payload template does not have that file, so a pure V25→V26 token transform omitted it from the installed payload even though `package-v26.ps1` packaged and hashed it.

The source handoff therefore requires `scripts/new-v26-script-from-v25.ps1` to add exactly that V26-only runtimeconfig payload entry and fail closed if the installer payload anchor changes. The runner must verify the runtimeconfig is packaged, hash-covered, installed byte-for-byte from the package and targets `Microsoft.WindowsDesktop.App`.

The install-lifecycle host-identity guard also accepts the canonical initialized x64 registry key `V26x64` (and the existing V26/V26.x family) while remaining bounded to major 26. V25, cross-major and malformed version keys remain rejected. This mirrors the already-qualified V26 package-update lifecycle contract and prevents a real licensed V26 profile from being rejected before any owned package mutation.

## Local command

For a future material-change requalification, start from a clean checkout at the exact intended pushed candidate SHA. Close every BricsCAD process and use a disposable V26 user profile/registry target that has no existing QS3D V26 DemandLoad registration.

```powershell
.\scripts\test-v26-package-install-lifecycle.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -VersionKey 'V26x64' `
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
- `VersionKey` is not a canonical V26 family key (`V26`, `V26.x`, `V26x64` or `V26x64.x`) or `LanguageKey` is malformed;
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

The accepted #3916 run satisfied all ten assertions at exact runtime source `e90c6aba7ef7bf903042d42dd991f9e7112fe659`.

## Sanitized evidence

Keep `v26-package-install-lifecycle.json` under ignored `artifacts/`. It may contain only exact source SHA, product version, package SHA-256, host major and bounded boolean results. It must not contain raw registry paths, install paths, private DLL paths, user identity, ProjectId/Handle, customer drawing names, proprietary BricsCAD binaries, signing secrets or unsanitized exceptions.

A successful source/static/CI run is **not** `LOCAL_PASS`. The accepted `LOCAL_PASS / BOUNDED` above comes only from the licensed local Windows/V26 execution recorded by #3916. Future failures after a material source change must be returned as sanitized exact-SHA evidence; production/source defects then reopen or continue a source-fix carrier before local retry.

## Scope left pending

Even after this bounded install/uninstall cell passes, #1462 remains responsible for the broader V26 matrix, including actual DemandLoad/NETLOAD behavior, signed package finalization, clean update, rollback/cancel, SECURELOAD/trust, native command behavior, interactive UI/DPI, private/customer-like acceptance where authorized, and release publication policy.

# Secure V25 Update Chain

The production updater is intentionally stricter than local packaging. A package may be useful for local/manual testing without signing, but it must not be published through the production update manifest until every executable payload is Authenticode-pinned.

## Executable payload trust boundary

The production signer must cover all code that is loaded now or installed for later execution:

- `QS3D.BricsCAD.V25.dll`
- `QS3D.Core.dll`
- `install-v25-autoload.ps1`
- `uninstall-v25-autoload.ps1`
- `update-v25.ps1`

This matters because a signed plugin can still load a modified dependency DLL, and a signed installer can still install a modified updater/uninstaller for later execution. Production therefore requires the same expected signer on all five files.

## Updater checks

`update-v25.ps1` requires all of the following before installation:

1. Manifest and package URLs use HTTPS with no embedded credentials.
2. The package host is explicitly allowed.
3. Compressed ZIP size and SHA-256 match the manifest.
4. Every ZIP entry is validated **before extraction**: no rooted/traversal/ADS-style path, bounded entry count, bounded total expanded size.
5. `SHA256SUMS.txt` validates packaged payload paths and hashes.
6. Both DLLs and install/update/uninstall scripts have valid Authenticode signatures from `ExpectedSignerThumbprint`.
7. All signature checks finish before the downloaded installer script is executed.
8. Installed-version downgrade protection reconciles the installed plugin assembly version with `PACKAGE-METADATA.json` before comparing versions. A mismatched installed DLL/metadata state fails closed instead of silently treating the installation as `0.0.0.0`; same-version repair still requires the explicit switch.
9. The downloaded manifest/package version is bound to the Authenticode-verified plugin assembly version, preventing metadata-only replay/downgrade substitution.
10. A one-click update freezes the matching V25/V26 registration's existing `OnStartup` or `OnCommand` mode only after every locale registration points to the running DLL and agrees on one valid `LoadCtrls` value. Missing, stale, malformed, or mixed registration state fails closed; new/manual installs still default to `OnCommand`.
11. BricsCAD must be closed before replacement; the installer keeps staging/backup rollback and never lowers `SECURELOAD`.

These checks close the supply-chain cases where a compromised package origin combines a legitimate signed plugin with a modified Core DLL or PowerShell script, and prevent damaged/missing local metadata from weakening the downgrade decision while an installed plugin is still present.

## Production packaging sequence

1. Run `scripts/package-v25.ps1` to create `dist/QS3D-BricsCAD-V25`.
2. Authenticode-sign all five executable payload files in that staging directory using `scripts/sign-v25.ps1` and the production code-signing certificate.
3. Run `scripts/finalize-v25-signed-package.ps1 -ExpectedSignerThumbprint <thumbprint>`.
   - verifies all five signatures;
   - binds metadata version to the signed plugin assembly version;
   - records the signed executable payload and signer in `PACKAGE-METADATA.json`;
   - rebuilds `SHA256SUMS.txt` after signing;
   - recreates `QS3D-BricsCAD-V25.zip` from the signed staging directory.
4. Run `scripts/new-v25-update-manifest.ps1`.
   - verifies all five staging signatures;
   - binds the release version to the signed plugin assembly;
   - reads the actual ZIP and requires its executable payload + package metadata to match staging byte-for-byte;
   - verifies the signatures again from the ZIP payload itself before emitting the manifest.
5. Publish the finalized ZIP and generated manifest to the approved HTTPS origin.

Do not modify or sign executable payload files after finalization without finalizing again. Authenticode changes file bytes, so `SHA256SUMS.txt` and the ZIP must be rebuilt after signing.

## Manual installer

`install-v25-autoload.ps1 -RequireSigned` applies the same executable-payload rule. With `-ExpectedSignerThumbprint`, all five executable files must have that signer before anything is copied to the install directory. DemandLoad registry state and payload replacement are snapshotted/rolled back together on failure.

## Safe uninstall

`uninstall-v25-autoload.ps1` validates a destructive file cleanup **before** it touches DemandLoad registration:

- normal deletion is limited to the canonical `%LOCALAPPDATA%\QS3D\...` tree;
- an existing target directory must contain both `QS3D.BricsCAD.V25.dll` and `PACKAGE-METADATA.json`;
- metadata must identify `product = QS3D` and `target = BricsCAD V25 x64`;
- a custom path or an intentionally damaged/migrated installation requires explicit `-Force` after the operator verifies the path;
- `-KeepFiles` removes only the selected DemandLoad registration and does not require a file-deletion identity check.

This prevents a typo or overly broad path from turning the uninstaller's recursive delete into unrelated LocalAppData data loss.

## Runtime limits

The updater exposes bounded controls for compressed size, total expanded size, and archive entry count. These defaults are intentionally well above the normal QS3D package footprint while still preventing unbounded extraction work and common ZIP-bomb/path-traversal classes.

## CI and release policy

All GitHub workflows remain manual-only. Source changes, commits, or pushes do not authorize a workflow dispatch. `preflight-updater.py` is auto-discovered by `preflight-all.py`, and both manual CI workflows parse the release PowerShell scripts. Real code signing and BricsCAD runtime evidence remain explicit release-owner operations.

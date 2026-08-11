# Agent Work Claim — installer package identity binding

- Claim ID: `INSTALLER-PACKAGE-IDENTITY-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `RELEASED`
- Registered: `2026-08-11T22:10:00+07:00`
- Released: `2026-08-11T22:14:00+07:00`
- Baseline main SHA: `aac1e9b148fdf775ba70ae35b867fded02fc92be`
- Priority: make the atomic V25 DemandLoad installer independently validate QS3D package identity, not only hashes/signatures/commands.

## Verified defect

`install-v25-autoload.ps1` verified `SHA256SUMS.txt`, required executable payload signatures when requested, command registration and transactional install/rollback, but did not independently require `PACKAGE-METADATA.json` product/target/assembly version/productVersion to match the managed DLL payload.

The hardened one-click updater already validated those identities before invoking the installer. Direct/manual invocation of the signed installer is a separate supported path, so the atomic install boundary also needed its own identity proof.

## Reserved scope

- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-installer-package-identity.py`
- this claim file

## Completed changes

### Installer identity boundary — `7b289bd9a63100eb36d5b3405b7b0dcaa58b66f4`

After the existing hash/signature/command integrity checks and before target discovery/staging/registry mutation, the installer now runs `Assert-PackageIdentity` and requires:

- `PACKAGE-METADATA.json` exists and parses;
- `product == QS3D` and `target == BricsCAD V25 x64`;
- metadata `version` parses as AssemblyVersion;
- metadata `productVersion` is strict SemVer with numeric-prerelease leading-zero rejection;
- metadata AssemblyVersion major/minor/build equals product SemVer major/minor/patch, allowing prereleases to share the same four-part AssemblyVersion;
- **both** `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` exist, expose the exact metadata AssemblyVersion, and expose `FileVersionInfo.ProductVersion` exactly equal to metadata productVersion.

This identity layer does not replace package trust: `SHA256SUMS.txt` verification and Authenticode publisher verification (when required/pinned) still run independently first.

The prior installer contracts were preserved: running-BricsCAD refusal/details, safe hash-manifest paths, command validation, verified-copy MOTW clearing, DemandLoad registration readback, registry snapshots, payload backup, rollback and original-error propagation.

### Regression gate — `6d9e4ec195d02ee3200f5771aea524d47ff884e6`

Added auto-discovered `scripts/preflight-installer-package-identity.py` requiring:

- strict product/target/version/productVersion metadata checks;
- both adapter/Core AssemblyVersion and ProductVersion binding;
- ordering `hash/signature integrity -> package identity -> target discovery -> staging -> registry mutation`;
- preservation of Authenticode, running-host refusal, MOTW, DemandLoad readback and transactional rollback markers.

## Validation / coordination

- Re-read the current installer before implementation and preserved the recent package-root, MOTW and actionable running-process/DemandLoad fixes.
- Confirmed `QS3D.Core.csproj` uses the same `Version`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` contract as the V25 adapter before binding Core identity in the installer.
- Compare from gate commit `6d9e4ec195d02ee3200f5771aea524d47ff884e6` to then-current `main` reported `behind_by: 0`; later compared commits touched unrelated Core/docs/preflight files.
- No force-push, reset or rebase was used.
- No GitHub Actions workflow was dispatched and no release was published.
- No signed clean-machine installer execution was performed in this connector session, so no local/runtime PASS is claimed.

## Result

The atomic DemandLoad installer now independently rejects mislabelled or internally version-inconsistent packages before any payload or registry mutation, covering both manual installs and defense-in-depth for updater-driven installs. Production clean-machine signing/install/update/rollback proof remains `LOCAL-009 / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.

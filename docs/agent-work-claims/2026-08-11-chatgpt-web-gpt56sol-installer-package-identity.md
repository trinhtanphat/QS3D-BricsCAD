# Agent Work Claim — installer package identity binding

- Claim ID: `INSTALLER-PACKAGE-IDENTITY-20260811`
- Owner: `ChatGPT Web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Registered: `2026-08-11T22:10:00+07:00`
- Baseline main SHA: `aac1e9b148fdf775ba70ae35b867fded02fc92be`
- Priority: make the atomic V25 DemandLoad installer independently validate QS3D package identity, not only hashes/signatures/commands.

## Verified defect

`install-v25-autoload.ps1` verifies `SHA256SUMS.txt`, required executable payload signatures when requested, command registration and transactional install/rollback. It does not independently require `PACKAGE-METADATA.json` product/target/assembly version/productVersion to match `QS3D.BricsCAD.V25.dll`.

The hardened one-click updater validates those identities before invoking the installer, so one-click is already protected. However, direct/manual invocation of the signed installer is a separate supported path. A stale/mislabelled same-publisher package can therefore reach the atomic install boundary without the same product/assembly identity proof.

## Reserved scope

- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-installer-package-identity.py` (new)
- this claim file

## Non-overlap / preservation

- Preserve existing SHA256SUMS path safety, Authenticode publisher pinning, command discovery, MOTW clearing order, running-BricsCAD refusal, DemandLoad validation, payload staging, registry snapshot/rollback and fresh-install cleanup.
- Do not edit updater C#, update/manifest PowerShell, package/finalize/sign scripts, release workflow or unrelated product lanes.

## Intended contract

1. After hash/signature verification and before any payload/registry mutation, require `PACKAGE-METADATA.json` to exist and parse.
2. Require `product == QS3D`, `target == BricsCAD V25 x64`, valid assembly `version`, and strict SemVer `productVersion`.
3. Read `QS3D.BricsCAD.V25.dll` AssemblyVersion and `FileVersionInfo.ProductVersion`; require exact equality with package metadata version/productVersion.
4. Require the metadata assembly major/minor/build to match the product SemVer major/minor/patch, while allowing prereleases to share the same four-part AssemblyVersion.
5. Keep signed publisher verification independent; metadata cannot substitute for Authenticode.
6. Add an auto-discovered regression gate proving identity checks occur before staging/install mutation and prior security/rollback contracts remain present.

## Validation / release conditions

- Re-read current installer before writes and preserve concurrent installer fixes.
- Re-fetch installer/gate and verify ancestry with `behind_by: 0`.
- Do not dispatch GitHub Actions or publish a release.
- Actual signed clean-machine install remains `LOCAL-009 / PENDING_LOCAL`; no remote runtime PASS claim.
- Mark `RELEASED` only after installer + gate are committed on `main`.

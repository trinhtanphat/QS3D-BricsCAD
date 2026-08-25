# Local V26 package update lifecycle qualification

Status: `PENDING_LOCAL` / `LOCAL_ONLY` / `DO_NOT_RETRY_REMOTE`.

This gate qualifies the source-side V26 package updater and installer transaction against a disposable per-user installation. Hosted CI may validate the runner and source contract, but it cannot replace Windows + licensed BricsCAD V26 + real signed release assets.

## What this gate proves

The runner `scripts/test-v26-package-update-lifecycle.ps1` performs one coherent lifecycle:

1. verifies a clean checkout at the exact `ExpectedSourceSha`;
2. verifies the configured host is BricsCAD major 26 and the registry identity is V26-only;
3. builds/packages the exact source and installs that package into a GUID-named directory under `%LOCALAPPDATA%\QS3D\Qualification`;
4. verifies the baseline installed payload against `SHA256SUMS.txt`;
5. invokes the generated `update-v26.ps1` against a real HTTPS, signed, strictly newer V26 release manifest;
6. re-verifies the upgraded payload and requires `productVersion` to change;
7. invokes a real signed older-release manifest and requires downgrade refusal without changing the upgraded payload;
8. deterministically forces the **real generated installer transaction** to fail after payload replacement by temporarily shadowing `New-ItemProperty` only inside the qualification PowerShell process; the shipped installer/updater receives no test switch or security bypass;
9. requires the installer catch path to restore both the upgraded payload tree and exact DemandLoad registration digest, and to restore the upgraded `productVersion`;
10. invokes the same-version update with `-AllowSameVersion -WhatIf` and requires the cancel/no-op path to preserve the upgraded payload;
11. verifies an unrelated sentinel remains untouched;
12. invokes the packaged uninstaller during cleanup and writes sanitized JSON evidence.

The forced failure is deliberately external to shipped production scripts: command shadowing is installed immediately before the qualification installer call and removed in `finally`. The production installer still executes its normal staging, backup, replacement, registry snapshot and rollback code. No signing, HTTPS, package-integrity, ownership, mutex, SemVer or official-release check is weakened.

The runner does not launch BricsCAD and does not claim runtime qualification. Keep the broader V26 runtime/one-click-update matrix in `docs/LOCAL-V26-QUALIFICATION.md` pending until its own licensed evidence exists.

## Required local inputs

Use a disposable V26 profile with all BricsCAD processes closed. Prepare two **real signed** V26 release asset sets using the repository release pipeline:

- `UpgradeManifestUri`: HTTPS manifest for a version strictly newer than the exact-source baseline package;
- `RollbackManifestUri`: HTTPS manifest for an older version than the successfully installed upgrade;
- `ExpectedSignerThumbprint`: the approved 40-hex signing certificate thumbprint shared by those release assets.

Both manifests/packages must satisfy the production updater's existing HTTPS, host-major, target, SemVer, package-size, archive-entry, SHA-256, Authenticode signer and official GitHub-release snapshot rules. Do not weaken those rules or introduce a test-only updater bypass.

## Command

From a clean checkout of the exact pushed candidate SHA:

```powershell
.\scripts\test-v26-package-update-lifecycle.ps1 `
  -BricsCadDir $env:BRICSCAD_V26_DIR `
  -VersionKey '<installed V26 registry version key>' `
  -LanguageKey '<installed language key, e.g. en_US>' `
  -ExpectedSourceSha '<40-hex exact candidate SHA>' `
  -UpgradeManifestUri 'https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<NEWER_TAG>/QS3D-BricsCAD-V26.update.json' `
  -RollbackManifestUri 'https://github.com/trinhtanphat/QS3D-BricsCAD/releases/download/<OLDER_TAG>/QS3D-BricsCAD-V26.update.json' `
  -ExpectedSignerThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT `
  -ArtifactDir '<outside-repository artifact directory>' `
  -ConfirmDisposableInstall
```

Before the licensed/local run, the source-side guard must pass:

```powershell
python scripts/preflight-v26-package-update-lifecycle.py
```

## Expected evidence

The runner emits schema-2 `v26-package-update-lifecycle.json` with no private paths, certificate material or raw registry data. A successful local result requires all of these fields:

- `status = PASS`;
- `sourceSha` equals the exact tested commit;
- `hostMajor = 26`;
- non-empty and different `baselineVersion` / `upgradedVersion`;
- `baselineInstalled = true`;
- `upgradeSucceeded = true`;
- `upgradedPayloadValid = true`;
- `downgradeRejected = true`;
- `downgradePreservedState = true`;
- `transactionalFailureRejected = true`;
- `transactionalPayloadRolledBack = true`;
- `transactionalRegistryRolledBack = true`;
- `cancelPreservedState = true`;
- `unrelatedSentinelPreserved = true`;
- `cleanupComplete = true`.

Until a local agent records that exact evidence against the exact merged/pushed candidate, this gate remains `PENDING_LOCAL`; source/static CI success alone is not `LOCAL_PASS`.

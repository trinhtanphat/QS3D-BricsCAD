# Work claim — installer replacement identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-installer-replacement-identity`
- Registered: `2026-08-12T00:23:00+07:00`
- Completed: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `77448cac49464953b69983f00aa4d36036753cc2`
- Priority: owner-requested continue-all review; close a destructive install boundary where `-Force` could move and later delete an arbitrary existing `InstallDirectory` without first proving that directory was an existing QS3D V25 installation.

## Completed changes

- `665390681226ec48c2e71b267ee580bb51d16287` — `scripts/install-v25-autoload.ps1` now has `Assert-ExistingInstallDirectorySafeToReplace`. An existing target must be a directory and must pass the canonical `Assert-PackageIdentity` metadata + managed DLL assembly/product-version checks before the installer assigns a backup path or moves the target. `-Force` remains required but is no longer sufficient by itself.
- `e998ba20248aba454b7dd65483fabdf380f63245` — `scripts/preflight-installer-package-identity.py` now models first-install/force/foreign-path cases, requires the replacement identity helper/call, and pins source ordering through existing target identity -> backup assignment/move -> registry mutation.
- `fc078a447deadb6d5ad4063a5f4ae689bd89877a` — documented the forced-replacement identity boundary in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Reconstructed the exact current installer blob from SHA-consistent ranged reads before the write because the file exceeded connector display limits.
- Inspected GitHub's exact implementation diff for `66539068...`: it adds one helper plus one invocation immediately before the backup assignment/move. Existing hash/signature/source-package identity, staging, DemandLoad mutation and rollback code was preserved; the only unrelated textual artifact is removal of the final newline at EOF.
- Re-fetched current `main` installer blob `9447f857b275b65a701889f230a16b0944f2acd5`; the existing-directory helper still delegates to `Assert-PackageIdentity`, rejects non-directory targets and is invoked after `-Force` but before `$backup` / `Move-Item`.
- Re-fetched current `scripts/preflight-installer-package-identity.py` blob `8decff2b571533bec8c39a488df12ce71999585a`; it requires all new guards and ordering.
- Executed the deterministic replacement policy model: nonexistent target PASS; existing target without force FAIL; forced file FAIL; forced foreign directory FAIL; forced verified QS3D directory PASS.
- No Windows install, registry mutation, BricsCAD runtime or customer data was exercised in this connector environment. No GitHub Actions were dispatched/re-run.

## Coordination / exclusions respected

The active updater generation-publication lane explicitly excluded installer/uninstaller scripts. No uninstall, updater, package-finalization/signing, product source under `src/**`, tests under `tests/**` or active feature lane was modified. All writes were SHA-guarded; no force-push was used.

## Result

A mistaken custom `InstallDirectory` can no longer be destructively replaced merely by adding `-Force`: if the path exists, the installer must first prove it is a canonical QS3D BricsCAD V25 payload. First installs and verified QS3D upgrades remain supported. This lane is complete.

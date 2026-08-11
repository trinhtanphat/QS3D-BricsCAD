# Work claim — installer replacement identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-installer-replacement-identity`
- Registered: `2026-08-12T00:23:00+07:00`
- Baseline main SHA: `77448cac49464953b69983f00aa4d36036753cc2`
- Priority: owner-requested continue-all review; close a destructive install boundary where `-Force` can move and later delete an arbitrary existing `InstallDirectory` without first proving that directory is an existing QS3D V25 installation.

## Reserved scope

Harden `scripts/install-v25-autoload.ps1` so replacement of an already-existing install directory is permitted only after fail-closed QS3D V25 package identity validation of that existing directory. A first install into a nonexistent directory remains supported, and legitimate custom directories remain supported when their existing payload has canonical QS3D identity. Extend the existing installer package-identity regression and document the force-replacement boundary.

## Expected surfaces

- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-installer-package-identity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- uninstall transaction/identity code, updater source/coordinator, package creation/finalization, signing policy, registry target semantics, BricsCAD runtime, `src/**`, `tests/**`, active product lanes and GitHub Actions dispatch/re-run.

## Validation plan

- Re-fetch exact installer/preflight blobs and inspect resulting diffs.
- Require existing-directory identity validation before the `Move-Item $installFull -> $backup` destructive boundary.
- Preserve current package hash/signature/identity checks before staging and current transactional rollback behavior.
- Regression must prove replacement identity guard precedes backup move and cannot be bypassed by `-Force` alone.
- Execute the Python source regression/model with `python -S` where practical; no local install or registry mutation is available in this connector environment.

## Coordination

The active updater generation-publication claim explicitly excludes installer/uninstaller scripts. Historical package-identity, uninstall-transaction and MSI-provisioning claims are completed. No current claim was found for force-replacement ownership of `InstallDirectory`.

## Completion condition

An existing non-QS3D directory cannot be moved/deleted by forced install, regression/docs are on `main`, and this claim is marked `COMPLETED` with validation evidence.

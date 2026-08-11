# Work claim — uninstall force identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-uninstall-force-identity`
- Registered: `2026-08-12T00:30:00+07:00`
- Baseline main SHA: `ce8f8a7a02517cb944e4abe559bb65bd2748e129`
- Priority: owner-requested continue-all review; close a destructive uninstall boundary where `-Force` currently bypasses both custom-path scope and QS3D package identity, allowing an arbitrary existing directory to reach quarantine/recursive deletion.

## Reserved scope

Harden `scripts/uninstall-v25-autoload.ps1` so `-Force` may authorize an intentional custom path outside the default QS3D LocalAppData scope, but never bypasses QS3D V25 ownership/identity validation before files are staged for recursive removal. Keep `-KeepFiles` registry-only behavior and transactional quarantine/registry rollback semantics. Extend the existing uninstall transaction regression and align release/install documentation.

## Expected surfaces

- `scripts/uninstall-v25-autoload.ps1`
- `scripts/preflight-uninstall-transaction.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- installer replacement code, updater/coordinator, package generation/finalization, signing policy, registry snapshot algorithm, BricsCAD runtime, `src/**`, `tests/**`, active product lanes and GitHub Actions dispatch/re-run.

## Validation plan

- Re-fetch exact uninstall/preflight blobs before writes and inspect diffs.
- Identity files/metadata must be required for every file-removal path; `-Force` only controls whether an out-of-default-scope custom path is permitted.
- Regression model must reject default/custom foreign directories regardless of force, allow verified default directory without force, allow verified custom directory only with force, and preserve `-KeepFiles` no-file-removal behavior.
- Pin identity validation before quarantine `Move-Item` and recursive cleanup.
- Execute deterministic Python policy/source regression where practical; no Windows registry/filesystem uninstall is available in this connector environment.
- No GitHub Actions dispatch/re-run.

## Coordination

Historical uninstall transaction/serialization lanes are completed. The currently active updater generation-publication lane explicitly excludes installer/uninstaller scripts. No current claim was found for `-Force` bypass of uninstall ownership identity.

## Completion condition

`-Force` can no longer authorize recursive removal of a foreign/non-QS3D directory, regression/docs are on `main`, and this claim is marked `COMPLETED` with validation evidence.

# Work claim — BricsCAD V25 MSI identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-msi-identity`
- Registered: `2026-08-11T23:54:00+07:00`
- Baseline main SHA: `ed6f268312a46d4639bcb7c6b630ad479d16903c`
- Priority: owner-requested whole-repository review; close a verified local installer-helper identity gap where a valid Bricsys-signed MSI can reach `msiexec` without proving that MSI ProductName/ProductVersion identify BricsCAD V25.

## Reserved scope

Harden `scripts/install-bricscad-v25.ps1` so the resolved MSI must identify BricsCAD and ProductVersion major 25 before installation. Keep existing HTTPS download, optional exact SHA-256 pin, Authenticode publisher gate, filename advisory and install arguments. Add an auto-discovered static regression and document the local helper boundary.

## Expected surfaces

- `scripts/install-bricscad-v25.ps1`
- `scripts/preflight-bricscad-v25-installer-identity.py` (new)
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- cloud release MSI pin/digest/URI logic, package installer/updater, signing certificate policy, cached installer version changes or MSI download source policy.
- `src/**`, `tests/**`, active product lanes, GitHub Actions dispatch/re-run and licensed BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch exact helper blob before write and inspect resulting diff.
- Regression model accepts BricsCAD with V25 ProductVersion and rejects non-BricsCAD and non-V25 versions; source order requires identity validation before `Start-Process msiexec.exe`.
- Preserve signature/hash checks before MSI execution.
- Execute Python regression with `python -S`; PowerShell/MSI execution is not available in this connector environment.

## Coordination

Historical installer/package identity and cloud MSI identity lanes are completed. Recent current-main work is in browser, quantity, documentation, formula and other product lanes; no current claim was found for `scripts/install-bricscad-v25.ps1` MSI ProductName/ProductVersion validation.

## Completion condition

The local BricsCAD installer helper fails closed on non-BricsCAD/non-V25 MSI identity before `msiexec`, regression/docs are on `main`, and this claim is marked `COMPLETED`.

# Work claim — BricsCAD V25 MSI identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-v25-msi-identity`
- Registered: `2026-08-11T23:54:00+07:00`
- Completed: `2026-08-11T23:58:00+07:00`
- Baseline main SHA: `ed6f268312a46d4639bcb7c6b630ad479d16903c`
- Priority: owner-requested whole-repository review; close a verified local installer-helper identity gap where a valid Bricsys-signed MSI could reach `msiexec` without proving that MSI ProductName/ProductVersion identify BricsCAD V25.

## Reserved scope

Harden `scripts/install-bricscad-v25.ps1` so the resolved MSI must identify BricsCAD and ProductVersion major 25 before installation. Keep existing HTTPS download, optional exact SHA-256 pin, Authenticode publisher gate, filename advisory and install arguments. Add an auto-discovered static regression and document the local helper boundary.

## Completed changes

- `e7cfdc9160568140ec9b1d21792730bd019bd2ec` — `scripts/install-bricscad-v25.ps1` now opens the MSI Property table after hash/signature checks, requires ProductName to identify BricsCAD and ProductVersion to identify major 25, logs the verified identity and only then constructs/invokes `msiexec`.
- `5c7d4d981d4ce7ce9f7ae40af3dd263dda2fd7e5` — added `scripts/preflight-bricscad-v25-installer-identity.py`; it contains positive/negative MSI identity models, source-token guards and an ordering assertion for hash → signature → identity → `msiexec`.
- `633191a5472dc16bd67c1f7790cf5de02ba5afe3` — documented the local V25 installer-helper identity boundary in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Inspected exact implementation commit `e7cfdc91...`; GitHub diff only adds MSI ProductName/ProductVersion reads/guards plus one verified-identity log. Existing SHA-256/signature checks and install arguments were not changed.
- Regression model accepts `BricsCAD`/`25`, `BricsCAD Ultimate`/`25.2.10` and `BricsCAD Pro`/`25.1.07.1`.
- Negative cases reject non-BricsCAD ProductName, V24, V26, `250.x`, missing ProductName and missing ProductVersion.
- Executed the exact authored Python regression with `python -S` against a synthetic installer source fixture matching the pushed ordering/tokens; exit `0`, output `BricsCAD V25 installer identity preflight passed.`
- PowerShell/Windows Installer COM and a real MSI were not executed in this connector environment. No GitHub Actions were dispatched/re-run and no licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

No cloud MSI pin/digest/URI logic, QS3D package installer/updater, signing policy, source product code, tests or active feature lanes were changed. This lane remained limited to the local BricsCAD V25 provisioning helper and its static contract.

## Result

A renamed or otherwise misidentified Bricsys-signed MSI for another BricsCAD major version can no longer pass the helper merely because the signature/publisher and filename look acceptable; MSI product identity is now a required pre-install gate. This lane is complete and released.

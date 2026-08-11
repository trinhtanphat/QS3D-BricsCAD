# Work claim — updater Authenticode verification

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-updater-authenticode-verification`
- Registered: `2026-08-11T21:15:00+07:00`
- Baseline main SHA: `289b9350c084a81f05f0826f6447ceb3536d6152`
- Priority: security hardening of the newly released one-click update lane.

## Verified defect

`SecureUpdateLauncher.TryGetCurrentSignerThumbprint(...)` currently treats `X509Certificate.CreateFromSignedFile(...)` plus a 40-character thumbprint as sufficient proof that the running plugin has a valid Authenticode signature. Certificate extraction alone is not an Authenticode integrity/trust verification, yet that thumbprint becomes the publisher anchor used to validate the detached updater.

## Reserved scope

Require a real Windows Authenticode verification result for the running QS3D plugin before its signer thumbprint can become the one-click update trust anchor, and update the auto-update regression gate accordingly.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Updates/SecureUpdateLauncher.cs`
- `scripts/preflight-auto-update.py`
- this claim file

## Excluded scope

- GitHub release selection/SemVer/channel behavior
- update UI/Ribbon/Start Center redesign
- release publication or workflow dispatch
- package/update PowerShell semantics unless source proof requires a narrowly scoped compatibility change
- Quantity, Workspace, Direct Draw, reporting, Core mutation or other active feature lanes
- native/local update qualification already delegated to `LOCAL-009`

## Validation plan

- re-read latest `main` before writing and preserve concurrent updater changes
- fail closed unless Windows Authenticode verification succeeds before certificate/thumbprint extraction is accepted
- regression gate must reject the old certificate-extraction-only trust-anchor contract
- inspect committed source/status evidence; do not dispatch manual Actions

## Completion condition

Running-plugin publisher pinning requires real Authenticode verification in source, the source contract is regression-covered, changes are pushed to `main`, and this claim is marked `COMPLETED` with runtime limitations explicit.

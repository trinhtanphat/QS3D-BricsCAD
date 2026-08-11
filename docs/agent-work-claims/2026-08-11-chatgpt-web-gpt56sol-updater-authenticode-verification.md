# Work claim — updater Authenticode verification

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-updater-authenticode-verification`
- Registered: `2026-08-11T21:15:00+07:00`
- Completed: `2026-08-11T21:22:00+07:00`
- Baseline main SHA: `289b9350c084a81f05f0826f6447ceb3536d6152`
- Priority: security hardening of the newly released one-click update lane.

## Verified defect

`SecureUpdateLauncher.TryGetCurrentSignerThumbprint(...)` treated `X509Certificate.CreateFromSignedFile(...)` plus a 40-character thumbprint as sufficient proof that the running plugin had a valid Authenticode signature. Certificate extraction alone did not enforce the Windows Authenticode integrity/trust result, yet that thumbprint became the publisher anchor used to validate the detached updater.

## Completed changes

- `512972f03825e65cccb73979fa35258892918899` — added Windows `WinVerifyTrust` verification using the `WINTRUST_ACTION_GENERIC_VERIFY_V2` policy before signer-certificate extraction is allowed to establish the updater publisher anchor. The trust call uses file verification, no UI, explicit verify/close state actions and fails closed on any nonzero verification result.
- Existing signer-thumbprint pinning, updater-script `Get-AuthenticodeSignature` validation, graceful BricsCAD close/no-kill handoff and package/release trust chain remain unchanged.
- `c0df541db200622a25f25341aad37ca57c8a3950` — strengthened `scripts/preflight-auto-update.py` so the source contract requires `WinVerifyTrust`, the generic Authenticode policy GUID, verification-state cleanup, success-only trust and ordering that verifies the running plugin before `CreateFromSignedFile(...)` can provide the thumbprint.

## Validation evidence

- Microsoft Win32 documentation states that `WinVerifyTrust` invokes the software-publisher trust provider and can verify a PE comes from a trusted software publisher and has not been modified since signing; the official PE-signature example uses `WINTRUST_ACTION_GENERIC_VERIFY_V2`, `WTD_CHOICE_FILE`, `WTD_STATEACTION_VERIFY` and a subsequent `WTD_STATEACTION_CLOSE` call.
- Re-read `SecureUpdateLauncher.cs` and `scripts/preflight-auto-update.py` from current `main` after the implementation commits; the verification call precedes signer extraction and the source gate enforces that ordering.
- Attempted an isolated C# compile check, but the available container has no `dotnet`, `csc`, `mcs`, `mono` or `msbuild`; no compile PASS is claimed from this remote session.
- GitHub reports no combined status checks and no automatic workflow runs for the regression commit, consistent with manual-only CI. No Actions workflow was dispatched.
- No signed Windows/BricsCAD V25 environment was available here; actual WinVerifyTrust behavior on the release DLL and the full detached update flow remain covered by existing local qualification item `LOCAL-009`.

## Coordination / exclusions respected

No edits were made to release selection/SemVer, Update Center UI, Ribbon/Start Center, package/update PowerShell semantics, Quantity, Workspace, Direct Draw, reporting or Core mutation lanes.

## Result

The running QS3D DLL must now pass real Windows Authenticode verification before its signer thumbprint can become the one-click updater trust anchor. Source regression coverage is on `main`; native signed-release qualification remains local-only.

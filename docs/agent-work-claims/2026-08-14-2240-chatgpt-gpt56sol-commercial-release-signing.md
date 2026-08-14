# Work claim — commercial release signing hardening

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-commercial-signing`
- Registered: `2026-08-14T22:40:00+07:00`
- Baseline main SHA: `cf786992754c2d8c7cc0d8a471280a6ac9d539e1`
- Implementation branch: `agent/chatgpt-gpt56sol/commercial-release-signing-hardening`
- Integration batch: `integration/20260814-commercial-release-signing`
- Priority: owner requested Signing / commercial release hardening from the current approximately 80% assessment to production-grade remote-safe completeness.

## Reserved scope

Add a dedicated fail-closed commercial release signing path for first-party QS3D V25 PE artifacts without changing the existing unsigned preview release workflow. Commercial publication must require Authenticode SHA-256 signing, RFC3161 timestamping, exact signer verification, package integrity verification, deterministic provenance/checksum output and secret/certificate cleanup before release publication.

## Expected surfaces

- `.github/workflows/release-v25-commercial.yml` — new commercial-only workflow with least-privilege jobs and build -> sign -> verify -> package -> verify -> draft -> verify -> publish ordering.
- `scripts/sign-v25-commercial.ps1` — fail-closed signing helper using an ephemeral imported PFX certificate, SHA-256 file digest and RFC3161 timestamp.
- `scripts/verify-v25-commercial-signatures.ps1` — verify all required first-party PE artifacts against the expected signer thumbprint and valid Authenticode/timestamp status.
- `scripts/preflight-commercial-release-signing.py` — static regression guard for commercial signing/publish ordering and no unsigned fallback.
- `docs/COMMERCIAL-RELEASE-SIGNING.md` — operator runbook for certificate/secrets, rotation/revocation, emergency stop and manual artifact verification.
- this claim for coordination and close-out.
- `scripts/package-v25.ps1` is a read-only dependency; commercial signing occurs on build outputs before packaging.

## Excluded scope

- Do not modify `.github/workflows/release-v25-cloud.yml`; it is reserved by the active CI/package-integrity claim and remains the unsigned preview path.
- Do not modify `.github/workflows/ci.yml`, `scripts/verify-v25-package.ps1`, `scripts/test-v25-package-verifier.ps1`, `CI_POLICY.md`, or other surfaces reserved by the active CI/package-integrity claim.
- Do not modify preview tag/ordinal derivation, `scripts/prepare-v25-cloud-release.ps1`, release-preview sequence guards or dispatcher behavior.
- Do not modify BricsCAD licensed/native runtime behavior, source features, updater product behavior, proprietary BricsCAD assemblies or unrelated active claims.
- No implementation workflow/script/doc commit directly to `main`; implementation remains on the declared agent branch until integration.

## Validation plan

- PowerShell helpers fail closed on missing/invalid PFX material, missing private key/EKU, invalid or unexpected signer thumbprint, missing PE artifacts, invalid signature or missing trusted RFC3161 timestamp.
- Signing helper never prints certificate password/PFX bytes; PFX file and imported certificate are removed in `finally`/always cleanup paths.
- Commercial workflow is main-only, exact stable SemVer tag only, explicit confirmation only, checks source/tag identity, grants `contents: write` only to the release publication job, and has no unsigned fallback.
- First-party `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` are signed before `package-v25.ps1`; the packaged copies are verified again before draft upload and downloaded draft bytes are verified before publication.
- Static preflight proves the required step/dependency/order/security tokens remain present and publication cannot depend on an unsigned path.
- Re-read final branch diff against refreshed `main`; use combined integration/current-main CI evidence where available. A real commercial certificate signing run is reported only when the required repository secrets/certificate are configured and a commercial workflow run proves the exact artifact.

## Coordination

The active `chatgpt-web-gpt56sol-ci-package-integrity` lane owns preview package/archive verification and `.github/workflows/release-v25-cloud.yml`; its claim explicitly excludes installer-signing policy. This lane therefore uses only new commercial workflow/signing surfaces and treats `package-v25.ps1` as read-only. If a concurrent claim begins reserving any expected surface above, this lane stops and reconciles before further implementation writes.

## Completion condition

Repository-side commercial release hardening is integrated when the dedicated workflow can only publish after exact-source validation, first-party Authenticode SHA-256 + RFC3161 signing, exact signer verification, package/draft-byte verification, deterministic checksums/provenance, and secret/certificate cleanup; source guard/runbook coverage prevents regression; the integration candidate is landed once to `main`; and the claim records exact implementation/integration/main SHAs. External commercial-certificate issuance/configuration and a real signed-artifact run remain evidence gates and are not falsely represented by static repository completion.

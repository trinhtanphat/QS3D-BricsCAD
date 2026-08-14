# Work claim — commercial release signing hardening

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260814-commercial-signing`
- Registered: `2026-08-14T22:40:00+07:00`
- Baseline main SHA: `cf786992754c2d8c7cc0d8a471280a6ac9d539e1`
- Implementation branch: `agent/chatgpt-gpt56sol/commercial-release-signing-hardening`
- Integration batch: `integration/20260814-commercial-release-signing`
- Priority: owner requested Signing / commercial release hardening from the current approximately 80% assessment to production-grade remote-safe completeness.

## Reserved scope

Harden the existing self-hosted V25 manual/commercial release path so every artifact published through that path is fail-closed Authenticode signed, SHA-256 digested and timestamped, exact-signer verified, runtime-qualified where existing policy requires it, package/tag identity bound exactly, and published only after draft-asset byte verification. Add ephemeral PFX import/cleanup support so a persistent private signing key is not required on the runner. Keep the separate cloud preview workflow untouched.

## Expected surfaces

- `.github/workflows/release-v25.yml` — existing manual/commercial workflow: remove the unsigned fallback, require signing, use least privilege with a separate write-scoped publish job, import/clean signing certificate material ephemerally, exact-bind release tag to package product version, preserve signed runtime gate and verify final packaged/downloaded assets before publication.
- `scripts/sign-v25.ps1` — harden PE signing to SHA-256 + RFC3161 (`signtool /fd SHA256 /tr ... /td SHA256`) while preserving supported script signing and fail-closed post-sign verification.
- `scripts/verify-v25-signatures.ps1` — exact signer/timestamp verification and Windows trust verification for PE payloads.
- `scripts/finalize-v25-signed-package.ps1` — existing signed-package finalizer; only minimal compatible hardening if required by the workflow/verification contract.
- `scripts/import-v25-signing-certificate.ps1` — new secret-safe PFX import helper returning one exact imported certificate thumbprint.
- `scripts/preflight-commercial-release-signing.py` — static regression guard for no unsigned commercial fallback, exact version binding, RFC3161 signing, ephemeral certificate cleanup and write-permission isolation.
- `docs/COMMERCIAL-RELEASE-SIGNING.md` — operator runbook for secrets, certificate rotation/revocation, emergency stop and manual artifact verification.
- this claim for coordination and close-out.
- `scripts/package-v25-release.ps1`, `scripts/package-v25.ps1`, the BricsCAD runtime harness and auto-update manifest generator remain read-only dependencies unless a compile/runtime correctness defect proves a minimal adjustment is essential; any scope expansion is registered first.

## Excluded scope

- Do not modify `.github/workflows/release-v25-cloud.yml`; it is reserved by the active CI/package-integrity claim and remains the separate preview path.
- Do not modify `.github/workflows/ci.yml`, `scripts/verify-v25-package.ps1`, `scripts/test-v25-package-verifier.ps1`, `CI_POLICY.md`, or other surfaces reserved by the active CI/package-integrity claim.
- Do not modify preview tag/ordinal derivation, `scripts/prepare-v25-cloud-release.ps1`, release-preview sequence guards or dispatcher behavior.
- Do not modify BricsCAD licensed/native runtime implementation, source features, updater product behavior, proprietary BricsCAD assemblies or unrelated active claims.
- No implementation workflow/script/doc commit directly to `main`; implementation remains on the declared agent branch until integration.

## Validation plan

- PFX import fails closed on missing/invalid base64/password, missing private key/EKU, invalid lifetime, multiple/no matching certificates or configured thumbprint mismatch; raw PFX/password is never emitted and the temporary PFX plus imported certificate are removed in an `always()` cleanup step.
- Commercial workflow is main-only, explicit-confirmation-only, has no `sign_package=false` / unsigned publication path, and checks exact `RELEASE_TAG.Substring(1) == PACKAGE-METADATA.productVersion` before release creation.
- Signing engine uses SHA-256; PE payloads use RFC3161 timestamping via `signtool /tr` + `/td SHA256`; post-sign verification proves exact signer and trusted timestamp.
- Required first-party package executable/script payloads are signed, verified, finalized, then the signed plugin is used by the existing licensed V25 runtime validation path where current release policy requires it.
- Build/sign job has `contents: read`; only a downstream publish job gets `contents: write`. Signed artifacts cross the job boundary through Actions artifacts and are re-verified before draft creation/publication.
- Draft release assets are downloaded and SHA-256 compared with the exact local publish inputs before the draft is made public; no publish occurs after a failed verification.
- Static preflight proves the required step/dependency/order/security tokens remain present and publication cannot depend on an unsigned path.
- Re-read final branch diff against refreshed `main`; use combined integration/current-main CI evidence where available. A real commercial certificate signing run is reported only when repository secrets/certificate/secure runner are configured and an exact workflow run proves the artifact.

## Coordination

The active `chatgpt-web-gpt56sol-ci-package-integrity` lane owns preview package/archive verification and `.github/workflows/release-v25-cloud.yml`; its claim explicitly excludes installer-signing policy. Existing `.github/workflows/release-v25.yml` and signing scripts are therefore the canonical commercial surfaces for this lane. The earlier claim draft proposed parallel `*-commercial` scripts/workflow before those existing surfaces were discovered; this amendment intentionally reuses and hardens the canonical implementation instead of duplicating it. If a concurrent claim begins reserving any expected surface above, this lane stops and reconciles before further implementation writes.

## Completion condition

Repository-side commercial release hardening is integrated when the canonical manual V25 release path has no unsigned publish fallback; exact source/product/tag identity, ephemeral signing material, first-party Authenticode SHA-256 + RFC3161 PE signing, exact signer/timestamp verification, signed-package finalization, least-privilege publication, draft-byte verification and cleanup are all fail-closed; source guard/runbook coverage prevents regression; the integration candidate is landed once to `main`; and the claim records exact implementation/integration/main SHAs. External commercial-certificate issuance/configuration and a real signed-artifact run remain evidence gates and are not falsely represented by static repository completion.

# Work claim — commercial release signing hardening

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260814-commercial-signing`
- Registered: `2026-08-14T22:40:00+07:00`
- Completed: `2026-08-14T23:22:00+07:00`
- Baseline main SHA: `cf786992754c2d8c7cc0d8a471280a6ac9d539e1`
- Implementation branch: `agent/chatgpt-gpt56sol/commercial-release-signing-hardening`
- Implementation commit: `45fb33ce371bc0d24c091fa6e649fcaf4f139895`
- Integration batch: `integration/20260814-commercial-release-signing`
- Integration candidate: `341e99d2239a2825503eaa62bece7a28086d6d48`
- Integration PR: `#1373`
- Main merge SHA: `67854db0bd996f32a7c4c206c390fb2ed74c921f`
- Priority: owner requested Signing / commercial release hardening from the current approximately 80% assessment to production-grade remote-safe completeness.

## Reserved scope

Harden the existing self-hosted V25 manual/commercial release path so every artifact published through that path is fail-closed Authenticode signed, SHA-256 digested and timestamped, exact-signer verified, runtime-qualified where existing policy requires it, package/tag identity bound exactly, and published only after draft-asset byte verification. Add ephemeral PFX import/cleanup support so a persistent private signing key is not required on the runner. Keep the separate cloud preview workflow untouched.

## Implemented

- `.github/workflows/release-v25.yml` is now the signed-only canonical commercial path. The old `sign_package` fallback was removed.
- Commercial build/sign runs in the protected `commercial-release` environment with `contents: read`; only the downstream publish job receives `contents: write`.
- Exact source/product/tag binding is enforced against `PACKAGE-METADATA.json` and `GITHUB_SHA` before publication.
- `scripts/import-v25-signing-certificate.ps1` imports one exact PFX signing identity ephemerally, validates private key/EKU/lifetime/thumbprint, and exposes only public thumbprints for cleanup.
- `scripts/sign-v25.ps1` uses SHA-256 signing and RFC3161 timestamping for PE payloads through SignTool, while retaining supported PowerShell Authenticode signing.
- `scripts/verify-v25-signatures.ps1` rejects wrong signer, invalid/missing timestamp, invalid Authenticode state, and failed SignTool trust verification for PE payloads.
- The workflow finalizes the signed ZIP, removes imported certificate/private-key material with an `always()` cleanup step, then re-verifies the finalized package after private-key cleanup.
- A SHA-256 checksum plus provenance JSON binds release tag, product version, source SHA, signer thumbprint, update manifest digest, and package digest.
- The publish job re-verifies the candidate after the Actions artifact job boundary, creates the GitHub release as a draft, downloads the exact draft assets, compares asset names and SHA-256 bytes, re-verifies the downloaded signed ZIP, and only then clears `draft`.
- `scripts/preflight-commercial-release-signing.py` protects the fail-closed commercial contract against regression.
- `docs/COMMERCIAL-RELEASE-SIGNING.md` documents protected-environment setup, secret handling, certificate rotation/revocation, emergency stop, and manual verification.

## Excluded scope

- `.github/workflows/release-v25-cloud.yml`, `.github/workflows/ci.yml`, preview tag/ordinal derivation, cloud package-integrity surfaces, and dispatcher behavior were not modified by this lane.
- BricsCAD native/runtime implementation, source features, updater product behavior, proprietary BricsCAD assemblies, and unrelated active claims were not modified.
- External CA certificate issuance, production PFX material, GitHub environment reviewers/secrets, trusted timestamp service availability, and a real commercial signed-artifact publication are deployment/evidence gates outside repository source completion.

## Validation and integration evidence

- Claim-only coordination landed on `main` before implementation.
- Implementation was committed as one lane-level atomic commit on the declared agent branch: `45fb33ce371bc0d24c091fa6e649fcaf4f139895`.
- Concurrent `main` work was reconciled without force-push through integration candidate `341e99d2239a2825503eaa62bece7a28086d6d48`, with then-current `main` as primary parent and the implementation commit as second parent.
- PR `#1373` was re-checked as mergeable and merged using exact expected head SHA; GitHub produced main merge SHA `67854db0bd996f32a7c4c206c390fb2ed74c921f`.
- Post-merge read-back confirmed `main` equals `67854db0bd996f32a7c4c206c390fb2ed74c921f` and contains the six intended commercial-signing surfaces.
- The repository automatic post-integration dispatcher started run `31818896438` for exact head SHA `67854db0bd996f32a7c4c206c390fb2ed74c921f`. Its result is tracked as integration CI evidence and must not be rewritten as PASS until the run actually concludes successfully.
- A real commercial signing/publication run is intentionally not claimed: it requires configured protected-environment secrets, a valid production code-signing certificate/private key, trusted timestamp service access, and the licensed Windows/BricsCAD V25 runner.

## Completion condition

Repository-side commercial release hardening is complete: the canonical manual V25 release path has no unsigned publish fallback; exact source/product/tag identity, ephemeral signing material, first-party Authenticode SHA-256 + RFC3161 PE signing, exact signer/timestamp verification, signed-package finalization, least-privilege publication, provenance, draft-byte verification and cleanup are fail-closed; source guard/runbook coverage prevents regression; implementation was integrated through the declared agent/integration branches and PR and landed once to `main`. External commercial-certificate configuration and a real signed-artifact run remain explicit deployment/evidence gates rather than being falsely represented as repository-source PASS evidence.

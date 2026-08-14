# Work claim — CI/package integrity and uploaded preview asset verification

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ci-package-integrity`
- Registered: `2026-08-14T22:11:00+07:00`
- Baseline main SHA: `3216207949b3d4c589c147d2c3a40fb90ba90043`
- Claim commit: `53f0b93bc47bd65af785d1bb2c9decc0235faee0` (parent is the baseline above)
- Implementation branch: `agent/chatgpt-web-gpt56sol-ci-package-integrity/ci-package-integrity-20260814`
- Integration batch: `integration/ci-package-integrity-20260814` or owner-selected batch
- Priority: owner requested CI + packaging + preview release hardening from 80% toward 100% evidence.

## Reserved scope

Harden the existing V25 packaging/release contract without changing public preview tag/ordinal selection. Add a reusable archive verifier, deterministic positive/tamper coverage in regular CI, and release-workflow verification of the exact uploaded draft-release bytes before publication.

## Expected surfaces

- `scripts/verify-v25-package.ps1` — new reusable verifier for ZIP checksum, safe archive paths, required package checksum manifest and per-entry SHA-256 integrity.
- `scripts/test-v25-package-verifier.ps1` — deterministic synthetic positive/tamper/path-safety contract tests.
- `.github/workflows/ci.yml` — run the package-verifier contract test in regular CI.
- `.github/workflows/release-v25-cloud.yml` — verify the locally built package before upload, download the exact draft-release assets, and verify those downloaded bytes before finalizing the prerelease.
- `CI_POLICY.md` only if a small documentation update is required to describe the new remote-safe packaging/release integrity gate.
- this claim for coordination and close-out.

## Explicit exclusions / collision boundaries

- Do not modify `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.
- Do not modify `scripts/prepare-v25-cloud-release.ps1`, `scripts/validate-preview-release-sequence.ps1`, `scripts/preflight-release-preview-sequence.py`, preview-series ordinal/tag derivation, historical `v0.1.0-preview.10014` migration behavior, or any surface reserved by `2026-08-14-2053-gpt56sol-release-preview-sequence-migration.md`.
- Do not weaken or bypass `All discovered feature source guards`; current V25 #192 fails before packaging, and that independent gate remains authoritative.
- Do not enter BricsCAD licensed/native runtime, source-feature, installer-signing policy, or unrelated active claims.
- No implementation source/script/workflow commit directly to `main`; implementation remains on the declared agent branch until integration.

## Validation plan

- Positive synthetic package: external ZIP SHA-256 matches, archive contains one internal `SHA256SUMS.txt`, every listed entry exists exactly once and hashes correctly.
- Negative tests: tampered external checksum, tampered archive payload/internal checksum mismatch, unsafe traversal entry and duplicate/case-colliding archive path fail closed.
- Run PowerShell parser checks for all touched `.ps1` files and YAML/source guards already defined by CI.
- In the release workflow, verify the package before upload; then download the draft release's ZIP/checksum assets to a clean directory and run the same verifier before `gh release edit --draft=false --prerelease`.
- Preserve existing exact-source, installer-pin/signature, naming, sequence and source-guard gates.
- Re-read branch diff against refreshed `main`, publish exact implementation SHA, and hand it to the integration coordinator. End-to-end release success is reported only from a fresh exact-main workflow after final integration.

## Overlap / coordination notes

The active preview-sequence claim owns tag/ordinal derivation and preparation-time sequence validation. This claim deliberately shares only the release workflow file at a disjoint step-level surface: package asset integrity verification after package creation/upload and before release finalization. If that claim later expands into these exact steps, this lane must stop and reconcile before implementation continues.

## Completion condition

The implementation branch contains deterministic package-integrity verification plus CI regression coverage, the V25 release workflow proves both local and uploaded draft-release assets with the same verifier before publication, relevant remote-safe checks are green on the combined candidate, the implementation is represented in the final integrated `main`, and the claim is closed with exact SHAs. A source-guard failure before packaging is reported as a separate blocker and is not bypassed to manufacture release evidence.

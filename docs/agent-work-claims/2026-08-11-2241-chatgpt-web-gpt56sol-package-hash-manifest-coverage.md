# Work claim — package hash manifest coverage

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-package-hash-manifest-coverage`
- Registered: `2026-08-11T22:41:00+07:00`
- Refined: `2026-08-11T22:48:00+07:00`
- Baseline main SHA: `48a23082d5d45b8bd2d135e0695e0d8873ae30c3`
- Priority: owner-requested whole-repository review; close a verified direct-installer integrity fail-open where manifest entries are verified but complete package-file coverage was not required.

## Reserved scope

Make the package hash-manifest contract fail closed at the final installer mutation boundary. `package-v25.ps1` and signed-package finalization already generate `SHA256SUMS.txt` over every regular package file except the manifest itself; `install-v25-autoload.ps1` must enforce the same complete set rather than accepting an unlisted file. Reject duplicate/case-colliding manifest names, require exact manifest-vs-package file coverage using package-relative paths, preserve existing path traversal/hash/signature/version guards, add a static regression preflight, and document the trust chain.

Deeper review established that `update-v25.ps1` already verifies the SHA-256 of the entire downloaded ZIP before extraction and then invokes the downloaded `install-v25-autoload.ps1` before installation. Therefore duplicating the same internal file-set algorithm inside `Assert-PackageRoot` is not required for the update trust boundary; the regression will instead lock the chain: producer hashes all package files, updater verifies the whole ZIP and delegates to the hardened installer, installer requires exact internal manifest coverage.

## Expected surfaces

- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-package-hash-manifest-coverage.py` (new)
- `docs/SECURE-UPDATES.md` or `docs/HEALTH-AND-PREFLIGHT.md` as the minimal canonical documentation surface
- this claim file for close-out

## Reviewed but intentionally unchanged

- `scripts/update-v25.ps1` — keep its existing outer ZIP SHA-256 verification and installer delegation; regression-protect those links rather than duplicating internal manifest-set logic.
- `scripts/package-v25.ps1` / signed-package finalization — producer already emits complete manifests; regression-protect that behavior.

## Excluded scope

- `src/**` updater/UI/product behavior.
- `.github/workflows/**`, GitHub Actions dispatch/re-run or release publication.
- certificate/signing key policy, Authenticode trust semantics, installer registry behavior, package producer contents, release tag/version policy or licensed BricsCAD V25 execution.
- Any active updater, signing, export, UI, quantity, geometry, documentation-feature or Core lane owned by another agent.

## Validation plan

- Re-fetch current `main` and target blob SHAs before each write.
- Inspect the exact installer commit diff to prove only package-integrity logic changed.
- Parse and execute the new Python preflight locally.
- Regression must prove producer full-file hashing, installer actual-vs-manifest set equality, duplicate/case-collision rejection, unlisted-file rejection, updater whole-ZIP hash verification and delegation to the hardened installer.
- Preserve all existing required-payload, hash, signature and path-safety checks.
- Re-read pushed blobs from `main`; no GitHub Actions or V25 runtime qualification.

## Coordination

Recent source/claim searches found no ACTIVE/BLOCKED claim naming `SHA256SUMS`, `install-v25-autoload.ps1` or `update-v25.ps1`; current active neighboring work is elsewhere. This lane remains limited to package hash-manifest integrity and its source/static regression contract.

## Completion condition

The final installer mutation boundary requires exact manifest coverage, the secure-update chain is regression-protected from producer through outer ZIP hash to installer, documentation records the boundary, all changes are pushed to `main`, and this claim is marked `COMPLETED`.
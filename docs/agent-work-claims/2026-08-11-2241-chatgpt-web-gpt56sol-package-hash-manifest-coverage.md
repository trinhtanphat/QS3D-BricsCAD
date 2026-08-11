# Work claim — package hash manifest coverage

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-package-hash-manifest-coverage`
- Registered: `2026-08-11T22:41:00+07:00`
- Baseline main SHA: `48a23082d5d45b8bd2d135e0695e0d8873ae30c3`
- Priority: owner-requested whole-repository review; close a verified integrity fail-open where package consumers verify manifest entries but do not require the manifest to cover every package file.

## Reserved scope

Make the package hash-manifest contract fail closed in the PowerShell installer/updater consumers. `package-v25.ps1` and signed-package finalization already generate `SHA256SUMS.txt` over every regular package file except the manifest itself; consumers must enforce the same complete set rather than accepting an unlisted file. Reject duplicate/case-colliding manifest names, require exact manifest-vs-package file coverage using repository/package relative paths, preserve existing path traversal/hash/signature/version guards, add a static regression preflight, and document the contract.

## Expected surfaces

- `scripts/install-v25-autoload.ps1`
- `scripts/update-v25.ps1`
- `scripts/preflight-package-hash-manifest-coverage.py` (new)
- `docs/SECURE-UPDATES.md` or `docs/HEALTH-AND-PREFLIGHT.md` as the minimal canonical documentation surface
- this claim file for close-out

## Excluded scope

- `src/**` updater/UI/product behavior.
- `.github/workflows/**`, GitHub Actions dispatch/re-run or release publication.
- certificate/signing key policy, Authenticode trust semantics, installer registry behavior, package producer contents, release tag/version policy or licensed BricsCAD V25 execution.
- Any active updater, signing, export, UI, quantity, geometry, documentation-feature or Core lane owned by another agent.

## Validation plan

- Re-fetch current `main` and target blob SHAs before each write.
- Parse the new Python preflight with `ast.parse` and execute it locally against the exact source text when possible.
- Regression must prove both consumers enumerate actual regular package files, exclude only `SHA256SUMS.txt`, normalize relative separators, reject duplicate/case-colliding manifest entries and reject actual files missing from the manifest.
- Preserve all existing required-payload, hash, signature and path-safety checks.
- Re-read pushed blobs from `main`; no GitHub Actions or V25 runtime qualification.

## Coordination

Recent source/claim searches found no ACTIVE/BLOCKED claim naming `SHA256SUMS`, `install-v25-autoload.ps1` or `update-v25.ps1`; current active neighboring work is elsewhere (modeless UI, XLSX, revisions, semantic schedules, BOM/geometry and other feature lanes). This lane is intentionally limited to package hash-manifest coverage in PowerShell consumers.

## Completion condition

Both package consumers require exact manifest coverage, the regression gate protects the contract, documentation records the boundary, all changes are pushed to `main`, and this claim is marked `COMPLETED`.
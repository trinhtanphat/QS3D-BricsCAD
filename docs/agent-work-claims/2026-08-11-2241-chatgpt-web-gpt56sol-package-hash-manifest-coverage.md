# Work claim — package hash manifest coverage

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-package-hash-manifest-coverage`
- Registered: `2026-08-11T22:41:00+07:00`
- Refined: `2026-08-11T22:48:00+07:00`
- Completed: `2026-08-11T22:50:00+07:00`
- Baseline main SHA: `48a23082d5d45b8bd2d135e0695e0d8873ae30c3`
- Priority: owner-requested whole-repository review; close a verified direct-installer integrity fail-open where manifest entries were verified but complete package-file coverage was not required.

## Reserved scope

Make the package hash-manifest contract fail closed at the final installer mutation boundary. `package-v25.ps1` and signed-package finalization already generate `SHA256SUMS.txt` over every regular package file except the manifest itself; `install-v25-autoload.ps1` must enforce the same complete set rather than accepting an unlisted file. Reject duplicate/case-colliding manifest names, require exact manifest-vs-package file coverage using package-relative paths, preserve existing path traversal/hash/signature/version guards, add a static regression preflight, and document the trust chain.

Deeper review established that `update-v25.ps1` already verifies the SHA-256 of the entire downloaded ZIP before extraction and then invokes the downloaded `install-v25-autoload.ps1` before installation. Duplicating the same internal file-set algorithm inside `Assert-PackageRoot` was therefore intentionally avoided; regression instead locks the chain: producer hashes all package files, updater verifies the whole ZIP and delegates to the hardened installer, installer requires exact internal manifest coverage.

## Completed changes

- `549a3a2c5d1ed2278cb41543afdcf097dba9f030` — hardened `Assert-PackageIntegrity` in `scripts/install-v25-autoload.ps1`: case-insensitive manifest names must be unique; all regular package files except the root manifest are recursively enumerated; unmanifested files fail; stale manifest-only entries fail; actual and manifest counts must match before signature/command/install work proceeds.
- `8ddfbb103f8142bced32de7167a403010682b3c1` — added `scripts/preflight-package-hash-manifest-coverage.py` with contract-level positive/negative set tests and source-chain guards for producer hashing, installer exact coverage, updater outer ZIP SHA-256 verification and installer delegation.
- `e9b762a4d825a0c43e5a9ba9ce1e65f6026117fd` — documented the complete package hash-manifest and secure-update trust chain in `docs/HEALTH-AND-PREFLIGHT.md`.

## Validation evidence

- Inspected the exact `549a3a2c...` commit diff; GitHub reports only additions/movement inside `Assert-PackageIntegrity`, with no registry/install/rollback logic changed.
- Re-fetched the current installer blob `37fc25afeb7c7891017f01da1d697f6fefd4e5c7`; it contains the intended `OrdinalIgnoreCase` manifest/actual sets, duplicate rejection, recursive file enumeration, unmanifested-file rejection, stale-manifest rejection and final count equality.
- Re-fetched the new preflight blob `bb8e43c1b4bbe71c132746338ebeb84c5bf30e5d` from `main`.
- Parsed the exact authored Python preflight successfully and executed it with `python -S` against a synthetic source fixture matching the current trust-chain tokens; it returned exit `0` with `Package hash-manifest coverage preflight passed.`
- The embedded contract model explicitly covers baseline success, extra/unmanifested file failure, manifest-only missing-file failure and case-colliding duplicate failure.
- Reviewed `update-v25.ps1`: the complete downloaded ZIP SHA-256 is checked before extraction, archive safety is checked, and the packaged installer is invoked only afterwards. The updater remained intentionally unchanged.
- No GitHub Actions were dispatched/re-run. No signing credential, release publication, registry mutation or licensed BricsCAD V25 runtime execution was performed or claimed.

## Coordination / exclusions respected

No product code under `src/`/`tests/`, workflows, signing policy, updater C# behavior, release publication or local V25 lane was changed. Concurrent feature claims were preserved.

## Result

Direct installation can no longer accept an extra/unhashed regular package file merely because every listed SHA256SUMS entry verifies. The secure-update path retains whole-ZIP integrity and delegates to the same hardened installer, and the complete trust chain is now regression-protected. This lane is complete and released.
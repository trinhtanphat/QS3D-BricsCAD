# Work claim — package root manifest exclusion

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-package-root-manifest-exclusion`
- Registered: `2026-08-11T23:44:00+07:00`
- Completed: `2026-08-11T23:48:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: owner-requested whole-repository review; close a producer/consumer hash-manifest mismatch where package generation excluded every file named `SHA256SUMS.txt` although the installer excludes only the root manifest itself.

## Reserved scope

Make `scripts/package-v25.ps1` hash every regular payload file present before the root manifest is created, including any nested payload whose basename happens to be `SHA256SUMS.txt`. Keep only the root generated manifest self-exclusion semantics used by finalization/installer. Strengthen the package hash-manifest regression accordingly.

## Completed changes

- `9a0aee642f235ef7121598f206b155365ab45a7c` — removed the basename-wide `Where-Object { $_.Name -ne 'SHA256SUMS.txt' }` filter from package hash generation. Because `dist` is recreated and the root manifest is written only after enumeration, every pre-existing regular payload is now hashed without self-hashing the root manifest.
- `580079fc7832138186be362314fc85a7faad50de` — strengthened `scripts/preflight-package-hash-manifest-coverage.py` with a nested `Samples/SHA256SUMS.txt` positive model and source guards requiring all pre-manifest regular payloads to be enumerated while banning the old basename filter.
- `422d890d5278818112b42c5f860193d150700056` — clarified the root-only manifest exclusion contract in `docs/HEALTH-AND-PREFLIGHT.md`.

## Validation evidence

- Inspected exact source commit `9a0aee64...`; GitHub diff is exactly one pipeline change removing the basename-wide filter, with no build/copy/hash algorithm, version, signing or ZIP changes.
- The coverage model treats root `SHA256SUMS.txt` as the manifest while requiring nested `Samples/SHA256SUMS.txt` to appear in the payload manifest.
- The regression explicitly rejects reintroduction of `Where-Object { $_.Name -ne 'SHA256SUMS.txt' }` in `package-v25.ps1`.
- The signed finalizer already uses root-path semantics and the hardened installer excludes only the root manifest, so producer/consumer semantics now align.
- Claim publication encountered several expected concurrency races (`409`/non-fast-forward) while `main` moved rapidly. No force-push was used; unreferenced low-level attempt commits were abandoned and the claim was eventually published through the conflict-safe Contents API before implementation began.
- No GitHub Actions were dispatched/re-run. No package/release was executed and no licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

No package contents, signed finalization, installer algorithm, updater, workflow, `src/**`, `tests/**` or active feature lane was changed. Concurrent work was preserved.

## Result

Unsigned package generation and the hardened installer now agree on root-only `SHA256SUMS.txt` exclusion; nested same-basename files are ordinary hashed payloads. This lane is complete and released.

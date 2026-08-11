# Work claim — package root manifest exclusion

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-package-root-manifest-exclusion`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: owner-requested whole-repository review; close a producer/consumer hash-manifest mismatch where package generation excludes every file named `SHA256SUMS.txt` although the installer excludes only the root manifest itself.

## Reserved scope

Make `scripts/package-v25.ps1` hash every regular payload file present before the root manifest is created, including any nested payload whose basename happens to be `SHA256SUMS.txt`. Keep only the root generated manifest self-exclusion semantics used by finalization/installer. Strengthen the package hash-manifest regression accordingly.

## Expected surfaces

- `scripts/package-v25.ps1`
- `scripts/preflight-package-hash-manifest-coverage.py`
- `docs/HEALTH-AND-PREFLIGHT.md`
- this claim file for close-out

## Excluded scope

- package contents, signed finalization, installer coverage algorithm, updater behavior, signing or release publication.
- `src/**`, `tests/**`, workflows, active feature lanes, Actions dispatch and V25 runtime qualification.

## Validation plan

- Inspect exact package diff; it should only remove basename-wide filtering at manifest generation.
- Regression model/source guard must prove a nested `Samples/SHA256SUMS.txt` remains a hashed payload while only root `SHA256SUMS.txt` is treated as the manifest.
- No Actions dispatch.

## Completion condition

Unsigned package generation and hardened installer agree on root-only manifest exclusion, regression/docs are pushed to `main`, and this claim is marked `COMPLETED`.

# Work claim — update ZIP staging parity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-update-zip-staging-parity`
- Registered: `2026-08-11T22:52:00+07:00`
- Completed: `2026-08-11T22:57:00+07:00`
- Baseline main SHA: `0d6874710daf68b1e8b7d981066e7a4cb56afd97`
- Priority: owner-requested whole-repository review; close a verified release-manifest fail-open where the update ZIP was compared to signed staging for only a subset of files before its whole-archive hash was published.

## Reserved scope

Harden `scripts/new-v25-update-manifest.ps1` so the ZIP blessed into an update manifest must exactly match the staging package for every regular file, not only `PACKAGE-METADATA.json` and signed executable payloads. Require case-insensitively unique ZIP/staging paths, exact file-set equality and SHA-256 equality per staged file before computing/publishing the archive SHA. Preserve Authenticode checks for executable payloads and existing product/version/URI semantics. Add a focused static regression and minimal canonical documentation update.

## Completed changes

- `41620ef0760e80e554d8f3732bb392ca7e33a99e` — `scripts/new-v25-update-manifest.ps1` now recursively indexes all staging regular files and all ZIP file entries with case-insensitive path identity; rejects unsafe, duplicate/case-colliding, extra and missing ZIP files; stream-hashes every ZIP file against staging; requires equal file counts; then retains per-executable Authenticode verification before the archive SHA can be published.
- `ab192bc1f8842879d9dccec70ff8d19c76bf9975` — added `scripts/preflight-update-zip-staging-parity.py` with positive/negative parity models and source-order guards that require full parity before `$zipHash` is written into the update manifest.
- `be910813c21e2192e8f172688dbe2e34b63981d4` — documented full ZIP/staging parity in `docs/HEALTH-AND-PREFLIGHT.md`.

## Validation evidence

- Inspected the exact `41620ef0...` commit diff; changes are confined to the ZIP-vs-staging verifier and the new streaming ZIP-entry SHA-256 helper. Existing product/version/URI/manifest-write semantics were not changed.
- Re-fetched current `main`; `scripts/new-v25-update-manifest.ps1` blob is `2cbaf2a0f494f4d7b413fa7d024d3e094f73599c` and contains the intended case-insensitive staging/ZIP maps, extra/missing/collision guards, per-file streaming hash comparison, equal-count guard and retained Authenticode check.
- Re-fetched `scripts/preflight-update-zip-staging-parity.py` blob `9e6b16f84df32398707a1e2e774bc54b52f7d842`.
- Parsed the regression source successfully and executed it with `python -S` in a synthetic source fixture matching the current contract; it returned exit `0` with `Update ZIP staging parity preflight passed.`
- Embedded negative cases cover an extra ZIP file, missing ZIP file, changed content and case-colliding ZIP path.
- PowerShell is unavailable in this connector environment, so the release script itself was not executed. No GitHub Actions, release publication, signing operation or licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

No `scripts/update-v25.ps1`, installer registry behavior, package producer contents, `src/**`, workflow, signing-policy or V25-runtime lane was changed. Concurrent feature work was preserved while `main` continued moving.

## Result

The update manifest generator can no longer bless a ZIP whose non-executable metadata, command list, hash manifest, samples or any other regular payload differs from signed staging. Full file-set and content parity is now required before the archive SHA is published, and the lane is complete and released.
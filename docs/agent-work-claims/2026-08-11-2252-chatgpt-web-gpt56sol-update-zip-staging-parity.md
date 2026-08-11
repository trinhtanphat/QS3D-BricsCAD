# Work claim — update ZIP staging parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-update-zip-staging-parity`
- Registered: `2026-08-11T22:52:00+07:00`
- Baseline main SHA: `0d6874710daf68b1e8b7d981066e7a4cb56afd97`
- Priority: owner-requested whole-repository review; close a verified release-manifest fail-open where the update ZIP is compared to signed staging for only a subset of files before its whole-archive hash is published.

## Reserved scope

Harden `scripts/new-v25-update-manifest.ps1` so the ZIP blessed into an update manifest must exactly match the staging package for every regular file, not only `PACKAGE-METADATA.json` and signed executable payloads. Require case-insensitively unique ZIP/staging paths, exact file-set equality and SHA-256 equality per staged file before computing/publishing the archive SHA. Preserve Authenticode checks for executable payloads and existing product/version/URI semantics. Add a focused static regression and minimal canonical documentation update.

## Expected surfaces

- `scripts/new-v25-update-manifest.ps1`
- `scripts/preflight-update-zip-staging-parity.py` (new)
- `docs/HEALTH-AND-PREFLIGHT.md` or `docs/SECURE-UPDATES.md`
- this claim file for close-out

## Excluded scope

- `scripts/update-v25.ps1`, installer registry behavior or package producer contents.
- `src/**` updater/UI code.
- `.github/workflows/**`, Actions dispatch/re-run, signing key/certificate policy or release publication.
- licensed BricsCAD V25 runtime qualification and unrelated active feature lanes.

## Validation plan

- Re-fetch current target blob before write and inspect the resulting commit diff.
- Preserve one-to-one signed executable extraction/Authenticode checks after full ZIP parity verification.
- New Python regression must guard exact staging-vs-ZIP file-set/hash parity source contract and include positive/negative set-model cases.
- Execute the Python regression with `python -S` in a synthetic fixture because PowerShell is unavailable in this connector environment.
- No Actions dispatch or V25 runtime claim.

## Coordination

No current claim search matched `new-v25-update-manifest` or ZIP staging parity. Recent updater work is around direct secure-update serialization and is excluded; this lane only owns release-manifest ZIP-vs-staging attestation.

## Completion condition

The update manifest generator refuses stale/extra/missing/different ZIP payloads before publishing the ZIP SHA, regression/docs are pushed to `main`, and this claim is marked `COMPLETED`.
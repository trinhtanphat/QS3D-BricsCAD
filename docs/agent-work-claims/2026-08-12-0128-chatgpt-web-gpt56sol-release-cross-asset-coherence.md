# Work claim — V25 release cross-asset coherence

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release-cross-asset-coherence`
- Registered: `2026-08-12T01:28:00+07:00`
- Priority: owner-requested continue-all review; prevent publication of internally inconsistent local release artifacts even when every uploaded GitHub asset individually matches its local file.

## Verified defect

The release workflow creates the ZIP checksum and, for signed releases, an update manifest containing the ZIP SHA-256 in earlier steps. The later publication block verifies uploaded bytes against local files, but does not immediately re-bind the local `.zip.sha256` contents or signed manifest `sha256` to the current local ZIP. A post-generation mutation of the ZIP on a self-hosted runner could therefore publish three individually uploaded-correct files that disagree with each other.

## Reserved scope

Before creating a GitHub draft, re-hash the current local V25 ZIP, strictly parse the checksum sidecar and require its filename/hash to match, and for signed releases require the update manifest SHA-256 plus package URI to match the same ZIP and exact release asset URL. Extend the V25 release publication regression and runbook.

## Expected surfaces

- `.github/workflows/release-v25.yml`
- `scripts/preflight-release-asset-integrity.py`
- `docs/MANUAL-BUILD-RELEASE.md`
- this claim file for close-out

## Excluded scope

- V26 workflow/lane; actual workflow dispatch/release; tag target and per-asset upload integrity already completed; package/finalizer/manifest generation semantics; updater/installer; `src/**`; `tests/**`; licensed V25 runtime.

## Validation plan

- Strict checksum format: one 64-hex digest + two spaces + exact `QS3D-BricsCAD-V25.zip` filename.
- Current ZIP hash must equal checksum digest immediately before draft creation.
- Signed manifest must parse, expose 64-hex `sha256`, match current ZIP hash, and carry exact expected HTTPS release-download URI for repository/tag/ZIP name.
- Unsigned prerelease path skips only manifest checks, never checksum coherence.
- Regression model covers exact PASS, stale checksum FAIL, malformed/wrong-name checksum FAIL, signed manifest stale hash/wrong URI FAIL.
- Pin local coherence before draft creation, remote tag assertions, asset upload verification and publication.
- No GitHub Actions dispatch/re-run.

## Completion condition

The V25 release cannot create/publish a draft when the local ZIP, checksum sidecar and signed update manifest disagree, with regression/docs on `main` and this claim `COMPLETED`.

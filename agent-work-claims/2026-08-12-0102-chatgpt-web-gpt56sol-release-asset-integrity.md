# Work claim — V25 release asset integrity before publish

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release-asset-integrity`
- Registered: `2026-08-12T01:02:00+07:00`
- Completed: `2026-08-12T01:07:00+07:00`
- Baseline main SHA: `50ac762364be318d65e046eeb09af5b0f5af0581`
- Priority: owner-requested continue-all review; close a manual release publication fail-open where a draft was published after checking only that expected asset names existed.

## Completed changes

- `f84d22f1b8dd391159e1cfb0c9e964873b68ed89` — `.github/workflows/release-v25.yml` now maps exactly the local files uploaded, requires one case-exact remote asset per expected name, compares GitHub-reported asset size to local byte length, re-downloads each asset through its GitHub API asset URL with `application/octet-stream`, compares SHA-256 with the local artifact, cleans the temporary download in `finally`, and only then reaches `draft=false` publication.
- `9e1650ff5ff08508e90b9711a91f4cb73c81aae2` — added auto-discovered `scripts/preflight-release-asset-integrity.py` with exact/missing/duplicate/truncated/equal-size-hash-mismatch policy cases plus source ordering from upload through publish.
- `57b032b615de4d8a92c1ffbe3380dd66457269ea` — documented draft asset byte-integrity verification in `docs/MANUAL-BUILD-RELEASE.md`.

## Validation evidence

- Inspected exact workflow diff for `f84d22f1...`; changes are confined to the draft release asset verification block and local asset mapping. Manual-only trigger, build/runtime/signing/package steps, upload list and draft-first publication flow remain unchanged.
- Re-fetched current `main` workflow blob `3b0daac7ab104b2a6a8281f5b74bff6cf0c00d90`; exact-name uniqueness, size check, octet-stream re-download, SHA-256 equality and temporary cleanup all remain before `$publishBody = @{ draft = $false }`.
- Re-fetched regression blob `634b7ee6e05af73b14f98cf128b0597bcef8ecc0`; it pins upload -> draft read -> unique name -> size -> re-download -> local/remote hash -> publish ordering.
- Executed the deterministic asset policy model: exact uploaded bytes PASS; missing asset FAIL; duplicate name FAIL; truncated size FAIL; equal-size hash mismatch FAIL; missing digest evidence FAIL.
- No GitHub Actions were dispatched/re-run. No draft/release was created, no asset was uploaded to GitHub Releases, and no licensed BricsCAD runtime/signing publication was performed or claimed.

## Coordination / exclusions respected

The concurrent BricsCAD V26 package/release claim remains isolated to V26-specific surfaces and requires V25 behavior not be weakened. No V26 file, package/finalizer/manifest byte semantics, updater/installer, `src/**`, `tests/**` or active product lane was modified. Writes were SHA-guarded and no force-push was used.

## Result

A V25 GitHub draft can no longer become public merely because expected asset names exist: each published release asset must match the locally qualified artifact by exact name, byte length and re-downloaded SHA-256 before the workflow can request `draft=false`. This lane is complete.

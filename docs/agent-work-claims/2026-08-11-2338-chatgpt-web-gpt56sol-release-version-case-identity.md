# Work claim — release version case identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release-version-case-identity`
- Registered: `2026-08-11T23:38:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: owner-requested whole-repository review; close a verified identity mismatch where release code says product/tag versions must match exactly but compares them case-insensitively.

## Reserved scope

Make exact product-version and `v<productVersion>` identity comparisons ordinal/case-sensitive at the release package boundary and in the customer-release regression. SemVer prerelease identity must not silently treat `preview` and `PREVIEW` as the same release string. Preserve strict SemVer validation, version ordering semantics, assembly-version checks and existing workflow order.

## Expected surfaces

- `scripts/package-v25.ps1`
- `scripts/preflight-customer-release.py`
- `docs/HEALTH-AND-PREFLIGHT.md`
- this claim file for close-out

## Reviewed but not necessarily changed

- `.github/workflows/release-v25.yml`
- `.github/workflows/release-v25-cloud.yml`

Both workflows route publication through aggregate preflight and `package-v25.ps1`; workflow-local duplicate comparisons need not be the semantic authority if the shared boundary is strict and regression-protected.

## Excluded scope

- updater SemVer precedence/comparison logic, build metadata ordering, release numbering policy or changing current project version.
- `src/**`, `tests/**`, active product lanes, signing/package payload logic, workflow dispatch/re-run and licensed V25 runtime qualification.

## Validation plan

- Re-fetch target blobs and inspect exact diffs.
- Regression must reject case-only differences between plugin/Core product version and between `RELEASE_TAG` and `v<productVersion>` while preserving valid exact matches.
- Execute Python model/source regression where possible; no Actions dispatch.

## Completion condition

The shared release package boundary enforces true exact case-sensitive identity, regression/docs are on `main`, and this claim is marked `COMPLETED`.

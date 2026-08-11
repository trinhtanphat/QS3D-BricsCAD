# Work claim — strict release SemVer contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-strict-release-semver`
- Registered: `2026-08-11T23:22:00+07:00`
- Baseline main SHA: `5a85de7b43922eb250b35c84fd33d3159e3adf2c`
- Priority: owner-requested whole-repository review; close a verified release-policy gap where the manual release entrypoints accept SemVer-shaped but semantically invalid product versions/tags.

## Reserved scope

Make the source/package release version boundary enforce strict SemVer 2.0 semantics rather than shape-only matching. Core numeric identifiers must not have leading zeroes; prerelease/build dot identifiers must be non-empty; numeric prerelease identifiers must not have leading zeroes. Preserve the existing exact `v<productVersion>` binding and stable/prerelease policy. Strengthen the existing customer-release preflight with deterministic positive/negative SemVer cases and source guards.

## Expected surfaces

- `scripts/package-v25.ps1`
- `.github/workflows/release-v25.yml`
- `.github/workflows/release-v25-cloud.yml`
- `scripts/preflight-customer-release.py`
- `docs/HEALTH-AND-PREFLIGHT.md`
- this claim file for close-out

## Excluded scope

- updater SemVer ordering/manifest semantics already handled by completed updater lanes.
- signing credentials, package payload/signature algorithms, installer/updater runtime behavior, GitHub release publication mechanics beyond tag validation.
- `src/**`, `tests/**`, active quantity/material/documentation/browser/geometry/UI lanes.
- GitHub Actions dispatch/re-run and licensed BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch exact target blobs before each write and inspect resulting commit diffs.
- Static regression must accept strict examples such as `1.2.3`, `1.2.3-rc.1`, `1.2.3-rc.1+build.4` and reject leading-zero core/prerelease values, empty identifiers and malformed separators.
- Preserve workflow `workflow_dispatch` and `confirm_release == 'RELEASE'` guards; do not dispatch Actions.
- Execute Python preflight/regression locally with `python -S` against exact authored source when possible.

## Coordination

Recent current-main commit/claim review shows active work in quantity, material catalog, semantic sheets and other product lanes. Historical updater/product-SemVer work is completed and does not reserve these release entrypoint surfaces. No current claim was found for strict release-tag SemVer validation.

## Completion condition

Both release entrypoints and package generation fail closed on invalid SemVer, exact tag/source binding remains intact, regression/docs are updated on `main`, and this claim is marked `COMPLETED`.

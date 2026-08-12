# Work claim — strict release SemVer contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-strict-release-semver`
- Registered: `2026-08-11T23:22:00+07:00`
- Completed: `2026-08-11T23:28:00+07:00`
- Baseline main SHA: `5a85de7b43922eb250b35c84fd33d3159e3adf2c`
- Priority: owner-requested whole-repository review; close a verified release-policy gap where the manual release entrypoints accepted SemVer-shaped but semantically invalid product versions/tags until the package boundary.

## Reserved scope

Make the source/package release version boundary enforce strict SemVer 2.0 semantics rather than shape-only matching. Core numeric identifiers must not have leading zeroes; prerelease/build dot identifiers must be non-empty; numeric prerelease identifiers must not have leading zeroes. Preserve the existing exact `v<productVersion>` binding and stable/prerelease policy. Strengthen the existing customer-release preflight with deterministic positive/negative SemVer cases and source guards.

## Completed changes

- `3ece5ad57cfff5b2490d0da3de92ac2738d864a4` — `scripts/package-v25.ps1` now validates both plugin/Core source product versions with a strict SemVer parser before distribution output is created, preserves exact plugin/Core equality and the existing exact `RELEASE_TAG == v<productVersion>` binding.
- `d9d09ffdebbd8dce9f6b295ff203a1ce5bd32ead` — expanded `scripts/preflight-customer-release.py` with strict SemVer positive/negative regressions, current project-version validation, package parser wiring checks, and release-workflow ordering checks.
- `f449ec5b561fe9536d18c4e2577ffa4fd71ddd71` — corrected the new workflow publication guard to recognize both stable `Publish GitHub Release` and cloud `Publish GitHub prerelease` labels semantically.
- `72da56a7ed9e907b14ddb0a0ac5f4d6c0d272c7f` — documented the strict SemVer semantic authority in `docs/HEALTH-AND-PREFLIGHT.md`.

## Reviewed but intentionally unchanged

- `.github/workflows/release-v25.yml` and `.github/workflows/release-v25-cloud.yml` retain their early tag-shape checks. Both workflows already execute aggregate preflight and then `package-v25.ps1` before publication, so the newly hardened package/preflight boundary is the semantic authority and invalid SemVer cannot reach publication.

## Validation evidence

- Inspected exact commit `3ece5ad5...`; its diff is confined to adding `Convert-ToStrictSemVerText` and routing the two project version reads through it. Existing build/copy/hash/signature/package logic was not changed.
- Current plugin/Core `<Version>` values are both `0.1.0-preview.2`, which passes the strict SemVer model.
- Regression valid cases: `0.0.0`, `1.2.3`, `1.2.3-rc.1`, `1.2.3-rc.1+build.4`, `1.2.3+001`.
- Regression invalid cases include leading-zero core versions, numeric prerelease leading zeroes, empty prerelease/build identifiers, doubled separators, and an unexpected leading `v`.
- Parsed the exact authored Python regression successfully and executed it with `python -S` in a synthetic repository fixture covering both release workflows; exit `0`, output `PASS`.
- No GitHub Actions were dispatched/re-run. No package was published or signed and no licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

No product code under `src/**` or `tests/**`, updater SemVer ordering, signing policy, runtime behavior, active feature lane, or workflow dispatch policy was changed. Concurrent work on `main` was preserved with SHA-guarded Contents API writes and no force-push.

## Result

Release packaging now fails closed on semantically invalid source product versions before dist creation, exact tag/source identity remains enforced, and both manual release paths are regression-protected to execute that strict boundary before GitHub publication. This lane is complete and released.

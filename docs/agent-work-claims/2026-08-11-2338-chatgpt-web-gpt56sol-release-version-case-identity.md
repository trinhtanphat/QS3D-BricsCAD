# Work claim — release version case identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release-version-case-identity`
- Registered: `2026-08-11T23:38:00+07:00`
- Completed: `2026-08-11T23:42:00+07:00`
- Baseline main SHA: `0ab55e0e96e0a386bc76f5f8aedb432bf81fd43a`
- Priority: owner-requested whole-repository review; close a verified identity mismatch where release code said product/tag versions must match exactly but compared them case-insensitively.

## Reserved scope

Make exact product-version and `v<productVersion>` identity comparisons ordinal/case-sensitive at the release package boundary and in the customer-release regression. SemVer prerelease identity must not silently treat `preview` and `PREVIEW` as the same release string. Preserve strict SemVer validation, version ordering semantics, assembly-version checks and existing workflow order.

## Completed changes

- `6a6ae20bd8907fa7e58d0ec63d18bf1e99ef0bbe` — changed only the plugin/Core product-version comparison and `RELEASE_TAG` comparison in `scripts/package-v25.ps1` from `OrdinalIgnoreCase` to `Ordinal`.
- `64cbbb7d823cfd9bfa1733652d30160179048e28` — strengthened `scripts/preflight-customer-release.py` with exact-case identity model cases, exact current project-version comparison, required ordinal package tokens and a regression ban on `OrdinalIgnoreCase` in the shared package boundary.
- `6dac8ae0fab07708d90a6ad577afe90fba51a5e6` — documented ordinal/case-sensitive release identity in `docs/HEALTH-AND-PREFLIGHT.md`.

## Validation evidence

- Inspected exact source commit `6a6ae20b...`; GitHub diff contains exactly two comparator changes and no packaging/build/hash/signature changes.
- Regression model accepts identical `1.2.3-preview.2` product/Core/tag identity and rejects Core `1.2.3-PREVIEW.2` or tag `v1.2.3-PREVIEW.2` when source is lowercase.
- Current plugin/Core versions remain the same exact string and strict SemVer; no source project version was changed.
- Existing release workflows remain ordered through aggregate preflight and `package-v25.ps1` before publication, so the shared ordinal package boundary cannot be bypassed by their redundant case-insensitive display/validation checks.
- One regression write encountered a transient `409` during concurrent `main` movement; the file was re-fetched and the update retried without force or overwriting other work.
- No GitHub Actions were dispatched/re-run. No release was published and no licensed BricsCAD V25 runtime qualification was performed or claimed.

## Coordination / exclusions respected

No updater SemVer ordering, `src/**`, `tests/**`, workflow behavior, signing/package payload semantics or active feature lane was changed. Concurrent work was preserved with SHA-guarded writes and no force-push.

## Result

The repository's documented “exact” source/Core/tag release identity is now actually exact and case-sensitive at the mandatory package boundary. This lane is complete and released.

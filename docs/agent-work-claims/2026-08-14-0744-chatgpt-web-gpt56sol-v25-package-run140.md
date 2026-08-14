# Work claim — V25 preview package run #140

- Status: `ACTIVE / DIRTY_TREE_SAFETY_FOLLOWUP`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T07:44:00+07:00`
- Baseline main SHA: `ebf41473af72970d7911b3b7ad5e3b9297b604ff`
- Source fix: `dddfd34e0fd190abf347ec3c59a4818e80450ebb` (`fix(release): align source version with preview.6`)
- Safety follow-up baseline: `60806a43aa04408b17e1793e1c6eddd06bc268d6`
- Automation commits:
  - `9a668d33ba9cc74d1511390fd1dfffa6e595a9c7` — fail-closed preview source identity synchronizer
  - `ba6a966d569d2294091b307cd7b8f130851e73a8` — exact release-commit preparation/push helper
  - `ee6fdb8e14e7dae58be5d6fdf0a882fa3115205f` — V25 cloud workflow automatic source sync + exact publish provenance
  - `49ddf3bdcd03197e200ff45acb2ef86deb318c82` — focused static regression guard
- Priority: fresh V25 workflow run #140 passed deterministic Core smoke and plugin build, then failed at `Build V25 preview package`; the automation safety audit found a dirty-index provenance gap before fresh automation acceptance.

## Reserved scope

Triage and fix only the V25 preview packaging/release-identity automation lane. Preserve release/version binding and do not weaken packaging validation. The safety follow-up is limited to making release preparation fail closed on pre-existing staged/untracked/dirty paths and to pinning that contract in the focused static guard.

## Verified safety gap

`prepare-v25-cloud-release.ps1` validates post-sync paths with `git diff --name-only`, which only observes unstaged tracked changes. It does not first prove that the checkout/index is clean and therefore can miss a pre-existing staged or untracked path. A pre-existing staged path could then be included by the later `git commit` even though it is outside the three product-version project files, violating the automation contract that unexpected changed/staged paths fail closed.

## Expected surfaces

- `scripts/prepare-v25-cloud-release.ps1` — require a pristine checkout/index before synchronization; inspect full porcelain status after synchronization; permit only unstaged modifications to the three product `.csproj` files; require the staged set to exactly equal the validated changed set before commit
- `scripts/preflight-v25-preview-release-sync.py` — guard clean-checkout and full-status checks plus exact staged-set validation
- `.github/workflows/release-v25-cloud.yml` — read-only unless the helper contract proves workflow wiring must change
- `scripts/sync-preview-release-version.ps1` — read-only
- this claim file

## Automation contract

- `release_tag` is the single owner-provided release-version input.
- Release preparation starts from the exact dispatched SHA and a pristine working tree/index with no untracked files.
- If source identity already matches the requested preview, no version commit is created.
- If it differs, only the three aligned product `.csproj` files may become dirty, and only those validated changes may become staged.
- Pre-existing or unexpected staged, unstaged, renamed, deleted, added, or untracked paths fail closed before a release-preparation commit can be created.
- Remote `main` must still equal the dispatched SHA before any non-force push.
- Package metadata and GitHub prerelease provenance remain bound to exact `RELEASE_COMMIT_SHA`.
- Manual-only trigger policy remains unchanged.

## Existing validation evidence

- Run #140 isolated the preview.6 tag/source mismatch after earlier Core/V25 build gates passed.
- Source identity correction was proven by run #141 (`31758733099`) end-to-end `SUCCESS`; `v0.1.0-preview.6` was published with ZIP + SHA256 assets.
- Run #141 predates the automatic preparation commits and is not automation acceptance evidence.
- No new GitHub Actions run is authorized by this `continue all / fix / commit` request.

## Excluded scope

- Core health/source-handle/QSC code and smoke tests
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated feature preflights, V26 packaging behavior, updater/product UX, MAP/selection claims
- weakening package/version checks or force-pushing/rebasing over concurrent `main` work

## Validation plan

- Refresh `main` again after this claim-only reservation.
- Harden helper and focused preflight only if no concurrent agent has already fixed the dirty-tree gap.
- Read back both pushed files at the resulting exact SHA.
- Keep GitHub Actions undispatched; fresh automation CI remains pending explicit owner authorization.

## Completion condition

The dirty-tree/index provenance gap is fixed and guarded on current `main`; the lane then returns to `SOURCE_FIXED / AUTOMATION_HARDENED / PENDING_FRESH_CI` until a separately authorized fresh workflow run proves the automatic preparation path.

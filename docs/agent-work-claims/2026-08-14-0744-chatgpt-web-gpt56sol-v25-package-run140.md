# Work claim — V25 preview package run #140

- Status: `SOURCE_FIXED / AUTOMATION_HARDENED / PENDING_FRESH_CI`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T07:44:00+07:00`
- Baseline main SHA: `ebf41473af72970d7911b3b7ad5e3b9297b604ff`
- Source fix: `dddfd34e0fd190abf347ec3c59a4818e80450ebb` (`fix(release): align source version with preview.6`)
- Automation commits:
  - `9a668d33ba9cc74d1511390fd1dfffa6e595a9c7` — fail-closed preview source identity synchronizer
  - `ba6a966d569d2294091b307cd7b8f130851e73a8` — exact release-commit preparation/push helper
  - `ee6fdb8e14e7dae58be5d6fdf0a882fa3115205f` — V25 cloud workflow automatic source sync + exact publish provenance
  - `49ddf3bdcd03197e200ff45acb2ef86deb318c82` — focused static regression guard
  - `c7e5356829a50fb1fe3706f90c355dcd0720c84b` — dirty-index/worktree fail-closed hardening
  - `ec8671d8095865d6edddc758eaf431b3a04b64ea` — regression guard for dirty-tree/staged provenance
- Priority: V25 preview release identity and exact-source provenance.

## Reserved scope

Triage and fix only the V25 preview packaging/release-identity automation lane. Preserve release/version binding and do not weaken packaging validation. Exact licensed BricsCAD runtime work and unrelated feature lanes remain excluded.

## Source/version status

Run #140 (`31757912057`, job `94637682238`) failed only at preview packaging because `RELEASE_TAG=v0.1.0-preview.6` did not match source `0.1.0-preview.5`. Commit `dddfd34e0fd190abf347ec3c59a4818e80450ebb` advanced V25/V26/Core product identities together to `0.1.0-preview.6` while preserving stable `AssemblyVersion` and the strict package source/tag guard.

Fresh run #141 (`31758733099`, job `94640258685`) on `e8b2625310c9772934f8f2e8e85f022c175da087` completed `SUCCESS`; package/release-tag binding, ZIP/checksum assets and `v0.1.0-preview.6` publication all succeeded. This proves the source version correction, not the later automatic preparation implementation.

## Automation contract

- `release_tag` remains the single owner-provided preview-version input.
- The workflow prepares one exact release source commit and records `RELEASE_COMMIT_SHA`.
- Only the three aligned product `.csproj` files may be modified/staged by automatic version preparation.
- Release preparation rejects any pre-existing staged or tracked dirty path and any unexpected untracked path. The workflow's known Actions cache under `.nuget/packages/` is the sole pre-existing untracked exception and is never staged by the helper.
- After synchronization, only unstaged modifications to the explicit three-project allowlist are accepted; renamed/deleted/added/staged/unexpected statuses fail closed.
- After explicit staging, the staged set must exactly equal the validated changed set, with no residual unstaged/unexpected path; the helper checks the cached diff again before commit.
- After commit, no dirty/staged path may remain other than the known untracked NuGet cache.
- `origin/main` must still equal the dispatched SHA before the non-force preparation push, and the pushed commit must read back exactly.
- Package metadata `gitCommit`, local `HEAD`, and GitHub prerelease `target_commitish` remain bound to exact `RELEASE_COMMIT_SHA`, never stale `GITHUB_SHA` after an automatic version commit.
- Manual-only trigger policy remains unchanged.

## Dirty-tree safety follow-up

A remote audit found that the original preparation helper used `git diff --name-only`, which could not see a pre-existing staged path. That could allow an unrelated already-staged file to be included by the later release-preparation commit despite the three-project allowlist.

`c7e5356829a50fb1fe3706f90c355dcd0720c84b` fixes that gap by using full porcelain status boundaries before synchronization, after synchronization, after staging and after commit. It also compares the staged set bidirectionally against the validated changed set and runs `git diff --cached --check` before commit. The only untracked exception is `.nuget/packages/`, because the workflow restores that repository-local Actions cache before release preparation; it remains untracked and is never included by the explicit `git add -- @allowed`.

`ec8671d8095865d6edddc758eaf431b3a04b64ea` extends `scripts/preflight-v25-preview-release-sync.py` to pin the clean-index/full-status ordering, exact staged-set checks, cached-diff check, post-commit cleanliness and non-force push contract. Exact-file readback confirms both commits are present, and GitHub compare confirms `ec8671d...` is an ancestor of current `main` after unrelated concurrent commits.

## Validation status

- Source version correction: proven by run #141 end-to-end `SUCCESS`.
- Automatic source-sync/provenance implementation: pushed to `main`.
- Dirty-index/worktree safety hardening and focused static guard: pushed and read back from exact SHA.
- No PowerShell runtime is available in this remote execution environment, so the preparation helper was not executed locally and no PowerShell/runtime PASS is fabricated.
- A direct container checkout could not be obtained because this execution container has no outbound DNS; this does not alter GitHub source readback evidence.
- No new GitHub Actions run was dispatched because the current owner request authorizes source fix/update/commit/push, not Actions execution.
- Fresh automation acceptance remains required on a workflow run whose `head_sha` contains the automation and dirty-tree hardening commits. Rerunning #140/#141 is not sufficient because reruns use their original source/workflow SHA.

## Excluded scope

- Core health/source-handle/QSC/Rebar and other concurrent source lanes
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated V26 packaging behavior, updater/product UX, MAP/selection claims
- weakening package/version checks or force-pushing/rebasing over concurrent `main` work

## Completion condition

A separately owner-authorized fresh V25 cloud workflow run containing the hardened automation passes the release gates. For a future tag whose source identity differs from checked-out source, acceptance additionally requires evidence that the workflow creates and publishes from the exact automatic release-preparation commit without manual `.csproj` edits.

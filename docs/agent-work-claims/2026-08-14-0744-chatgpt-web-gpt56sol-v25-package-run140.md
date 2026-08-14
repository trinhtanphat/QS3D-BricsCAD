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
- Priority: fresh V25 workflow run #140 passed deterministic Core smoke and plugin build, then failed at `Build V25 preview package`.

## Reserved scope

Triage and fix only the fresh CAD-independent packaging/release-identity failure from V25 workflow run #140 (`31757912057`, job `94637682238`) after all earlier Core/build gates passed. Preserve release/version binding and do not weaken packaging validation to force green status. Harden the manual preview workflow so future `release_tag` inputs synchronize the aligned source identity automatically without hand-editing project files, while preserving exact-commit provenance and fail-closed concurrency behavior.

Exact failure evidence: the workflow was dispatched with `RELEASE_TAG=v0.1.0-preview.6`, while the checked-out V25/V26/Core product identity remained `0.1.0-preview.5`; `scripts/package-v25.ps1` correctly rejected that mismatch. The already-published `v0.1.0-preview.5` release meant the justified source-side correction was to advance the aligned product identity to preview.6, not to weaken the package guard or dispatch preview.5 again.

## Source fix proven

Commit `dddfd34e0fd190abf347ec3c59a4818e80450ebb` advanced all runtime-product identities together:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`: `Version`/`InformationalVersion` = `0.1.0-preview.6`, `FileVersion` = `0.1.0.6`
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`: same aligned identity
- `src/QS3D.Core/QS3D.Core.csproj`: same aligned identity
- `AssemblyVersion` remains stable at `0.1.0.0`
- `scripts/package-v25.ps1` strict `RELEASE_TAG == v + source Version` guard is unchanged

Fresh workflow run #141 (`31758733099`, job `94640258685`) on `e8b2625310c9772934f8f2e8e85f022c175da087`, a descendant containing the source fix, completed `SUCCESS`. Core guards/build/smoke, V25 reference acquisition/validation, V25 plugin build, package build, release-tag/package binding, checksum, artifact upload, and GitHub prerelease publish all passed. `v0.1.0-preview.6` is published against `e8b2625310c9772934f8f2e8e85f022c175da087` with the V25 ZIP and checksum assets.

## Expected surfaces

- GitHub Actions run #140 failure evidence and #141 source-fix success evidence
- `.github/workflows/release-v25-cloud.yml` — automate source identity synchronization, exact release-commit provenance, and safe push behavior while remaining `workflow_dispatch` only
- `scripts/sync-preview-release-version.ps1` — fail-closed helper validating one `vX.Y.Z-preview.N` input and synchronizing V25/V26/Core `Version`, `FileVersion`, and `InformationalVersion`
- `scripts/prepare-v25-cloud-release.ps1` — validates exact dispatch SHA, runs identity preflight, restricts changed paths, verifies `origin/main` has not moved, creates one release-preparation commit only when needed, pushes without force, and read-backs the pushed commit
- `scripts/preflight-v25-preview-release-sync.py` — focused static regression guard for helper wiring, fail-closed remote-main checks, release commit provenance, and no stale `GITHUB_SHA` publication target
- `scripts/package-v25.ps1` — preserve its exact source/tag version guard and package Git commit provenance
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` — aligned preview product identity
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` — aligned preview product identity required by runtime-version guard
- `src/QS3D.Core/QS3D.Core.csproj` — aligned preview product identity required by package/runtime-version guards
- `scripts/preflight-runtime-product-version-identity.py` — preserve V25/V26/Core identity equality
- this claim file and issue/handoff status tied only to the package/release-identity lane

## Automation contract

- `release_tag` is the single owner-provided release-version input.
- The workflow validates an exact `vX.Y.Z-preview.N` tag and existing-tag collision before any source mutation.
- If source identity already matches the requested preview, no version commit is created.
- If it differs, the workflow synchronizes all three projects atomically, validates runtime-product identity, verifies remote `main` still equals the dispatched SHA, creates one release-preparation commit, and pushes it non-force.
- Unexpected changed/staged paths fail closed; only the three product project files may be changed by automatic version preparation.
- The workflow records the resulting exact `RELEASE_COMMIT_SHA`; package metadata `gitCommit` and local Git `HEAD` must match it.
- GitHub prerelease `target_commitish` uses `RELEASE_COMMIT_SHA`, never stale `GITHUB_SHA` after an automatic version commit.
- If remote `main` moves concurrently, preparation aborts instead of rebasing/force-pushing concurrent work into an owner-approved release.
- Manual-only trigger policy remains unchanged; no `push:` trigger is added.

## Validation status

- run #140: expected failure isolated to preview.6 tag vs preview.5 source mismatch after all earlier Core/V25 build gates passed
- source identity correction: proven by run #141 end-to-end `SUCCESS`
- `v0.1.0-preview.6`: published successfully with ZIP + SHA256 assets
- package strict source/tag binding: preserved and proven in #141
- auto-sync helper + exact-commit helper + workflow wiring + focused regression guard: pushed to `main`
- automation itself: requires one fresh workflow run whose `head_sha` contains `ee6fdb8e14e7dae58be5d6fdf0a882fa3115205f` and `49ddf3bdcd03197e200ff45acb2ef86deb318c82`; #141 predates those commits and is not automation acceptance evidence
- rerunning #140/#141 is not automation acceptance because reruns use their original source/workflow SHA

## Excluded scope

- Core health/source-handle code and smoke tests from completed #1092 lane
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated feature preflights, V26 packaging behavior, updater/product UX, or existing MAP/selection claims
- weakening package/version checks or force-pushing/rebasing over concurrent `main` work

## Completion condition

A fresh V25 cloud workflow run containing the hardened automation commits passes the release gates. For a future tag whose source identity differs (for example preview.7 while source still says preview.6), acceptance additionally requires evidence that the workflow creates and publishes from the exact automatic release-preparation commit without manual `.csproj` edits.

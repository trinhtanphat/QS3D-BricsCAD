# Work claim — V25 preview package run #140

- Status: `ACTIVE / AUTOMATION_HARDENING`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-14T07:44:00+07:00`
- Baseline main SHA: `ebf41473af72970d7911b3b7ad5e3b9297b604ff`
- Source fix: `dddfd34e0fd190abf347ec3c59a4818e80450ebb` (`fix(release): align source version with preview.6`)
- Priority: fresh V25 workflow run #140 passed deterministic Core smoke and plugin build, then failed at `Build V25 preview package`.

## Reserved scope

Triage and fix only the fresh CAD-independent packaging/release-identity failure from V25 workflow run #140 (`31757912057`, job `94637682238`) after all earlier Core/build gates passed. Preserve release/version binding and do not weaken packaging validation to force green status. Harden the manual preview workflow so future `release_tag` inputs can synchronize the aligned source identity automatically without hand-editing project files, while preserving exact-commit provenance and fail-closed concurrency behavior.

Exact failure evidence: the workflow was dispatched with `RELEASE_TAG=v0.1.0-preview.6`, while the checked-out V25/V26/Core product identity remained `0.1.0-preview.5`; `scripts/package-v25.ps1` correctly rejected that mismatch. The already-published `v0.1.0-preview.5` release means the justified source-side correction was to advance the aligned product identity to preview.6, not to weaken the package guard or dispatch preview.5 again.

## Source fix landed

Commit `dddfd34e0fd190abf347ec3c59a4818e80450ebb` advances all runtime-product identities together:

- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj`: `Version`/`InformationalVersion` = `0.1.0-preview.6`, `FileVersion` = `0.1.0.6`
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`: same aligned identity
- `src/QS3D.Core/QS3D.Core.csproj`: same aligned identity
- `AssemblyVersion` remains stable at `0.1.0.0`
- `scripts/package-v25.ps1` strict `RELEASE_TAG == v + source Version` guard is unchanged

## Expected surfaces

- GitHub Actions run #140 packaging-step evidence
- `.github/workflows/release-v25-cloud.yml` — automate source identity synchronization, exact release-commit provenance, and safe push behavior while remaining `workflow_dispatch` only
- `scripts/sync-preview-release-version.ps1` — new fail-closed helper that validates one `vX.Y.Z-preview.N` input and synchronizes V25/V26/Core `Version`, `FileVersion`, and `InformationalVersion`
- `scripts/package-v25.ps1` — read/diagnose only; preserve its exact source/tag version guard
- `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` — aligned preview product identity
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` — aligned preview product identity required by runtime-version guard
- `src/QS3D.Core/QS3D.Core.csproj` — aligned preview product identity required by package/runtime-version guards
- `scripts/preflight-runtime-product-version-identity.py` — read/validate only; preserve V25/V26/Core identity equality
- this claim file and issue/handoff status tied only to the package failure

## Automation contract

- `release_tag` remains the single owner-provided release-version input.
- The workflow validates the tag and existing-tag collision before any source mutation.
- If source identity already matches the requested preview, no version commit is created.
- If it differs, the workflow synchronizes all three projects atomically, validates the runtime-product identity guard, verifies remote `main` has not moved since checkout, creates one release-preparation commit, and pushes it with no force.
- The workflow records the resulting exact `RELEASE_COMMIT_SHA`; package provenance uses Git `HEAD`, and GitHub prerelease `target_commitish` must use this release commit rather than stale `GITHUB_SHA`.
- If remote `main` moves concurrently, the workflow fails closed and requires a fresh dispatch rather than overwriting another agent.
- Manual-only trigger policy remains unchanged; no `push:` trigger is added.

## Excluded scope

- Core health/source-handle code and smoke tests from completed #1092 lane
- LOCAL_ONLY BricsCAD native qualification lanes
- unrelated feature preflights, V26 packaging behavior, updater/product UX, or existing MAP/selection claims
- weakening package/version checks or force-pushing over concurrent `main` work

## Validation status

- run #140 evidence: Core guards/build/smoke and V25 plugin build passed before packaging
- source/package identity mismatch: fixed in `dddfd34e0fd190abf347ec3c59a4818e80450ebb`
- package strict guard: preserved
- automation hardening: in progress
- fresh exact-SHA V25 workflow run with `release_tag=v0.1.0-preview.6`: still required before claiming end-to-end success
- do not rerun #140 for acceptance because GitHub reruns use its original SHA `337e97d32b6642b3a3d013596ffa1545df168999`

## Coordination

Recheck current `main` and exact-path claims immediately before every further write. Do not stack another source-version patch unless fresh CI proves a distinct defect.

## Completion condition

A fresh V25 workflow run using the hardened automation and containing the release identity fix (or a descendant with equivalent aligned identity) passes the package/release gates, after which this claim may become `COMPLETED`.

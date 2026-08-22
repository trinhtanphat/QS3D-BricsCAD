# Manual BricsCAD V25 build and release runbook

Updated 2026-08-10.

## Policy

QS3D does **not** use automatic GitHub CI/CD.

Every workflow in `.github/workflows/` must remain `workflow_dispatch` only. Executable jobs are additionally hard-guarded to `github.event_name == 'workflow_dispatch'`. Commits, pushes, pull requests, merges, documentation updates, source fixes, reviews and `continue all` requests must leave GitHub Actions idle.

A workflow may be dispatched only after the repository owner explicitly requests the run. See `CI_POLICY.md`.

## Available manual workflows

- `ci.yml` — Core/static validation.
- `bricscad-v25.yml` — V25 integration build/runtime evidence.
- `curved-opening.yml` — focused curved-opening validation.
- `geometry-extensions.yml` — focused geometry-extension validation.
- `project-data-gate.yml` — focused Zone/Floor/Family/Material/Project Tools/project-assignment-integrity validation.
- `release-v25.yml` — complete owner-approved V25 build/package/GitHub Release flow.

## Preferred release workflow

Use `.github/workflows/release-v25.yml` only after the owner explicitly requests a release.

Required inputs:

- `release_tag` — semantic-style tag such as `v0.1.0` or `v0.1.0-rc.1`;
- `confirm_release` — must be exactly `RELEASE` or the release job is skipped;
- `run_runtime` — normally `true` for a release candidate/final release when the licensed interactive V25 runner is available;
- `prerelease` — `true` for RC/beta builds, otherwise `false`.

The workflow deliberately has no `push`, `pull_request`, `schedule`, `workflow_run`, `workflow_call`, `repository_dispatch`, release-event or deployment-event trigger. Its release job also checks `github.event_name == 'workflow_dispatch'` before execution.

## What the manual release workflow does

1. checks out the explicitly selected commit;
2. validates the release tag and manual CI policy;
3. runs generic and auto-discovered feature preflights, including project-assignment integrity;
4. compiles `QS3D.Core` in Release;
5. runs deterministic Core smoke tests;
6. verifies `BRICSCAD_V25_DIR` and required licensed V25 runtime files on the self-hosted runner;
7. compiles `QS3D.BricsCAD.V25` Release/x64 against the installed V25 assemblies;
8. optionally performs real V25 NETLOAD/runtime validation and captures evidence;
9. runs `scripts/package-v25.ps1` against the x64 Release/net48 output;
10. creates `dist/QS3D-BricsCAD-V25.zip` plus `dist/QS3D-BricsCAD-V25.zip.sha256`;
11. uploads the build/runtime artifacts to the workflow run;
12. publishes a GitHub Release and attaches the ZIP/checksum only after all preceding required steps succeed.

## Required runner

The release workflow uses these labels:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

The machine must have a licensed BricsCAD V25 installation and repository variables configured for:

- `BRICSCAD_V25_DIR`
- `BRICSCAD_V25_PROFILE` when runtime validation uses a dedicated profile.

Do not commit `BrxMgd.dll`, `TD_Mgd.dll`, private DWGs, BLT binaries/source, certificates or other proprietary/private fixtures into the repository.

## Release safety rules

- Never publish from an ambiguous moving head. Resolve the exact commit/tag first.
- Never dispatch a release because a source commit landed; owner approval is a separate action.
- Never mark the release runtime-verified unless the V25 runtime step actually completed successfully.
- Never silently skip a failed preflight/build/runtime step to force a release.
- Never replace an existing release tag from this workflow.
- Keep `confirm_release=RELEASE` as an explicit publication gate.
- Keep `scripts/preflight-ci-manual-only.py` in the aggregate gate so automatic CI/CD triggers cannot be introduced unnoticed.

## When the owner says “build” but not “release”

Use `bricscad-v25.yml` or the appropriate focused manual workflow. Do not publish a GitHub Release unless the request explicitly asks for a release/publish action.

## When the owner says “build and release”

Use the exact requested commit/tag, run `release-v25.yml` manually, keep runtime validation enabled when the licensed V25 runner is available, and report separately:

- source/preflight result;
- Core build/smoke result;
- V25 adapter build result;
- runtime/NETLOAD result;
- package SHA-256;
- GitHub Release tag and attached artifact names.

Source implementation progress and local/static checks remain distinct from release/runtime proof.

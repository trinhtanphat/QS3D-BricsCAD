# CI strategy

## Repository policy

The repository-wide source of truth is `CI_POLICY.md` at the project root. Multi-agent coordination rules are in `AGENTS.md`.

GitHub Actions on `main` are owner-controlled and manual-only:

- every workflow must remain `workflow_dispatch` only;
- every executable workflow job is additionally guarded by `github.event_name == 'workflow_dispatch'`;
- documentation-only, `*.md`, `docs/**`, `docs:` and `chore:` changes do not need GitHub CI;
- commits, pushes, merges, reviews, refactors, fixes, handoffs, or `continue all` instructions do not authorize an Actions run by themselves;
- even source-code changes do not automatically authorize CI;
- dispatch or re-run GitHub Actions only when the repository owner explicitly requests a CI/build/test/runtime/release run;
- preparing/editing a workflow is not permission to execute it.

Do not add `push`, `pull_request`, `pull_request_target`, `schedule`, `workflow_run`, `workflow_call`, `repository_dispatch`, release-event, deployment-event or any other automatic/event-driven trigger unless the owner explicitly changes this policy.

`scripts/preflight-ci-manual-only.py` enforces this rule across every `.yml`/`.yaml` workflow and requires the manual-event guard on every executable job. It is auto-discovered by `scripts/preflight-all.py`.

Because multiple agents may commit concurrently, sync the latest `main` before making changes and again immediately before shared-file writes. Never overwrite newer concurrent work.

## Manual workflows

### Hosted Core/static CI

`.github/workflows/ci.yml`

- manual dispatch only;
- Windows hosted runner;
- generic + feature preflight;
- compile `QS3D.Core`;
- run package-free deterministic smoke tests.

No BricsCAD installation is required.

### V25 integration build/runtime

`.github/workflows/bricscad-v25.yml`

- manual dispatch only;
- never dispatch automatically after commit/push/merge;
- compiles the V25 adapter against a licensed self-hosted BricsCAD V25 installation;
- can run NETLOAD/runtime/screenshot evidence when explicitly requested;
- runtime/artifact paths use the x64 Release output `bin/x64/Release/net48`.

### Focused source gates

- `.github/workflows/curved-opening.yml` — curved-opening source/Core validation.
- `.github/workflows/geometry-extensions.yml` — geometry-extension source/Core validation.
- `.github/workflows/project-data-gate.yml` — Zone/Floor/Family/Material/Project Tools/project-assignment-integrity validation plus Core build/smoke.
- `.github/workflows/schedule-gate.yml` — Schedule Hub, Material usage, Door/Opening schedule, Room Finish schedule/UI and Core build/smoke validation.

Focused gates remain manual-only and also run the strict manual-CI policy preflight.

### Manual build + GitHub Release

`.github/workflows/release-v25.yml`

- manual `workflow_dispatch` only;
- hard-guarded to the manual event;
- requires a `release_tag` and explicit `confirm_release=RELEASE`;
- runs source/preflight/Core/V25 build gates;
- optionally runs real V25 NETLOAD/runtime validation;
- packages from `src/QS3D.BricsCAD.V25/bin/x64/Release/net48`;
- creates `QS3D-BricsCAD-V25.zip` and its SHA-256 checksum;
- publishes a GitHub Release only after all required preceding steps succeed.

The package command manifest is generated from current `[CommandMethod]` source declarations rather than a hand-maintained command list. The package excludes BricsCAD-owned runtime assemblies.

This workflow is an owner-triggered release tool, **not continuous deployment**. Do not run it until the owner explicitly asks for a release.

See `docs/MANUAL-BUILD-RELEASE.md` for the operator runbook.

## V25 runner

Runner labels:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

Repository variable: `BRICSCAD_V25_DIR`.

Example: `C:\Program Files\Bricsys\BricsCAD V25 en_US`.

Optional runtime profile variable: `BRICSCAD_V25_PROFILE`.

The runner must have a valid licensed BricsCAD V25 installation and an interactive Windows session for runtime/screenshot validation.

## Static/local review versus CI

Static review and repository-local validation may be performed without starting GitHub Actions. Do not describe those checks as GitHub CI or BricsCAD runtime verification. A CI/runtime result is claimed only after the corresponding explicitly requested workflow actually completes.

## Owner-approved release gate

When the owner explicitly asks to build and release:

1. resolve the exact commit/tag;
2. dispatch `release-v25.yml` manually with `confirm_release=RELEASE`;
3. run strict manual-policy + aggregate source preflights;
4. build Core Release;
5. run deterministic Core smoke tests;
6. compile the V25 adapter Release/x64;
7. run scripted BricsCAD runtime/NETLOAD validation when requested and available;
8. package + SHA-256;
9. publish GitHub Release.

`QS3DRELEASECHECK` is a project/DWG health tool and should be run on representative project data during release qualification; it is not treated as a meaningful blank-DWG replacement for private-DWG runtime regression.

Do not upload BricsCAD-owned DLLs as source-controlled artifacts and do not publish a release merely because a commit/tag was pushed.

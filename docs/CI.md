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

`scripts/preflight-ci-manual-only.py` enforces this rule across every `.yml`/`.yaml` workflow and is auto-discovered by `scripts/preflight-all.py`.

Because multiple agents may commit concurrently, sync the latest `main` before making changes and again immediately before committing/pushing. Never overwrite newer concurrent work.

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
- runtime/artifact paths use the actual x64 Release output `bin/x64/Release/net48`.

### Focused source gates

- `.github/workflows/curved-opening.yml` — manual curved-opening source/Core validation.
- `.github/workflows/geometry-extensions.yml` — manual geometry-extension source/Core validation.
- `.github/workflows/project-data-gate.yml` — manual Zone/Floor/Family/Material/Project Tools/project-assignment-integrity validation plus Core build/smoke tests.

### Manual build + GitHub Release

`.github/workflows/release-v25.yml`

- manual `workflow_dispatch` only;
- hard-guarded to the manual event;
- requires a `release_tag` and explicit `confirm_release=RELEASE`;
- runs source/preflight/Core/V25 build gates;
- optionally runs real V25 NETLOAD/runtime validation;
- creates `QS3D-BricsCAD-V25.zip` and its SHA-256 checksum;
- uploads workflow artifacts;
- publishes a GitHub Release only after all required preceding steps succeed.

This workflow is an owner-triggered release tool, **not continuous deployment**. Do not run it until the owner explicitly asks for a release.

See `docs/MANUAL-BUILD-RELEASE.md` for the operator runbook.

## V25 runner

Runner labels:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

Repository variable:

`BRICSCAD_V25_DIR`

Example value:

`C:\Program Files\Bricsys\BricsCAD V25 en_US`

Optional profile variable:

`BRICSCAD_V25_PROFILE`

The runner must have a valid licensed BricsCAD V25 installation.

Agents with real local-machine/BricsCAD access should prioritize runtime-only validation. Remote/hybrid agents should handle repository/source/docs/static work that does not require that local environment.

## Static/local review versus CI

Static review and repository-local validation may be performed without starting GitHub Actions. Do not describe those checks as GitHub CI or BricsCAD runtime verification. A CI/runtime result is claimed only after the corresponding explicitly requested workflow actually completes.

## Owner-approved release gate

When the owner explicitly asks to build and release:

1. resolve the exact commit/tag;
2. dispatch `release-v25.yml` manually with `confirm_release=RELEASE`;
3. preflight including strict manual-CI policy gate;
4. Core Release build;
5. deterministic Core smoke tests;
6. V25 adapter compile;
7. scripted BricsCAD runtime/NETLOAD validation when enabled;
8. package + SHA-256;
9. publish GitHub Release.

Do not upload BricsCAD-owned DLLs as source-controlled artifacts and do not publish a release by merely pushing a commit/tag.

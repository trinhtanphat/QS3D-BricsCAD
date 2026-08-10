# CI strategy

## Repository policy

The repository-wide source of truth is `CI_POLICY.md` at the project root. Multi-agent coordination rules are in `AGENTS.md`.

GitHub Actions on `main` are owner-controlled and manual-only:

- workflows must remain `workflow_dispatch` only;
- documentation-only, `*.md`, `docs/**`, `docs:` and `chore:` changes do not need GitHub CI;
- commits, pushes, merges, reviews, refactors, fixes, or "continue all" instructions do not authorize an Actions run by themselves;
- even source-code changes do not automatically authorize CI;
- dispatch or re-run GitHub Actions only when the repository owner explicitly requests a CI/build/test run.

Do not add `push`, `pull_request`, `schedule`, `workflow_run`, or other automatic triggers unless the owner explicitly changes this policy.

Because multiple agents may commit concurrently, sync the latest `main` before making changes and again immediately before committing/pushing. Never overwrite newer concurrent work.

## Why two workflows

The core engine must compile and test without BricsCAD.
The V25 plugin needs the proprietary assemblies shipped with a licensed BricsCAD V25 installation.

## Hosted CI

`.github/workflows/ci.yml`

- manual dispatch only
- Windows hosted runner
- preflight
- compile `QS3D.Core`
- run package-free smoke tests

No BricsCAD installation is required.

## V25 integration CI

`.github/workflows/bricscad-v25.yml`

- manual dispatch only
- never dispatch automatically after commit/push/merge

Runner labels:

- `self-hosted`
- `windows`
- `x64`
- `bricscad-v25`

Repository variable:

`BRICSCAD_V25_DIR`

Example value:

`C:\Program Files\Bricsys\BricsCAD V25 en_US`

The runner must have a valid BricsCAD V25 installation.

Agents with real local-machine/BricsCAD access should prioritize runtime-only validation. Remote/hybrid agents should handle repository/source/docs/static work that does not require that local environment.

## Static/local review versus CI

Static review and repository-local validation may be performed without starting GitHub Actions. Do not describe those checks as GitHub CI or BricsCAD runtime verification. A CI/runtime result is claimed only after the corresponding explicitly requested workflow actually completes.

## Release gate later

1. preflight
2. core build
3. core smoke tests
4. V25 adapter compile
5. scripted BricsCAD load smoke test
6. manual DWG regression set
7. package/sign
8. release

Do not upload BricsCAD DLLs as source-controlled artifacts.

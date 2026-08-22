# GitHub Actions / CI Policy

This file is the repository-level source of truth for when GitHub Actions may run.

## Owner-controlled CI/CD only

GitHub Actions are **manual-only** on `main`.

- Every workflow under `.github/workflows/` must use `workflow_dispatch` as its **only** trigger.
- Every executable job must hard-guard `github.event_name == 'workflow_dispatch'`.
- Do **not** add `push`, `pull_request`, `pull_request_target`, `schedule`, `workflow_run`, `workflow_call`, `repository_dispatch`, release-event, deployment-event, or any other automatic/event-driven trigger unless the repository owner explicitly asks to change this policy.
- Do **not** automatically run, re-run, or dispatch CI/CD after a commit, push, merge, review, refactor, fix, documentation update, handoff, or `continue all` request.
- A GitHub Actions run is allowed only when the repository owner explicitly requests a CI/build/test/runtime/release run.
- Merely preparing or editing a workflow does **not** authorize dispatching it.

The intended operating mode is: keep developing and committing normally with Actions idle; when the owner explicitly requests a build/release, dispatch the requested manual workflow for the chosen commit.

## Agent execution roles and CI authorization

The repository owner expects the normal/default agent pool to concentrate on **finding and fixing bugs, updating source code, adding deterministic regressions/static guards, reviewing diffs, and committing/pushing coherent code changes**. Those agents must keep GitHub Actions idle unless the owner separately designates them for CI/runtime/release execution.

- A normal `continue all`, `fix bug`, `update code`, `commit`, `push`, `merge`, review, or handoff assignment means **source/code work only**; it is not permission to dispatch, re-run, cancel, or otherwise operate GitHub Actions.
- The owner may explicitly designate one or more specific agents to operate GitHub CI/Actions, build, runtime, packaging, release, or related workflow tasks. Only those owner-designated agents may perform the CI operations covered by that designation.
- CI authorization is **agent- and scope-specific**. Permission granted to one designated agent does not automatically transfer to other concurrent agents, and permission for one workflow/task does not authorize unrelated workflows or releases.
- If an agent cannot establish that it is the owner-designated CI agent for the requested operation, it must behave as a normal coding agent: continue source-safe work and leave Actions undispatched.
- A designated CI agent must still follow every manual-only, exact-SHA, runner, release-confirmation, and safety requirement in this file. Designation does not permit automatic triggers or bypass repository guards.
- Coding agents and CI-designated agents may work concurrently. Coding agents should not stop bug-fix/source work merely because another explicitly designated agent is handling CI.

This role split is intentional: most agents maximize progress by fixing/updating code, while a smaller owner-selected set may spend CI minutes or operate specialized runners when explicitly assigned.

## Changes that do not need GitHub CI

The following changes do not require a GitHub Actions run and must not trigger one automatically:

- documentation-only changes;
- `*.md`, `README*`, and `docs/**` changes;
- `docs:` commits;
- `chore:` / housekeeping commits;
- comments, formatting, metadata, planning, research notes, and non-runtime documentation assets;
- CI-policy/workflow/documentation edits;
- normal source commits, fixes, reviews, refactors, and multi-agent integration work unless the owner separately requests validation.

Even source-code changes do **not** imply permission to run GitHub Actions. Source changes remain manual-CI until the owner explicitly requests a run.

## What counts as explicit approval

Examples of explicit approval:

- "run GitHub CI"
- "run Actions"
- "run the Core CI"
- "run the BricsCAD V25 workflow"
- "run the BricsCAD V26 workflow"
- "build/test this commit on GitHub Actions"
- "build bản release"
- "build và release app"
- "release bản này"

The following are **not** approval to run CI/CD by themselves:

- "review all"
- "fix/update code"
- "commit/push main"
- "merge"
- "continue all"
- "update README/docs"

When wording is ambiguous, do not spend Actions minutes and do not publish a release; leave Actions undispatched.

## Manual workflows

Current workflows are expected to remain manual-only. This inventory is not exhaustive; `scripts/preflight-ci-manual-only.py` scans every workflow file regardless of whether it is listed here.

Core and host integration:

- `.github/workflows/ci.yml` — Core/static validation on a hosted Windows runner.
- `.github/workflows/bricscad-v25.yml` — V25 build/runtime integration on the licensed self-hosted runner.
- `.github/workflows/bricscad-v26.yml` — V26 .NET 8 build/runtime integration on the licensed self-hosted runner.

Representative focused gates include:

- `.github/workflows/curved-opening.yml`;
- `.github/workflows/geometry-extensions.yml`;
- `.github/workflows/project-data-gate.yml`;
- `.github/workflows/schedule-gate.yml`.

Release tools:

- `.github/workflows/release-v25.yml` — owner-approved V25 **build + package + GitHub Release** flow.
- `.github/workflows/release-v25-cloud.yml` — owner-approved V25 cloud release helper where applicable.
- `.github/workflows/release-v26.yml` — owner-approved V26 **build + package + signed update manifest + GitHub Release** flow.

Every focused workflow must run `scripts/preflight-ci-manual-only.py` as part of its source gate so policy drift is detected inside an explicitly requested run as well.

All release workflows are manual release tools, not continuous-deployment pipelines. Publishing requires an explicit `workflow_dispatch` plus `confirm_release=RELEASE`. They must not be dispatched until the owner requests the release.

## Manual build/release sequence

When the owner explicitly requests a release, the preferred sequence is:

1. resolve the exact `main` commit/tag to release;
2. choose the requested host-major workflow (`release-v25.yml` or `release-v26.yml`);
3. dispatch it manually with an explicit release tag and `confirm_release=RELEASE`;
4. run repository preflights and deterministic Core smoke tests;
5. compile the matching host adapter against the licensed BricsCAD installation;
6. run the required host-major runtime/signing gates for the requested release type;
7. package only the matching host-major ZIP/checksum/update-manifest assets;
8. publish the GitHub Release only after the workflow's release-integrity checks succeed.

See `docs/MANUAL-BUILD-RELEASE.md` for V25 and `docs/MANUAL-BUILD-RELEASE-V26.md` for V26 operator details.

## Multi-agent repository rule

This repository may be changed by multiple agents at the same time. Before editing shared files and again immediately before creating/pushing a commit, refresh/sync the latest `main` and inspect what changed. Never assume the branch head is still the same as when the task started.

Do not overwrite, revert, squash away, or silently replace another agent's newer work. Rebase/reapply/merge the intended patch onto the latest `main` when necessary. Prefer small, focused commits so concurrent work can be reconciled safely.

Detailed agent coordination rules live in `AGENTS.md` at the repository root.

## BricsCAD host workflows

V25:

- `.github/workflows/bricscad-v25.yml` and `.github/workflows/release-v25.yml` require a licensed Windows x64 self-hosted runner labeled `bricscad-v25` with `BRICSCAD_V25_DIR`.
- Managed adapter target: `net48`.

V26:

- `.github/workflows/bricscad-v26.yml` and `.github/workflows/release-v26.yml` require a licensed Windows x64 self-hosted runner labeled `bricscad-v26` with `BRICSCAD_V26_DIR`.
- `bricscad.exe` must identify major 26 and the runner requires .NET 8 Windows Desktop support.
- Managed adapter target: `net8.0-windows`.

These workflows must never be dispatched automatically. Runtime/NETLOAD/UI validation and release publication run only after an explicit owner request and when the required runner is available.

## Local/static validation

Repository-local or static checks may be used during review without starting GitHub Actions. Passing static review is not the same as a successful GitHub CI or licensed BricsCAD runtime test; do not claim CI/runtime verification unless that run actually completed.

V25 and V26 runtime proof are independent. Source sharing between host adapters does not allow a V25 runtime PASS to be reported as V26 evidence or vice versa.

## Enforcement

- `scripts/preflight.py` retains the manual-CI trigger guard and private/reference artifact policy.
- `scripts/preflight-ci-manual-only.py` is the strict policy gate: every workflow must expose `workflow_dispatch` only, **every executable job** must hard-guard the manual event, and release workflows must retain explicit `RELEASE` confirmation.
- `scripts/preflight-all.py` auto-discovers the strict CI policy gate along with the other feature preflights.

Keep these guards in place unless the repository owner explicitly changes this policy.

Related documentation: `AGENTS.md`, `README.md`, `docs/CI.md`, `docs/CI-READINESS.md`, `docs/MANUAL-BUILD-RELEASE.md`, `docs/MANUAL-BUILD-RELEASE-V26.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-V26-QUALIFICATION.md`.

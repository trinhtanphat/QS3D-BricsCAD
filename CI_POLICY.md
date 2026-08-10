# GitHub Actions / CI Policy

This file is the repository-level source of truth for when GitHub Actions may run.

## Owner-controlled CI/CD only

GitHub Actions are **manual-only** on `main`.

- Every workflow under `.github/workflows/` must use `workflow_dispatch` as its **only** trigger.
- Do **not** add `push`, `pull_request`, `pull_request_target`, `schedule`, `workflow_run`, `workflow_call`, `repository_dispatch`, release-event, deployment-event, or any other automatic/event-driven trigger unless the repository owner explicitly asks to change this policy.
- Do **not** automatically run, re-run, or dispatch CI/CD after a commit, push, merge, review, refactor, fix, documentation update, handoff, or `continue all` request.
- A GitHub Actions run is allowed only when the repository owner explicitly requests a CI/build/test/runtime/release run.
- Merely preparing or editing a workflow does **not** authorize dispatching it.

The intended operating mode is: keep developing and committing normally with Actions idle; when the owner explicitly requests a build/release, dispatch the requested manual workflow for the chosen commit.

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

Current workflows are expected to remain manual-only:

- `.github/workflows/ci.yml` — Core/static validation on a hosted Windows runner.
- `.github/workflows/bricscad-v25.yml` — V25 build/runtime integration on the licensed self-hosted runner.
- `.github/workflows/curved-opening.yml` — focused curved-opening gate.
- `.github/workflows/geometry-extensions.yml` — focused geometry-extension gate.
- `.github/workflows/project-data-gate.yml` — focused Zone/Floor/Family/Material/Project Tools/integrity gate.
- `.github/workflows/release-v25.yml` — owner-approved **build + package + GitHub Release** workflow.

`release-v25.yml` is not a continuous-deployment pipeline. It is a manual release tool. Publishing requires an explicit `workflow_dispatch` plus the `RELEASE` confirmation input. It must not be dispatched until the owner requests the release.

## Manual build/release sequence

When the owner explicitly requests a release, the preferred sequence is:

1. resolve the exact `main` commit/tag to release;
2. dispatch the manual V25 release workflow with an explicit release tag and `confirm_release=RELEASE`;
3. run repository preflights and deterministic Core smoke tests;
4. compile the BricsCAD V25 adapter against the licensed V25 installation;
5. optionally run real NETLOAD/runtime validation and collect evidence;
6. package `QS3D-BricsCAD-V25.zip` and its SHA-256 checksum;
7. publish the GitHub Release only after the preceding steps succeed.

See `docs/MANUAL-BUILD-RELEASE.md` for operator details.

## Multi-agent repository rule

This repository may be changed by multiple agents at the same time. Before editing shared files and again immediately before creating/pushing a commit, refresh/sync the latest `main` and inspect what changed. Never assume the branch head is still the same as when the task started.

Do not overwrite, revert, squash away, or silently replace another agent's newer work. Rebase/reapply/merge the intended patch onto the latest `main` when necessary. Prefer small, focused commits so concurrent work can be reconciled safely.

Detailed agent coordination rules live in `AGENTS.md` at the repository root.

## BricsCAD V25 workflow

`.github/workflows/bricscad-v25.yml` and `.github/workflows/release-v25.yml` require a licensed Windows x64 self-hosted runner with BricsCAD V25. They must never be dispatched automatically. Runtime/NETLOAD/screenshot validation and release publication run only after an explicit owner request and when the required runner is available.

## Local/static validation

Repository-local or static checks may be used during review without starting GitHub Actions. Passing static review is not the same as a successful GitHub CI or BricsCAD V25 runtime test; do not claim CI/runtime verification unless that run actually completed.

## Enforcement

- `scripts/preflight.py` retains the original manual-CI trigger guard.
- `scripts/preflight-ci-manual-only.py` is the strict policy gate: every workflow must expose `workflow_dispatch` only, every executable workflow must hard-guard the manual event, and any other trigger is rejected.
- `scripts/preflight-all.py` auto-discovers the strict CI policy gate along with the other feature preflights.

Keep these guards in place unless the repository owner explicitly changes this policy.

Related documentation: `AGENTS.md`, `README.md`, `docs/CI.md`, `docs/CI-READINESS.md`, `docs/MANUAL-BUILD-RELEASE.md`, `docs/V25-RUNNER.md`.

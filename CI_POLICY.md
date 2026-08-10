# GitHub Actions / CI Policy

This file is the repository-level source of truth for when GitHub Actions may run.

## Owner-controlled CI only

GitHub Actions are **manual-only** on `main`.

- Workflows on `main` must use `workflow_dispatch` only.
- Do **not** add `push`, `pull_request`, `schedule`, `workflow_run`, or other automatic triggers unless the repository owner explicitly asks for that change.
- Do **not** automatically run, re-run, or dispatch CI after a commit, push, merge, review, refactor, fix, or documentation update.
- A GitHub Actions run is allowed only when the repository owner explicitly requests a CI/build/test run.

## Changes that do not need GitHub CI

The following changes do not require a GitHub Actions run and must not trigger one automatically:

- documentation-only changes;
- `*.md`, `README*`, and `docs/**` changes;
- `docs:` commits;
- `chore:` / housekeeping commits;
- comments, formatting, metadata, planning, research notes, and non-runtime documentation assets;
- CI-policy/documentation edits such as this file.

Even source-code changes do **not** imply permission to run GitHub Actions. Source changes remain manual-CI until the owner explicitly requests a run.

## What counts as explicit approval

Examples of explicit approval:

- "run GitHub CI"
- "run Actions"
- "run the Core CI"
- "run the BricsCAD V25 workflow"
- "build/test this commit on GitHub Actions"

The following are **not** approval to run CI by themselves:

- "review all"
- "fix/update code"
- "commit/push main"
- "merge"
- "continue all"

When wording is ambiguous, do not spend Actions minutes; leave CI undispatched.

## Multi-agent repository rule

This repository may be changed by multiple agents at the same time. Before editing shared files and again immediately before creating/pushing a commit, refresh/sync the latest `main` and inspect what changed. Never assume the branch head is still the same as when the task started.

Do not overwrite, revert, squash away, or silently replace another agent's newer work. Rebase/reapply the intended patch onto the latest `main` when necessary. Prefer small, focused commits so concurrent work can be reconciled safely.

Detailed agent coordination rules live in `AGENTS.md` at the repository root.

## BricsCAD V25 workflow

`.github/workflows/bricscad-v25.yml` requires a licensed Windows x64 self-hosted runner with BricsCAD V25. It must never be dispatched automatically. Runtime/NETLOAD/screenshot validation is run only after an explicit owner request and when the required runner is available.

## Local/static validation

Repository-local or static checks may be used during review without starting GitHub Actions. Passing static review is not the same as a successful GitHub CI or BricsCAD V25 runtime test; do not claim CI/runtime verification unless that run actually completed.

## Enforcement

`scripts/preflight.py` protects the release tree by requiring manual-only workflows and rejecting automatic `push` / `pull_request` triggers. Keep that guard in place unless the repository owner explicitly changes this policy.

Related documentation: `AGENTS.md`, `docs/CI.md`, `docs/CI-READINESS.md`, `docs/V25-RUNNER.md`.

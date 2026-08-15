# GitHub Actions / CI Policy

This file is the repository-level source of truth for GitHub Actions behavior. `docs/MAIN-WRITE-AUTHORIZATION.md` remains authoritative for who may change `main`.

## Main remains read-only for normal agents

Normal AI agents/chat sessions must not push, update refs, or merge to `main`. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs and chores all use a dedicated task branch/PR unless the owner explicitly authorizes a named integration/merge operation.

CI authorization does not grant merge authorization, and merge authorization does not grant unrelated release authorization.

## Mandatory per-agent task CI

Every normal task branch must receive remote validation for its **exact head SHA** before the agent may report the task completed.

The canonical validation workflow is:

```text
.github/workflows/ci.yml
```

It runs automatically on:

- pushes to `agent/**`;
- pushes to `recovery/**`;
- pushes to `integration/**`;
- pull requests targeting `main`;
- pushes to `main` for exact-main validation;
- explicit `workflow_dispatch` for operator recovery.

Ten independent agents with ten different task branches therefore receive ten independent workflow runs. They share the same workflow definition and GitHub runner pool, but each run is isolated by repository/ref/event and exact commit SHA. Concurrency cancellation is scoped by source branch so superseded commits do not waste validation capacity.

### Task completion gate

A normal agent **must not report the task completed, mark its reservation `COMPLETED`, or stop as completed** until all of the following are true:

1. all intended task changes are pushed to the task branch;
2. the branch/PR head SHA is recorded;
3. the automatic `QS3D Task / Core CI` run for that exact head SHA has reached `success`;
4. no newer task-branch commit exists without equivalent green evidence;
5. if the task requires an environment-gated native/runtime check, that gate is either successful for the exact required SHA or the task remains `BLOCKED`/handed off rather than falsely completed.

A green run for an older SHA, another agent branch, another PR, or `main` does not satisfy this gate.

If CI fails, the same agent/task remains active: diagnose the failing exact run, fix the real defect on the task branch, push again, and wait for the replacement exact-SHA CI result. Do not weaken tests or policy checks merely to obtain green status.

## Issues, branches and pull requests

A GitHub Issue is a reservation/coordination surface; it has no source tree and therefore does not have a meaningful build by itself. The Issue must reference the task branch and PR. CI evidence belongs to the branch/PR commit SHA.

Branch and PR CI use the same validation workflow instead of separate implementations so their build/test contract cannot drift. A PR event validates the PR head SHA explicitly rather than silently accepting a stale merge-base or unrelated run.

## What task CI proves

Automatic task CI is repository-safe validation. It includes policy/source guards, Core build, deterministic smoke tests, and release-script/package contract checks that are safe on GitHub-hosted runners.

It does **not** by itself prove licensed BricsCAD `NETLOAD`, interactive/native UI behavior, private-DWG behavior, signing credentials, or other `LOCAL_ONLY` evidence classes. V25 and V26 runtime proof remain independent. When those are required by a task, the agent may not convert remote CI success into native/runtime PASS.

## Main-only release

`release-v25-cloud.yml` is a **main-only release workflow**, not an agent validation workflow.

It remains `workflow_dispatch`-only, has `contents: write`, requires explicit `confirm_release=RELEASE`, validates that the dispatch ref is `refs/heads/main`, and requires the requested source SHA to be reachable from current `main`.

Do not run `release-v25-cloud.yml` for `agent/**`, `recovery/**`, feature branches, Issues, or ordinary PRs. Doing so would mix task validation with tag/release publication and defeat branch isolation.

The sole automatic release exception is:

```text
.github/workflows/dispatch-v25-cloud-after-main-integration.yml
```

That dispatcher may react to integration-relevant pushes to `main` and dispatch `release-v25-cloud.yml` from `main` using the exact triggering source SHA. Documentation-only landings outside its watched paths remain excluded.

## Other build/runtime/release workflows

All workflows other than `ci.yml` and the approved post-main dispatcher remain owner-controlled `workflow_dispatch` lanes unless the owner explicitly changes this policy.

This includes BricsCAD V25/V26 self-hosted/native qualification and release workflows. They are capability-specific gates, not the default per-agent CI mechanism.

## Integration branches

For multi-agent work, an owner-authorized coordinator should assemble the named batch on:

```text
integration/<batch-id>
```

The integration branch receives the same automatic validation. The coordinator must not land a red or untested integration head to `main`. After an authorized merge, the resulting exact `main` SHA receives task/core CI again; integration-relevant main changes may additionally start the approved V25 cloud release dispatcher.

## Definition of CI_GREEN

For a branch, PR, integration candidate, or `main`, `CI_GREEN` means the required automatic validation workflow completed with `success` for the exact current SHA being claimed. `queued`, `in_progress`, `cancelled`, `skipped`, `neutral`, `timed_out`, `action_required`, or success for another SHA is not `CI_GREEN`.

## Definition of ALL MERGED TO MAIN

Report **ALL MERGED TO MAIN** only after an authorized integration reviewer freshly verifies that every required lane is represented in current `main`, no required work remains only off-main, the exact final `main` SHA is recorded, and required exact-main remote-safe CI is green. Environment-gated evidence that cannot run remotely must remain explicitly classified rather than fabricated.

## Enforcement

- `scripts/preflight-ci-manual-only.py` retains its historical filename for compatibility but now enforces the current split policy: automatic per-agent task CI plus one approved post-main release dispatcher, with release/native workflows otherwise manual-only.
- `.github/workflows/ci.yml` must remain read-only (`contents: read`) and automatic for `main`, `agent/**`, `recovery/**`, `integration/**`, and PRs to `main`.
- `.github/workflows/release-v25-cloud.yml` must remain a confirmed main-only release path.
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` must remain main/path scoped and may target only `release-v25-cloud.yml`.
- GitHub branch protection/rulesets should require the stable task-CI status on `main` PRs, reject force-push/deletion, and preserve the intended PR/integration path where repository settings allow it.

Related canonical documents: `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, and `docs/GITHUB-MAIN-PROTECTION.md` when present.

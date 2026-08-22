# GitHub Actions / CI Policy

This file is the repository-level source of truth for GitHub Actions behavior. `docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main`.

## Main is read-only for normal agents

Normal AI agents/chat sessions must not push, write, update refs, or merge to `main`. This applies to source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores.

Requests such as `fix bug`, `update code`, `implement all`, `continue all`, `commit push git`, `update docs`, `chore`, `run CI`, or `fix CI` do not grant `main` write/merge permission.

Only a session explicitly authorized by the repository owner to merge/integrate may change `main`, and only for the named PR/batch/task.

## Default Actions policy

GitHub Actions remain **manual-only by default**, with one owner-approved automatic post-integration exception:

```text
.github/workflows/dispatch-v25-cloud-after-main-integration.yml
```

That dispatcher may run on an integration-relevant push to `main` and may dispatch exactly:

```text
.github/workflows/release-v25-cloud.yml
```

All other workflows remain `workflow_dispatch`-only unless the owner explicitly changes this policy.

The automatic dispatcher is not permission for an agent to merge. It reacts only after an independently authorized `main` landing.

## Documentation/Markdown/chore-only changes

Ordinary documentation and Markdown changes must use a branch/PR like every other task. After an authorized merge, they must **not** trigger the V25 cloud release path when they touch only paths outside the dispatcher's watched set.

The current dispatcher watches integration-relevant paths such as:

- `src/**`
- `tests/**`
- `scripts/**`
- `Directory.Build.props`
- `QS3D.sln`
- `QS3D.V26.sln`
- `.github/workflows/release-v25-cloud.yml`
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`

Ordinary `docs/**`, generic `*.md`, claim/handoff/status Markdown and README-only changes are intentionally not in that automatic release path set.

**Changed paths are authoritative, not commit-message prefixes.** A commit named `docs:` or `chore:` that also modifies `scripts/**`, source, tests, build props, solutions, or watched workflows is integration-relevant and may trigger the automatic V25 cloud path after an authorized `main` merge.

## Canonical normal-agent flow

1. Read/fetch current `origin/main`.
2. Check overlapping Issues/PRs/claims.
3. Register the lane with an Issue when practical.
4. Create `agent/<agent-id>/<scope>` from the latest valid baseline.
5. Implement all task changes on that branch, including docs/Markdown/chores.
6. Validate and push only the task branch.
7. Open/update a PR.
8. Stop before merge unless explicit owner merge authorization was granted.

Agent branches and ordinary PR activity do not constitute the final integrated release candidate.

## Multi-agent integration

For a multi-agent owner request, an explicitly owner-authorized integration coordinator should assemble the participating branches/PRs on:

```text
integration/<batch-id>
```

Before an authorized landing to `main`, the coordinator must:

1. refresh `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches;
3. integrate every required change without silently dropping commits;
4. resolve semantic/API/test conflicts deliberately;
5. verify no required work remains only on an unmerged branch/PR;
6. run relevant remote-safe preflights/build/tests/smoke on the combined tree;
7. inspect for accidental reversions, duplicate implementations and contract mismatches;
8. freeze and record the integration candidate SHA;
9. merge to `main` only within explicit owner authorization;
10. fetch `main` again and record the exact final SHA.

Do not independently merge every agent PR to `main` merely to assemble the batch unless the owner explicitly requests that strategy.

## Definition of `ALL MERGED TO MAIN`

Report **ALL MERGED TO MAIN** only when an authorized integration reviewer freshly verifies:

- every required Issue/reservation is terminal or explicitly excluded/superseded;
- every required implementation/docs commit is represented in current `main`;
- no required work remains only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- the combined tree has no unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required remote-safe validation passed or environment-gated evidence is explicitly handed off;
- the exact current `main` SHA after the authorized landing is recorded.

A branch existing/deleted, Issue state, PR `Merged` UI state, or green CI for an older SHA is not sufficient proof by itself.

## Automatic post-integration V25 cloud CI

The owner-approved dispatcher is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

Its contract is:

- automatic trigger: integration-relevant `push` to `main` only;
- `workflow_dispatch` remains available for operator recovery/testing;
- documentation-only landings outside watched paths do not trigger it;
- `github-actions[bot]` pushes do not execute the dispatch job;
- adjacent integration landings are debounced/cancelled so the newest candidate wins before dispatch;
- it dispatches only `release-v25-cloud.yml` from `main`;
- it derives the next canonical preview tag in the repository's reserved series;
- it supplies `confirm_release=RELEASE` because this dispatcher is the owner's standing approval for the automatic post-integration preview path;
- the release workflow retains exact-source, source-guard, Core smoke, BricsCAD V25 compile-reference, packaging and release-integrity gates.

The automatic cloud run does not prove licensed local BricsCAD `NETLOAD`, native UI/runtime, private-DWG behavior, signing credentials, or other `LOCAL_ONLY` evidence classes.

## Manual workflow authorization

Except for the single automatic dispatcher above, workflows remain owner-controlled manual lanes.

A normal `continue all`, `fix bug`, `update code`, `commit`, review, docs or handoff assignment does not authorize manually dispatching/re-running/cancelling unrelated workflows.

Manual CI authorization is scope-specific and does not imply merge authorization. Merge authorization is scope-specific and does not imply unrelated manual CI/release authorization.

Release workflows such as the following remain manually invokable tools except when `release-v25-cloud.yml` is launched by the approved dispatcher:

- `.github/workflows/release-v25.yml`
- `.github/workflows/release-v25-cloud.yml`
- `.github/workflows/release-v26.yml`

Do not add another automatic trigger (`push`, `pull_request`, `schedule`, `workflow_run`, `repository_dispatch`, release/deployment events, etc.) to other workflows without another explicit owner policy change.

## Local worker boundary

The local workers (`agent/local002`, `agent/local003`, and successor sessions acting in those roles) remain LOCAL_ONLY by default. They must not treat GitHub Actions failures as their general coding backlog and must not dispatch/re-run/cancel Actions unless the owner explicitly assigns that exact CI operation to that local worker.

A local runtime finding may be handed to a remote/source agent for a normal repository-safe fix; reproduction location and source-fix ownership are separate.

## Local/static validation

Repository-local/static validation may run on agent or integration branches without starting GitHub Actions. Passing static review is not licensed BricsCAD runtime evidence.

V25 and V26 runtime proof are independent. A V25 PASS cannot be reported as V26 evidence, and vice versa.

## Enforcement

- `scripts/preflight.py` retains broad repository/source policy checks.
- `scripts/preflight-ci-manual-only.py` must enforce manual-only-by-default plus exactly one approved automatic post-integration dispatcher.
- The strict gate must reject any second automatic workflow, broadened automatic event, dispatcher that can target a workflow other than `release-v25-cloud.yml`, or release workflow that loses explicit release confirmation.
- `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` must keep documentation/Markdown-only paths outside its automatic watched set unless the owner explicitly changes that intent.
- GitHub branch protection/rulesets should protect `main`, require the intended PR/integration path for normal writers, and prevent force-push/deletion. Track hard-enforcement work in the repository governance issue for `main` protection.

Related canonical documents: `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/GITHUB-MAIN-PROTECTION.md` when present, and the manual/local qualification runbooks.

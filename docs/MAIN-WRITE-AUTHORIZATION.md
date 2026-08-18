# Main write authorization policy

This document is the canonical repository rule for who may change `main` **and for the default merge-completion endpoint of normal owner-requested repository tasks**. It overrides older wording in `AGENTS.md`, `CI_POLICY.md`, `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`, handoffs, claim files, or historical instructions when those documents conflict with this policy on main-write permission or whether an otherwise authorized same-task lifecycle should stop before merge.

## Default rule: agents treat `main` as read-only

`origin/main` is read-only for direct task writes. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores must be committed to a dedicated task branch and land through a PR.

### Explicit authorization required

A normal agent must never use a direct ref update, direct contents write, force-push, or equivalent write primitive against `main` for ordinary task work. Merge authorization is exercised through the task PR after the repository's required checks and merge gates are satisfied.

## Standing owner authorization: green task PRs merge themselves

The repository owner has granted a standing integration instruction for normal owner-requested task work:

> After the task is fixed and validated, if all required branch/PR checks are green, the PR is current and mergeable, the agent should proactively merge that PR into `main` without waiting for a second owner message.

This standing authorization applies only to the task/PR the current agent is actively completing for an owner request. It does **not** authorize merging unrelated PRs, bulk-merging other agents' work, bypassing required checks, pushing directly to `main`, force-pushing, or weakening branch protection.

The owner may override the standing rule for any task with an explicit instruction such as `do not merge main`, `PR only`, `stop before merge`, `đừng merge`, or another clearly equivalent restriction.

## Default owner-task completion endpoint: `MERGED_MAIN`

For a normal owner-requested repository task, the default successful endpoint is **`MERGED_MAIN`**, not merely edited code, a pushed branch, green branch CI, an open PR, or green PR checks.

Unless the owner explicitly opts out of merge for that exact task, the owning agent/session must proactively advance the same canonical carrier through the complete repository-safe lifecycle:

```text
implement/fix
  -> commit + push canonical branch
  -> exact-head branch CI SUCCESS when applicable
  -> refresh/reconcile current main
  -> fresh exact-head branch CI when required
  -> open/update the one canonical PR
  -> protected PR preflight + core SUCCESS on the current candidate
  -> re-check freshness/mergeability
  -> merge that same task PR through the protected PR path
  -> refresh and record resulting main SHA
  -> MERGED_MAIN
```

The following are **not valid self-selected stopping points** for an otherwise actionable owner task:

- a few minutes of elapsed session time;
- `edited`, `committed`, or `pushed` while later repository actions are available;
- branch CI green when the PR has not yet been opened;
- PR open when protected checks can still be observed/acted on;
- PR checks green when the same-task standing merge authorization applies;
- the first failed CI attempt when the failure is safely fixable in the current lane;
- a stale branch or stale green run that can be reconciled and revalidated safely;
- a queued/running gate merely because it is inconvenient to continue checking while useful authorized lifecycle work remains.

A task may end before `MERGED_MAIN` only when at least one concrete exception applies:

1. the owner explicitly opted out of merge for that exact task (`PR only`, `do not merge`, `stop before merge`, or equivalent);
2. another canonical owner/carrier owns the same Lane-Key and current-session mutation would violate collision rules;
3. a real external/authorization/tooling/platform blocker prevents all safe authorized progress, such as an unavailable secret, third-party outage, unsupported environment, or required LOCAL_ONLY/licensed evidence that is explicitly part of merge acceptance;
4. GitHub protection itself rejects the candidate and no safe current-lane remediation remains.

A temporary queued/running CI state is a lifecycle gate, not completion. The agent should continue same-lane safe work and re-check/advance the gate within the active execution whenever tooling permits. If an execution/platform boundary makes further observation impossible in that invocation, record the exact current gate and leave the canonical task `ACTIVE`; the next invocation resumes the same carrier automatically without asking the owner to repeat authorization. Do not shift ordinary CI/PR/merge work back to the owner merely because the agent has already spent some time on the task.

A failed CI/check on the current owned carrier is an automatic remediation trigger. Inspect the exact failing run/job/step, fix the root cause safely on the same canonical branch, commit/push, and revalidate. If the next run fails, repeat the loop. **The first attempted CI fix is never a stopping point while another safe current-lane remediation is available.**

## Main remains PR-only

Normal requests such as the following authorize task work on the task branch/PR, and under the standing rule above the agent should merge that **same task PR** once all merge gates are green:

- `fix bug`
- `update code`
- `implement all`
- `continue all`
- `commit`
- `commit push git`
- `review and fix`
- `run tests`
- `prepare release`
- `update docs`
- `update md`
- `chore`

They do not authorize direct contents writes or direct ref updates to `main`.

## Mandatory workflow for normal agents

1. Fetch/read the latest `origin/main` and record the exact baseline SHA.
2. Check current issues, open PRs, active claims and relevant branches for overlap.
3. Register the task using a GitHub Issue or the task branch/PR when practical. Do not publish a reservation by pushing to `main`.
4. Create a dedicated branch from the latest valid `main`, normally `agent/<agent-id>/<scope>`.
5. Put **all** task changes on that branch: source, tests, scripts, workflows, docs, Markdown, claim/handoff/status files and chores.
6. Commit and push only to that branch.
7. For watched paths, wait for automatic branch-push CI on the **exact current branch SHA** to finish successfully before opening a new PR.
8. Re-fetch `main`; if it moved, reconcile safely on the task branch, push the reconciled result, and obtain fresh green branch CI when applicable.
9. Open/update the PR targeting `main`.
10. Wait for the PR's required protected-main checks, currently `preflight` and `core`, to finish `SUCCESS` on the exact current PR head SHA.
11. Re-check that the PR is mergeable and that `main` has not moved in a way that invalidates strict required-status freshness. If GitHub requires another sync/revalidation, do it on the task branch and wait for fresh green checks.
12. When all required checks are green and GitHub permits the merge, **merge the task PR into `main` proactively** unless the owner explicitly opted out for that task.
13. Fetch `main` again and record/report the exact resulting SHA and merged PR number.

A green earlier SHA is not enough. The SHA being merged must be the SHA covered by the current required checks after any required synchronization with `main`.

For normal owner tasks, steps 1-13 are one continuation contract. Do not present an intermediate step as the intended final result while later steps are authorized and executable.

## Merge safety gates

The standing authorization is conditional. Do **not** merge when any of the following is true:

- a required status check is pending, failed, cancelled, missing, or stale;
- the PR is not mergeable or has unresolved conflicts;
- GitHub reports that strict required-status freshness requires an updated branch/candidate;
- the task branch no longer contains all intended work after a sync/conflict resolution;
- a current owner instruction says not to merge;
- the PR includes unrelated or unreviewed work outside the current task scope;
- repository protection/ruleset state is unexpectedly weakened or bypassed.

When a gate blocks merge, fix/reconcile/revalidate the task branch and continue automatically. Do not ask the owner to repeat `merge main` merely because CI took time to become green. A red gate re-enters the remediation loop; a stale gate re-enters the reconcile/revalidate loop.

## Documentation, Markdown, claims and chores

There is **no docs-only direct-to-main exception**. The following also use a dedicated branch/PR and follow the same green-then-merge rule:

- `docs/**`
- `*.md`
- `docs/agent-work-claims/**`
- handoff/status/inbox files
- README/policy updates
- non-functional chores
- release notes prepared by an agent

For work registration, prefer a GitHub Issue as the immediately visible reservation. If a Markdown claim is useful for repository history, create/update it on the same task branch/PR; it does not need to land on `main` before implementation starts.

## Integration coordinator and multi-agent batches

For an owner-requested multi-agent batch, a designated coordinator may use `integration/<batch-id>` and should merge the authorized batch once its combined candidate is green and mergeable.

The coordinator must:

1. refresh current `origin/main`;
2. identify the participating Issues/PRs/branches in the requested batch;
3. assemble/review the combined candidate without silently dropping work;
4. resolve semantic/API/test conflicts deliberately;
5. verify no required task remains only on an agent branch/unmerged PR;
6. run required combined-tree remote-safe validation;
7. freeze the candidate and record its exact SHA;
8. satisfy protected-main required checks and strict freshness;
9. merge through a PR when all required gates are green unless the owner explicitly opted out;
10. fetch `main` again and record the exact resulting SHA.

Standing authorization for the current task/batch is not permission to merge unrelated PRs.

## CI behavior

The automatic V25 cloud dispatcher must remain path-filtered to integration-relevant files. Documentation/Markdown/chore-only changes that do not touch those paths must not trigger the V25 release workflow.

Current integration-relevant automatic-dispatch paths are defined in `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` and include source/tests/scripts/build-solution/workflow surfaces. Ordinary `docs/**` and generic `*.md` changes are intentionally outside that path set.

A commit message such as `docs:`, `chore:` or `md:` is **not** sufficient evidence that CI should be skipped; changed paths are authoritative. A change labelled `chore` that modifies `scripts/**`, workflow files, solution files, build props or production source is integration-relevant and may trigger CI after an authorized merge to `main`.

Manual workflow dispatch/re-run/cancel remains separately controlled by `CI_POLICY.md`. The standing green-merge rule does not authorize unrelated manual CI or release operations.

## Release reporting boundary

For ordinary owner-requested code/docs/chore tasks, the default owner-facing completion target is `MERGED_MAIN`. Automatic exact-main/release/publish pipelines may continue separately after landing and are **not routine completion-status fields** for an ordinary merged task.

Do not routinely report release/version/publish status after an ordinary task reaches `MERGED_MAIN` unless one of these is true:

- the owner explicitly asks about release/version/update availability;
- release/publication/package/deployment is part of the current prompt's acceptance;
- the current lane is specifically a release/publishing lane;
- a release failure is the actual blocker to the owner's requested outcome.

This is reporting suppression, not permission to falsify release state or disable automatic release workflows. Automatic release machinery should continue according to its own workflow/policy.

## GitHub protection

Repository policy must be backed by GitHub branch protection/rulesets where available:

- require PR-based changes to `main`;
- require stable status checks `preflight` and `core`;
- keep strict required-status freshness enabled;
- protect `main` from force-push and deletion;
- keep bypass narrow and deliberate.

The standing authorization is designed to work **with** these protections: fix on a branch, obtain green evidence, merge the PR, then verify the resulting `main` SHA. It is not permission to bypass protection.

## Precedence

When another repository document conflicts with this file on `main` write/merge permission, the standing same-task merge authorization, or the default owner-task completion endpoint through `MERGED_MAIN`, this file wins unless the repository owner explicitly changes the policy again.
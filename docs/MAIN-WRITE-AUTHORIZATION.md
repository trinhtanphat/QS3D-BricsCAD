# Main write authorization policy

This document is the canonical repository rule for who may change `main`. It overrides older wording in `AGENTS.md`, `CI_POLICY.md`, handoffs, claim files, or historical instructions when those documents conflict with this policy.

## Standing owner authorization: green task PRs merge themselves

The repository owner has granted a standing integration instruction for normal owner-requested task work:

> After the task is fixed and validated, if all required branch/PR checks are green, the PR is current and mergeable, the agent should proactively merge that PR into `main` without waiting for a second owner message.

This standing authorization applies only to the task/PR the current agent is actively completing for an owner request. It does **not** authorize merging unrelated PRs, bulk-merging other agents' work, bypassing required checks, pushing directly to `main`, force-pushing, or weakening branch protection.

The owner may override the standing rule for any task with an explicit instruction such as `do not merge main`, `PR only`, `stop before merge`, `đừng merge`, or another clearly equivalent restriction.

## Main remains PR-only

`origin/main` remains read-only for direct task writes. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores must be committed to a dedicated task branch and land through a PR.

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

## Merge safety gates

The standing authorization is conditional. Do **not** merge when any of the following is true:

- a required status check is pending, failed, cancelled, missing, or stale;
- the PR is not mergeable or has unresolved conflicts;
- GitHub reports that strict required-status freshness requires an updated branch/candidate;
- the task branch no longer contains all intended work after a sync/conflict resolution;
- a current owner instruction says not to merge;
- the PR includes unrelated or unreviewed work outside the current task scope;
- repository protection/ruleset state is unexpectedly weakened or bypassed.

When a gate blocks merge, fix/reconcile/revalidate the task branch and continue automatically. Do not ask the owner to repeat `merge main` merely because CI took time to become green.

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

## GitHub protection

Repository policy must be backed by GitHub branch protection/rulesets where available:

- require PR-based changes to `main`;
- require stable status checks `preflight` and `core`;
- keep strict required-status freshness enabled;
- protect `main` from force-push and deletion;
- keep bypass narrow and deliberate.

The standing authorization is designed to work **with** these protections: fix on a branch, obtain green evidence, merge the PR, then verify the resulting `main` SHA. It is not permission to bypass protection.

## Precedence

When another repository document conflicts with this file on `main` write/merge permission, this file wins unless the repository owner explicitly changes the policy again.

# Main write authorization policy

This document is the canonical repository rule for who may change `main`. It overrides older wording in `AGENTS.md`, `CI_POLICY.md`, handoffs, claim files, or historical instructions that allowed claim/docs/chore/source commits directly to `main`.

## Activation boundary

This policy becomes repository-active when the owner-authorized governance PR containing it is merged into `main`. Until then, current `main` may still contain older direct-to-main wording and other concurrent agents may continue following that older policy.

The existence of an open policy PR does not itself rewrite `main`. The migration is complete only after an explicitly authorized merge plus a fresh read-back of current `main`.

## Default rule: agents treat `main` as read-only

Unless the repository owner explicitly authorizes the current agent/session to integrate or merge, every AI agent/chat session must treat `origin/main` as **read-only**.

Normal owner requests such as any of the following do **not** grant write/merge permission to `main`:

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

Those requests authorize work only inside the task's own issue/branch/PR scope unless the owner also gives explicit `main` integration authorization.

## Explicit authorization required

An agent/session may change `main` only when the owner clearly grants that role in the current instruction or an unambiguously still-active instruction, for example:

- `merge all về main`
- `bạn là integration coordinator`
- `cho phép merge PR này vào main`
- `merge integration branch này vào main`
- another equally explicit instruction naming the `main` merge/integration action

Authorization is scope-specific. Permission to merge one PR/batch does not become standing permission for later tasks. Permission to run CI does not imply permission to merge. Permission to fix CI does not imply permission to push source directly to `main`.

## Mandatory workflow for normal agents

1. Fetch/read the latest `origin/main` and record the exact baseline SHA.
2. Check current issues, open PRs, active claims and relevant branches for overlap.
3. Register the task using a GitHub Issue or the task branch/PR. Do not publish a reservation by pushing to `main`.
4. Create a dedicated branch from the latest valid `main`, normally `agent/<agent-id>/<scope>`.
5. Put **all** task changes on that branch: source, tests, scripts, workflows, docs, Markdown, claim/handoff/status files and chores.
6. Commit and push only to that branch.
7. Re-fetch `main`, reconcile safely if it moved, validate the branch, and inspect the final diff.
8. Open/update a PR targeting the intended integration branch or `main` as appropriate.
9. Stop before merge unless the owner explicitly granted merge/integration authorization.

A normal agent must never use a direct ref update, direct contents write, force push, merge API, or equivalent operation that changes `main`.

## Documentation, Markdown, claims and chores

There is **no docs-only exception** to the read-only-main rule for normal agents.

The following must also use a dedicated branch/PR:

- `docs/**`
- `*.md`
- `docs/agent-work-claims/**`
- handoff/status/inbox files
- README/policy updates
- non-functional chores
- release notes prepared by an agent

This prevents coordination commits from racing implementation commits and keeps one auditable integration path.

For work registration, prefer a GitHub Issue as the immediately visible reservation. If a Markdown claim is useful for repository history, create/update it on the same task branch/PR; it does not need to land on `main` before implementation starts.

## Integration coordinator

Only an owner-authorized integration coordinator may merge a batch into `main`.

The coordinator must:

1. refresh current `origin/main`;
2. identify the exact authorized PRs/branches/issues in the batch;
3. assemble/review the combined candidate, preferably on `integration/<batch-id>` for multi-agent batches;
4. verify required commits are represented and no task remains only on an unmerged branch;
5. resolve conflicts deliberately and run required remote-safe validation;
6. freeze the candidate and record its exact SHA;
7. merge to `main` only within the owner's explicit authorization;
8. fetch `main` again and record the exact resulting SHA;
9. never treat that authorization as permission for unrelated later merges.

## CI behavior

The automatic V25 cloud dispatcher must remain path-filtered to integration-relevant files. Documentation/Markdown/chore-only changes that do not touch those paths must not trigger the V25 release workflow.

Current integration-relevant automatic-dispatch paths are defined in `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` and include source/tests/scripts/build-solution/workflow surfaces. Ordinary `docs/**` and generic `*.md` changes are intentionally outside that path set.

A commit message such as `docs:`, `chore:` or `md:` is **not** sufficient evidence that CI should be skipped; changed paths are authoritative. A change labelled `chore` that modifies `scripts/**`, workflow files, solution files, build props or production source is integration-relevant and may trigger CI after an authorized merge to `main`.

## GitHub protection

Repository policy must be backed by GitHub branch protection/rulesets where available:

- protect `main` from force-push and deletion;
- require PR-based changes for normal writers;
- keep owner/admin bypass narrow and deliberate;
- use required checks when stable check names are available.

Until GitHub reports protection/ruleset enforcement, this Markdown rule remains mandatory after activation but cannot physically stop a credential with write permission from bypassing it. Track hard-enforcement work in the repository governance issue for `main` protection.

## Precedence

When another repository document conflicts with this file on `main` write permission, this file wins unless the repository owner explicitly changes the policy again.

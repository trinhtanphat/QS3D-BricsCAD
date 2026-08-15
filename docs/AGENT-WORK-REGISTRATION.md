# QS3D agent work registration and integration

**Owner rule:** normal AI agents/chat sessions treat `origin/main` as read-only. Every task—including source, tests, scripts, workflows, documentation, Markdown, claim/handoff/status and chores—must be done on a dedicated issue/branch/PR. Only an agent/session explicitly authorized by the repository owner as an integration/merge coordinator may change `main`.

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for `main` write permission. This file is the canonical work-registration and batch-integration protocol. `CI_POLICY.md` is authoritative for CI behavior after an authorized integration landing.

## Source of truth for reservations

Use a GitHub Issue as the immediately visible work reservation whenever practical. Historical Markdown claims remain under:

```text
docs/agent-work-claims/
```

New or updated Markdown claims may still be used for repository history, but they must be committed on the task branch/PR; they are **not** pushed directly to `main` as a prerequisite for implementation.

A reservation should identify:

- status (`ACTIVE`, `BLOCKED`, `COMPLETED`, or `RELEASED`);
- stable agent/session identity;
- exact baseline `main` SHA;
- exact scope and exclusions;
- expected files/symbols/tests/runtime surfaces;
- validation plan;
- task branch and PR when created;
- related issue and integration batch when known.

`ACTIVE` and `BLOCKED` reservations remain owned until completed, released, superseded, or explicitly reassigned by the owner/coordinator.

## Strict lane non-interference — highest priority for normal agents

A normal AI agent/chat session owns **only its assigned/reserved lane**. Work owned by another agent/session—including local agents—is out of scope unless the repository owner explicitly expands this session's role.

A normal agent must **not** opportunistically inspect, audit, review, validate, fix, merge, close, modify, reassign, rerun CI for, or otherwise manage another agent/session's work. In particular, do not open another agent's branch/PR/Issue merely to judge progress, evidence quality, merge readiness, LOCAL_ONLY status, CI status, or whether that agent has finished.

Cross-agent visibility is limited to the **minimum coordination metadata necessary to avoid an obvious collision**, for example: whether a lane/file/symbol is already reserved and the reservation's stated scope/exclusions. Once another owner is identified, stop there and choose a different non-overlapping lane unless the owner explicitly assigns coordination with that agent.

For normal agents:

- do not fetch another agent's PR diff/patch for curiosity or general review;
- do not read another agent's local/runtime evidence unless the owner explicitly asks this session to review that exact evidence;
- do not monitor another agent's branch commits, CI runs, draft status, or completion status;
- do not merge/close/update another agent's PR or Issue;
- do not take over another agent's lane because it appears stale, slow, blocked, or incomplete;
- do not "continue all" by sweeping unrelated agents' open work;
- do not treat LOCAL_ONLY work owned by local agents as this session's backlog;
- if another agent's already-landed work on current `main` overlaps this lane, inspect **current `main` only** as implementation truth; do not backtrack into that agent's branch/PR history unless explicitly authorized.

The only normal exception is a **minimal collision check** against visible reservations. Any broader cross-agent inspection requires explicit owner wording such as `review PR #...`, `coordinate with agent ...`, `merge this named batch`, or `you are the integration coordinator`.

This section overrides broader wording elsewhere that could be read as permission for a normal agent to inspect "concurrent work". For normal agents, "check concurrent work" means **minimal reservation/collision metadata only**, not auditing other agents.

## Mandatory sequence for a normal agent

1. Fetch/read current `origin/main` and record the exact SHA.
2. Read `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, this file, and the Issue/claim/runbook for **this lane**.
3. Perform only the minimal reservation/collision check needed to verify that this lane is not already owned; do not audit other agents' work.
4. Choose a non-overlapping lane.
5. Create or update a GitHub Issue to register the lane, unless an existing owner-created issue already uniquely identifies it.
6. Create a dedicated branch from the latest valid baseline, normally:

   ```text
   agent/<agent-id>/<scope>
   ```

7. Put every repository change for the task on that branch, including docs/Markdown/claims/chores.
8. Implement only the reserved lane.
9. Run relevant branch-local/static/unit/smoke validation for this lane.
10. Re-fetch `origin/main`; if it moved, reconcile against current `main` safely without inspecting or overwriting another agent's unmerged work.
11. Push only the task branch.
12. Open/update a PR targeting the intended integration branch or `main`.
13. Stop before merge unless the owner explicitly authorized this session to merge/integrate.

A chat message, local patch, or unpushed branch is not a visible reservation. An Issue plus pushed task branch/PR is the preferred coordination surface.

## No implicit `main` authorization

The following owner phrases authorize task work but **do not authorize a write or merge to `main`** by themselves:

- `fix bug`
- `update code`
- `implement all`
- `continue all`
- `commit`
- `commit push git`
- `review and fix`
- `update docs`
- `update md`
- `chore`
- `run CI`
- `fix CI`

A session may change `main` only after explicit owner authorization such as `merge all về main`, `bạn là integration coordinator`, `cho phép merge PR này vào main`, or another equally clear instruction naming the merge/integration action.

Authorization is limited to the named PR/batch/task. It does not carry forward automatically to later work.

## Branch discipline

Every normal agent must:

1. base its branch on the latest valid `main` baseline;
2. periodically refresh `origin/main` without auditing other agents' branches/PRs;
3. keep edits inside the reserved scope;
4. make coherent lane/request-level commits rather than file-by-file noise;
5. never force-push or reset shared `main`;
6. never update the `main` ref directly;
7. never use the GitHub contents API against `main` for docs, claims, chores or code;
8. never merge its own PR unless explicit owner merge authorization was granted;
9. record its branch/commit/PR in the Issue or task handoff.

A pushed branch or open PR is **not** `ALL MERGED TO MAIN`.

## Documentation, Markdown, claims and chores

There is no docs-only exception. These surfaces also stay on task branches until an authorized merge:

```text
docs/**
*.md
docs/agent-work-claims/**
README.md
handoff/status/inbox files
policy files
release-note preparation
non-functional chores
```

If a task contains only documentation/Markdown/chore changes, it still uses a branch and PR.

## Multi-agent integration branch

For a multi-agent owner request, the owner-authorized coordinator should assemble the combined candidate on:

```text
integration/<batch-id>
```

The coordinator exception begins **only after explicit owner authorization**. Only then may that coordinator inspect the exact named participating Issues/PRs/branches required for the authorized batch.

The coordinator must:

1. refresh latest `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches;
3. merge/cherry-pick/rebase required agent branches into the integration branch without silently dropping commits;
4. resolve semantic/API/test conflicts deliberately rather than choosing `ours`/`theirs` blindly;
5. verify no required work remains only on an unmerged branch/PR;
6. run relevant combined-tree validation;
7. inspect the final diff for accidental reversions, duplicate implementations and contract mismatches;
8. freeze and record the integration candidate SHA;
9. merge to `main` only within the owner's explicit authorization;
10. fetch `main` again and record the exact resulting SHA.

Do not assemble a multi-agent batch by independently landing each agent PR on `main` unless the owner explicitly requests that specific integration strategy.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, state **ALL MERGED TO MAIN** only after an **owner-authorized integration reviewer/coordinator** freshly verifies:

- every required Issue/reservation in the explicitly authorized batch is terminal or explicitly excluded/superseded;
- every required implementation/docs commit is represented in the integrated result and reachable from current `main`;
- no required work for that authorized batch exists only on an agent branch, worktree, stash, draft patch or unmerged PR;
- current `main` was refreshed after the authorized landing;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required remote-safe validation passed or environment-gated evidence is explicitly handed off;
- the exact current `main` SHA is recorded.

A normal non-coordinator agent must not perform this repository-wide/multi-agent sweep merely to decide whether its own chat can end. Its completion question is limited to **its own Issue/branch/PR/lane**.

Branch deletion, Issue state, PR UI state, or a previous CI run is not sufficient proof.

## Scope changes and handoff

If work expands beyond the registered scope:

1. stop before touching the added implementation surface;
2. refresh `main` and perform only the minimum collision check for the added scope;
3. update the task Issue and branch claim/handoff with the added scope;
4. if the added scope is owned by another agent, do not inspect or take it over; keep it excluded unless the owner explicitly reassigns it;
5. continue on the same task branch or a new dedicated branch as appropriate.

Do not push a claim amendment to `main` merely to reserve the expanded scope.

If another agent should continue, leave exact completed state, remaining work, branch/commit/PR references and successor boundary in **this lane's** Issue/PR/handoff; do not manage the successor's execution.

## Closing a task

Before an authorized merge, update the Issue/PR with:

- branch name;
- implementation/docs commit SHA(s);
- validation actually executed;
- known LOCAL_ONLY/policy gates belonging to this lane;
- intended integration batch when known.

After the authorized merge, the coordinator may close/update the Issue and, when repository-history Markdown is useful, include the claim close-out in the same authorized integration/docs PR. A normal agent does not push close-out Markdown directly to `main` and does not close another agent's Issue/PR.

## CI boundary

The automatic V25 cloud dispatcher is path-filtered. Agent branches and PRs do not trigger the final `main` integration release path. After an authorized `main` landing, ordinary docs/Markdown-only changes remain outside the automatic dispatch path unless they also modify an integration-relevant watched file.

Changed paths, not commit-message prefixes, determine whether an automatic dispatch is eligible. A `chore:` commit that changes `scripts/**`, workflows, build props, solution files, source or tests is integration-relevant despite its commit message.

See `CI_POLICY.md` and `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- Never inspect/manage another agent's work beyond the minimum collision metadata unless the owner explicitly authorizes that cross-agent role.
- Normal task authorization never implies `main` merge authorization.
- CI authorization never implies `main` merge authorization.
- `main` merge authorization never implies unrelated manual CI/release authorization.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.
- GitHub branch protection/rulesets should enforce this policy where possible; track hard-enforcement work in the repository governance issue for `main` protection.

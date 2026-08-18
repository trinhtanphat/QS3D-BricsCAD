# QS3D agent work registration and integration

**Owner rule:** normal AI agents/chat sessions treat `origin/main` as read-only. Every task—including source, tests, scripts, workflows, documentation, Markdown, claim/handoff/status and chores—must be done on a dedicated issue/branch/PR. Only an agent/session explicitly authorized by the repository owner as an integration/merge coordinator may change `main`.

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for `main` write permission. This file is the canonical work-registration and batch-integration protocol. `docs/AGENT-LANE-LOCK.md` is authoritative for concurrent Lane-Key ownership and canonical-carrier uniqueness. `docs/CHATGPT-SCHEDULE-BOUNDARY.md` is authoritative for the boundary between external ChatGPT account scheduled tasks and repository ownership/lane semantics. `CI_POLICY.md` is authoritative for CI behavior. `docs/GITHUB-MAIN-PROTECTION.md` records the current hard-protection contract.

## Source of truth for reservations

Use a GitHub Issue as the immediately visible work reservation whenever practical. Historical Markdown claims remain under:

```text
docs/agent-work-claims/
```

New or updated Markdown claims may still be used for repository history, but they must be committed on the task branch/PR; they are **not** pushed directly to `main` as a prerequisite for implementation.

A reservation should identify:

- status (`ACTIVE`, `BLOCKED`, `COMPLETED`, or `RELEASED`);
- stable **Lane-Key**, normally `issue-<number>`;
- stable agent/session identity;
- exact baseline `main` SHA;
- exact scope and exclusions;
- expected files/symbols/tests/runtime surfaces;
- validation plan;
- the one canonical task branch and PR when created;
- explicit supersession of any historical carrier;
- related issue and integration batch when known.

`ACTIVE` and `BLOCKED` reservations remain owned until completed, released, superseded, or explicitly reassigned by the owner/coordinator.

## Canonical Lane-Key / carrier lock

Every concrete task has one stable Lane-Key. For normal issue-backed work use `issue-<number>`. An umbrella audit Issue is not a shared Lane-Key for every discovered implementation; each concrete fix needs its own unique task Issue/Lane-Key.

At any time a Lane-Key may have at most:

- one ACTIVE owner/session;
- one canonical task branch;
- one open canonical PR.

If an equivalent active carrier already exists, the required status is:

```text
DUPLICATE_CARRIER / NO MUTATION
```

Do not create a second implementation because another carrier is stale, red, behind `main`, queued, slower, less clean, or owned by another agent model/chat session. Those states do not release ownership.

When rebuilding a carrier is genuinely required, explicit supersession/reassignment must be recorded first. Close the old open PR before representing the replacement as canonical, preserve the Lane-Key, and keep only one active carrier.

Do not create branch-to-branch/internal PRs solely to sync/replay `main` or another task branch into an agent branch. Reconcile the canonical task branch non-force, or explicitly supersede it and create exactly one replacement carrier.

### External ChatGPT scheduler boundary

External invokers, including ChatGPT account scheduled tasks/automations, may include convenience labels such as `C0`, `W1-W4`, `QS3D-CONTROL`, `QS3D-WORKER-*`, `controller`, `worker`, or `Task 0-4` in a prompt. Those labels are **account-side orchestration metadata only**. They do not define a repository lane, persistent repository owner, canonical carrier, GitHub reservation, CI authority, merge authority, or direct-`main` authority.

Schedule existence, count, cadence, enabled/disabled state, account task IDs, and live execution state exist outside this repository and must not be inferred from Markdown, Issues, comments, branches, PRs, or historical controller/worker records. Every scheduled execution must resolve the current GitHub Lane-Key / Issue / branch / PR state at execution time and follow the ordinary collision, ownership, CI, and authorization rules in this document. See `docs/CHATGPT-SCHEDULE-BOUNDARY.md`.

The shared PR preflight enforces Lane-Key uniqueness for same-repository `agent/**` and `integration/**` carriers. PR metadata must include `Lane-Key`, canonical owner/session, canonical carrier, and explicit supersession information.

## Strict lane non-interference — highest priority for normal agents

A normal AI agent/chat session owns **only its assigned/reserved lane**. Work owned by another agent/session—including local agents—is out of scope unless the repository owner explicitly expands this session's role.

A normal agent must **not** opportunistically inspect, audit, review, validate, fix, merge, close, modify, reassign, manually rerun CI for, or otherwise manage another agent/session's work. Automatic shared branch/PR CI running because a branch or PR changed is repository infrastructure, not a cross-agent takeover.

Cross-agent visibility is limited to the **minimum coordination metadata necessary to avoid an obvious collision**, for example: Lane-Key, whether a lane/file/symbol is already reserved, canonical carrier identity, and the reservation's stated scope/exclusions. Once another owner is identified, stop there and choose a different non-overlapping lane unless the owner explicitly assigns coordination with that agent.

For normal agents:

- do not fetch another agent's PR diff/patch for curiosity or general review;
- do not read another agent's local/runtime evidence unless the owner explicitly asks this session to review that exact evidence;
- do not monitor another agent's branch commits, CI runs, draft status, or completion status;
- do not merge/close/update another agent's PR or Issue;
- do not take over another agent's lane because it appears stale, slow, blocked, red, behind, or incomplete;
- do not create a competing carrier for an existing Lane-Key;
- do not "continue all" by sweeping unrelated agents' open work;
- do not treat LOCAL_ONLY work owned by local agents as this session's backlog;
- if another agent's already-landed work on current `main` overlaps this lane, inspect **current `main` only** as implementation truth; do not backtrack into that agent's branch/PR history unless explicitly authorized.

The only normal exception is a **minimal collision check** against visible reservations and canonical-carrier metadata. Any broader cross-agent inspection requires explicit owner wording such as `review PR #...`, `coordinate with agent ...`, `merge this named batch`, or `you are the integration coordinator`.

## Mandatory sequence for a normal agent

1. Fetch/read current `origin/main` and record the exact SHA.
2. Read `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, this file, `docs/AGENT-LANE-LOCK.md`, `docs/CHATGPT-SCHEDULE-BOUNDARY.md` when the session was invoked by or discusses an external schedule, and the Issue/claim/runbook for **this lane**.
3. Perform only the minimal reservation/collision check needed to verify that this lane is not already owned; do not audit other agents' work.
4. Determine the stable Lane-Key and verify there is no equivalent ACTIVE owner/canonical carrier. If one exists, stop as `DUPLICATE_CARRIER / NO MUTATION`.
5. Choose a non-overlapping lane.
6. Create or update a GitHub Issue to register the lane, unless an existing owner-created issue already uniquely identifies it.
7. Create a dedicated branch from the latest valid baseline, normally:

   ```text
   agent/<agent-id>/<scope>
   ```

   Do not create a second task branch when the Lane-Key already has an active canonical carrier unless explicit supersession was recorded first.

8. Put every repository change for the task on that one canonical branch, including docs/Markdown/claims/chores.
9. Implement only the reserved lane.
10. Run relevant local/static/unit/smoke validation available to this lane.
11. Push the task branch. When watched integration-relevant paths changed, the shared branch CI must automatically validate the exact branch SHA.
12. **Before opening a new PR for watched/integration-relevant work, wait for the exact current branch SHA to reach terminal branch-CI `SUCCESS`.** A PR or draft PR must not be used as the first CI attempt. If the branch run fails, fix it on the branch and obtain a new green run first.
13. Re-fetch `origin/main`. If it moved, reconcile the same canonical carrier against current `main` safely without inspecting or overwriting another agent's unmerged work, push the reconciled branch, and obtain fresh branch-CI `SUCCESS` again before opening the PR.
14. Open/update the one canonical PR targeting the intended integration branch or `main`. Include `Lane-Key`, canonical owner/session, canonical carrier, and `Supersedes` metadata. GitHub's PR CI and protected-main rules validate the merge candidate/freshness when applicable.
15. Stop before merge unless the owner explicitly authorized this session to merge/integrate.

A chat message, local patch, or unpushed branch is not a visible reservation. An Issue plus pushed task branch is the preferred coordination surface before PR creation; after branch CI is green, the PR becomes the handoff/review surface.

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

Automatic CI is validation infrastructure, not merge authorization. GitHub ruleset eligibility is also not merge authorization.

## Branch discipline

Every normal agent must:

1. base its branch on the latest valid `main` baseline;
2. periodically refresh `origin/main` without auditing other agents' branches/PRs;
3. keep edits inside the reserved scope and Lane-Key;
4. keep exactly one canonical active carrier for the Lane-Key;
5. make coherent lane/request-level commits rather than file-by-file noise;
6. never force-push or reset shared `main`;
7. never update the `main` ref directly;
8. never use the GitHub contents API against `main` for docs, claims, chores or code;
9. never create transport/reconciliation PRs solely to move `main` or another branch into the canonical carrier;
10. for watched/integration-relevant work, obtain green branch CI on the exact current branch SHA before opening a new PR;
11. never merge its own PR unless explicit owner merge authorization was granted;
12. record its Lane-Key, branch/commit/PR and actual validation evidence in the Issue or task handoff.

A pushed branch, open PR, or green branch CI run is **not** `ALL MERGED TO MAIN`.

## Shared branch/PR CI

The repository uses one common `.github/workflows/ci.yml`; agents do not create one workflow per branch.

For watched integration-relevant paths:

- push to `agent/**` validates the exact branch tree and is the mandatory pre-PR gate;
- a PR targeting `main` or `integration/**` validates GitHub's merge candidate against that target when the workflow applies;
- PR preflight rejects a same-repository `agent/**` or `integration/**` PR that omits required Lane-Key metadata or duplicates another open carrier with the same Lane-Key;
- push to `integration/**` validates the exact combined tree assembled by an authorized coordinator.

Shared CI is non-publishing: it has read-only repository/Actions/PR metadata permission and must not create tags/releases, sign packages, dispatch release workflows, mutate Issues, or write `main`.

A green agent branch proves only the exact tested branch SHA. A green PR merge candidate proves only that merge candidate. A green integration branch proves only the combined integration tree. Use each evidence class for the tree it actually tested.

The pre-PR rule does not mean the repository conceptually requires two arbitrary identical full runs. Branch CI is the isolated-candidate admission gate. PR CI / required checks are merge-candidate, Lane-Key-uniqueness and freshness evidence. If `main` moves or the candidate tree changes, fresh applicable evidence is mandatory.

## Active `main` hard protection

GitHub ruleset `protectedMain` (ruleset ID `20890901`) is active on the default branch. The expected effective `main` rules are:

- deletion protection;
- non-fast-forward / force-push protection;
- require pull request;
- require status checks `preflight` and `core`;
- strict required-status freshness;
- empty bypass list.

Repository Markdown cannot prove that an external GitHub setting is still active. When hard-protection state matters, verify GitHub's effective branch rules. If `main` loses protection, required checks, target matching, or unexpectedly gains a bypass actor, treat it as a governance defect rather than silently continuing as if protection still existed.

The hard ruleset prevents many invalid writes, but it does not decide which agent is authorized by the owner to merge. The owner-authorization policy remains authoritative.

## Documentation, Markdown, claims and chores

There is no direct-main docs-only exception. These surfaces also stay on task branches until an authorized merge:

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

Canonical CI/governance files are watched by shared branch CI. For those watched files, branch CI must be green before opening the PR. Ordinary docs-only files outside the watch set may skip heavy pre-PR branch CI, but they still require a branch/PR and must satisfy the protected-main merge gate without bypassing it.

## Multi-agent integration branch

For a multi-agent owner request, the owner-authorized coordinator should assemble the combined candidate on:

```text
integration/<batch-id>
```

The coordinator exception begins **only after explicit owner authorization**. Only then may that coordinator inspect the exact named participating Issues/PRs/branches required for the authorized batch.

The coordinator must:

1. refresh latest `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches and their Lane-Keys/canonical carriers;
3. verify participating watched lanes had green branch CI before their PRs were opened under the current policy, unless they predate the policy and are explicitly handled as legacy candidates;
4. reject duplicate active carriers before assembly; explicitly resolve/supersede one carrier rather than integrating both;
5. merge/cherry-pick/rebase required agent branches into the integration branch without silently dropping commits;
6. resolve semantic/API/test conflicts deliberately rather than choosing `ours`/`theirs` blindly;
7. verify no required work remains only on an unmerged branch/PR;
8. obtain green **combined-tree CI** on the exact frozen `integration/**` SHA when watched paths changed;
9. inspect the final diff for accidental reversions, duplicate implementations and contract mismatches;
10. freeze and record the integration candidate SHA;
11. merge to `main` only within the owner's explicit authorization and only when GitHub protected-main requirements are satisfied;
12. fetch `main` again and record the exact resulting SHA;
13. require the applicable **exact-main release CI** before claiming cloud/release completion.

Do not assemble a multi-agent batch by independently landing each agent PR on `main` unless the owner explicitly requests that specific integration strategy.

## CI evidence ladder

The normal evidence ladder is:

```text
agent/** push
  -> shared branch CI on exact branch SHA
  -> SUCCESS required before opening PR for watched work
PR -> main or integration/**
  -> Lane-Key uniqueness + protected candidate / required-check freshness evidence
integration/** push
  -> combined-tree CI on frozen integration SHA
owner-authorized protected-main landing
  -> exact-main release CI through dispatch-v25-cloud-after-main-integration.yml
```

A failure should be fixed at the earliest stage that reproduces it. Do not intentionally open a new PR with a known-red watched branch merely to obtain diagnostics, and do not intentionally land a known-red branch on `main`.

The final main release run is still required when the task's acceptance includes cloud packaging/release because earlier branch/integration runs do not prove the exact landed SHA or release side effects.

Licensed/native runtime evidence is independent and remains `PENDING_LOCAL` until executed.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, state **ALL MERGED TO MAIN** only after an **owner-authorized integration reviewer/coordinator** freshly verifies:

- every required Issue/reservation in the explicitly authorized batch is terminal or explicitly excluded/superseded;
- every Lane-Key has one winning canonical implementation and no duplicate open carrier;
- every required implementation/docs commit is represented in the integrated result and reachable from current `main`;
- no required work for that authorized batch exists only on an agent branch, worktree, stash, draft patch or unmerged PR;
- required participating branch/PR CI evidence is green and fresh where applicable;
- the exact frozen integration candidate has green combined-tree CI when applicable;
- current `main` was refreshed after the authorized landing;
- GitHub still reports the intended protected-main effective rules or an explicitly owner-approved replacement;
- applicable exact-main release CI is green for the landed SHA;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- environment-gated evidence is explicitly handed off rather than falsely reported as PASS;
- the exact current `main` SHA is recorded.

A normal non-coordinator agent must not perform this repository-wide/multi-agent sweep merely to decide whether its own chat can end. Its completion question is limited to **its own Issue/branch/PR/lane**.

Branch deletion, Issue state, PR UI state, or a previous CI run is not sufficient proof.

## Scope changes and handoff

If work expands beyond the registered scope:

1. stop before touching the added implementation surface;
2. refresh `main` and perform only the minimum collision check for the added scope/Lane-Key;
3. update the task Issue and branch claim/handoff with the added scope;
4. if the added scope or equivalent Lane-Key is owned by another agent, do not inspect or take it over; keep it excluded unless the owner explicitly reassigns it;
5. continue on the same canonical task branch, or explicitly supersede before creating a replacement carrier when genuinely required.

Do not push a claim amendment to `main` merely to reserve the expanded scope.

If another agent should continue, leave exact completed state, remaining work, Lane-Key, canonical branch/commit/PR references and successor boundary in **this lane's** Issue/PR/handoff; do not manage the successor's execution.

## Closing a task

Before an authorized merge, update the Issue/PR with:

- Lane-Key and canonical owner/session;
- canonical branch name and any explicit supersession;
- implementation/docs commit SHA(s);
- validation actually executed;
- mandatory pre-PR branch-CI run identity and exact tested SHA when applicable;
- PR/integration CI evidence, Lane-Key uniqueness result and freshness when applicable;
- known LOCAL_ONLY/policy gates belonging to this lane;
- intended integration batch when known.

After the authorized merge, the coordinator may close/update the Issue and, when repository-history Markdown is useful, include the claim close-out in the same authorized integration/docs PR. A normal agent does not push close-out Markdown directly to `main` and does not close another agent's Issue/PR.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- Never create a duplicate carrier for an ACTIVE Lane-Key.
- Never inspect/manage another agent's work beyond the minimum collision metadata unless the owner explicitly authorizes that cross-agent role.
- Normal task authorization never implies `main` merge authorization.
- Automatic branch/PR CI never implies `main` merge authorization.
- Protected-main eligibility never implies owner merge authorization.
- `main` merge authorization never implies unrelated manual release authorization.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.
- GitHub ruleset `protectedMain` is the current hard-enforcement layer, while repository Markdown remains the behavioral/authorization layer.
# Agent Collaboration Policy

This repository is expected to have multiple agents working concurrently. Every agent must protect other agents' work, avoid overlapping lanes, and choose tasks that match its actual execution environment.

## Highest-priority Git/Main rule

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main`.

**Default:** every normal AI agent/chat session treats `origin/main` as read-only.

The following requests do **not** grant permission to push or merge to `main` by themselves:

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

A session may change `main` only when the repository owner explicitly grants a merge/integration role for the named PR/batch/task, for example `merge all về main`, `bạn là integration coordinator`, or `cho phép merge PR này vào main`.

Authorization is scope-specific and does not automatically carry forward.

There is **no docs/Markdown/chore exception**. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores all go to a dedicated task branch/PR. Normal agents must not use direct contents writes, ref updates, merge APIs, force pushes, or equivalent operations against `main`.

## Locked product form: BricsCAD plugin

QS3D is a **BricsCAD V25 + V26 Windows x64 hosted plugin**, not a standalone CAD desktop executable. A matching licensed BricsCAD host is required at runtime; the native BricsCAD viewport/database/editor remain the CAD host.

V25 loads the `QS3D.BricsCAD.V25` Library/DLL built for `net48`; V26 loads the `QS3D.BricsCAD.V26` Library/DLL built for `net8.0-windows`. Each host-major assembly is loaded by the matching BricsCAD host through DemandLoad or `NETLOAD` and must never be relabeled across majors.

`BLT-like`, `BLT-style`, `BLT3D-familiar`, “giống BLT” and similar wording refer to clean-room workflow/UX familiarity only. Do not reinterpret them as a requirement for `QS3D.exe` or a QS3D-owned CAD engine.

`docs/PRODUCT-BOUNDARY.md` is authoritative unless the owner explicitly changes the product boundary.

## Mandatory reading order

Before substantive work, read:

1. `AGENTS.md`;
2. `docs/MAIN-WRITE-AUTHORIZATION.md`;
3. `docs/PRODUCT-BOUNDARY.md`;
4. `CI_POLICY.md`;
5. fetch/read the latest `origin/main` and record its exact SHA;
6. `docs/AGENT-WORK-REGISTRATION.md`;
7. `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`;
8. relevant open Issues/PRs plus `ACTIVE`/`BLOCKED` historical claims under `docs/agent-work-claims/`;
9. `docs/REMOTE-AGENT-SCOPE.md`;
10. the newest current handoff/status docs relevant to the task;
11. `docs/LOCAL-AGENT-INBOX.md` for LOCAL_ONLY work;
12. the exact feature/runbook documents required by the assigned lane.

Current source wins over stale historical handoffs for implementation truth. `docs/LOCAL-AGENT-INBOX.md` is the live LOCAL_ONLY priority index when older local documents disagree on status/priority.

## Mandatory work registration

Before implementation, every normal agent must:

1. fetch/read current `origin/main`;
2. inspect relevant Issues, PRs, branches and active/blocking claims;
3. choose a non-overlapping lane and determine its stable **Lane-Key**, normally `issue-<number>`;
4. identify the one current canonical owner/carrier for that Lane-Key, if any; an existing equivalent active carrier means `DUPLICATE_CARRIER / NO MUTATION`;
5. create/update a GitHub Issue for the lane when practical, unless an existing owner-created issue already uniquely identifies the task;
6. create a dedicated branch from the latest valid baseline, normally `agent/<agent-id>/<scope>`, only when no active canonical carrier already owns the Lane-Key;
7. put **all** task changes on that one canonical branch, including source, tests, scripts, workflows, docs, Markdown, claim/handoff/status files and chores;
8. validate, commit and push only that branch;
9. when watched/integration-relevant paths changed, wait for the automatic shared **branch-push CI on the exact current branch SHA to finish `SUCCESS` before opening a new PR**; a PR or draft PR must not be the first CI attempt;
10. refresh `origin/main`; if the baseline moved, reconcile the same canonical carrier safely, push the reconciled branch and obtain fresh green branch CI before PR creation;
11. open/update the single canonical PR and include `Lane-Key`, canonical owner/session, canonical carrier and explicit supersession metadata; protected-main required checks and PR/integration CI then validate merge-candidate freshness as applicable;
12. stop before merge unless this session has explicit owner merge/integration authorization.

Historical Markdown work claims may still be used, but new/updated claim files belong on the task branch/PR. A claim does not need to be pushed to `main` before implementation starts.

An Issue plus pushed task branch is the preferred visible coordination surface before PR creation. The PR becomes the review/handoff surface only after the applicable branch-CI gate is green.

### Single-owner / single-carrier invariant

`docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` is mandatory for all concurrent agents and chat sessions.

- One Lane-Key has at most one ACTIVE owner, one canonical task branch and one open canonical PR.
- Stale, red, queued, behind or inconvenient work remains owned until explicitly released/superseded; another session must not create a cleaner competing carrier.
- If a replacement carrier is genuinely required, explicitly record supersession first and close the old open PR before the replacement is represented as canonical.
- Do **not** create branch-to-branch/internal PRs whose only purpose is to sync/replay `main` or another branch into the task branch. Reconcile the canonical task branch non-force, or rebuild one explicitly superseding carrier from current `main`.
- Umbrella audit Issues do not authorize multiple sessions to create equivalent concrete fixes. Every concrete implementation needs its own unique Lane-Key, and an equivalent active lane is an automatic stop.

A clean Git merge does not prove semantic non-overlap. Same production-file ownership or equivalent behavior remains a collision signal even when Lane-Keys differ.

## Mandatory sync discipline

Before starting a code or documentation change:

1. refresh/fetch the latest `origin/main`;
2. inspect relevant recent commits and concurrent PRs;
3. base work on the current valid task-branch baseline, not on an old conversation snapshot.

Before each branch push and before PR handoff:

1. refresh `origin/main` again;
2. verify whether relevant concurrent work moved;
3. if needed, rebase/reapply/merge safely on the task branch without discarding newer work;
4. review the final diff so it contains only intended changes;
5. for watched work, make sure the exact final branch SHA has fresh green branch CI before opening the PR.

Never force-push `main`, reset it backwards, silently overwrite another agent's work, or use `ours`/`theirs` blindly to hide semantic conflicts.

## Request-scoped commit batching

The repository owner prefers coherent commits scoped to the owner request/lane rather than a stream of tiny file-by-file commits.

- Treat one owner request or `continue all` lane as the default commit unit on the task branch.
- Accumulate related implementation, regression/static guards, docs and handoff updates into coherent commits.
- Split only when parts are genuinely independent or separately risky/revertable.
- If another agent lands overlapping work, review and reuse the winning implementation instead of committing a duplicate.
- Refresh `main` before final branch handoff/PR update.

## Normal agent stopping point

For a normal agent, the successful endpoint is generally:

```text
latest main read
  -> issue/reservation checked
  -> agent/<agent-id>/<scope>
  -> implementation/docs/chore commits
  -> validation
  -> branch pushed
  -> watched branch CI SUCCESS on exact branch SHA
  -> refresh/reconcile main if needed
  -> PR opened/updated
  -> protected-main/PR checks as applicable
  -> STOP BEFORE MERGE
```

An open PR, pushed branch, passing branch tests or completed task does not authorize merging it.

## Owner-authorized integration coordinator

Only an agent/session explicitly authorized by the owner may integrate/merge a named batch into `main`.

For multi-agent work, prefer:

```text
integration/<batch-id>
```

The authorized coordinator must:

1. refresh current `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches;
3. integrate all required commits without silently dropping work;
4. resolve semantic/API/test conflicts deliberately;
5. verify no required task remains only on an agent branch/unmerged PR;
6. run relevant combined-tree remote-safe validation;
7. inspect the combined diff for accidental reversions and duplicate implementations;
8. freeze and record the integration candidate SHA;
9. satisfy the active protected-main rules and merge to `main` only within explicit owner authorization;
10. fetch `main` again and record the exact resulting SHA.

Authorization to merge one batch is not standing authorization for later batches.

## Definition of `ALL MERGED TO MAIN`

State **ALL MERGED TO MAIN** only after an authorized integration reviewer verifies against current `main` that:

- every required Issue/reservation is terminal or explicitly excluded/superseded;
- every required implementation/docs commit is represented in current `main`;
- no required work exists only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- required branch/PR/integration evidence is green and fresh where applicable;
- current `main` was refreshed after the authorized landing;
- current `main` still reports the intended effective protected-main rules or an explicitly owner-approved replacement;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required remote-safe validation passed or environment-gated evidence is explicitly handed off;
- the exact current `main` SHA is recorded.

Branch deletion, Issue state, PR UI state or stale CI is not sufficient proof.

## Divide work by execution capability

### Local-machine agents

Agents with real/local access should prioritize work that genuinely requires that environment, such as:

- licensed BricsCAD V25/V26 runtime access;
- real `NETLOAD` / DemandLoad and interactive validation;
- Windows desktop/UI interaction and screenshots;
- proprietary SDK/runtime dependencies unavailable in GitHub;
- private DWG fixtures/assets;
- machine-specific crashes, DPI/layout behavior, file locks, signing credentials or hardware-specific behavior.

### Hard scope lock for `agent/local002` / `agent/local003`

The two local workers and successor sessions in those roles are restricted to **LOCAL_ONLY / local-agent-only** work.

They may implement/qualify only an item explicitly marked `LOCAL_ONLY`, `PENDING_LOCAL`, assigned in `docs/LOCAL-AGENT-INBOX.md`, assigned in a relevant reservation, or directly assigned by the owner as that exact local task.

Even for a LOCAL_ONLY item, local workers may edit implementation code only when the code change genuinely requires local/proprietary BricsCAD/AutoCAD/BLT3D/private-DWG/UI/runtime resources that remote agents cannot reproduce from repository source alone.

General source-safe bug fixing, tests, docs, refactors, source guards, packaging logic and ordinary adapter fixes belong to non-local agents unless the owner explicitly assigns otherwise.

If local validation discovers a normal source bug, capture the smallest sanitized evidence, hand off the defect, and stop coding at that boundary. A remote/source agent fixes it; the local worker may later resume LOCAL_ONLY validation against the new exact SHA.

Local workers must not:

- perform broad general bug hunting or opportunistic repository cleanup;
- treat GitHub Actions failures as their default backlog;
- dispatch/re-run/cancel GitHub Actions unless the owner explicitly assigns that exact CI operation to that local worker;
- broaden scope into remote-safe work when no compatible LOCAL_ONLY item exists.

Start permitted local passes from `docs/LOCAL-AGENT-INBOX.md`, then follow the linked exact runbook such as `docs/LOCAL-V25-QUALIFICATION.md` or the relevant preview/runtime document. Keep raw/private evidence under gitignored `artifacts/` and commit only sanitized summaries when allowed.

### Remote / hybrid online agents

Remote/hybrid agents handle repository-safe work including:

- source review and implementation;
- core/domain/persistence/reporting/test code;
- static analysis and code-quality fixes;
- general bug fixing, including source defects reported by local agents;
- Markdown/documentation/planning;
- workflow/policy review without unauthorized Actions dispatch;
- Git history inspection and multi-agent integration preparation;
- scripts/tests/probes for later local execution.

`docs/REMOTE-AGENT-SCOPE.md` is authoritative for remote backlog filtering. Remote agents must skip execution gates already classified LOCAL_ONLY rather than repeatedly rechecking them.

Remote agents may strengthen source contracts, deterministic tests and local probes around LOCAL_ONLY areas. If source changes alter a required local scenario, update `docs/LOCAL-AGENT-INBOX.md` **on the same task branch/PR** with the minimum exact local evidence requirement.

Remote/static evidence must never be reported as `LOCAL_PASS`.

## Unavailable-work handoff

If an agent cannot complete/prove work because it lacks local licensed runtime, private fixtures, Windows UI, signing credentials, hardware or another non-repository resource:

1. classify the blocked part as LOCAL_ONLY when appropriate;
2. update the matching `docs/LOCAL-AGENT-INBOX.md` item on the task branch/PR with the exact scenario, prerequisite, expected result and minimum evidence;
3. reference existing detailed runbooks instead of creating competing live queues;
4. leave source-safe implementation/tests/probes ready when possible;
5. continue other remote-safe work instead of repeatedly retrying the same unavailable gate.

Lack of local capability is a handoff condition, not a reason for repeated remote attempts.

## GitHub Actions / release

Follow `CI_POLICY.md` strictly.

- Workflows are manual-only by default.
- The shared non-publishing branch/PR CI in `.github/workflows/ci.yml` is an owner-approved automatic validation exception.
- For watched task branches, its branch-push run must be green on the exact final branch SHA before a new PR is opened.
- The sole owner-approved automatic publishing/dispatch exception is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` after an authorized integration-relevant `main` landing.
- Normal task authorization does not authorize manual workflow dispatch/re-run/cancel.
- Manual CI authorization does not imply `main` merge authorization.
- `main` merge authorization does not imply unrelated manual CI/release authorization.
- Ordinary docs/Markdown-only landings outside the dispatcher's watched paths must not trigger the V25 cloud release path.
- Changed paths, not `docs:`/`chore:` commit-message prefixes, determine automatic-dispatch eligibility.

For approved release operations, follow the applicable manual build/release runbook and resolve the exact commit/tag before dispatching.

## GitHub hard protection

GitHub ruleset **`protectedMain`** (ruleset ID **`20890901`**) is active on the default branch and is the current hard-enforcement layer for `main`.

The expected effective contract is:

- require PR-based updates to `main`;
- require stable status checks `preflight` and `core`;
- strict required-status freshness enabled;
- block force pushes / non-fast-forward updates;
- block deletion;
- bypass list empty.

Repository policy and GitHub hard protection are complementary. The ruleset prevents many invalid writes, while `docs/MAIN-WRITE-AUTHORIZATION.md` decides which session is allowed by the owner to merge.

When protection state matters, verify GitHub's effective rules instead of trusting Markdown alone. If the ruleset stops targeting `main`, required checks disappear, force-push/deletion protection is lost, or an unexpected bypass actor appears, treat it as a governance defect and do not claim hard protection is active.

See `docs/GITHUB-MAIN-PROTECTION.md` for the verification and recovery contract.

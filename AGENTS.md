# Agent Collaboration Policy

**POLICY_VERSION: 2026-08-21-v1**

This repository is expected to have multiple agents working concurrently. Every agent must protect other agents' work, avoid overlapping lanes, and choose tasks that match its actual execution environment.

## MUST READ / RUN THIS BOOTSTRAP ON EVERY OWNER PROMPT

For every prompt that asks an agent/chat session to change, continue, fix, validate, integrate, merge, release, update docs/Markdown, or otherwise advance repository work:

1. Read this `AGENTS.md` from current `origin/main`.
2. Read `docs/AGENT-RUNTIME-CONTRACT.md` from current `origin/main`.
3. Resolve current `origin/main` to an exact SHA; do **not** rely on chat memory for repository state or policy.
4. Check the requested scope against current Issues/Lane-Keys, owners/sessions, branches, PRs and active/blocking claims before mutation.
5. Reuse/continue the one canonical carrier when it already exists. An equivalent active carrier means `DUPLICATE_CARRIER / NO MUTATION` unless explicitly reassigned/superseded.
6. Direct task writes/ref updates/force pushes to `main` are forbidden. Normal task content lands through a dedicated branch + protected PR.
7. For normal owner-requested work, the default successful endpoint is **`MERGED_MAIN`** under `docs/MAIN-WRITE-AUTHORIZATION.md` unless the owner opts out for that exact task or a real terminal blocker remains.
8. Red current-carrier CI is an automatic diagnose/fix/push/recheck trigger while safe same-lane remediation exists.
9. Markdown-only does **not** mean no CI: ordinary docs may be lightweight; governance/policy Markdown may require source/policy guards. Changed paths are authoritative.
10. Full owner-facing lifecycle reporting is terminal-first. Normal success reports begin exactly with `✅ Prompt result: MERGED_MAIN`; blocker reports are permitted only when no further safe authorized action remains.

If older wording elsewhere conflicts with direct-main permission, standing same-task merge authorization, or the normal `MERGED_MAIN` endpoint, `docs/MAIN-WRITE-AUTHORIZATION.md` wins. If older wording treats branch-CI/PR timestamp order alone as permanent carrier validity, `docs/PR-CI-LIFECYCLE.md` wins.

## Highest-priority Git/Main rule

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for who may change `main` and for the default merge-completion endpoint of normal owner-requested repository tasks.

**Direct-write default:** every normal AI agent/chat session treats `origin/main` as read-only for direct task writes.

Requests such as the following never authorize a direct contents write, direct ref update, force push, protection bypass, or equivalent operation against `main`:

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
- `merge main`

There is **no docs/Markdown/chore direct-main exception**. Source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores all go to a dedicated task branch/PR.

For a normal repository-owner request, standing authorization applies to the **same task PR** after the task is fixed/validated, every current required check is green, the candidate is current and mergeable, and the owner has not explicitly opted out. The agent should then merge that same task PR through the protected PR path and verify the resulting `main` SHA without waiting for a second owner message.

This standing authorization never permits unrelated/bulk merges, bypassing required checks, weakening branch protection, force-pushing, or directly writing `main`.

## Locked product form: BricsCAD plugin

QS3D is a **BricsCAD V25 + V26 Windows x64 hosted plugin**, not a standalone CAD desktop executable. A matching licensed BricsCAD host is required at runtime; the native BricsCAD viewport/database/editor remain the CAD host.

V25 loads the `QS3D.BricsCAD.V25` Library/DLL built for `net48`; V26 loads the `QS3D.BricsCAD.V26` Library/DLL built for `net8.0-windows`. Each host-major assembly is loaded by the matching BricsCAD host through DemandLoad or `NETLOAD` and must never be relabeled across majors.

`BLT-like`, `BLT-style`, `BLT3D-familiar`, “giống BLT” and similar wording refer to clean-room workflow/UX familiarity only. Do not reinterpret them as a requirement for `QS3D.exe` or a QS3D-owned CAD engine.

`docs/PRODUCT-BOUNDARY.md` is authoritative unless the owner explicitly changes the product boundary.

## Mandatory reading order

Before substantive work, read from current `origin/main`:

1. `AGENTS.md`;
2. `docs/AGENT-RUNTIME-CONTRACT.md`;
3. `docs/MAIN-WRITE-AUTHORIZATION.md`;
4. `docs/PRODUCT-BOUNDARY.md`;
5. `CI_POLICY.md`;
6. `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`;
7. current `origin/main` exact SHA;
8. `docs/AGENT-WORK-REGISTRATION.md`;
9. `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`;
10. `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`;
11. `docs/PR-CI-LIFECYCLE.md`;
12. relevant open Issues/PRs plus `ACTIVE`/`BLOCKED` historical claims under `docs/agent-work-claims/`;
13. `docs/REMOTE-AGENT-SCOPE.md`;
14. the newest current handoff/status docs relevant to the task;
15. `docs/LOCAL-AGENT-INBOX.md` for LOCAL_ONLY work;
16. the exact feature/runbook documents required by the assigned lane.

Current source wins over stale historical handoffs for implementation truth. `docs/LOCAL-AGENT-INBOX.md` is the live LOCAL_ONLY priority index when older local documents disagree on status/priority.

## Mandatory work registration and single-carrier rule

Before implementation, every normal agent must:

1. fetch/read current `origin/main`;
2. inspect relevant Issues, PRs, branches and active/blocking claims;
3. choose a non-overlapping lane and determine its stable **Lane-Key**, normally `issue-<number>`;
4. identify the one current canonical owner/carrier for that Lane-Key;
5. create/update a GitHub Issue for the lane when practical, unless an existing owner-created issue already uniquely identifies the task;
6. create a dedicated branch from the latest valid baseline, normally `agent/<agent-id>/<scope>`, only when no canonical carrier already owns the Lane-Key;
7. put **all** task changes on that one canonical branch;
8. validate, commit and push only that branch;
9. observe/remediate any known red exact-head branch CI on the same carrier;
10. open/continue the single canonical PR with Lane-Key, owner/session, carrier and supersession metadata;
11. refresh/reconcile `origin/main` when needed and obtain fresh current-candidate evidence;
12. for a normal owner-requested task, merge the same PR through protected `main` once all current required gates are satisfied unless the owner explicitly opted out.

Single-owner/single-carrier invariants:

- one Lane-Key has at most one ACTIVE owner, one canonical branch and one open canonical PR;
- stale, red, queued, behind or inconvenient work remains owned until explicitly released/superseded;
- if replacement is genuinely required, record supersession and close the old open PR before representing a replacement as canonical;
- do not create branch-to-branch/internal PRs merely to sync/replay `main` or another branch into a task branch;
- umbrella/control Issues do not authorize duplicate concrete implementation lanes;
- a clean Git merge does not prove semantic non-overlap.

## Markdown / docs execution path

`Markdown-only` does **not** imply `no CI`. Changed paths, not commit prefixes such as `docs:`, `md:` or `chore:`, determine validation and release impact.

### ORDINARY_DOCS

Ordinary guidance/notes/claims/handoffs outside the policy/source-guard watched set:

- remain branch + PR work;
- may use the lightweight non-build shared-CI path;
- may omit heavy pre-PR source/build validation when the path is intentionally outside that watched set;
- do not require Core/V25 build or licensed BricsCAD runtime merely because `.md` changed;
- still require protected current-candidate `preflight` + `core` before merge.

### GOVERNANCE_POLICY_MD

Policy Markdown explicitly classified by `.github/workflows/ci.yml`, including `AGENTS.md`, `CI_POLICY.md`, `README.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/AGENT-STATUS-MARKER-SEMANTICS.md`, and `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`:

- remains branch + PR work;
- must run the policy/source guards selected by shared CI;
- does **not** require Core/V25 build unless another build-relevant path changed;
- must not be expanded into scripts/workflows/source merely to make a documentation clarification appear more enforceable unless executable enforcement is explicitly part of the task.

Ordinary docs/Markdown/chore-only landings outside the V25 dispatcher's watched integration-relevant paths must not trigger the V25 cloud release path.

## Branch CI and PR timing

Every `agent/**` / `integration/**` push is eligible for the owner-approved shared non-publishing CI. Inspect/remediate known red exact-head results.

For watched work, prefer exact-head branch-CI success before opening a new PR when the current admission gate requires it. However, branch-CI completion timestamp is **not** permanent PR identity:

- a canonical PR may coexist with queued/running branch CI;
- completion after PR creation does not poison the PR;
- later same-carrier remediation may change the head SHA;
- do not close/recreate a PR or branch merely to make timestamps look ordered;
- revalidate the current candidate and keep the same canonical carrier unless there is a real ownership/scope reason to supersede it.

Protected current-candidate `preflight` + `core`, strict freshness, mergeability/collision checks and expected-head protection are the merge gate.

## Mandatory continuation and terminal-first reporting

Every owner prompt that asks to change, continue, fix, validate, integrate, merge or release work is a continuation of the **current canonical GitHub lifecycle**, not a fresh isolated attempt.

These states are distinct:

```text
edited locally
  != committed
  != pushed branch
  != branch CI green
  != PR ready/open
  != PR/protected candidate green
  != merged to main
  != exact-main validated
  != released/published
```

Do **not** emit a full owner-facing lifecycle completion report merely because an intermediate state was reached. Continue the same canonical carrier while safe authorized actions remain.

Full lifecycle reporting is required when:

1. success reaches `MERGED_MAIN` (or a stricter endpoint the owner explicitly requested); or
2. a legitimate blocker leaves no further safe authorized action in the current execution.

Normal success report begins exactly:

```text
✅ Prompt result: MERGED_MAIN
```

Legitimate blocker reports begin exactly:

```text
❌ Prompt result: BLOCKED
```

Use the complete mandatory fields/forms in `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`. Queued/running CI, a red-but-fixable current carrier, an open PR, review feedback that can be fixed, or a stale branch that can be reconciled are not terminal blockers by themselves.

## Mandatory sync discipline

Before starting a code or documentation change and again before final handoff/merge decisions:

1. refresh current `origin/main`;
2. inspect relevant recent commits and concurrent PRs;
3. verify the current canonical branch/head;
4. reconcile safely on the task branch if `main` moved;
5. review the final diff so it contains only intended scope;
6. obtain fresh applicable exact-head/protected evidence after any candidate change.

Never force-push `main`, reset it backwards, silently overwrite another agent's work, or use `ours`/`theirs` blindly to hide semantic conflicts.

## Request-scoped commit batching

The repository owner prefers coherent commits scoped to the owner request/lane rather than a stream of tiny file-by-file commits.

- Treat one owner request or `continue all` lane as the default commit unit on the task branch.
- Accumulate related implementation, regressions/guards, docs and handoff updates into coherent commits.
- Split only when parts are genuinely independent or separately risky/revertable.
- If another agent lands overlapping work, review/reuse the winning implementation instead of committing a duplicate.

## Normal owner-task endpoint

For a normal repository-owner task, the successful path is generally:

```text
latest main read
  -> issue/reservation checked
  -> one canonical agent branch
  -> implementation/docs/chore commits
  -> appropriate validation
  -> branch pushed; exact-head CI observed/remediated when applicable
  -> canonical PR opened/continued
  -> refresh/reconcile main if needed
  -> protected current-candidate preflight + core SUCCESS
  -> current + mergeable + expected-head verified
  -> merge same task PR under MAIN-WRITE-AUTHORIZATION
  -> refresh resulting main SHA
  -> MERGED_MAIN
```

An owner may explicitly opt a task out with `PR only`, `do not merge main`, `stop before merge`, `đừng merge`, or clear equivalent wording. A non-owner contribution context that has no applicable owner standing authorization stops at its authorized PR boundary.

## Owner-authorized integration coordinator

Standing same-task authorization does not permit unrelated/bulk integration. A named multi-agent batch or unrelated PR set requires the applicable owner integration authorization.

For multi-agent work, prefer `integration/<batch-id>`. The authorized coordinator must refresh current `main`, identify exact participating lanes, integrate without silently dropping work, resolve semantic/API/test conflicts deliberately, validate the combined candidate, satisfy protected current-candidate rules, merge only the authorized batch, and then record the resulting `main` SHA.

## Divide work by execution capability

### Local-machine agents

Agents with real/local access should prioritize work that genuinely requires that environment: licensed BricsCAD V25/V26 runtime, real `NETLOAD`/DemandLoad, Windows desktop/UI interaction, proprietary SDK/runtime dependencies unavailable in GitHub, private DWG fixtures/assets, signing credentials, or machine-specific behavior.

### Hard scope lock for `agent/local002` / `agent/local003`

The two local workers and successor sessions in those roles are restricted to **LOCAL_ONLY / local-agent-only** work. They may implement/qualify only an item explicitly marked `LOCAL_ONLY`, `PENDING_LOCAL`, assigned in `docs/LOCAL-AGENT-INBOX.md`, assigned in a relevant reservation, or directly assigned by the owner as that exact local task.

General source-safe bug fixing, tests, docs, refactors, source guards, packaging logic and ordinary adapter fixes belong to non-local agents unless the owner explicitly assigns otherwise. If local validation discovers a normal source bug, capture sanitized evidence and hand it off; a remote/source agent fixes it, and the local worker may resume LOCAL_ONLY validation against the new exact SHA.

Local workers must not perform broad opportunistic cleanup, treat Actions failures as their default backlog, dispatch/re-run/cancel Actions without exact owner assignment, or broaden into remote-safe work when no compatible LOCAL_ONLY item exists.

### Remote / hybrid online agents

Remote/hybrid agents handle repository-safe source review/implementation, core/domain/persistence/reporting/test code, static analysis, general source bugs, Markdown/documentation/planning, workflow/policy review without unauthorized Actions dispatch, Git history inspection, and probes for later local execution.

`docs/REMOTE-AGENT-SCOPE.md` is authoritative for remote backlog filtering. Remote/static evidence must never be reported as `LOCAL_PASS`.

## Unavailable-work handoff

When a required part cannot be completed/proved because the session lacks licensed runtime, private fixtures, Windows UI, signing credentials, hardware or another non-repository resource, classify/handoff the unavailable part precisely, update the relevant local inbox/runbook reference on the task branch when appropriate, leave source-safe implementation/tests/probes ready, and continue other safe work. Lack of local capability is a handoff condition, not a reason for repeated remote retries.

## GitHub Actions / release

Follow `CI_POLICY.md` and use `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md` for CI self-observation/evidence recovery.

- Workflows are manual-only by default.
- `.github/workflows/ci.yml` shared branch/PR CI is an owner-approved automatic validation exception.
- AI agents must exhaust repository-native CI evidence-recovery routes before asking the owner for routine run/check information.
- The owner-approved automatic publishing/dispatch exception is `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` after an authorized integration-relevant `main` landing.
- Normal task authorization does not authorize unrelated manual workflow dispatch/re-run/cancel.
- Manual CI authorization does not imply unrelated merge authorization.
- Ordinary docs/Markdown-only landings outside dispatcher watched paths must not trigger V25 cloud release.
- Changed paths, not commit-message prefixes, determine automatic-dispatch eligibility.

## GitHub hard protection

GitHub ruleset **`protectedMain`** (ruleset ID **`20890901`**) is the expected hard-enforcement layer for `main`:

- require PR-based updates to `main`;
- require stable status checks `preflight` and `core`;
- strict required-status freshness enabled;
- block force pushes/non-fast-forward updates;
- block deletion;
- bypass list empty.

Repository policy and GitHub hard protection are complementary. Verify effective rules when protection state matters. If required protection disappears or an unexpected bypass appears, treat that as a governance defect and do not bypass it.

See `docs/GITHUB-MAIN-PROTECTION.md` for verification/recovery details.

## Definition of `ALL MERGED TO MAIN`

State **ALL MERGED TO MAIN** only after the authorized integration reviewer verifies against current `main` that required lanes are terminal or explicitly excluded/superseded, all required work is reachable from current `main`, no required work remains only off-main, required evidence is green/fresh where applicable, current protection remains valid, the combined tree has no known semantic/API/test collision or accidental reversion, and the exact current `main` SHA has been recorded.

Branch deletion, Issue state, PR UI state or stale CI alone is not sufficient proof.

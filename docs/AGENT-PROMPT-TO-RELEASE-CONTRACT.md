# Agent prompt-to-release continuation and reporting contract

This document supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/LOCAL-ONLY-RUNTIME-REPORTING.md`, and `CI_POLICY.md`.

`docs/MAIN-WRITE-AUTHORIZATION.md` remains authoritative for same-task merge authorization and the normal `MERGED_MAIN` completion endpoint. This file is authoritative for **owner-facing lifecycle reporting behavior**. When older wording in `AGENTS.md`, historical handoffs, claims, or this file's previous revisions requires a per-prompt intermediate status report, allows a routine task to stop at queued/running/red CI, or otherwise encourages report-first behavior, the terminal-first rules below win.

## Owner intent: action first, terminal state first

An owner prompt to change, continue, fix, validate, integrate, merge, or release repository work is an instruction to **perform the work and advance the one canonical GitHub lifecycle**, not an instruction to narrate each intermediate state.

For normal owner-requested repository work, the owning agent/session must keep advancing the same canonical carrier while safe authorized actions remain. The normal successful endpoint is `MERGED_MAIN` unless the owner explicitly opts out of merge for that exact task or a stricter release/runtime acceptance is explicitly part of the prompt.

The following are lifecycle states, not acceptable self-selected completion points:

```text
edited locally
  != committed
  != pushed branch
  != branch CI green
  != PR ready/open
  != PR CI/protected candidate green
  != merged to main
```

**Reporting is not a substitute for execution.** Discovering a bug, failed test, red CI check, stale branch, review feedback, merge conflict, or missing regression is normally an automatic action trigger. If the agent can safely fix or reconcile it inside the owned lane, it must do so immediately, commit/push the remediation, re-check the exact current evidence, and continue toward the terminal endpoint.

## Mandatory continuation check

Before mutation, determine the current canonical lifecycle from GitHub rather than from chat memory:

1. Fetch/read exact current `origin/main`.
2. Search for the semantically matching Issue/Lane-Key.
3. Search for the canonical branch and open PR for that lane.
4. Check the minimum active claim/reservation metadata required to detect ownership collision.
5. Determine whether the requested behavior already landed on current `main`.
6. Continue the existing canonical carrier when this session owns it; do not create a duplicate carrier merely because the existing branch is stale, red, queued, or inconvenient.
7. If no equivalent carrier exists, create/reuse one Issue, one Lane-Key, and one canonical task branch from the latest valid baseline.

Use semantic behavior, expected files/symbols, acceptance criteria, Issue metadata, and current GitHub state—not titles alone—to decide whether work is the same lane.

## One lane, one carrier

- One Lane-Key has at most one active owner, one canonical task branch, and one open canonical PR.
- Another active canonical owner means `DUPLICATE_CARRIER / NO MUTATION` unless the owner/coordinator explicitly reassigns the lane.
- Red, stale, behind, queued, or incomplete work remains owned; those states do not authorize a replacement carrier.
- Reconcile the same carrier safely and non-force when `main` moves.
- Never broaden the current Lane-Key merely to absorb unrelated defects discovered during the work. Register a separate lane when policy permits and when the finding is genuinely separate.

## Required delivery sequence

For normal owner-requested work, follow the repository-safe path continuously:

```text
owner prompt
  -> current main + collision check
  -> continue/register one canonical carrier
  -> implement/fix + regression coverage/docs as needed
  -> validate within actual capability
  -> coherent commit(s)
  -> push canonical branch
  -> exact-head branch CI SUCCESS when required
  -> refresh/reconcile current main
  -> fresh exact-head branch CI when reconciliation changes the candidate
  -> open/update one canonical PR
  -> protected PR preflight + core SUCCESS on the current candidate
  -> re-check freshness/mergeability/review blockers
  -> merge the same task PR under MAIN-WRITE-AUTHORIZATION
  -> refresh and record resulting main SHA
  -> MERGED_MAIN
```

A watched branch must not use a new PR as its first CI attempt. A stale or failed earlier run is never evidence for the new exact head.

## CI is agent-owned work, not owner homework

The owning agent/session is responsible for checking the applicable CI state itself through the available GitHub/Actions tooling. Do not tell the owner to check CI, paste logs, press refresh, retry a routine gate, or determine whether checks are green when the session has a tool surface that can obtain that evidence.

For every required branch/PR check:

1. Bind the observation to the exact current branch/PR head or merge candidate.
2. Inspect the terminal result and, for failures, the exact failing job/step/log evidence available.
3. If the check is queued/running and the current execution can continue observing it, keep observing/advancing the same lifecycle instead of emitting an owner-facing lifecycle report merely because the gate is pending.
4. Do not bypass admission gates, invent success, reuse stale green evidence, manufacture no-op commits, or manually dispatch/re-run/cancel workflows unless separately authorized by `CI_POLICY.md`.
5. If the available execution environment genuinely cannot observe a required gate at all and no other safe authorized progress remains, that tooling/observability boundary may be reported as a blocker with exact attempted evidence. It must not be disguised as CI success or failure.

A pending CI gate is ordinarily **ACTIVE work**, not a terminal outcome.

## Mandatory bug and red-CI self-remediation loop

A fixable defect or failed repository-safe check on the current owned carrier is an automatic remediation trigger.

When implementation review, local deterministic validation, branch CI, PR/protected checks, or merge-candidate validation exposes a defect inside the current lane:

1. Verify the exact current failing evidence; never diagnose a stale SHA as the current head.
2. Identify the root cause before editing.
3. Fix the root cause on the same canonical branch. Add or strengthen regression coverage/source guards when the defect demonstrates a missing invariant.
4. Commit and push the remediation to the same carrier.
5. Re-run/re-observe the repository-authorized automatic validation for the new exact head/candidate.
6. If it fails again for another safely fixable cause, repeat diagnosis -> fix -> commit/push -> revalidate.
7. If `main` moved, reconcile safely and obtain fresh evidence before PR/merge when required.
8. Continue through PR and merge once all gates are green/current/mergeable.

**Do not stop after merely finding or reporting a fixable bug. Do not stop after the first failed CI attempt. Do not ask the owner to repeat `fix`, `continue`, `check CI`, or `merge main` when the same-task authorization and tooling already permit the next action.**

## Legitimate terminal blocker boundary

A normal task may produce an owner-facing blocker report before `MERGED_MAIN` only when no safe authorized remediation/progress remains in the current execution. Examples include:

- another canonical owner/carrier owns the same Lane-Key and mutation would violate collision policy;
- an owner-only decision or authorization is genuinely required and is not already present;
- a required secret, third-party service, licensed/private runtime, signing credential, hardware capability, or other non-repository dependency is unavailable and is an explicit acceptance gate;
- GitHub protection rejects the candidate and no safe current-lane remediation remains;
- the available GitHub/tooling surface cannot perform or observe a required action/evidence and all permitted fallback paths have actually been attempted;
- the defect has been investigated and cannot be safely fixed inside the current lane without violating ownership, scope, product boundary, or repository policy.

A blocker report must state the exact blocker, what was attempted, the last exact Git/CI evidence, and why no safe authorized remediation remains. `CI is running`, `CI is red`, `branch is behind`, `PR is open`, or `review found a bug` are **not** terminal blockers by themselves.

## Terminal-only owner-facing lifecycle reporting

For normal owner-requested repository work, suppress routine lifecycle/status reports until one of these two conditions is true:

1. **Success terminal:** the requested repository work reached `MERGED_MAIN` (or a stricter explicitly requested release/runtime terminal state); or
2. **Blocker terminal:** a legitimate blocker under the section above prevents all further safe authorized progress.

This terminal-only rule replaces the previous requirement to emit a full lifecycle table at the end of every prompt. It also replaces previous wording that allowed ending a prompt merely because branch/PR CI was queued or running.

Intermediate progress may still be recorded in GitHub Issues/PRs/commits as repository evidence. Brief execution-environment progress updates may also be emitted when the surrounding tool/runtime requires them, but they are not owner-facing lifecycle reports and must not become a substitute for continuing the work.

### Successful terminal report

After `MERGED_MAIN`, keep the owner-facing report concise and evidence-based:

```text
✅ Prompt result: MERGED_MAIN
✅ Issue / Lane-Key: #<number> / issue-<number>
✅ Canonical branch: <branch>
✅ Final task head: <sha>
✅ Branch CI: SUCCESS — <run + tested sha, when applicable>
✅ PR: #<number> — MERGED
✅ Protected checks: SUCCESS — <candidate/run>
✅ Merged to main: YES — main@<landed sha>
```

Omit fields that are genuinely not applicable. Do not add release/version/runtime status unless it is part of the current prompt's acceptance or is the actual blocker.

### Blocker terminal report

When no safe progress remains:

```text
❌ Prompt result: BLOCKED
✅ Issue / Lane-Key: #<number> / issue-<number>
✅ Canonical branch/head: <branch>@<sha>
<marker> Last verified CI/PR evidence: <exact evidence>
❌ Exact blocker: <specific external/authorization/tooling/ownership/unfixable condition>
❌ Remediation attempted: <what was actually tried>
➖ Further safe action: none in current execution — <why>
```

Do not label a merely pending or fixable state as `BLOCKED`.

## Visual status markers

When a terminal report is emitted, each lifecycle line begins with:

- `✅` verified satisfied/successful;
- `❌` verified failed/blocked/unsatisfied;
- `⏳` genuinely pending only when pending state is itself part of a legitimate terminal external blocker explanation;
- `➖` not applicable.

Do not use markers to create an intermediate status dump while executable work remains.

## Merge authorization boundary

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative. Its standing owner instruction requires a normal owner-requested task PR to merge itself once required branch/PR checks are green, current, and mergeable, unless the owner explicitly opts out for that exact task.

This authorization is PR-only. It never permits direct contents writes/ref updates to `main`, force-push, protection bypass, merging unrelated PRs, or weakening required checks.

## Release and LOCAL_ONLY reporting boundary

After an authorized merge, ordinary code/docs/chore tasks are complete at `MERGED_MAIN` unless release/publication/package/deployment or licensed/local runtime evidence is explicitly part of the owner's acceptance.

- Do not append routine `release pending` bookkeeping to an otherwise completed merged fix.
- Do not claim `RELEASED` without exact publication evidence.
- Remote/source-only agents never infer `LOCAL_PASS`.
- Parked LOCAL_ONLY evidence is not an owner-facing blocker unless the prompt explicitly makes that evidence a completion gate.

## Durable owner corrections

When the owner corrects how agents should work, report, continue, merge, release, or communicate, treat a durable correction as repository policy work rather than a chat-only promise.

- Persist the correction in the canonical policy Markdown on the same task carrier.
- Do not substitute `từ giờ mình sẽ...`, `noted`, or a status explanation for actually updating policy.
- If the current session can perform the next lifecycle action, perform it instead of assigning the owner unnecessary work.
- Ask the owner only for input that is genuinely required and unavailable.

## Completion wording

Use completion language precisely:

- `MERGED_MAIN`: protected PR merge completed and refreshed current `main` contains the work.
- `RELEASED`: only when release is explicitly in scope and exact publication evidence is verified.
- `BLOCKED`: no safe authorized remediation/progress remains for a concrete external/authorization/tooling/ownership/unfixable condition.
- `DUPLICATE_CARRIER`: another canonical owner/carrier owns the lane; no overlapping mutation was performed.

`BRANCH_GREEN`, `PR_OPEN`, `PR_GREEN`, queued CI, red CI under active remediation, and stale-branch states are internal lifecycle states only. They are **not normal owner-facing completion reports** and are not valid self-selected stopping points.

## Success criterion for repeated prompts

Repeated prompts about one feature advance the same canonical carrier toward the requested terminal state. A future agent must be able to read GitHub and determine the Issue/Lane-Key, canonical branch/PR, exact current head and CI evidence, whether the work landed on current `main`, and the exact blocker only if the task genuinely could not be completed.

Traceability supports execution; it must never replace execution.
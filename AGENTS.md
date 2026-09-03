# Agent Collaboration Policy

**POLICY_VERSION: 2026-08-30-v2**

This is the everyday operating contract for AI/agent work in `QS3D-BricsCAD`.

The goal is simple: understand the owner's prompt, reuse or create one safe task carrier, implement the requested outcome, fix current-lane failures, pass the protected PR gates, and merge the same task PR when eligible unless the owner explicitly opts out.

## Mandatory reading order

For a normal owner prompt, do **not** preload a large stack of governance Markdown.

Start with only:

1. this `AGENTS.md` from current `origin/main`;
2. the exact current `origin/main` SHA;
3. the current Issue/Lane-Key/branch/PR for the requested work, if one already exists.

Then read a specialist document only when the task actually needs it:

- main/merge authorization → `docs/MAIN-WRITE-AUTHORIZATION.md`;
- CI behavior or a CI failure → `CI_POLICY.md`;
- new/concurrent agent reservation → `docs/AGENT-RESERVATION-V2.md`;
- detailed work registration → `docs/AGENT-WORK-REGISTRATION.md`;
- LOCAL_ONLY / licensed BricsCAD work → `docs/REMOTE-AGENT-SCOPE.md` and `docs/LOCAL-AGENT-INBOX.md`;
- MCP / ChatGPT / Cloudflare / host automation → `docs/MCP-CANONICAL-RUNBOOK.md`;
- product boundary → `docs/PRODUCT-BOUNDARY.md`;
- release → the relevant release runbook.

Historical claims, dated audits, plans and handoffs are evidence/history. They are **not** a second source of governance truth.

## 2. Precedence

When current documents disagree:

1. explicit current owner instruction for the named task;
2. `docs/MAIN-WRITE-AUTHORIZATION.md` for main/merge authority and the normal task endpoint;
3. this `AGENTS.md` for everyday lifecycle behavior;
4. `CI_POLICY.md` for CI semantics;
5. `docs/AGENT-RESERVATION-V2.md` for reservation/collision rules;
6. specialist runbook for the applicable feature/environment.

Stale wording such as `manual-only`, `stop before merge`, or `branch CI must finish before PR creation` in historical/directed material does not override the current canonical contracts above.

## 3. Understand the owner's requested outcome

Treat the owner's prompt as an instruction to **perform the requested work**, not merely to report or propose steps.

Examples such as:

- `fix bug`;
- `continue all`;
- `implement all`;
- `review and fix`;
- `update code`;
- `fix CI`;
- `commit push git`;

mean: advance the owned task as far as safely possible through the repository lifecycle.

Do not invent unrelated work merely because the prompt is broad. Use current repository evidence, exact failures, source behavior and the user's stated goal.

### Carrier boundary

Use **one cohesive, independently reviewable/revertible outcome = one carrier**.

Related implementation steps, regressions, tests and docs stay together. Genuinely independent defects with separate root causes/revert/risk boundaries may use separate lanes even if discovered from one broad prompt. Do not create a mega-PR solely because the user said `fix all`, and do not create micro-PRs for every file.

## 4. Main is PR-only

Direct task writes to `main` are forbidden.

Never:

- write file contents directly to `main`;
- update the `main` ref directly;
- force-push `main`;
- bypass protected checks;
- weaken protection to land a task.

All source, tests, scripts, workflows, docs, Markdown, claims, handoffs and chores use a dedicated task branch and protected PR.

## 5. One lane, one canonical carrier

Before mutation:

1. refresh current `main`;
2. find a semantically equivalent open Issue/Lane-Key/branch/PR;
3. continue that carrier if this session owns it;
4. if another active owner owns it, stop overlapping mutation as `DUPLICATE_CARRIER / NO MUTATION`;
5. if no equivalent carrier exists, register one Issue/reservation and one branch.

For Reservation v2 work, use the branch form:

```text
agent/<globally-distinct-session-token>/issue-<N>-<short-scope>
```

One Lane-Key has at most one active owner, one canonical task branch and one open canonical PR.

A red, stale, queued, behind-main or inconvenient carrier remains the canonical carrier until completed, explicitly released, reassigned or superseded. Do not replace it merely to get cleaner CI/history.

## 6. Normal implementation lifecycle

The default owner-task lifecycle is:

```text
owner prompt / bug
  -> refresh current main
  -> find/reuse or register one carrier
  -> implement/fix
  -> focused local/static tests available to the session
  -> coherent commit(s)
  -> push canonical branch
  -> automatic branch CI starts
  -> open/update the canonical PR when ready for protected review
  -> diagnose/fix any known red current-lane evidence on the same branch
  -> protected PR `preflight` + `core` SUCCESS
  -> satisfy strict freshness + mergeability + collision checks
  -> merge same task PR under MAIN-WRITE-AUTHORIZATION
  -> refresh and verify resulting main SHA
  -> close/complete the task Issue and release the reservation
  -> delete the merged task branch when practical
  -> MERGED_MAIN
```

Do not stop at `edited`, `committed`, `pushed`, `branch green`, `PR open`, or `PR green` when the next authorized action is available.

## 7. Branch CI and PR timing

Automatic branch CI is **early exact-head evidence**, not permanent PR identity.

Preferred low-churn flow is push → observe branch CI → open/update PR when the task is ready. However:

- a canonical PR may coexist with queued/running branch CI;
- branch-CI completion after PR creation does not poison the PR;
- never close/recreate a correct PR merely to repair timestamps;
- a known red branch run must be diagnosed and fixed on the same carrier;
- stale green evidence never qualifies a new head;
- protected current-candidate `preflight` + `core`, freshness and mergeability are the hard merge gate.

See `CI_POLICY.md` and `docs/PR-CI-LIFECYCLE.md` for detail.

## 8. Red CI and bug remediation are agent work

For a fixable current-lane failure:

```text
exact failure
  -> root cause
  -> fix same canonical branch
  -> add/strengthen regression coverage when appropriate
  -> commit + push
  -> re-observe fresh CI
  -> repeat until green or a real blocker remains
```

Do not ask the owner to inspect routine CI, paste logs, repeat `fix`, repeat `continue`, or repeat `merge main` when the available GitHub tooling can do the next step.

Never make unrelated correctness/security regressions just to turn CI green.

## 9. Same-task merge authorization

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative.

For a normal owner-requested task, once the same task PR is current, mergeable, collision-clean and every required protected check is green, the owning agent should merge that PR through the protected PR path without waiting for a second owner message.

The owner may opt out for that task with wording such as `PR only`, `do not merge main`, `stop before merge`, `đừng merge`, or clear equivalent wording.

Standing same-task authorization never permits unrelated/bulk merges or direct-main writes.

## 10. Reservation terminal cleanup

A task is not fully cleaned up merely because code landed.

After verifying the merged result on current `main`:

1. close/complete the task Issue when it is still open;
2. thereby release its active reservation;
3. update any narrowly required handoff/inbox state;
4. delete the merged task branch when practical.

Do not leave an Issue marked ACTIVE after its canonical implementation has reached `MERGED_MAIN`.

## 11. LOCAL_ONLY boundary

Remote/source agents finish all repository-safe implementation, deterministic guards/tests, docs and available remote validation first.

Remote agents must skip execution gates already classified LOCAL_ONLY rather than repeatedly rechecking them.

Only the remaining execution/evidence that genuinely requires licensed BricsCAD, private DWG, Windows UI, signing credentials, proprietary dependencies or another machine-only capability is LOCAL_ONLY.

Park new/materially changed local work in `docs/LOCAL-AGENT-INBOX.md` with the exact pushed SHA. Register that handoff on the same task branch/PR so source truth and local evidence stay bound to one candidate. Remote/static evidence must never be called `LOCAL_PASS`.

A parked LOCAL_ONLY item does not block an otherwise eligible repository merge unless that exact local evidence is explicitly part of the current task's acceptance.

## Unavailable-work handoff

Start permitted local passes from `docs/LOCAL-AGENT-INBOX.md` and follow the linked exact runbook for the selected item. For licensed BricsCAD V25 qualification use `docs/LOCAL-V25-QUALIFICATION.md`. Do not manufacture local PASS from remote/static evidence, and do not create duplicate handoffs for the same exact candidate/scenario.

## 12. Cross-agent non-interference

Inspect only the minimum metadata needed to avoid collision with another active lane. Do not take over another agent's work merely because it is slow/red/stale.

A clean Git merge does not prove semantic non-overlap.

Broader coordination, unrelated PR review or multi-agent batch integration requires the applicable owner assignment/coordinator scope.

## 13. Validation truth

Bind every claimed result to the exact SHA/candidate that produced it.

Never:

- reuse stale green CI for a newer head;
- report a quoted failure as reproduced when it was not observed;
- report licensed/runtime PASS from static evidence;
- hide a known red branch failure behind an open PR;
- weaken a guard solely to make the current candidate pass.

## 14. Reporting

Action first; terminal state first.

Normal successful repository work ends with a concise report beginning:

```text
✅ Prompt result: MERGED_MAIN
```

Include the Issue/Lane-Key, canonical branch/head, PR, protected-check evidence and resulting `main` SHA when applicable.

Use a blocker report only when no further safe authorized action remains. Pending or fixable CI is not by itself a terminal blocker.

If an intermediate progress update is emitted, describe a pending CI gate with exact available run/job/step detail; the existence of pending CI does not itself force a lifecycle status dump.

## 15. Product boundary

Locked product form: BricsCAD plugin. QS3D remains a **BricsCAD V25 + V26 Windows x64 hosted plugin**. Do not reinterpret workflow language as a request to create a separate CAD engine/standalone product unless the owner explicitly changes the product boundary. See `docs/PRODUCT-BOUNDARY.md`.

For MCP/ChatGPT/host automation work, read `docs/MCP-CANONICAL-RUNBOOK.md` before changing MCP source.

---

**Everyday rule:** read this file, resolve current GitHub truth, then do the user's requested work on the one safe carrier. Specialist rules are loaded only when the task needs them.
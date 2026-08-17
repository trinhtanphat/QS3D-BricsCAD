# Agent prompt-to-release continuation and reporting contract

This document supplements `AGENTS.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, and `CI_POLICY.md`.

Its purpose is to prevent repeated AI/agent/chat prompts from producing disconnected edits that never become a validated PR, never reach `main`, or are reported as complete before the required release evidence exists.

## Owner intent

A new owner prompt about an existing feature, bug, UI surface, workflow, or release is normally a request to **continue the one canonical GitHub carrier for that work**, not permission to start another independent implementation.

Every agent/chat session must determine the exact lifecycle state of the requested work before mutation and must leave the work in a visible GitHub state before claiming progress.

The lifecycle is not complete merely because code was edited. The following are distinct states and must never be conflated:

```text
edited locally
  != committed
  != pushed branch
  != branch CI green
  != PR ready/open
  != PR CI/protected candidate green
  != merged to main
  != exact-main validated
  != released/published
```

## Mandatory continuation check for every owner prompt

Before changing source, tests, docs, workflows, configuration, or release metadata for a requested behavior:

1. Fetch/read the exact current `origin/main` SHA.
2. Search for a semantically matching open or recently relevant GitHub Issue and determine the stable Lane-Key.
3. Search for the canonical branch for that Lane-Key or equivalent behavior.
4. Search for the canonical open PR and its current head SHA/status.
5. Check the minimum active claim/reservation metadata needed to detect another owner/session.
6. Determine whether the requested behavior already landed on current `main`.
7. Determine whether applicable CI, exact-main validation, packaging, or release work is still pending.

Use semantic behavior, Lane-Key, expected files/symbols and acceptance criteria—not only titles or branch names—to decide whether work is the same lane.

## Existing carrier wins

If the requested work already has a valid canonical Issue/branch/PR carrier:

- continue or update that carrier when this session is its authorized owner;
- do not create a second Issue, branch, implementation or PR for the same lane;
- if another session owns it, stop overlapping mutation as `DUPLICATE_CARRIER / NO MUTATION` unless the owner/coordinator explicitly reassigns it;
- if the carrier is behind `main`, red, queued, stale-looking or incomplete, that does not release ownership;
- if the implementation already landed on current `main`, do not recreate it; inspect current `main` and create a narrowly scoped follow-up lane only for a real remaining gap.

A repeated owner prompt does not reset GitHub state and does not create a new lane automatically.

## No existing carrier

If no equivalent active carrier exists:

1. create or reuse one uniquely identifying GitHub Issue;
2. assign the stable Lane-Key, normally `issue-<number>`;
3. create exactly one dedicated canonical task branch from the latest valid `main` baseline;
4. record scope, exclusions, expected validation and carrier identity;
5. implement all related code/tests/docs on that branch;
6. validate, commit and push real changes to that branch.

An unpushed local edit or chat-only explanation is not a completed task and is not a visible reservation.

## Required delivery sequence

For watched/integration-relevant changes, the normal delivery path is:

```text
owner prompt
  -> current main + semantic Issue/branch/PR collision check
  -> continue existing canonical carrier OR register one new carrier
  -> implement + regression coverage/docs as needed
  -> validate locally/remotely within actual capability
  -> coherent commit(s)
  -> push canonical task branch
  -> exact branch SHA shared CI SUCCESS
  -> refresh current main
  -> reconcile same carrier if main moved
  -> fresh exact branch SHA CI SUCCESS if reconciliation changed the tree
  -> open/update one canonical PR
  -> PR/protected-main checks on the current candidate
  -> owner-authorized merge only
  -> refresh and record exact resulting main SHA
  -> applicable exact-main validation/release pipeline
  -> verify release/publish outcome when release is part of acceptance
  -> report exact state using the mandatory form below
```

A watched branch must not use a new PR or draft PR as its first CI attempt. Fix branch failures on the canonical branch until the exact current branch SHA is green, then create/update the PR according to repository policy.

## Merge authorization boundary

Normal prompts such as `fix`, `continue`, `update code`, `commit push git`, `fix CI`, or repeated requests for the same feature authorize work on the canonical task carrier but do not by themselves authorize a write/merge to `main`.

Only explicit owner merge/integration authorization permits the session to merge the named PR/batch/task. Branch protection and required checks must still be satisfied. Never bypass protection merely to finish the prompt.

If merge authorization is absent, the correct endpoint is a validated canonical branch/PR plus an exact report that merge/release remain pending.

## Release completion boundary

After an authorized merge, first refresh `main` and record the exact landed SHA.

When the task's acceptance includes packaging, cloud build, publish, tag, release, installer/package artifact, or another release side effect:

- branch CI is not release proof;
- PR CI is not release proof;
- merge success is not release proof;
- an older successful release run is not proof for the newly landed SHA;
- verify the applicable exact-main release pipeline and its artifact/tag/publish result for the landed SHA before reporting `RELEASED`.

If the change does not require a product release under `CI_POLICY.md`, report `Release: N/A` with the reason instead of pretending a release occurred.

Licensed BricsCAD runtime validation remains separate and must be reported as `PENDING_LOCAL` unless actually executed in the required environment.

## Mandatory per-prompt status report

At the end of **every owner prompt that asks an agent/chat session to change, continue, fix, validate, integrate, merge, or release repository work**, report the exact current state in this form. Do not replace it with a generic `done`, `fixed`, or `completed` statement.

```text
Prompt result: <ACTIVE | DUPLICATE_CARRIER | BRANCH_GREEN | PR_OPEN | PR_GREEN | MERGED_MAIN | RELEASED | BLOCKED | PENDING_LOCAL>
Issue: #<number> — <title/status>
Lane-Key: issue-<number>
Canonical owner/session: <id>
Canonical branch: <branch or N/A>
Baseline main: <sha used to start/currently reconciled base>
Latest task commit: <sha(s) or N/A>
Branch CI: <run/job + exact tested SHA + SUCCESS/FAILURE/PENDING/N/A>
PR: #<number or N/A> — <OPEN/DRAFT/READY/MERGED/CLOSED>
PR/protected checks: <SUCCESS/FAILURE/PENDING/N/A + exact candidate when known>
Merged to main: <NO | YES, main@sha>
Release required: <YES | NO/N/A + reason>
Release: <run/tag/artifact/deployment + SUCCESS/FAILURE/PENDING/N/A>
Local/runtime evidence: <PASS | PENDING_LOCAL | N/A, never infer PASS>
Remaining blocker: <exact blocker or none>
Next exact action: <one concrete next lifecycle action or none>
```

The report must use real GitHub/CI evidence from the current carrier. Do not fill unknown fields with guessed identifiers or stale conversation state.

## Completion wording rules

Use completion language precisely:

- `BRANCH_GREEN`: implementation is committed/pushed and applicable exact-branch CI is green, but PR/main/release may still be pending.
- `PR_OPEN`: canonical PR exists; do not imply its protected candidate is green unless verified.
- `PR_GREEN`: current PR/protected candidate is green, but it is not merged.
- `MERGED_MAIN`: owner-authorized merge completed and exact current `main` contains the work; release may still be pending.
- `RELEASED`: only when release is required and the applicable exact-main release/publish outcome for the landed SHA is verified successful.
- `PENDING_LOCAL`: source-safe work may be complete but required licensed/private/runtime evidence has not been executed.
- `DUPLICATE_CARRIER`: another canonical owner/carrier already owns the same lane; no overlapping mutation was performed.

Never say `ALL MERGED TO MAIN`, `released`, `production complete`, or equivalent unless the repository's stricter definitions and evidence requirements are actually satisfied.

## Success criterion for repeated prompts

Repeated prompts about one feature should advance the **same canonical lifecycle** toward completion, not create an expanding collection of disconnected Issues/branches/PRs.

A future agent receiving another prompt for the same function should be able to read GitHub and answer immediately:

1. what Issue/Lane-Key owns it;
2. which one branch/PR is canonical;
3. what exact commit and CI evidence exist;
4. whether it is merged into current `main`;
5. whether a release is required and, if so, whether that exact landed SHA was released;
6. what single next action remains.

That traceability is part of the deliverable, not optional reporting overhead.

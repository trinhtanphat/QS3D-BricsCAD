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
8. When the task is product/release relevant, determine the currently published release/version and whether any published release already contains the exact landed change.

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
  -> identify the exact published version/tag that first contains the change
  -> report exact state using the mandatory form below
```

A watched branch must not use a new PR or draft PR as its first CI attempt. Fix branch failures on the canonical branch until the exact current branch SHA is green, then create/update the PR according to repository policy.

## Merge authorization boundary

Normal prompts such as `fix`, `continue`, `update code`, `commit push git`, `fix CI`, or repeated requests for the same feature authorize work on the canonical task carrier but do not by themselves authorize a write/merge to `main`.

Only explicit owner merge/integration authorization permits the session to merge the named PR/batch/task. Branch protection and required checks must still be satisfied. Never bypass protection merely to finish the prompt.

If merge authorization is absent, the correct endpoint is a validated canonical branch/PR plus an exact report that merge/release remain pending.

## Release completion and version provenance boundary

After an authorized merge, first refresh `main` and record the exact landed SHA.

When the task's acceptance includes packaging, cloud build, publish, tag, release, installer/package artifact, or another release side effect:

- branch CI is not release proof;
- PR CI is not release proof;
- merge success is not release proof;
- an older successful release run is not proof for the newly landed SHA;
- the latest public release is not automatically proof that it contains the change;
- verify the applicable exact-main release pipeline and its artifact/tag/publish result for the landed SHA before reporting `RELEASED`;
- identify the exact release/version/tag that first contains the landed change whenever such a release exists;
- record the release commit/source SHA and enough ancestry/manifest evidence to show that the release actually contains the change.

Every release-relevant per-prompt report must distinguish:

```text
Current/latest published release: <version/tag or none>
First release containing this change: <version/tag | PENDING | NONE/N/A>
Release source/commit: <sha or N/A>
```

If the change is merged to `main` but has not yet appeared in a published release, report `First release containing this change: ⏳ PENDING`; do not name a future version unless it is already formally defined by repository/release metadata.

If the change does not require a product release under `CI_POLICY.md`, report `Release required: ➖ N/A` and `First release containing this change: ➖ N/A` with the reason instead of pretending a release occurred.

Licensed BricsCAD runtime validation remains separate and must be reported as `PENDING_LOCAL` unless actually executed in the required environment.

## Mandatory visual status markers

Every lifecycle/status line in the final per-prompt report must begin with one of these markers so the owner can scan the state without interpreting prose:

- `✅` — verified satisfied/successful/reached using current evidence;
- `❌` — verified failed, red, rejected, or a required condition is currently unsatisfied;
- `⏳` — pending, queued, in progress, waiting for an allowed next gate, or `PENDING_LOCAL`;
- `➖` — genuinely not applicable; include the reason when it is not obvious.

Do not use `✅` for assumptions, stale runs, chat-memory claims, or work that merely appears likely to pass. Do not use `❌` for ordinary in-progress work when the correct state is `⏳`.

For yes/no lifecycle questions, make the meaning visually explicit. Examples:

```text
✅ Branch pushed: YES — abc1234
✅ Branch CI: SUCCESS — run 123 / abc1234
⏳ PR: not opened yet — waiting for required branch CI
❌ PR/protected checks: FAILURE — required check core failed
❌ Merged to main: NO — PR not merged
⏳ First release containing this change: PENDING — merged but release pipeline not complete
➖ Local/runtime evidence: N/A — docs-only change
```

## Mandatory per-prompt status report

At the end of **every owner prompt that asks an agent/chat session to change, continue, fix, validate, integrate, merge, or release repository work**, report the exact current state in this form. Do not replace it with a generic `done`, `fixed`, or `completed` statement.

```text
<marker> Prompt result: <ACTIVE | DUPLICATE_CARRIER | BRANCH_GREEN | PR_OPEN | PR_GREEN | MERGED_MAIN | RELEASED | BLOCKED | PENDING_LOCAL>
<marker> Issue: #<number> — <title/status>
<marker> Lane-Key: issue-<number>
<marker> Canonical owner/session: <id>
<marker> Canonical branch: <branch or N/A>
<marker> Baseline/current main: <sha used to start/currently reconciled base>
<marker> Latest task commit: <sha(s) or N/A>
<marker> Branch pushed: <YES/NO + exact head SHA>
<marker> Branch CI: <run/job + exact tested SHA + SUCCESS/FAILURE/PENDING/N/A>
<marker> PR: #<number or N/A> — <OPEN/DRAFT/READY/MERGED/CLOSED/NOT_OPENED>
<marker> PR/protected checks: <SUCCESS/FAILURE/PENDING/N/A + exact candidate when known>
<marker> Merged to main: <NO | YES, main@sha>
<marker> Exact-main validation: <run + landed SHA + SUCCESS/FAILURE/PENDING/N/A>
<marker> Release required: <YES | NO/N/A + reason>
<marker> Current/latest published release: <version/tag/none/N/A>
<marker> First release containing this change: <version/tag/PENDING/NONE/N/A>
<marker> Release source/commit: <sha or N/A>
<marker> Release: <run/tag/artifact/deployment + SUCCESS/FAILURE/PENDING/N/A>
<marker> Local/runtime evidence: <PASS | PENDING_LOCAL | N/A, never infer PASS>
<marker> Remaining blocker: <exact blocker or none>
<marker> Next exact action: <one concrete next lifecycle action or none>
```

The report must use real GitHub/CI/release evidence from the current carrier and current published state. Do not fill unknown fields with guessed identifiers, predicted versions, or stale conversation state.

## Completion wording rules

Use completion language precisely:

- `BRANCH_GREEN`: implementation is committed/pushed and applicable exact-branch CI is green, but PR/main/release may still be pending.
- `PR_OPEN`: canonical PR exists; do not imply its protected candidate is green unless verified.
- `PR_GREEN`: current PR/protected candidate is green, but it is not merged.
- `MERGED_MAIN`: owner-authorized merge completed and exact current `main` contains the work; release may still be pending.
- `RELEASED`: only when release is required and the applicable exact-main release/publish outcome for the landed SHA is verified successful, including the exact release/version/tag that contains it.
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
5. what the current published release is;
6. whether a release is required and, if so, the exact first published version/tag that contains the landed change, or that it is still pending;
7. what single next action remains.

That traceability is part of the deliverable, not optional reporting overhead.

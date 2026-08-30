# Agent prompt-to-release continuation and reporting contract

This file defines owner-facing continuation/reporting behavior. Everyday execution starts with `AGENTS.md`; main/merge authority comes from `docs/MAIN-WRITE-AUTHORIZATION.md`; CI semantics come from `CI_POLICY.md`.

## Action first

An owner prompt to change, continue, fix, validate, integrate or merge repository work is an instruction to advance the one canonical lifecycle, not an instruction to stop after analysis or an intermediate status.

Normal owner-requested repository work should continue while safe authorized actions remain.

## Continuation check

Before mutation:

1. resolve current `main`;
2. find the semantically matching Issue/Lane-Key;
3. find the canonical branch/PR;
4. continue that carrier if this session owns it;
5. create a new carrier only when no equivalent active carrier exists.

Do not create a replacement merely because the existing carrier is red, stale, queued, behind or inconvenient.

## Delivery sequence

```text
owner prompt
  -> current main + carrier check
  -> implement/fix
  -> focused available validation
  -> coherent commit(s)
  -> push canonical branch
  -> automatic branch CI starts
  -> open/update canonical PR when ready
  -> remediate known red current-head evidence
  -> protected current-candidate `preflight` + `core` SUCCESS
  -> freshness + mergeability + collision checks
  -> merge same task PR under MAIN-WRITE-AUTHORIZATION
  -> verify resulting main SHA
  -> close/complete Issue + release reservation
  -> MERGED_MAIN
```

Branch CI is early evidence; it does not create a permanent timestamp identity for the PR.

## Self-remediation loop

When current source review, tests, branch CI, PR checks or merge-candidate validation exposes a fixable same-lane defect:

1. verify exact current evidence;
2. identify root cause;
3. fix the same canonical branch;
4. add/strengthen regression coverage when appropriate;
5. commit + push;
6. observe fresh validation;
7. repeat while another safe remediation exists.

Do not stop after merely reporting a fixable bug or the first red run.

## LOCAL_ONLY sequencing

A missing licensed/private/local capability changes only the remaining execution/evidence boundary.

Remote/source agents must finish repository-safe implementation/tests/guards/docs and push the exact candidate before handing the narrow unavailable scenario to `docs/LOCAL-AGENT-INBOX.md`.

A parked local gate does not block an otherwise eligible merge unless the owner explicitly made that exact local proof part of acceptance.

## Legitimate blocker

A task may stop before `MERGED_MAIN` only when no safe authorized progress remains, for example:

- another canonical owner owns the same lane;
- an owner-only decision is genuinely required;
- a required secret/service/licensed/private capability is unavailable and explicitly task-gating;
- GitHub protection rejects the candidate and no safe same-lane remediation remains;
- the available tooling cannot perform or observe a required action after permitted fallbacks were attempted.

Queued/running CI, red-but-fixable CI, an open PR, or a stale branch are not terminal blockers by themselves.

## Owner-facing reporting

Normal success is concise and begins:

```text
✅ Prompt result: MERGED_MAIN
```

Include, when applicable:

- Issue / Lane-Key;
- canonical branch/head;
- branch CI evidence;
- PR number;
- protected check evidence;
- resulting `main` SHA.

A blocker report begins:

```text
❌ Prompt result: BLOCKED
```

and states the exact blocker, last verified evidence, remediation attempted and why no safe action remains.

Intermediate progress updates are allowed when useful, but must not replace execution. If an intermediate update mentions pending CI, provide exact available run/job/step detail instead of a vague `CI pending` line.

## Completion terms

- `MERGED_MAIN` — protected PR merged and refreshed current `main` contains the task; task reservation is completed/released.
- `RELEASED` — only when publication/release is explicitly in scope and exact evidence exists.
- `BLOCKED` — no further safe authorized progress exists.
- `DUPLICATE_CARRIER` — another canonical owner/carrier owns the lane and no overlapping mutation was performed.

`BRANCH_GREEN`, `PR_OPEN`, `PR_GREEN`, pending CI and active remediation are lifecycle states, not normal completion endpoints.
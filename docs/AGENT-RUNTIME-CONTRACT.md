# Agent runtime contract

This document is the compact operational contract for owner-facing AI/chat sessions working in this repository. Read it from the current `origin/main` on every prompt that asks to change, continue, fix, validate, integrate, merge, release, update docs, or otherwise advance repository work.

`docs/MAIN-WRITE-AUTHORIZATION.md` remains authoritative for direct-main prohibition, standing same-task merge authorization, and the normal owner-task completion endpoint. `CI_POLICY.md` remains authoritative for Actions behavior. `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` remains authoritative for collision handling.

## Per-prompt bootstrap

Before mutation:

1. Read current `AGENTS.md` and this file from current `origin/main`.
2. Resolve current `origin/main` to an exact SHA; do not rely on chat memory.
3. Check the concrete Issue/Lane-Key, owner/session, canonical branch, open PR, current CI and merge state.
4. If an equivalent canonical carrier already exists, continue that carrier when authorized; do not create a competing branch/PR.
5. Keep the task inside its registered scope and execution boundary.
6. Treat direct writes/ref updates/force pushes to `main` as forbidden for ordinary task work.

## Default owner-task lifecycle

For a normal repository-owner request, the successful endpoint is `MERGED_MAIN` unless the owner explicitly opts out of merge for that exact task or a real terminal blocker leaves no safe authorized action.

```text
owner prompt
  -> current main + collision check
  -> continue/register one canonical lane
  -> implement/fix/docs update
  -> validate appropriately for changed paths
  -> coherent commit(s)
  -> push canonical branch
  -> observe/remediate exact-head branch CI when applicable
  -> refresh/reconcile current main when needed
  -> open/update the one canonical PR
  -> protected current-candidate preflight + core SUCCESS
  -> current + mergeable + expected-head verified
  -> merge the same task PR under MAIN-WRITE-AUTHORIZATION
  -> fetch resulting main SHA
  -> MERGED_MAIN
```

The following are intermediate states, not normal completion: edited, committed, pushed, branch CI green, PR open, PR green, or stale earlier-main evidence.

A failed current-carrier CI check is an automatic remediation trigger while a safe same-lane fix exists. Diagnose the exact failing SHA/job/step, fix the root cause on the same canonical branch, commit/push, and revalidate. Do not require the owner to repeat `fix CI`, `continue`, or `merge main` for the same task.

## Deferred LOCAL_ONLY validation

When source-safe implementation, tests, guards, documentation, or adapter work can be completed without a licensed BricsCAD host, continue that work instead of waiting for a local agent. Complete the available source/static/build/CI validation, commit coherently, and push the canonical branch.

For the unavailable runtime tail:

- record the exact intended source-ready SHA;
- mark it `PENDING_LOCAL` / `PENDING_LOCAL_AGENT` and `DO_NOT_RETRY_REMOTE` where applicable;
- register any new or materially changed local scenario in `docs/LOCAL-AGENT-INBOX.md`;
- let a later local agent fetch/sync Git, check out the exact intended SHA in a clean workspace, run the linked licensed/runtime runbook, and record sanitized exact-SHA PASS/FAIL evidence;
- never promote source review, hosted CI, managed-reference compile, mock execution, or `-SkipRuntime` output to `LOCAL_PASS`.

Local-agent unavailability is not by itself a blocker for source coding, source-safe fixes, docs, commit/push, branch CI, or PR preparation. It blocks merge/release only when the exact task acceptance contract, repository rule, or explicit owner instruction requires LOCAL_ONLY evidence before that step.

If local validation later exposes a normal source bug, the local agent records sanitized evidence and hands it back; the source lane fixes and pushes a new exact SHA, then the local agent syncs Git and resumes the affected local validation against that SHA.

If the owner explicitly says to `commit + push and leave the branch`, `stop before merge`, `PR only`, or clearly equivalent wording for the exact task, treat that as an opt-out of the default same-task merge endpoint for that task. Keep the canonical carrier available for later pickup instead of recreating the work.

See `docs/DEFERRED-LOCAL-VALIDATION.md` for the full source-to-local handoff contract.

## Main write and merge authorization

- `origin/main` is read-only for direct task writes, including source, tests, scripts, workflows, docs, Markdown, claims, handoffs, status files and chores.
- Requests such as `commit push git`, `update docs`, `update md`, `fix bug`, `continue all`, or `merge main` never authorize a direct contents write/ref update to `main`.
- For a normal owner-requested task, `docs/MAIN-WRITE-AUTHORIZATION.md` supplies standing authorization to merge the **same task PR** once every current required gate is green, the candidate is current/mergeable, and the owner has not opted out.
- That standing authorization never permits unrelated/bulk merges, branch-protection bypass, force push, direct-main writes, or weakening required checks.

## Markdown/docs classification

`Markdown-only` does **not** mean `no CI`, and a commit prefix such as `docs:`, `md:` or `chore:` does not decide validation. Changed paths are authoritative.

### ORDINARY_DOCS

Examples are ordinary guidance, notes, handoffs or documentation outside the policy/source-guard watched set.

- Keep the task Markdown-only unless the requested behavior genuinely requires another surface.
- Dedicated branch + PR remain mandatory.
- Automatic shared CI may classify the candidate as lightweight/non-build.
- Heavy pre-PR source/build validation may be omitted when the path is intentionally outside the relevant watched set.
- Core/BricsCAD V25 build and licensed runtime evidence are not required merely because a `.md` file changed.
- Protected PR `preflight` + `core` must still succeed before merge.

### GOVERNANCE_POLICY_MD

Examples include policy files explicitly classified by `.github/workflows/ci.yml`, including `AGENTS.md`, `CI_POLICY.md`, `README.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/AGENT-STATUS-MARKER-SEMANTICS.md`, and `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`.

- Dedicated branch + PR remain mandatory.
- Policy/source guards required by the shared CI classifier must run.
- A policy-Markdown-only change does not require Core/V25 build unless another build-relevant path changed.
- Do not add scripts/workflows/source solely to make a documentation clarification look more enforceable unless that executable enforcement is explicitly part of the task.

## Branch CI and PR timing

Preferred sequencing for watched work is to push the canonical branch and obtain exact-head branch-CI success before opening a new PR when the current admission gate requires it. However, CI completion time is **not** permanent PR identity.

If the one canonical PR already exists while branch CI is queued/running, or branch CI completes after PR creation:

- do not close/recreate the PR for timestamp cosmetics;
- do not create a replacement branch;
- keep the same canonical carrier;
- remediate any real red exact-head result on that carrier;
- require fresh protected current-candidate checks before merge.

`docs/PR-CI-LIFECYCLE.md` controls when older wording treats timing alone as a reason to replace an otherwise valid carrier.

## Release boundary for docs-only landings

Ordinary docs/Markdown/chore-only changes outside the V25 dispatcher's watched integration-relevant paths must not trigger the automatic V25 cloud release path. Changed paths, not commit-message labels, determine release-dispatch eligibility.

## Owner-facing reporting

Repository lifecycle reporting is terminal-first. Continue same-lane safe work instead of emitting a full completion report at each intermediate state.

A full owner-facing lifecycle report is required when either:

1. the normal success terminal is reached (`MERGED_MAIN`, or a stricter terminal explicitly requested by the owner); or
2. a legitimate terminal blocker exists and no further safe authorized action remains in the current execution.

Brief execution-progress updates are allowed when the environment requires them, but they are not completion reports.

Success form:

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

Omit genuinely not-applicable fields rather than inventing evidence.

Blocker form:

```text
❌ Prompt result: BLOCKED
✅ Issue / Lane-Key: #<number> / issue-<number>
✅ Canonical branch/head: <branch>@<sha>
<marker> Last verified CI/PR evidence: <exact evidence>
❌ Exact blocker: <specific blocker>
❌ Remediation attempted: <what was actually tried>
➖ Further safe action: none in current execution — <why>
```

Queued/running CI, a red-but-fixable current carrier, a stale branch that can be reconciled, an open PR, or review feedback that can be fixed safely are lifecycle states, not terminal blockers by themselves.

## Precedence and specialist runbooks

Use specialist documents for details instead of duplicating them here:

- main permission / same-task merge endpoint: `docs/MAIN-WRITE-AUTHORIZATION.md`;
- CI execution and manual Actions authority: `CI_POLICY.md`;
- CI evidence recovery: `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`;
- registration: `docs/AGENT-WORK-REGISTRATION.md`;
- duplicate/race handling: `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`;
- PR timing correction: `docs/PR-CI-LIFECYCLE.md`;
- full terminal reporting contract: `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`;
- deferred source-to-local validation: `docs/DEFERRED-LOCAL-VALIDATION.md`;
- local-only work: `docs/LOCAL-AGENT-INBOX.md` and linked local runbooks;
- product boundary: `docs/PRODUCT-BOUNDARY.md`.

When older documents conflict on direct-main permission, standing same-task merge authorization, or the default `MERGED_MAIN` endpoint, follow `docs/MAIN-WRITE-AUTHORIZATION.md`. When older wording treats branch-CI/PR timestamp ordering alone as permanent carrier validity, follow `docs/PR-CI-LIFECYCLE.md`.
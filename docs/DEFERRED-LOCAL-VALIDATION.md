# Deferred LOCAL_ONLY validation

This document records the repository handoff rule for source-safe work whose final interactive acceptance needs a licensed/local BricsCAD environment.

## Source first; local runtime later

When the requested implementation, tests, guards, documentation, or adapter source can be completed safely without a licensed BricsCAD runtime, the remote/hybrid source agent should continue that work instead of waiting for a local agent.

The normal handoff sequence is:

```text
implement source/docs/tests
  -> run available source/static/build/CI validation
  -> commit coherently
  -> push the canonical task branch
  -> record exact source-ready SHA
  -> mark unavailable runtime evidence PENDING_LOCAL / PENDING_LOCAL_AGENT
  -> local agent later syncs Git and checks out the exact intended SHA
  -> local agent runs the linked licensed/runtime matrix
  -> local agent records sanitized exact-SHA PASS/FAIL evidence
```

Local-agent availability is therefore **not** a prerequisite for source implementation, source-safe fixes, documentation, committing, pushing the canonical branch, or source/cloud CI. A remote agent must not idle, repeatedly retry unavailable licensed execution, or pretend source evidence is runtime evidence.

## Truthfulness boundary

Use evidence markers that describe what actually ran:

- `SOURCE/CI: PASS` means the applicable repository/source/cloud checks passed for the stated SHA.
- `LOCAL_RUNTIME: PENDING_LOCAL_AGENT` means the licensed/local interactive matrix has not yet run for that SHA.
- `LOCAL_PASS` may be written only after a real local agent ran the required licensed/local scenario and recorded sanitized evidence tied to the exact tested SHA.
- A remote build, static preflight, mock, managed-reference compile, or cloud runner must never be promoted to `LOCAL_PASS`.

If the source changes after a local handoff was written, the old runtime evidence does not automatically qualify the new source. Update the handoff to the new intended SHA and rerun only the local scenarios whose evidence was invalidated by the change.

## Local-agent pickup

When a local agent becomes available, it should start from `docs/LOCAL-AGENT-INBOX.md`, fetch/sync the repository, identify the canonical handoff and exact intended source SHA, then use a clean checkout/worktree for that SHA before running the linked runbook.

The local agent should not reconstruct source changes from chat history. Git plus the inbox/runbook/Issue are the handoff surface. Runtime evidence must record the exact tested SHA and remain sanitized: do not commit proprietary BricsCAD binaries, customer/private DWGs, credentials, signing material, private paths, raw project identifiers, or other sensitive host data.

A local failure that proves a normal source bug should be captured as sanitized evidence and handed back to the source lane. The source agent fixes and pushes a new exact SHA; the local agent then syncs Git and resumes validation on that new SHA.

## Merge and release boundary

Missing local-agent availability does not by itself block source-side progress. It blocks merge/release only when the exact task's acceptance contract, branch protection, release rule, or explicit owner instruction requires licensed LOCAL_ONLY evidence before that step.

Otherwise the source lifecycle may continue according to `docs/MAIN-WRITE-AUTHORIZATION.md`, while the licensed runtime tail remains explicitly `PENDING_LOCAL` for later qualification.

If the owner explicitly says to `commit + push and leave the branch`, `stop before merge`, `PR only`, or clearly equivalent wording for the exact task, that instruction is an opt-out of the default same-task merge endpoint for that task. Leave the canonical carrier intact and report its exact branch/SHA so later agents can resume it without recreating the work.

## Registration rule

Any new or materially changed LOCAL_ONLY scenario must still be registered in `docs/LOCAL-AGENT-INBOX.md` in the same source/docs batch that introduces or changes that scenario. Reuse an existing matching inbox item instead of creating duplicate local work.

This document does not weaken one-Lane-Key/one-carrier rules, main protection, required CI, or the requirement that local evidence be exact-SHA and real.
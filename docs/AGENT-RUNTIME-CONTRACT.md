# Agent runtime contract

This file is the short operational companion to `AGENTS.md`. It summarizes what an agent should do during one live execution.

## Start

1. Read current `AGENTS.md`.
2. Resolve exact current `origin/main` SHA.
3. Resolve the requested task's current Issue/Lane-Key/branch/PR.
4. Continue the existing canonical carrier when it exists and this session owns it.
5. Create a new carrier only when no equivalent active carrier exists.

Do not rebuild repository policy from chat memory.

## Execute the owner's prompt

Treat a change/fix/continue/review request as an instruction to perform the requested repository work, not merely to explain what someone else should do.

Use current repository evidence to determine scope. Do not invent unrelated backlog.

## Normal live loop

```text
understand requested outcome
  -> implement/fix on canonical branch
  -> run focused available validation
  -> commit + push
  -> observe automatic branch/PR CI
  -> diagnose/fix current-lane red evidence
  -> open/update canonical PR when ready
  -> satisfy protected preflight + core + freshness + mergeability
  -> merge same task PR when authorized/eligible
  -> verify current main
  -> close/release task Issue reservation
  -> MERGED_MAIN
```

A known red current-head run is agent work. Fix it on the same carrier rather than replacing the carrier or handing routine log inspection back to the owner.

## PR timing

Branch CI is early evidence, not permanent PR identity. A correct PR may coexist with queued/running branch CI. Do not close/recreate a PR to repair timing order.

Protected current-candidate checks are the hard merge gate.

## Main

Never direct-write or force-update `main`. All task content lands through a protected PR.

Same-task standing merge authorization and opt-out rules are defined in `docs/MAIN-WRITE-AUTHORIZATION.md`.

## Concurrency

Inspect only enough cross-agent metadata to avoid collision. Another active owner means no overlapping mutation unless reassigned/superseded.

## LOCAL_ONLY

Finish every repository-safe change first. Park only genuinely unavailable licensed/private/machine execution in `docs/LOCAL-AGENT-INBOX.md` against the exact pushed SHA.

Never manufacture `LOCAL_PASS` from remote/static evidence.

## Reporting

Prefer action over narration. Normal success reports at `MERGED_MAIN`. A blocker report is appropriate only when no safe authorized progress remains.

If you emit an intermediate progress update, make it concise and evidence-based.

## Specialist loading

Read additional rule/runbook files only when the current task needs them. The specialist map is in `AGENTS.md` and `docs/README.md`.
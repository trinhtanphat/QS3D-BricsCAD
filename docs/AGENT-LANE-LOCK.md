# Agent Lane Lock (compatibility alias)

The canonical duplicate-agent ownership, Lane-Key and single-carrier policy is:

`docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`

The canonical boundary between ChatGPT account scheduled tasks and repository ownership/lane semantics is:

`docs/CHATGPT-SCHEDULE-BOUNDARY.md`

A ChatGPT scheduled task is only an external account-side prompt/task trigger. Labels such as `C0`, `W1-W4`, `controller`, `worker`, or `Task 0-4` do not create repository Lane-Keys, GitHub ownership, canonical carriers, CI authority, or merge authority by themselves.

Any older wording elsewhere that refers to `scheduled/controller lanes`, `scheduled workers`, hourly controller pools, or similar concepts must be read through `docs/CHATGPT-SCHEDULE-BOUNDARY.md`: the schedule is only the invocation source; the resulting chat/session still follows the ordinary current GitHub Lane-Key / Issue / branch / PR ownership rules.

This file contains no independent duplicate-carrier policy. It exists as a compatibility pointer for branch/claim/handoff references while making the external-scheduler boundary unambiguous.

Read and follow the canonical policies above together with `AGENTS.md` and `docs/AGENT-WORK-REGISTRATION.md`.
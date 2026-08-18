# ChatGPT scheduled-task boundary

This document records the repository owner's correction for how ChatGPT scheduled tasks/automations relate to QS3D repository work.

## Canonical boundary

ChatGPT scheduled tasks are **external account-side orchestration only**. They exist in the owner's ChatGPT account/task system and are used to cause ChatGPT to run a prompt/task at a configured time or cadence.

They are **not** repository-native lanes, GitHub ownership records, CI workers, branch types, merge authorities, or canonical carriers.

The repository does not define the existence, count, cadence, enabled/disabled state, task IDs, or execution state of ChatGPT scheduled tasks. Those facts must be read or changed in the ChatGPT account/task system, not inferred from GitHub Issues, comments, Markdown, branches, PRs, or historical handoffs.

## Names such as C0 / W1-W4

Labels such as `C0`, `W1`, `W2`, `W3`, `W4`, `controller`, `worker`, or `Task 0-4` may appear inside an external ChatGPT schedule prompt as convenient account-side orchestration labels.

Those labels **do not by themselves**:

- create a repository Lane-Key;
- reserve source files, symbols, Issues, branches, or PRs;
- create a persistent GitHub worker identity;
- grant ownership of another session's carrier;
- grant cross-agent inspection or takeover authority;
- grant CI rerun/dispatch/cancel authority;
- grant merge or direct-`main` authority;
- prove that any ChatGPT schedule currently exists or is enabled.

Repository ownership exists only through the ordinary current GitHub coordination rules: semantic task scope, Lane-Key, visible reservation, canonical Issue/branch/PR carrier, explicit reassignment/supersession when required, and the applicable owner authorization.

## When a ChatGPT schedule fires

A chat/session started by an external scheduled task is treated as a normal AI agent/chat session for repository governance.

It must resolve the current repository state at execution time and follow the same ordinary rules as any interactive session:

1. refresh current `main`;
2. determine the concrete task and stable Lane-Key;
3. perform the minimum required collision/ownership check;
4. continue an existing canonical carrier only when authorized to own that carrier, otherwise stop overlapping mutation or select a genuinely non-overlapping task;
5. use the ordinary Issue/branch/PR/CI/merge lifecycle defined by repository policy.

A schedule prompt can request work; it does not manufacture repository ownership or preserve a stale assignment merely because an earlier scheduled run used a particular controller/worker label.

## Historical schedule/control-board Issues

Historical Issues such as #1910 and #2134 may describe an earlier hourly controller/worker automation design. They are historical orchestration records only and are **not a source of truth for current ChatGPT schedule configuration or repository ownership semantics**.

Do not use those Issues to infer that five schedules exist, that a particular schedule is currently running, that `C0/W1-W4` are permanent repository lanes, or that a worker owns a task solely because an old control-board comment assigned it.

If a historical control-board assignment also has a valid current GitHub Lane-Key/carrier, the current canonical GitHub reservation controls repository ownership. If not, the historical schedule label alone creates no ownership.

## No repository schedule registry

Do not create or maintain a GitHub Issue, Markdown table, branch, PR, or comment stream as the authoritative registry of ChatGPT account schedules unless the owner explicitly asks for descriptive documentation. Even when descriptive documentation is requested, it is informational only and must not be treated as proof of live schedule state.

Questions such as "how many ChatGPT scheduled tasks are running?", "is control schedule 0 enabled?", or "change the hourly schedule" must be answered or performed against the ChatGPT task/account scheduler, not against repository metadata.

## Compatibility correction

Any older repository wording that refers to `scheduled/controller lanes`, `scheduled workers`, an hourly controller pool, or similar language must be interpreted only as **an external ChatGPT invocation source feeding ordinary repository-governed sessions**.

Such wording does not establish a special repository lane class. In particular, the older sentence in `docs/AGENT-WORK-REGISTRATION.md` beginning `For scheduled/controller lanes...` is obsolete in its schedule-specific interpretation and is superseded by this document.

## Precedence

For the boundary between ChatGPT account schedules and GitHub repository work, this document is authoritative unless the repository owner explicitly changes the rule again.

It does not weaken `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, or `CI_POLICY.md`; it only prevents external scheduling/orchestration labels from being mistaken for repository ownership or authorization.
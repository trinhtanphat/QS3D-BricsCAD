# QS3D CHATGPT ACCOUNT SCHEDULED TASK CONFIGURATION

Status: REFERENCE / CHATGPT ACCOUNT AUTOMATION CONFIGURATION

Repository context:

`trinhtanphat/QS3D-BricsCAD`

Optional coordination surface used by the tasks when they perform repository work:

`#1910`

## 1. Critical boundary — what this file is and is not

This file is **not a repository scheduler contract**.

Its only purpose is to give ChatGPT a durable configuration/reference that ChatGPT can read when the owner asks it to create, inspect, update, replace, enable, disable, or recreate **scheduled tasks/automations on the owner's ChatGPT account**.

The actual scheduled tasks do **not** live in this repository.

Their real existence and state live in ChatGPT account automation/task state, including:

- whether a task currently exists;
- whether it is enabled or disabled;
- its exact recurrence;
- its next run;
- its account/task identifier;
- its prompt payload;
- whether a previous scheduled task was deleted, replaced, or paused.

Therefore this Markdown file MUST NOT be used as proof that any scheduled task currently exists or is running.

To answer questions such as:

- "how many schedules are running?";
- "is Control task 0 active?";
- "stop the schedules";
- "create the five hourly tasks";
- "change the timing";
- "show me the task IDs";

a ChatGPT session must inspect or mutate **ChatGPT scheduled-task/automation state with the appropriate ChatGPT task tooling**. It must not infer the answer from GitHub files, Issues, branches, PRs, or Actions.

### Explicit non-equivalences

The ChatGPT scheduled tasks described here are NOT:

- GitHub Actions scheduled workflows;
- GitHub cron jobs;
- repository-owned background workers;
- repository services or daemons;
- Windows Task Scheduler jobs;
- BricsCAD runtime jobs;
- source-code timers;
- CI jobs;
- a declaration that five agents are always active in the repository;
- a repository mechanism that can create or manage ChatGPT account tasks.

GitHub cannot create, enumerate, enable, disable, or time these ChatGPT account scheduled tasks merely because this file exists.

No agent may add or change `.github/workflows`, cron expressions, services, background loops, product runtime timers, or other repository machinery merely to "implement" the schedules in this document unless the owner separately and explicitly asks for such repository functionality.

## 2. Relationship to repository rules

This file is a **configuration source for ChatGPT account tasks**, not a higher-precedence repository policy.

When a ChatGPT scheduled task fires and its prompt asks ChatGPT to work on `QS3D-BricsCAD`, that particular execution must then follow the repository rules that are current at execution time, including as applicable:

- `AGENTS.md`;
- `docs/MAIN-WRITE-AUTHORIZATION.md`;
- `docs/PRODUCT-BOUNDARY.md`;
- `CI_POLICY.md`;
- `docs/AGENT-WORK-REGISTRATION.md`;
- `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`;
- `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`;
- `docs/REMOTE-AGENT-SCOPE.md`;
- relevant live Issues, PRs, claims, handoffs, and feature-specific rules.

Those repository rules govern the **repository work performed after a ChatGPT task runs**.

They do not create the ChatGPT task, do not set its timer, and do not prove its account-level state.

Likewise, this file does not grant permission to bypass repository ownership, CI, branch, PR, merge, release, or product-boundary rules.

## 3. Desired ChatGPT account task set

When the owner asks ChatGPT to create or recreate the QS3D scheduled-task set, the intended logical set is five ChatGPT account scheduled tasks:

| Logical task | Desired hourly time | Purpose |
|---|---:|---|
| C0 / QS3D Control / Task 0 | `HH:00` | Inspect current QS3D state, coordinate C0 + W1-W4, and execute/continue Task 0 |
| W1 / Worker 1 | `HH:10` | Core correctness / production bug-fix work |
| W2 / Worker 2 | `HH:15` | Tests / regression / defect discovery work |
| W3 / Worker 3 | `HH:20` | Build / CI / config / dependency reliability work |
| W4 / Worker 4 | `HH:25` | Robustness / performance / maintainability / UI integration work |

These times are **desired ChatGPT account schedule configuration**.

They are not GitHub scheduling semantics and are not enforced by repository code.

The actual ChatGPT task state is authoritative. If the actual account tasks differ from this reference, ChatGPT must report the difference instead of pretending this file changed the account automatically.

## 4. Task creation/update behavior

When the owner asks ChatGPT to create or update this set:

1. inspect the existing ChatGPT account scheduled tasks first when the tooling supports it;
2. avoid creating duplicate logical C0/W1/W2/W3/W4 tasks;
3. preserve or update the intended five logical roles rather than multiplying tasks every time the request is repeated;
4. use hourly recurrence with the intended minute offsets unless the owner gives a newer schedule;
5. treat C0 as logical task `0`, with W1-W4 as the other four logical tasks;
6. keep task prompts self-contained enough to operate when triggered later;
7. make each task read current repository rules and current GitHub state at execution time rather than relying on stale state embedded in this file;
8. if the owner asks to stop/disable/delete the schedules, mutate the ChatGPT account tasks themselves rather than editing this Markdown and claiming the schedules stopped.

This repository document may be updated to record a new desired configuration, but such an edit alone never mutates ChatGPT account automation state.

## 5. Shared behavior for a scheduled task after it fires

Once a ChatGPT account task actually fires, it may use this repository as its work target.

At that point the task should:

1. refresh current `main` and current repository rules;
2. inspect current Issues, PRs, branches, CI, and ownership relevant to its assigned logical role;
3. collision-check before creating or taking work;
4. continue the same canonical lane when that lane is still non-terminal and continuation is allowed;
5. avoid taking over a separately owned active lane;
6. perform only repository-safe work available to the current execution environment;
7. follow the current repository branch/CI/PR/merge/release lifecycle;
8. report current evidence truthfully.

This behavior is part of the **prompt template for the ChatGPT task execution**. It does not mean the repository itself is running a scheduler.

## 6. C0 / Task 0 prompt intent

C0 is the logical control task at `HH:00` in the desired ChatGPT account configuration.

When C0 fires, its prompt should direct ChatGPT to:

- inspect current `main` and current repository rules;
- inspect C0/W1/W2/W3/W4 logical task work state using current GitHub evidence;
- use issue `#1910` when it is still the applicable shared coordination surface;
- determine which logical workers already have non-terminal canonical lanes;
- keep unfinished work sticky to the same logical worker rather than creating replacement work every hour;
- assign new work only to logical workers that are actually free under current repository state;
- make newly assigned engineering packages substantive, normally representing at least about one hour of coherent work rather than filler;
- collision-check each new package and give independent work its own appropriate Issue/Lane-Key/carrier when implementation begins;
- also execute or continue C0's own Task 0 rather than acting only as a coordinator.

C0's presence in this file does not prove a C0 ChatGPT scheduled task currently exists. Actual ChatGPT task state must be inspected to establish that.

## 7. W1-W4 prompt intent

### W1 — core correctness / production bugs

When the W1 ChatGPT scheduled task fires, it should prefer core correctness, production defect investigation, invariant repair, and directly related regression coverage.

If W1 already owns a valid non-terminal canonical lane, continue that lane rather than inventing a new one.

### W2 — tests / regression / defect discovery

When the W2 ChatGPT scheduled task fires, it should prefer deterministic regression coverage, adversarial cases, defect discovery, and justified production fixes that arise from that evidence.

Do not add tests merely to mask a production defect or weaken a gate.

### W3 — build / CI / configuration reliability

When the W3 ChatGPT scheduled task fires, it should prefer build, CI, dependency, configuration, tooling, and workflow reliability problems supported by current evidence.

A red CI result on W3's own canonical carrier should follow the repository's current red-CI self-remediation rules.

### W4 — robustness / performance / maintainability / UI integration

When the W4 ChatGPT scheduled task fires, it should prefer robustness, lifecycle/resource handling, measurable performance issues, maintainability with concrete product impact, and UI integration work that is valid for the current execution environment.

Licensed/private/local runtime work remains subject to current repository local/remote boundaries.

## 8. Sticky logical assignment across ChatGPT task runs

The desired behavior of these ChatGPT tasks is to avoid hourly task multiplication.

A logical worker should not receive or invent a different heavy repository lane solely because another hour elapsed.

If current GitHub evidence shows that the logical worker still owns a non-terminal canonical lane, the next ChatGPT scheduled execution for that logical worker should normally continue that same lane, subject to current repository rules and actual ownership.

If the previous lane is terminal or no longer belongs to that logical worker, a new assignment may be selected after collision checking.

This is **prompt behavior for the ChatGPT account tasks**. It is not a repository-level declaration that a timer owns a GitHub lane forever.

Repository ownership truth remains the current Issue/Lane-Key/canonical-carrier state.

## 9. Coordination surface boundary

Issue `#1910` may be used by the scheduled ChatGPT tasks as a shared coordination surface when it remains applicable.

It is not the scheduler.

Creating or editing issue `#1910` does not create, start, stop, delay, or reschedule ChatGPT account tasks.

Likewise, deleting or disabling a ChatGPT account task does not automatically edit issue `#1910` or any repository branch/PR.

ChatGPT must keep these two state domains separate:

- **ChatGPT account automation state** — task existence, schedule, enabled state, task ID, next run;
- **GitHub repository state** — Issues, Lane-Keys, branches, PRs, CI, commits, merges, releases.

## 10. GitHub Actions boundary

GitHub Actions remains governed by `.github/workflows/**` and `CI_POLICY.md`.

The desired ChatGPT times `HH:00`, `HH:10`, `HH:15`, `HH:20`, and `HH:25` must not be copied into GitHub Actions cron schedules merely because they appear here.

A GitHub Actions workflow named or described as a "schedule" may refer to a product feature, build workflow, or GitHub cron concept and is a different thing from a ChatGPT account scheduled task.

Agents must resolve the context instead of assuming every use of the word `schedule` means the ChatGPT automation set.

## 11. Source/product boundary

Nothing in this file is a product feature requirement.

Do not add the following to QS3D source merely because this reference exists:

- an agent scheduler;
- a background orchestration service;
- a timer daemon;
- a scheduling UI;
- task persistence for these ChatGPT automations;
- a QS3D-owned cloud worker fleet;
- a BricsCAD timer loop.

If the owner later asks for a real QS3D product scheduling feature, that is a separate product request requiring its own scope, Issue/Lane-Key, design, implementation, and validation.

## 12. Truthfulness rules for future ChatGPT sessions

A future ChatGPT session reading this file must follow these rules:

- NEVER say that five schedules are active merely because this file lists five desired tasks;
- NEVER say a schedule was stopped merely because this file was edited or deleted;
- NEVER use GitHub Actions status as proof of ChatGPT account scheduled-task status;
- NEVER use ChatGPT scheduled-task status as proof that repository work merged or CI passed;
- ALWAYS inspect ChatGPT automation/task state when the owner asks about actual scheduled tasks;
- ALWAYS inspect current GitHub state when the task asks about repository work;
- KEEP the two state domains separate in reports;
- if one domain cannot be inspected with available tooling, state that limitation instead of inferring from the other domain.

## 13. Configuration summary for ChatGPT task creation

When the owner says to recreate the intended QS3D ChatGPT scheduled-task set, use this summary as the desired logical configuration unless a newer owner instruction overrides it:

- C0 / Task 0 — hourly at `:00` — coordination plus its own engineering task;
- W1 — hourly at `:10` — core correctness / production bugs;
- W2 — hourly at `:15` — regression / testing / defect discovery;
- W3 — hourly at `:20` — build / CI / configuration / dependency reliability;
- W4 — hourly at `:25` — robustness / performance / maintainability / UI integration.

The tasks should be separate ChatGPT account scheduled tasks and should use current repository rules when they execute.

The actual account task IDs, enabled states, and next-run times must be obtained from ChatGPT task tooling and must not be recorded here as permanent truth unless explicitly captured as time-stamped informational evidence.

## 14. Supersession note

This document supersedes the earlier interpretation introduced by PR `#2648` that labeled this file `Status: ACTIVE CONTRACT` and described the five entries as active scheduled repository engineering roles.

The corrected interpretation is:

**this file is a repository-hosted reference used by ChatGPT to configure and guide ChatGPT account scheduled tasks; it is not itself a repository scheduling rule or scheduler.**

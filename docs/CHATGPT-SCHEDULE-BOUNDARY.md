# ChatGPT scheduled-task boundary

This document records the repository owner's correction for how ChatGPT scheduled tasks/automations relate to QS3D repository work.

## Canonical boundary

ChatGPT scheduled tasks are **external account-side orchestration only**. They exist in a ChatGPT account/task system and cause ChatGPT to run a prompt at a configured time or cadence.

They are **not** repository-native lanes, GitHub ownership records, CI workers, branch types, merge authorities, or canonical carriers.

The repository does not define whether a ChatGPT task exists, whether it is enabled, its account/task ID, its exact next run, or its live execution state. Those facts must be inspected or changed with ChatGPT account task tooling rather than inferred from GitHub Issues, comments, Markdown, branches, PRs, Actions, or historical handoffs.

## One account = one local five-schedule group

The intended QS3D setup for **one ChatGPT account** is one local group of five scheduled tasks:

- `C0` at `HH:00`;
- `W1` at `HH:10`;
- `W2` at `HH:15`;
- `W3` at `HH:20`;
- `W4` at `HH:25`.

The five tasks inside the same account are intentionally **lightly related**:

- C0 runs first and may scan the portfolio, identify useful areas, and provide broad account-local direction;
- W1-W4 run later with complementary work affinities;
- each sibling can observe current GitHub state created by earlier siblings in that account;
- the staggered timing and affinities are intended to reduce duplicate effort inside that account.

This account-local relationship is convenience orchestration only. It does not itself create GitHub ownership.

A C0 suggestion does not reserve a repository lane for W1-W4. A worker becomes the repository owner of a concrete task only when the current GitHub state contains the valid Lane-Key / visible reservation / canonical carrier required by repository rules.

## Multiple ChatGPT accounts are independent groups

Each ChatGPT account is independent.

If the owner has 10 ChatGPT accounts and configures the same five-task set on every account, that means **10 independent local groups / up to 50 independent scheduled tasks**. There is no global C0, global W1, global W2, global W3, or global W4 shared by all accounts.

Therefore:

- `C0` on account A is not the same scheduler identity as `C0` on account B;
- `W1` on account A is not the same scheduler identity as `W1` on account B;
- the same applies to W2-W4;
- one account must not assume another account has the same task state, previous run, pending assignment, or local coordination context;
- one account must not use another account's schedule labels as ownership evidence.

A task prompt may include its own stable account-local automation/task identity so that a later run can distinguish its own carrier from another account using the same logical label.

### Global conflict rule across all accounts

The repository is the shared collision domain.

When two or more schedules from the same or different ChatGPT accounts independently discover equivalent work, they must follow the ordinary repository duplicate/race policy:

1. determine the concrete semantic scope and Lane-Key;
2. perform the required minimal collision check;
3. the first **visible valid GitHub reservation/canonical carrier** owns the overlapping lane;
4. later schedules/accounts must treat that lane as already owned and must not create a competing implementation;
5. stale, red, queued, behind, inconvenient, or slow work remains owned until current repository rules release, reassign, or supersede it.

Thus 50 scheduled tasks may safely work against one repository only when every execution respects the same GitHub single-owner/single-carrier rules. Schedule names do not resolve conflicts; current GitHub reservation/carrier state does.

## Persistent schedule prompts must not contain concrete repository tasks

The reusable ChatGPT automation prompt is configuration, not a task assignment database.

A persistent C0/W1-W4 schedule prompt MUST NOT be rewritten to permanently embed a concrete:

- GitHub Issue number;
- Lane-Key;
- branch name;
- PR number;
- commit SHA;
- CI run/job;
- feature;
- bug;
- release;
- controller assignment;
- historical coordination comment.

Every scheduled execution must refresh current GitHub state and determine what it may work on **at execution time**.

If current GitHub metadata explicitly shows that this account-local schedule identity already owns one valid non-terminal canonical carrier, the later hourly run should continue that same carrier subject to current repository rules.

If no current carrier belongs to that schedule identity, it may select new work matching its affinity only after current-main refresh and collision checking, and it must establish the required GitHub reservation/carrier before substantive mutation.

Elapsed time does not release a valid repository carrier, but the timer label by itself does not preserve ownership either. Current GitHub ownership evidence controls.

## Names such as C0 / W1-W4

Labels such as `C0`, `W1`, `W2`, `W3`, `W4`, `controller`, `worker`, or `Task 0-4` are account-local orchestration labels.

Those labels **do not by themselves**:

- create a repository Lane-Key;
- reserve source files, symbols, Issues, branches, or PRs;
- create a globally persistent GitHub worker identity;
- prove ownership of a canonical carrier;
- grant ownership of another session's carrier;
- grant cross-agent inspection or takeover authority;
- grant CI rerun/dispatch/cancel authority;
- grant merge or direct-`main` authority;
- prove that any ChatGPT schedule currently exists or is enabled.

Repository ownership exists only through ordinary current GitHub coordination rules: semantic task scope, Lane-Key, visible reservation, canonical Issue/branch/PR carrier, explicit reassignment/supersession when required, and applicable owner authorization.

## When a ChatGPT schedule fires

A session started by an external scheduled task is treated as a normal AI agent/chat session for repository governance.

It must resolve current repository state at execution time and follow the same ordinary rules as any interactive session:

1. refresh current `main` and current rules;
2. identify whether this exact account-local schedule identity already owns a current canonical carrier;
3. if so, continue that carrier when repository rules allow continuation;
4. otherwise determine a concrete candidate task and stable Lane-Key;
5. perform the required collision/ownership check;
6. stop overlapping mutation if another owner/carrier already exists;
7. establish exactly one valid Issue/Lane-Key/canonical carrier before substantive implementation when taking new work;
8. follow the ordinary branch/CI/PR/merge/release lifecycle.

A schedule prompt may request a category of work. It must not manufacture repository ownership or preserve a stale concrete task solely because an earlier scheduled run used the same C0/W1-W4 label.

## Historical schedule/control-board Issues

Historical Issues such as #1910 and #2134 describe earlier hourly controller/worker orchestration designs. They are historical records only and are **not a source of truth for current ChatGPT schedule configuration or repository ownership semantics**.

Do not use those Issues to infer that all five account tasks exist, that a particular account task is running, that C0/W1-W4 are permanent repository lanes, or that a worker owns a task solely because an old control-board comment assigned it.

If a historical assignment also corresponds to a valid current GitHub Lane-Key/carrier, the current canonical GitHub reservation controls. Otherwise the historical schedule label creates no ownership.

## No repository schedule registry

Do not create or maintain a GitHub Issue, Markdown table, branch, PR, or comment stream as the authoritative registry of ChatGPT account schedules unless the owner explicitly asks for descriptive documentation. Even then it is informational only and is not proof of live schedule state.

Questions such as "how many ChatGPT scheduled tasks are running?", "is C0 enabled?", or "change the hourly schedule" must be answered or performed against the relevant ChatGPT account task state.

## GitHub Actions boundary

ChatGPT scheduled tasks are not GitHub Actions schedules. Do not copy the account timings into `.github/workflows/**` merely because they appear in account automation documentation.

## Compatibility correction

Any older repository wording referring to `scheduled/controller lanes`, `scheduled workers`, a global hourly controller pool, or similar language must be interpreted through this document:

- the five labels form only an account-local orchestration group;
- separate ChatGPT accounts are independent groups;
- repository ownership is global only through GitHub Lane-Key/reservation/carrier state.

There is no repository-native global C0/W1-W4 worker pool.

## Precedence

For the boundary between ChatGPT account schedules and GitHub repository work, this document is authoritative unless the repository owner explicitly changes the rule again.

It does not weaken `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, or `CI_POLICY.md`; it prevents external account scheduling/orchestration labels from being mistaken for repository ownership or authorization.
# QS3D CHATGPT ACCOUNT SCHEDULED TASK CONFIGURATION

Status: REFERENCE / CHATGPT ACCOUNT AUTOMATION CONFIGURATION

Repository context:

`trinhtanphat/QS3D-BricsCAD`

Canonical schedule/repository boundary:

`docs/CHATGPT-SCHEDULE-BOUNDARY.md`

## 1. What this file configures

This file is a durable reference for creating, inspecting, updating, enabling, disabling, or recreating **ChatGPT account scheduled tasks**.

It is not a repository scheduler contract. The actual tasks live in ChatGPT account automation state, not in GitHub.

GitHub Markdown, Issues, branches, PRs, Actions, or historical controller comments do not prove whether a ChatGPT scheduled task currently exists, is enabled, or is running.

When the owner asks about the actual schedules, inspect or mutate ChatGPT account task state with the appropriate task tooling.

## 2. One ChatGPT account = one local five-task group

For one ChatGPT account, the intended QS3D set is:

| Account-local task | Hourly time | Primary affinity |
|---|---:|---|
| C0 / QS3D Control | `HH:00` | Portfolio scan, broad local coordination, plus its own useful repository work |
| W1 | `HH:10` | Core correctness / production bug-fix |
| W2 | `HH:15` | Tests / regression / defect discovery |
| W3 | `HH:20` | Build / CI / config / dependency / workflow reliability |
| W4 | `HH:25` | Robustness / performance / maintainability / UI integration |

These five tasks are **lightly related inside the same account**:

- C0 runs first;
- W1-W4 run later in staggered slots;
- the five tasks use complementary affinities;
- later siblings may observe current GitHub reservations/carriers created by earlier siblings;
- C0 may identify useful candidate areas for the sibling schedules.

This relationship is account-local orchestration only. It is not repository ownership.

A C0 suggestion is advisory. It does not bind a worker to a concrete Issue/Lane-Key or reserve a GitHub lane on that worker's behalf.

## 3. Multiple ChatGPT accounts are independent

Every ChatGPT account has its own independent schedule state.

If the owner configures this five-task set on 10 ChatGPT accounts, the result is 10 independent local groups and potentially 50 scheduled tasks.

There is no global C0/W1/W2/W3/W4 shared between accounts.

Therefore:

- account A's C0 is not account B's C0;
- account A's W1 is not account B's W1;
- the same applies to W2-W4;
- one account cannot infer another account's schedule state, previous task, pending work, or local coordination context;
- identical schedule labels across accounts do not create identical GitHub ownership.

Each task should use a stable account-local automation/task identity when possible so current GitHub reservations can distinguish one schedule from another schedule carrying the same logical label on another account.

## 4. The repository is the global collision domain

All schedules from all accounts ultimately share the same GitHub repository.

Repository conflict prevention is therefore controlled by the current GitHub ownership rules, not by schedule labels.

Before substantive mutation, every scheduled execution must:

1. refresh current `main`;
2. read the current authoritative repository rules;
3. determine the concrete semantic task and Lane-Key;
4. perform the required minimal collision check;
5. inspect the current valid visible reservation / canonical Issue / branch / PR carrier;
6. refuse duplicate ownership when an equivalent carrier already exists.

The first visible valid reservation/canonical carrier owns overlapping work under current repository policy.

A later schedule from the same account or another account must not create a competing carrier merely because the first owner is slow, blocked, red in CI, behind `main`, queued, or inconvenient.

## 5. Persistent schedule prompts are task-generic

The persistent automation prompt for C0/W1/W2/W3/W4 MUST remain reusable and task-generic.

Do not permanently embed or rewrite a schedule prompt with a concrete:

- Issue number;
- Lane-Key;
- branch;
- PR;
- commit SHA;
- CI run/job;
- specific feature;
- specific bug;
- specific release;
- controller assignment;
- historical coordination comment.

A schedule prompt may contain:

- the repository name;
- its account-local role label;
- its stable account-local automation/task identity;
- its hourly timing;
- its work affinity;
- required repository-rule reading and collision behavior;
- lifecycle/safety instructions.

Every run must rediscover current repository work from GitHub.

## 6. Work continuation across hourly runs

A schedule label alone does not own a GitHub task forever.

At the start of a run, determine whether current GitHub metadata explicitly shows that this exact account-local schedule identity owns one valid non-terminal canonical carrier.

If yes, and current repository rules permit continuation:

- continue that same carrier from the latest safe checkpoint;
- do not abandon it merely because another hour passed;
- do not create a replacement lane merely because CI is red, pending, stale, or the branch is behind;
- continue through the normal remediation/reconciliation/PR lifecycle until the repository carrier becomes terminal or ownership is explicitly released/reassigned/superseded.

If no current carrier belongs to that exact schedule identity:

- the schedule is free to select new useful work matching its affinity;
- it must collision-check first;
- it must establish the required GitHub Issue/Lane-Key/canonical carrier before substantive mutation.

Thus:

`elapsed time != repository task completion`

but also:

`schedule label != repository ownership`

Current GitHub ownership evidence controls both continuation and conflict prevention.

## 7. C0 prompt intent

C0 is the first account-local schedule at `HH:00`.

Its reusable prompt should direct it to:

- refresh current `main` and current rules;
- inspect current repository state broadly enough to identify useful unowned work;
- consider the complementary affinities of this account's W1-W4 schedules;
- avoid creating duplicate work already reserved by any schedule/account/session;
- optionally identify candidate areas that later sibling schedules may independently evaluate;
- take its own concrete lane only after ordinary GitHub collision checking and reservation;
- continue its own current canonical carrier when current GitHub metadata proves that this exact schedule identity still owns it.

C0 must not create a permanent GitHub assignment table for the sibling schedules merely because they share one ChatGPT account.

C0 must not treat a historical control-board Issue as the authoritative schedule registry.

## 8. W1-W4 prompt intent

### W1 — core correctness / production bugs

Prefer production defects, invariant correctness, user-visible correctness, and directly related regression coverage.

### W2 — tests / regression / defect discovery

Prefer deterministic reproduction, edge/boundary coverage, regression discovery, and justified production fixes discovered from that evidence.

Never add tests merely to hide a production defect or weaken a gate.

### W3 — build / CI / configuration reliability

Prefer build, CI, dependency, configuration, tooling, preflight, and workflow reliability defects supported by current evidence.

Red CI on W3's own canonical carrier follows the current repository red-CI self-remediation rules; W3 must not treat unrelated agents' failures as its automatic backlog.

### W4 — robustness / performance / maintainability / UI integration

Prefer robustness, lifecycle/resource handling, measurable performance issues, maintainability with concrete product impact, and valid UI integration work.

Licensed/private/local BricsCAD runtime evidence remains subject to current LOCAL_ONLY boundaries.

Each worker independently re-checks GitHub ownership before taking work. A C0 candidate suggestion from the same account is only advisory until current GitHub reservation/carrier state establishes ownership.

## 9. Mandatory repository rule gate after a schedule fires

Before substantive repository work, every scheduled execution must read current applicable repository rules, including as relevant:

- `AGENTS.md`;
- `docs/MAIN-WRITE-AUTHORIZATION.md`;
- `docs/PRODUCT-BOUNDARY.md`;
- `CI_POLICY.md`;
- `docs/AGENT-WORK-REGISTRATION.md`;
- `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`;
- `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`;
- `docs/REMOTE-AGENT-SCOPE.md`;
- `docs/CHATGPT-SCHEDULE-BOUNDARY.md`;
- directly applicable live Issue/claim/runbook/feature rules.

Do not rely on remembered rules from a previous scheduled run.

If required rules cannot be read, conflict, leave ownership ambiguous, or prohibit the intended operation, fail closed.

## 10. Historical coordination surfaces

Historical Issues such as `#1910` and `#2134` are not authoritative schedule registries and do not establish current repository ownership merely because an old C0/W1-W4 assignment appears there.

Do not require every schedule run to update such an Issue unless a current repository rule or explicit owner instruction independently makes that Issue relevant to the concrete current lane.

## 11. Repository lifecycle and safety

A scheduled execution that takes or continues a real GitHub lane must obey the same repository lifecycle as any other agent/session, including:

- single Lane-Key / single canonical carrier;
- no stolen scope;
- no duplicate competing branch/PR;
- dedicated task branch;
- exact-head branch CI before a new PR when required;
- red-CI self-remediation only for the owned carrier and only when safely in scope;
- current-main freshness/reconciliation requirements;
- protected PR checks;
- merge authorization rules;
- release rules;
- LOCAL_ONLY/runtime evidence boundaries;
- no direct write to protected `main`;
- no force-push/reset;
- no CI/protection bypass;
- no weakening gates merely to get green;
- no fabricated CI/runtime evidence;
- no unauthorized manual rerun/dispatch/cancel.

## 12. GitHub Actions boundary

The desired ChatGPT account timings `HH:00`, `HH:10`, `HH:15`, `HH:20`, and `HH:25` are not GitHub Actions cron semantics.

Do not copy these timings into `.github/workflows/**`, repository services, background loops, product timers, BricsCAD runtime loops, or other repository machinery merely because this reference exists.

## 13. Actual account-state truthfulness

This file records the **desired configuration**, not live account state.

Future ChatGPT sessions must:

- inspect ChatGPT task tooling to say how many schedules actually exist or are enabled;
- not claim a schedule stopped merely because Markdown changed;
- not claim a schedule exists merely because it appears in this file;
- keep ChatGPT account state separate from GitHub Issue/branch/PR/CI/release state.

## 14. Configuration summary

Unless a newer owner instruction overrides it, recreate **per ChatGPT account**:

- C0 — hourly at `:00`;
- W1 — hourly at `:10`;
- W2 — hourly at `:15`;
- W3 — hourly at `:20`;
- W4 — hourly at `:25`.

Treat the five tasks as one lightly coordinated account-local group with complementary affinities.

Treat separate ChatGPT accounts as independent groups.

Use GitHub Lane-Key / visible reservation / canonical carrier state as the shared global conflict-control mechanism across all accounts and sessions.

Never persist a concrete GitHub task inside the reusable schedule prompt.
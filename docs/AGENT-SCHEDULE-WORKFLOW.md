# QS3D AGENT SCHEDULE WORKFLOW

Status: ACTIVE CONTRACT

This document defines the coordination and execution contract for the five
scheduled QS3D engineering agents.

Repository:

`trinhtanphat/QS3D-BricsCAD`

Coordination issue:

`#1910`

This schedule contract does NOT override repository safety, CI, ownership,
release, branch-protection, or product-boundary rules.

If this document conflicts with a higher-precedence repository rule, the
higher-precedence rule wins.

---

# 1. Scheduled roles

Exactly five active scheduled roles participate in this workflow.

| Role | Schedule | Primary responsibility |
|---|---:|---|
| C0 / QS3D Control | `HH:00` | Coordination + Task 0 execution |
| W1 / Worker 1 | `HH:10` | Core correctness / production bug-fix |
| W2 / Worker 2 | `HH:15` | Tests / regression / defect discovery |
| W3 / Worker 3 | `HH:20` | Build / CI / config / dependency reliability |
| W4 / Worker 4 | `HH:25` | Robustness / performance / maintainability / UI integration |

All five schedules:

- run HOURLY;
- use exact schedule timing;
- operate independently;
- coordinate through issue `#1910`;
- must obey current repository Markdown rules;
- must not create duplicate ownership.

The legacy `QS3D CI integration` automation is NOT part of this five-agent
contract.

---

# 2. Mandatory rule gate

Every scheduled run MUST perform the rule gate before broad repository work.

At minimum, refresh current `main` and read the current authoritative rules
applicable to the task, including:

- `AGENTS.md`
- `CI_POLICY.md`
- `docs/MAIN-WRITE-AUTHORIZATION.md`
- `docs/PRODUCT-BOUNDARY.md`
- canonical work-registration rules
- canonical prompt-to-release rules
- canonical duplicate-race / collision rules
- canonical remote-scope rules
- directly referenced applicable `.md` rules

Rules MUST be read from the current repository state, not assumed from memory.

Read unchanged rule files once per run unless a repository mutation makes
re-reading necessary.

If required rules:

- cannot be read;
- are contradictory;
- leave ownership ambiguous;
- prohibit the intended operation;

the agent MUST fail closed.

It MUST NOT guess.

The blocker must be reported through the canonical coordination path,
normally issue `#1910`.

---

# 3. Core ownership invariant

## One role may own at most one non-terminal heavy task.

Task ownership is STICKY.

A task does NOT expire because one hour passed.

A scheduled role keeps the same task across future hourly runs until that task
is formally terminal according to the current repository rules.

Therefore:

`elapsed time != task completion`

and:

`next hourly run != permission to receive a new task`

Example:

- W1 receives issue `#2600`.
- At the next `HH:10`, `#2600` is still non-terminal.
- W1 MUST continue `#2600`.
- C0 MUST NOT give W1 another engineering task.
- This repeats for as many hourly runs as necessary.

---

# 4. Non-terminal means CONTINUE

A task is still non-terminal when any required lifecycle work remains.

Examples include:

- investigation still incomplete;
- implementation still incomplete;
- regression coverage incomplete;
- branch exists but work is unfinished;
- changes committed but not pushed;
- branch CI still running;
- branch CI failed and failure is fixable;
- exact-head CI has not been established;
- `main` advanced and reconciliation/currentness is required;
- PR has not yet been opened when required;
- PR is open but not yet terminal;
- PR CI is still running;
- PR CI failed;
- branch is behind current `main`;
- mergeability/currentness is not satisfied;
- required merge has not occurred;
- required post-merge verification remains;
- work registration has not reached the required terminal state;
- release/closure steps required by repository rules remain;
- ownership remains active despite an external blocker.

A blocker by itself does NOT release ownership.

Ownership is released only when repository rules explicitly make the task
terminal/released.

---

# 5. Things that do NOT prove completion

C0 and workers MUST NOT treat any of the following alone as completion:

- code written;
- local test passed;
- commit created;
- branch pushed;
- PR opened;
- one CI job green;
- branch CI green while freshness requirements remain;
- worker saying "done";
- one hour elapsed;
- another schedule cycle started;
- issue appears mostly complete;
- code looks correct.

Terminal status requires concrete evidence satisfying the current repository
contract.

---

# 6. Terminal verification

A worker may receive another task only after its previous task is verified
terminal.

The exact terminal criteria are defined by current repository rules and may
include, where applicable:

- implementation complete;
- regression coverage complete;
- required validation complete;
- exact-head branch CI green;
- branch current with required `main`;
- protected PR created correctly;
- exact PR CI green;
- branch/currentness/mergeability requirements satisfied;
- authorized protected merge completed;
- required post-merge verification completed;
- canonical issue/lane state closed/completed;
- work registration released/terminal;
- prompt-to-release contract satisfied.

C0 MUST verify this evidence.

A worker's statement that its task is complete is not sufficient by itself.

---

# 7. Per-role lifecycle states

Every role must be represented by one of these states during coordination.

## `NEW`

Previous task is verified terminal and the role has been assigned a new task.

## `CONTINUE`

Role still owns a non-terminal task from an earlier cycle.

The same issue/lane/branch ownership must be preserved.

## `RULE-BLOCKED`

Work cannot safely continue because a repository rule, ownership collision,
missing authorization, unavailable evidence, or other hard gate prevents it.

This state does not automatically release the task.

## `TERMINAL`

The previous task has concrete evidence satisfying all applicable repository
terminal requirements.

Only a TERMINAL role is eligible for a different NEW task.

---

# 8. C0 Controller workflow — `HH:00`

C0 is BOTH:

1. coordinator for C0 + W1 + W2 + W3 + W4; and
2. executor of Task 0.

Coordination alone is not completion of the C0 scheduled run.

## 8.1 Take one coordination snapshot

At `HH:00`, obtain a current snapshot containing at least:

- exact current `main` HEAD;
- relevant recent commits;
- open relevant issues;
- open relevant PRs;
- active work registrations/reservations;
- ownership/collision state;
- relevant CI state;
- coordination issue `#1910`.

Avoid repeatedly fetching unchanged information during the same run.

## 8.2 Resolve existing ownership first

Before creating any new assignment, inspect EACH role independently:

- C0
- W1
- W2
- W3
- W4

For each role determine its latest canonical:

- task;
- issue;
- Lane-Key;
- owner/session where applicable;
- branch;
- PR;
- CI;
- terminal/non-terminal state.

## 8.3 Carry over unfinished work

If a role's previous task is non-terminal:

- mark it `CONTINUE`;
- preserve the exact task;
- preserve issue ownership;
- preserve lane ownership;
- preserve canonical branch;
- DO NOT assign a replacement task.

## 8.4 Assign new work only to free roles

A new task may be created only when the role's previous task is verified
terminal.

New work must:

- be independent;
- not collide with active scopes;
- have explicit ownership;
- have a concrete component/file boundary;
- be substantive;
- normally target at least ~60 minutes of engineering work;
- contain related sub-items rather than unrelated cleanup;
- state exclusions/collision boundary;
- state acceptance criteria;
- state validation expectations;
- state expected branch/PR boundary;
- record cycle timestamp;
- record current exact `main` SHA.

C0 does NOT need to create five NEW tasks every hour.

An hourly pack may legitimately look like:

- C0 = CONTINUE
- W1 = CONTINUE
- W2 = NEW
- W3 = RULE-BLOCKED
- W4 = CONTINUE

That is correct behavior.

## 8.5 Publish coordination state

Publish one coordination update for the cycle.

For all five roles include:

- role;
- state: `NEW`, `CONTINUE`, `RULE-BLOCKED`, or `TERMINAL`;
- issue/task;
- Lane-Key;
- canonical branch where applicable;
- relevant PR;
- exact main SHA;
- reason for state.

Avoid duplicate assignment packs for the same cycle.

## 8.6 Execute C0 Task 0

After coordination, C0 must perform engineering work.

If C0 already has an unfinished Task 0:

`CONTINUE existing Task 0`

C0 MUST NOT invent another Task 0.

If previous Task 0 is terminal, C0 may register a new Task 0.

Then follow the normal repository lifecycle:

investigate
→ implement
→ test
→ commit
→ push canonical branch
→ exact-head CI
→ protected PR when allowed
→ PR CI
→ reconcile/currentness
→ authorized merge
→ required terminal verification

subject to all repository rules.

---

# 9. W1 workflow — `HH:10`

Primary lane:

`CORE CORRECTNESS / PRODUCTION BUG-FIX`

At every `HH:10` run:

1. perform rule gate;
2. inspect `#1910`;
3. inspect W1's canonical issue/lane/branch/PR/CI;
4. determine whether W1 already owns a non-terminal task.

If YES:

`CONTINUE EXACT SAME TASK`

Do NOT require a fresh controller assignment.

Continue from the latest safe state, including as necessary:

- root-cause investigation;
- production fix;
- adjacent invariant hardening;
- regression coverage;
- fixing CI failures;
- commit/push;
- current-main reconciliation;
- PR work;
- exact-head CI;
- merge lifecycle;
- terminal closure.

If NO previous non-terminal task exists:

- accept only the newest controller-assigned NEW Task 1;
- verify ownership/cycle/main;
- register/claim scope;
- execute only that scope.

W1 MUST NOT self-invent replacement heavy work unless current repository rules
explicitly permit it.

---

# 10. W2 workflow — `HH:15`

Primary lane:

`TEST / REGRESSION / DEFECT DISCOVERY`

The same sticky-task rules apply.

If W2 owns a previous non-terminal task:

`CONTINUE EXACT SAME TASK`

Typical W2 work may include:

- deterministic regression tests;
- positive cases;
- negative cases;
- edge/boundary cases;
- defect discovery;
- tracing failures into production code;
- justified production fixes;
- regression/source guards;
- validation of the same component.

W2 must not create tests merely to make CI green while masking a production
defect.

A new Task 2 is accepted only after W2's previous task is verified terminal.

---

# 11. W3 workflow — `HH:20`

Primary lane:

`BUILD / CI / CONFIG / DEPENDENCY / WORKFLOW RELIABILITY`

The same sticky-task rules apply.

If W3 owns a previous non-terminal task:

`CONTINUE EXACT SAME TASK`

Typical work may include:

- CI/preflight reliability;
- deterministic discovery;
- workflow defects;
- dependency/configuration problems;
- build-system issues;
- tooling defects;
- source guards;
- reliability regressions.

W3 MUST NEVER weaken a gate merely to obtain green CI.

Fix the underlying defect.

A new Task 3 is accepted only after W3's previous task is verified terminal.

---

# 12. W4 workflow — `HH:25`

Primary lane:

`ROBUSTNESS / PERFORMANCE / MAINTAINABILITY / UI-INTEGRATION`

The same sticky-task rules apply.

If W4 owns a previous non-terminal task:

`CONTINUE EXACT SAME TASK`

Typical work may include:

- robustness hardening;
- lifecycle/state handling;
- resource cleanup;
- measurable performance defects;
- brittle duplication cleanup;
- maintainability improvements with concrete product impact;
- UI integration;
- regression/source guards.

BricsCAD or other licensed/runtime-only visual validation that cannot actually
be performed remotely MUST remain explicitly:

`LOCAL_ONLY / PENDING`

Remote automation MUST NOT claim a real runtime PASS without executing the
required runtime evidence.

A new Task 4 is accepted only after W4's previous task is verified terminal.

---

# 13. Branch and integration safety

All five roles must obey repository branch/integration rules.

Unless explicitly authorized by higher-precedence repository rules:

- no direct write to protected `main`;
- no force-push;
- no branch-protection bypass;
- no fake merge evidence;
- no CI bypass;
- no weakening tests or gates merely to pass;
- no duplicate active lane;
- no stealing another role's scope.

Use canonical branch ownership.

Before integration, re-check required currentness against latest `main`.

If `main` moved and repository policy requires reconciliation:

- reconcile safely;
- no force rewrite;
- rerun the required exact-head validation.

---

# 14. Collision and duplicate-race handling

Before taking new work, agents must check:

- existing active issue ownership;
- Lane-Key ownership;
- canonical branch ownership;
- active PR ownership;
- overlapping component/file scope;
- current coordination state.

If another active lane owns the work:

DO NOT duplicate it.

Either:

- continue the already-owned canonical lane;
- choose another rule-safe task if the role is actually free;
- or mark `RULE-BLOCKED`.

---

# 15. Request-throttling contract

The five schedules must avoid unnecessary request amplification.

Within one run:

- take one main repository snapshot;
- read unchanged rule files once;
- reuse fetched issue/PR/CI information where possible;
- avoid repeated full-repository scans;
- avoid repeatedly reading complete issue histories;
- avoid polling unchanged CI unnecessarily;
- re-fetch only when a mutation or freshness decision requires it;
- prefer `#1910` as coordination source of truth.

Most importantly:

A worker with an existing non-terminal task must NOT create another heavy task.

This prevents hourly workload multiplication.

---

# 16. Worker completion report

At the end of every worker run, report at minimum:

- role;
- state:
  - ✅ TERMINAL
  - ⏳ CONTINUE
  - ❌ RULE-BLOCKED
  - or ✅ NEW ownership where appropriate;
- issue/task;
- Lane-Key;
- starting main SHA;
- ending/current main SHA;
- canonical branch;
- commits;
- push state;
- PR;
- exact CI evidence;
- completed sub-items;
- remaining sub-items;
- blockers;
- LOCAL_ONLY items where applicable;
- whether the task is truly terminal under current repository rules.

Do not report generic "done" when lifecycle work remains.

---

# 17. Controller completion report

C0 must report all five lane states each cycle.

Example:

- ⏳ C0 — CONTINUE — issue #2601
- ✅ W1 — TERMINAL — issue #2598
- ✅ W2 — NEW — issue #2610
- ⏳ W3 — CONTINUE — issue #2604
- ❌ W4 — RULE-BLOCKED — issue #2606

For C0 Task 0 additionally report:

- canonical branch;
- commit/push;
- PR;
- CI;
- completed work;
- remaining work;
- blockers;
- terminal status.

---

# 18. Canonical hourly sequence

Normal hourly sequence:

`HH:00` — C0
- refresh rules/main;
- determine status of all five lanes;
- carry over unfinished tasks;
- assign new tasks ONLY to terminal/free roles;
- execute/continue C0 Task 0.

`HH:10` — W1
- continue existing Task 1 if non-terminal;
- otherwise take NEW Task 1.

`HH:15` — W2
- continue existing Task 2 if non-terminal;
- otherwise take NEW Task 2.

`HH:20` — W3
- continue existing Task 3 if non-terminal;
- otherwise take NEW Task 3.

`HH:25` — W4
- continue existing Task 4 if non-terminal;
- otherwise take NEW Task 4.

No new assignment is implied merely because a schedule fired.

---

# 19. State-machine summary

```text
                 ┌─────────────┐
                 │   LANE FREE │
                 └──────┬──────┘
                        │ C0 assigns
                        ▼
                 ┌─────────────┐
                 │     NEW     │
                 └──────┬──────┘
                        │ worker starts
                        ▼
                 ┌─────────────┐
          ┌─────►│   CONTINUE  │◄─────┐
          │      └──────┬──────┘      │
          │             │             │ next hourly run
          │             │ unfinished  │
          └─────────────┘             │
                        │             │
                        │ hard blocker
                        ▼             │
                 ┌─────────────┐      │
                 │RULE-BLOCKED │──────┘
                 └──────┬──────┘
                        │ rule-safe continuation/
                        │ explicit release
                        ▼
                 ┌─────────────┐
                 │  TERMINAL   │
                 └──────┬──────┘
                        │
                        │ role becomes free
                        ▼
                 ┌─────────────┐
                 │   LANE FREE │
                 └─────────────┘
```

A blocked lane is NOT automatically free.

---

# 20. Absolute invariant

The most important scheduling invariant is:

> ONE ROLE = AT MOST ONE NON-TERMINAL ENGINEERING LANE.

And:

> AN HOURLY TRIGGER CONTINUES OWNERSHIP; IT DOES NOT RESET OWNERSHIP.

And:

> ONLY VERIFIED TERMINAL COMPLETION ALLOWS C0 TO ASSIGN A DIFFERENT TASK.

These invariants apply equally to C0, W1, W2, W3, and W4.

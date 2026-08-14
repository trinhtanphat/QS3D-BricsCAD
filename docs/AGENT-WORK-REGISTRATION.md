# QS3D agent work registration and integration

**Owner rule — 2026-08-14:** register every new AI agent/chat-session lane visibly before substantive work, but keep claim/status/implementation writes off `main` unless the owner explicitly grants integration authority for that exact operation.

This file is the canonical agent-ownership and batch-integration protocol. `docs/AI-SESSION-WORKFLOW.md` is the canonical per-session execution/completion loop and `CI_POLICY.md` is authoritative for Actions/release behavior. This 2026-08-14 rule supersedes older claim-only/status-only direct-`main` wording while preserving the stronger collision, exact-SHA, integration and evidence rules below.

## Source of truth

Existing work-claim files live under:

```text
docs/agent-work-claims/
```

They remain coordination history, and every existing `ACTIVE` / `BLOCKED` claim remains reserved until terminal or explicitly taken over by the owner.

For **new** work after the 2026-08-14 owner rule, registration is issue/PR-first so an ordinary session does not have to touch `main` merely to reserve a lane:

- preferred: one visible GitHub issue dedicated to the lane;
- acceptable: a dedicated claim PR;
- optional: a Markdown claim file on the claim/agent branch and included in its PR.

`docs/LOCAL-AGENT-INBOX.md` remains the LOCAL_ONLY product/runtime queue; registration records temporary agent ownership.

Claim/lane statuses:

- `ACTIVE` — lane is reserved and implementation/validation is in progress.
- `BLOCKED` — lane is blocked and remains reserved.
- `READY_FOR_INTEGRATION` — the assigned lane is fully implemented/validated on its branch/PR and awaits an authorized integrator.
- `COMPLETED` — the assigned prompt/lane acceptance criteria are fully implemented/validated with a self-contained repository-side handoff; `MERGED TO MAIN` is reported separately.
- `RELEASED` — the agent stopped and intentionally made the lane available without claiming completion.

Do not delete completed/released historical claim files solely to tidy the tree; issues/PRs may be closed only when their lifecycle allows it.

## Mandatory sequence before implementation

1. Fetch current `origin/main` and inspect recent commits.
2. Read `AGENTS.md`, `CI_POLICY.md`, `docs/AI-SESSION-WORKFLOW.md`, this file, every existing `ACTIVE` / `BLOCKED` claim file, and open claim issues/PRs relevant to the intended surfaces.
3. Choose a non-overlapping lane.
4. Create or update one visible issue/claim PR for the lane before substantive implementation.
5. Record exact scope, expected files/symbols/tests, exclusions, baseline `main` SHA, acceptance criteria, validation/CI plan and stable agent/session identity.
6. Re-read concurrently published claims/issues/PRs and resolve any overlap before code work begins.
7. Create or refresh a dedicated implementation branch from the latest valid baseline, normally:

   ```text
   agent/<agent-id>/<scope>
   ```

   CI repair may use `recovery/<agent-id>/<scope>`.
8. Publish a concrete implementation plan in the coordination issue/PR.
9. Implement only the reserved lane on that branch.

A private/local claim, chat message, draft patch or unpushed branch is not a reservation. **There is no claim-only/status-only direct-`main` exception for new work.**

## Main authorization boundary

Ordinary prompts such as `fix bug`, `update code`, `commit push git`, `continue all`, `implement all`, `run CI`, `fix CI` or `loop until success` do not authorize direct writes or merges to `main`.

Only an explicit owner instruction granting integration authority for the exact operation may change `main`, for example:

- `merge all to main`;
- `you are the integration coordinator`;
- `allow merge PR #... to main`.

CI ownership never implies integration authority. Release/publish authority is also separate.

## Implementation branches: no independent source landing to `main`

After the claim is visible, source/test/script/workflow implementation must stay on the agent implementation branch until integration.

Agents must not independently push their implementation batch directly to `main`, and must not merge their own feature PR directly to `main` without explicit owner integration authority.

Use this shape:

```text
origin/main
  ├─ visible claim issues / claim PRs (coordination, no main write)
  ├─ agent/remote001/wall-fix
  ├─ agent/remote002/schedule-fix
  ├─ agent/remote003/health-fix
  └─ integration/<batch-id>
        ├─ wall-fix
        ├─ schedule-fix
        └─ health-fix
             ↓
        one explicitly authorized final merge to main
```

The reason is deterministic final CI: the repository owner wants the combined batch tested after all participating code is together, not one expensive release workflow per individual agent landing.

## Agent branch discipline

Every implementation agent must:

1. start from the latest allowed baseline after its claim is published;
2. periodically refresh `origin/main` and inspect relevant concurrent changes;
3. keep edits inside the reserved lane;
4. make coherent request/lane-level commits rather than file-by-file noise;
5. run relevant local/static/unit/smoke validation on the branch;
6. rebase/reapply safely when needed; never force-push shared `main` or overwrite another agent's work;
7. publish the implementation branch and record its branch name plus implementation commit(s) in the claim issue/PR;
8. keep ownership visible until the lane is handed off or terminal;
9. hand the branch/commit SHA to the batch integrator when integration is authorized.

An implementation branch being pushed is **not** `ALL MERGED TO MAIN`.

## Mandatory planning and CI/fix loop

Before broad implementation, the coordination record must contain a plan covering acceptance criteria, reserved/excluded surfaces, risks, tests/preflights/runtime evidence, applicable CI and known external/LOCAL_ONLY prerequisites.

For task-scoped non-destructive CI that the session is permitted to operate:

1. run/observe the applicable branch/PR/integration-candidate CI;
2. bind every diagnosis to the exact run/check and exact tested SHA;
3. when red, inspect the failing job/step/log and diagnose root cause against current source;
4. fix on the agent/recovery branch, not on `main`;
5. add/retain deterministic regression coverage when appropriate;
6. commit and push;
7. run/observe a fresh relevant attempt;
8. repeat from the newest relevant failure until all required/applicable checks for the lane are green.

Never weaken tests, architecture/product contracts, source guards, security checks or release-integrity gates merely to obtain green CI. For docs-only changes with no applicable branch/PR CI, record what was intentionally skipped; do not manufacture a release run solely to create a green badge.

## Batch integration branch

For a multi-agent owner request, an **explicitly authorized integration coordinator** creates or selects:

```text
integration/<batch-id>
```

The integration branch is the only combined release/CI candidate before `main`.

The coordinator must:

1. refresh latest `origin/main`;
2. identify the exact participating claims for the owner request;
3. merge/cherry-pick/rebase each required implementation branch into `integration/<batch-id>` without silently dropping commits;
4. resolve semantic/API/test conflicts deliberately rather than choosing `ours` / `theirs` blindly;
5. verify no required lane remains only on an agent branch or unmerged PR;
6. run relevant remote-safe preflights, builds and smoke tests against the **combined integration tree**;
7. inspect the combined diff for accidental reversions, duplicate implementations and contract mismatches;
8. freeze the participating batch before the final landing;
9. merge the integration branch to `main` **once**, only under the explicit owner integration authorization;
10. refresh `main` and record the exact resulting SHA.

Do not merge agent implementation branches one-by-one into `main` just to assemble the batch there. Assemble the batch on `integration/<batch-id>`, then perform one authorized final landing.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, agents may state **ALL MERGED TO MAIN** only after an authorized integration reviewer has freshly verified all of the following against current `origin/main`:

- every participating required claim is terminal or explicitly excluded/superseded from the batch;
- every required implementation commit is represented in the integrated result and reachable from current `main`;
- no required code exists only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- the reviewer refreshed `main` after the one final integration landing;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations, or known semantic/API/test collisions;
- required remote-safe build/tests/smoke/preflights passed on the combined candidate or exact integrated tree;
- the final report names the exact current `main` SHA.

Branch existence is not proof. Branch deletion is not proof. Issue state is not proof. PR UI state alone is not proof. The current integrated tree and commit reachability are authoritative.

## Prompt/lane completion timing

A lane can become `READY_FOR_INTEGRATION` / `COMPLETED` without being merged to `main` **only when the user's prompt did not grant integration authority** and all of these are true:

- the assigned scope and acceptance criteria are fully implemented;
- no known in-scope defect remains;
- branch/PR and exact implementation SHA are pushed and reviewable;
- all required/applicable validation owned by that lane is green;
- the repository-side handoff is self-contained.

In that case the session must explicitly report `MERGED TO MAIN: NO`; final integration remains a separate coordinator responsibility.

If the user's prompt explicitly includes integration to `main`, `COMPLETED` for that prompt additionally requires verified integration into current `main` and the exact-main evidence required by `CI_POLICY.md`.

Every session must report:

```text
PROMPT/LANE STATUS: 100% COMPLETE | NOT 100% COMPLETE
SESSION CAN BE CLOSED/DELETED: YES | NO
MERGED TO MAIN: YES | NO
```

If the lane is not 100% complete and actionable work remains within the session's tools, permissions and reserved scope, continue the plan -> implement/fix -> validation/CI -> diagnose -> repair loop rather than stopping at a checkpoint.

## Integration freeze gate before final CI

Before the final merge from `integration/<batch-id>` to `main`:

1. identify the exact owner request/batch;
2. stop participating agents from adding more source changes to that candidate;
3. verify all required agent branches/commits are integrated or explicitly excluded;
4. verify no required `ACTIVE`/`BLOCKED` lane remains unresolved for the claimed completed integration scope;
5. run combined-tree validation;
6. inspect the combined integration diff;
7. record the integration candidate SHA;
8. perform one authorized final merge to `main`;
9. fetch `main` again and record the final exact SHA.

Canonical state progression for an integration-authorized batch:

```text
AGENTS_WORKING
    -> AGENT_BRANCHES_READY
    -> INTEGRATION_BRANCH
    -> INTEGRATION_REVIEW
    -> ONE_FINAL_MERGE_TO_MAIN
    -> ALL_MERGED_TO_MAIN
    -> AUTO_V25_CLOUD_CI
    -> CI_GREEN
    -> ALL_DONE
```

## Automatic CI after the one final main landing

`CI_POLICY.md` authorizes one automatic post-integration workflow:

```text
.github/workflows/dispatch-v25-cloud-after-main-integration.yml
```

When the final integration-relevant batch lands on `main`, that dispatcher automatically starts:

```text
.github/workflows/release-v25-cloud.yml
```

Important boundaries:

- docs-only updates are not a reason for an ordinary agent to write `main` and do not require a release run;
- agent implementation branches do not trigger the post-integration release workflow;
- integration work before the final `main` landing does not trigger it;
- the one final integration-relevant source landing to `main` triggers it according to `CI_POLICY.md`;
- release-preparation pushes made by `github-actions[bot]` are ignored by the dispatcher to prevent recursion;
- a green run for an older tree does not prove a newer integration-relevant `main` tree.

The cloud workflow is not licensed local BricsCAD runtime proof. LOCAL_ONLY NETLOAD/native UI/private-DWG/signing/performance gates remain separate evidence.

## Required claim contents

Every registration record must include:

- `Status`;
- stable agent/session identifier;
- registration date/time and timezone;
- exact baseline `main` SHA;
- priority/reason;
- reserved feature/runtime scope;
- expected files, commands, APIs or tests;
- explicit exclusions;
- acceptance criteria;
- validation/CI plan;
- overlap/coordination notes;
- implementation branch once created;
- implementation commit SHA(s) once available;
- intended integration batch when known;
- completion condition.

Suggested Markdown body/template when a claim file or PR body is useful:

```markdown
# Work claim — <short title>

- Status: `ACTIVE`
- Agent: `<stable-agent-id>`
- Registered: `<ISO-8601 timestamp with timezone>`
- Baseline main SHA: `<40-character SHA>`
- Implementation branch: `agent/<agent-id>/<scope>`
- Integration batch: `integration/<batch-id>` or `TBD`
- Priority: `<reason or LOCAL-### reference>`

## Reserved scope

<Exact lane.>

## Expected surfaces

- `<file, command, API, test or runtime scenario>`

## Excluded scope

- `<neighboring work this agent is not taking>`

## Acceptance criteria

- `<observable requested outcome>`

## Validation plan

- `<checks/evidence/CI>`

## Coordination

<Known neighboring claims and boundaries.>

## Completion condition

<Concrete lane outcome and whether integration is part of this prompt.>
```

## Overlap and collision rules

Overlap is broader than editing the same file. Claims conflict when they independently implement or qualify the same user-visible capability, command family, ownership/transaction contract, API behavior, test scenario or canonical status—even if they plan different files.

Immediately before material implementation writes and before handing work to integration:

1. refresh `origin/main`;
2. inspect commits/claims/issues/PRs added since the last check;
3. compare exact file/path/symbol/test/runtime surfaces;
4. stop rather than stacking a duplicate implementation;
5. record any ownership split or transfer in the visible coordination record.

An implementation commit does not retroactively count as registration.

## Scope changes and handoff

If work expands beyond the published reservation:

1. stop before editing/testing the added implementation surface;
2. refresh `main` and recheck claims/issues/PRs;
3. amend the existing visible issue/claim PR with the exact added scope, acceptance criteria and plan;
4. verify the amendment is visible and non-overlapping;
5. then continue implementation on the agent branch.

If another agent should continue a lane, record exact completed state, remaining work and successor boundary, then mark the old claim `RELEASED` or keep it `BLOCKED` until takeover is coordinated.

## Closing a claim

On successful lane completion/handoff, record:

- implementation branch;
- implementation commit(s);
- PR/reference;
- validation actually executed and exact CI result where applicable;
- remaining LOCAL_ONLY/policy gates;
- whether `MERGED TO MAIN` is `YES` or `NO`;
- integration batch/merge/final `main` SHA only when integration was actually authorized and performed.

Close/update the issue/PR or historical claim according to its lifecycle. An ordinary session must not create a direct `main` status-only commit merely to close the claim.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- Claim publication through an issue/claim PR does not authorize arbitrary Actions operations or `main` writes.
- Applicable task-scoped non-destructive CI is part of the completion loop; release/publish CI remains governed by `CI_POLICY.md`.
- The automatic post-integration V25 cloud run is standing owner policy only after an authorized integration-relevant `main` landing.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.

# QS3D agent work registration and integration

**Owner rule:** publish a Markdown reservation to `origin/main` before beginning substantive work, but keep implementation code on agent branches until the participating batch is integrated through one final `main` landing.

This file is the canonical agent-ownership and batch-integration protocol. It supersedes older direct-to-`main` implementation wording elsewhere in the repository. `CI_POLICY.md` is authoritative for the automatic post-integration CI behavior.

## Source of truth

Work claims live under:

```text
docs/agent-work-claims/
```

Use one file per reservation. `docs/LOCAL-AGENT-INBOX.md` remains the LOCAL_ONLY product/runtime queue; claim files record temporary agent ownership.

Claim statuses:

- `ACTIVE` — lane is reserved; implementation may be in progress or ready on an agent branch but not yet fully integrated.
- `BLOCKED` — lane is blocked and remains reserved.
- `COMPLETED` — required implementation is integrated into the intended final batch/main result and close-out is recorded.
- `RELEASED` — the agent stopped and intentionally made the lane available without claiming completion.

Do not delete completed/released claims; they are coordination history.

## Mandatory sequence before implementation

1. Fetch current `origin/main` and inspect recent commits.
2. Read `AGENTS.md`, `CI_POLICY.md`, this file, and every `ACTIVE` / `BLOCKED` claim.
3. Choose a non-overlapping lane.
4. Add one uniquely named claim:

   ```text
   docs/agent-work-claims/YYYY-MM-DD-<agent-id>-<short-scope>.md
   ```

5. Record exact scope, expected files/symbols/tests, exclusions, baseline `main` SHA, validation plan and agent identity.
6. Commit the claim **alone** and push that claim-only commit to `origin/main`.
7. Fetch again and verify the claim commit is reachable from current `origin/main`.
8. Re-read claims added concurrently; resolve any overlap before code work begins.
9. Create or refresh a dedicated implementation branch from the latest valid baseline, normally:

   ```text
   agent/<agent-id>/<scope>
   ```

10. Implement only the reserved lane on that branch.

A private/local claim, chat message, draft patch or unpushed branch is not a reservation. Claim visibility on `main` is intentional; source implementation on `main` is not.

## Implementation branches: no independent source landing to `main`

After the claim is visible, source/test/script/workflow implementation must stay on the agent implementation branch until integration.

Agents must not independently push their implementation batch directly to `main`, and must not merge their own feature PR directly to `main` when the work is part of a wider multi-agent owner request.

Use this shape:

```text
origin/main
  ├─ claim-only Markdown commits
  ├─ agent/remote001/wall-fix
  ├─ agent/remote002/schedule-fix
  ├─ agent/remote003/health-fix
  └─ integration/<batch-id>
        ├─ wall-fix
        ├─ schedule-fix
        └─ health-fix
             ↓
        one final merge to main
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
7. publish the implementation branch and record its branch name plus implementation commit(s) in the claim;
8. keep the claim `ACTIVE` until the batch integrator has actually integrated the lane into the combined candidate;
9. hand the branch/commit SHA to the batch integrator.

An implementation branch being pushed is **not** `ALL MERGED TO MAIN`.

## Batch integration branch

For a multi-agent owner request, one integration coordinator creates or selects:

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
9. merge the integration branch to `main` **once**;
10. refresh `main` and record the exact resulting SHA.

Do not merge agent implementation branches one-by-one into `main` just to assemble the batch there. Assemble the batch on `integration/<batch-id>`, then perform one final landing.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, agents may state **ALL MERGED TO MAIN** only after an integration reviewer has freshly verified all of the following against current `origin/main`:

- every participating required claim is `COMPLETED`, `RELEASED`, or explicitly excluded/superseded from the batch;
- every required implementation commit is represented in the integrated result and reachable from current `main`;
- no required code exists only on an agent branch, local worktree, stash, draft patch or unmerged PR;
- the reviewer refreshed `main` after the one final integration landing;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations, or known semantic/API/test collisions;
- required remote-safe build/tests/smoke/preflights passed on the combined candidate or exact integrated tree;
- the final report names the exact current `main` SHA.

Branch existence is not proof. Branch deletion is not proof. Issue state is not proof. PR UI state alone is not proof. The current integrated tree and commit reachability are authoritative.

## Claim completion timing

A lane should not be marked `COMPLETED` merely because the agent branch is ready.

Recommended progression inside the claim:

```text
ACTIVE
  -> implementation branch pushed
  -> implementation SHA recorded
  -> accepted into integration/<batch-id>
  -> final main landing verified
  -> COMPLETED
```

If a lane is intentionally dropped or superseded, mark it `RELEASED` and explain why.

The integration coordinator may perform the final claim-status close-out after verifying the lane is actually present in the combined result.

## Integration freeze gate before final CI

Before the final merge from `integration/<batch-id>` to `main`:

1. identify the exact owner request/batch;
2. stop participating agents from adding more source changes to that candidate;
3. verify all required agent branches/commits are integrated or explicitly excluded;
4. verify no required `ACTIVE`/`BLOCKED` lane remains unresolved for the claimed completed scope;
5. run combined-tree validation;
6. inspect the combined integration diff;
7. record the integration candidate SHA;
8. perform one final merge to `main`;
9. fetch `main` again and record the final exact SHA.

Canonical state progression:

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

- claim-only/docs-only commits to `main` do not trigger this final V25 cloud run;
- agent implementation branches do not trigger it;
- integration work before the final `main` landing does not trigger it;
- the one final source landing to `main` does trigger it;
- release-preparation pushes made by `github-actions[bot]` are ignored by the dispatcher to prevent recursion;
- a green run for an older tree does not prove a newer integration-relevant `main` tree.

The cloud workflow is not licensed local BricsCAD runtime proof. LOCAL_ONLY NETLOAD/native UI/private-DWG/signing/performance gates remain separate evidence.

## Required claim contents

Every claim must include:

- `Status`;
- stable agent identifier;
- registration date/time and timezone;
- exact baseline `main` SHA;
- priority/reason;
- reserved feature/runtime scope;
- expected files, commands, APIs or tests;
- explicit exclusions;
- validation plan;
- overlap/coordination notes;
- implementation branch once created;
- implementation commit SHA(s) once available;
- intended integration batch when known;
- completion condition.

Minimum template:

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

## Validation plan

- `<checks/evidence>`

## Coordination

<Known neighboring claims and boundaries.>

## Completion condition

<Concrete integrated outcome.>
```

## Overlap and collision rules

Overlap is broader than editing the same file. Claims conflict when they independently implement or qualify the same user-visible capability, command family, ownership/transaction contract, API behavior, test scenario or canonical status—even if they plan different files.

Immediately before material implementation writes and before handing work to integration:

1. refresh `origin/main`;
2. inspect commits/claims added since the last check;
3. compare exact file/path/symbol/test/runtime surfaces;
4. stop rather than stacking a duplicate implementation;
5. record any ownership split or transfer in the claim.

An implementation commit does not retroactively count as registration.

## Scope changes and handoff

If work expands beyond the published reservation:

1. stop before editing/testing the added implementation surface;
2. refresh `main` and recheck claims;
3. amend the existing claim with the exact added scope;
4. push that claim amendment alone to `main`;
5. verify the amendment is reachable and non-overlapping;
6. then continue implementation on the agent branch.

If another agent should continue a lane, record exact completed state, remaining work and successor boundary, then mark the old claim `RELEASED` or keep it `BLOCKED` until takeover is coordinated.

## Closing a claim

On successful integration, record:

- implementation branch;
- implementation commit(s);
- integration batch/merge reference;
- final current `main` SHA containing the work;
- validation actually executed;
- remaining LOCAL_ONLY/policy gates;
- related handoff/inbox updates.

Then mark the claim `COMPLETED` and publish the close-out Markdown to `main`.

Closing/status documentation after the source landing is allowed on `main` and does not cause the source-path automatic V25 cloud dispatcher to run again.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- Claim publication does not authorize arbitrary manual Actions operations.
- The automatic post-integration V25 cloud run is standing owner policy and is handled by the dispatcher, not by each coding agent.
- Other manual CI/release operations remain governed by `CI_POLICY.md`.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.

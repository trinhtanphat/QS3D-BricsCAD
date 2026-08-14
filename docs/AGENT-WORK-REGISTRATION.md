# QS3D agent work registration

**Owner rule:** publish a Markdown reservation to `origin/main` before beginning substantive work. This is the canonical agent-ownership protocol for a repository that is edited and qualified concurrently.

The goal is simple: another agent must be able to fetch `main`, see who owns a lane, and choose different work before duplicate implementation begins.

## Source of truth

Work claims live under:

```text
docs/agent-work-claims/
```

Use one file per reservation. Do not turn `docs/LOCAL-AGENT-INBOX.md` into an agent-assignment table: that inbox records LOCAL_ONLY product gates, while claim files record temporary agent ownership.

The following claim statuses reserve their stated scope:

- `ACTIVE` — implementation or qualification is in progress.
- `BLOCKED` — the lane is blocked and remains reserved until its status changes.

The following statuses do not reserve scope:

- `COMPLETED` — the reserved work and close-out were pushed.
- `RELEASED` — the agent stopped and made the lane available without claiming completion.

Do not delete completed/released claims; they are lightweight coordination history.

## Required sequence before work

1. Fetch `origin/main`, integrate it safely, and inspect recent commits.
2. Search current claims, including blocked reservations:

   ```powershell
   rg -n -C 3 '^- Status: `(ACTIVE|BLOCKED)`\r?$' docs/agent-work-claims
   ```

3. Read the full files for any claim that touches the same feature, files, commands, tests, runtime scenario or documentation surface.
4. Choose a non-overlapping lane. If overlap exists, select other work or obtain an explicit split recorded in both claims before proceeding.
5. Add a uniquely named claim:

   ```text
   docs/agent-work-claims/YYYY-MM-DD-<agent-id>-<short-scope>.md
   ```

6. Commit the claim alone and push it to `origin/main`. Do not include product source, tests, scripts, runtime evidence or implementation docs in that registration commit.
7. Fetch again and verify that the pushed commit is an ancestor of current `origin/main`:

   ```powershell
   git merge-base --is-ancestor <claim-commit> origin/main
   ```

8. Recheck claims added by concurrent agents during the push. If a new claim overlaps, stop and resolve ownership before implementation.
9. Begin work only after all preceding checks pass.

A local commit, private branch, chat note or unpushed Markdown file is not a reservation. If a registration push is rejected because `main` moved, fetch/integrate the new head, re-read claims, and publish the reservation only if it is still non-overlapping.

## Canonical multi-agent execution order

For every owner request that uses multiple agents, including broad `continue all` work, the following phase order is mandatory. Agents may execute implementation in parallel only after ownership is published and only when their lanes are demonstrably non-overlapping.

1. **Coordinator snapshot:** fetch the latest `origin/main`, record the exact head SHA, inspect recent commits, current `ACTIVE`/`BLOCKED` claims and the relevant product/runtime queue.
2. **Partition the request:** split the owner request into narrow, independently verifiable lanes. Treat semantic/API/test/runtime overlap as a collision even when the agents expect to edit different files.
3. **Claim before implementation:** every implementation/qualification agent publishes a claim-only commit, verifies that it is reachable from the latest `origin/main`, then rechecks concurrent claims. No source/test/script implementation begins before this step succeeds.
4. **Implement from current main:** work from the latest integrated head, keep the patch inside the reserved scope, and avoid opportunistic neighboring cleanup.
5. **Just-in-time collision check:** immediately before each material write and before integration, refresh `origin/main`, inspect new commits and claims, and stop rather than stacking a duplicate or conflicting patch.
6. **Integrate safely:** if `main` moved, rebase/reapply/merge the intended patch onto the newest head without discarding newer work. Never force-push `main`, reset it backwards, or choose `ours`/`theirs` blindly for a semantic conflict.
7. **Validate the integrated result:** rerun the relevant build, deterministic tests, smoke tests and preflights after the final rebase/reapply, not only on the stale pre-integration tree.
8. **Push and close the lane:** push the coherent implementation/close-out, update the claim to `COMPLETED` (or `RELEASED` when intentionally abandoned), and verify every claimed implementation commit is an ancestor of the current `origin/main`.
9. **Integration review:** after participating agents finish, one integration reviewer refreshes `main` again, checks that the request's claims are terminal, verifies commit ancestry and checks for duplicate/semantic collisions across the combined result.
10. **Exact-head evidence:** report the final `main` SHA and validation tied to that SHA. GitHub Actions/release execution remains subject to `CI_POLICY.md`; when the owner explicitly authorized CI for the request, the required exact-SHA workflow must be green before reporting CI completion.

The order of these phases is mandatory even when implementation agents run concurrently. A later phase never retroactively satisfies an earlier phase: an implementation commit is not a substitute for a claim, a branch push is not a merge to `main`, and a green stale-SHA test is not proof for the current integrated head.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, agents may state **ALL MERGED TO MAIN** only after an integration reviewer has verified all of the following against a freshly fetched current `origin/main`:

- every claim participating in that owner request is terminal: `COMPLETED` or intentionally `RELEASED`; a `BLOCKED` claim may remain only when the owner request explicitly leaves that work as a documented handoff rather than claiming it completed;
- every implementation/close-out commit that is part of the requested result is reachable from the current `origin/main` (`git merge-base --is-ancestor <sha> origin/main` succeeds);
- no required change for that request exists only on an agent branch, local worktree, draft patch or unmerged pull request;
- the reviewer refreshed `origin/main` after the last participating agent push, so the reported final head is not a stale snapshot;
- the integrated current-head tree contains the intended combined behavior without unresolved merge markers, duplicate competing implementations, silently reverted neighboring work or known semantic/API/test collisions;
- all required remote-safe build/tests/smoke/preflights for the integrated tree pass, or any environment-gated evidence is explicitly classified and handed off according to this repository's local/remote policy;
- if the owner separately authorized GitHub Actions for this request, required workflow evidence is tied to the exact integrated SHA and is green; if CI was not authorized, do not dispatch it merely to satisfy this definition;
- the final report names the exact current `main` SHA used for the verification.

`ALL MERGED TO MAIN` means repository integration is complete for the specified owner request. It does **not** automatically mean every LOCAL_ONLY runtime qualification, signing/release gate or unrelated repository claim is complete. Use `ALL DONE` only when those additional requested gates are also satisfied.

## Required claim contents

Every claim must include:

- `Status`;
- stable agent identifier and, when available, task/thread identifier;
- registration date/time and timezone;
- exact baseline `main` SHA;
- priority or reason this lane was selected;
- reserved feature/runtime scope;
- expected files, commands, APIs or test surfaces;
- explicit exclusions and neighboring lanes not owned;
- intended validation/evidence;
- overlap/coordination notes;
- completion condition.

Use this minimum template:

```markdown
# Work claim — <short title>

- Status: `ACTIVE`
- Agent: `<stable-agent-id>`
- Registered: `<ISO-8601 timestamp with timezone>`
- Baseline main SHA: `<40-character SHA>`
- Priority: `<reason or LOCAL-### reference>`

## Reserved scope

<Exact implementation or qualification lane.>

## Expected surfaces

- `<file, command, API, test or runtime scenario>`

## Excluded scope

- `<nearby work this agent is not taking>`

## Validation plan

- `<checks and evidence>`

## Coordination

<Known neighboring claims or explicit non-overlap boundary.>

## Completion condition

<Concrete pushed outcome.>
```

## Overlap rules

Overlap is broader than editing the same file. Two claims conflict when they independently implement or qualify the same user-visible capability, command family, ownership/transaction contract, test scenario, or canonical documentation status—even if they planned different files.

Parallel work is allowed only when the split is explicit and independently verifiable. For example, one claim may own deterministic Core planning while another owns a named V25 runtime matrix, but both claims must record that boundary. A vague statement such as “work on Direct Draw” is too broad to coexist safely with another Direct Draw claim.

Do not take over an `ACTIVE` or `BLOCKED` claim merely because it has not moved recently. The original agent must mark it `RELEASED`, or the repository owner must coordinate a takeover that is recorded in the new claim and the old claim's status/history.

## Scope changes and handoff

If work expands beyond the published reservation:

1. stop **before reading implementation code, diagnosing that implementation, editing, or testing** the added surface;
2. fetch latest `main` and recheck `ACTIVE`/`BLOCKED` claims, including exact expected path/symbol/test names;
3. update the existing claim with the exact expanded paths/symbols/scenario and why they are required;
4. commit and push that claim update **alone** — do not bundle source/test changes in a claim amendment;
5. fetch again, verify the amendment is an ancestor of current `origin/main`, and recheck for a concurrent overlapping claim;
6. only then read/diagnose/edit/test the newly reserved surface.

If another agent should continue a lane, the current owner records the exact completed state, remaining work and intended successor boundary, then marks the old claim `RELEASED` or keeps it `BLOCKED` until the coordinated successor claim is visible.

## Just-in-time collision check before every write

A visible claim reserves a lane but does not freeze `main`. Immediately before every source/test/script write or PR merge:

1. refresh `origin/main`;
2. inspect commits added since the last collision check;
3. re-scan `ACTIVE`/`BLOCKED` claims for the exact file/path/symbol/test/runtime surface being written;
4. if another agent has written the same lane after your claim, **do not stack a duplicate patch**. Stop, compare the concurrent change against the required contract, close/release redundant work, and record the collision/handoff in Markdown;
5. if another agent claims the same lane, stop until ownership is explicitly split or transferred in both claims.

An implementation commit does **not** retroactively count as registration. Every agent must publish and verify the claim commit before beginning the work it reserves, including follow-up work discovered during an existing task.

## Closing a claim

On successful completion, update the same claim to `COMPLETED` and record:

- implementation commit(s);
- final pushed SHA;
- validation actually executed;
- remaining LOCAL_ONLY or policy gates;
- related handoff/inbox updates.

The completion update may be part of the coherent implementation close-out commit because the reservation was already published beforehand. Push and verify current remote status before reporting completion.

When abandoning a lane, mark it `RELEASED`, state what was and was not changed, restore or preserve the worktree safely, and push the release note before another agent begins that scope.

## Git, CI and evidence boundaries

- Continue following the frequent fetch/rebase/reapply discipline in `AGENTS.md`; a claim does not freeze `main`.
- Never force-push or overwrite concurrent work.
- Registration does not authorize GitHub Actions, build/release dispatch or publication. `CI_POLICY.md` remains authoritative.
- Local/private evidence remains under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.

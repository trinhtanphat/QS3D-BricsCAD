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

1. stop before editing the added surface;
2. fetch latest `main` and recheck claims;
3. update the existing claim with the expanded scope;
4. commit and push that claim update alone;
5. verify it is present on current `origin/main` before continuing.

If another agent should continue a lane, the current owner records the exact completed state, remaining work and intended successor boundary, then marks the old claim `RELEASED` or keeps it `BLOCKED` until the coordinated successor claim is visible.

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

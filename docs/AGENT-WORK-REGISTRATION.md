# QS3D agent work registration and integration

**Owner rule:** normal AI agents/chat sessions treat `origin/main` as read-only. Every task—including source, tests, scripts, workflows, documentation, Markdown, claim/handoff/status and chores—must be done on a dedicated issue/branch/PR. Only an agent/session explicitly authorized by the repository owner as an integration/merge coordinator may change `main`.

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for `main` write permission. This file is the canonical work-registration and batch-integration protocol. `CI_POLICY.md` is authoritative for CI behavior after an authorized integration landing.

## Source of truth for reservations

Use a GitHub Issue as the immediately visible work reservation whenever practical. Historical Markdown claims remain under:

```text
docs/agent-work-claims/
```

New or updated Markdown claims may still be used for repository history, but they must be committed on the task branch/PR; they are **not** pushed directly to `main` as a prerequisite for implementation.

A reservation should identify:

- status (`ACTIVE`, `BLOCKED`, `COMPLETED`, or `RELEASED`);
- stable agent/session identity;
- exact baseline `main` SHA;
- exact scope and exclusions;
- expected files/symbols/tests/runtime surfaces;
- validation plan;
- task branch and PR when created;
- related issue and integration batch when known.

`ACTIVE` and `BLOCKED` reservations remain owned until completed, released, superseded, or explicitly reassigned by the owner/coordinator.

## Mandatory sequence for a normal agent

1. Fetch/read current `origin/main` and record the exact SHA.
2. Read `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, this file, open relevant Issues/PRs, and active/blocking claims.
3. Choose a non-overlapping lane.
4. Create or update a GitHub Issue to register the lane, unless an existing owner-created issue already uniquely identifies it.
5. Create a dedicated branch from the latest valid baseline, normally:

   ```text
   agent/<agent-id>/<scope>
   ```

6. Put every repository change for the task on that branch, including docs/Markdown/claims/chores.
7. Implement only the reserved lane.
8. Run relevant branch-local/static/unit/smoke validation.
9. Re-fetch `origin/main`; if it moved, reconcile safely without overwriting concurrent work.
10. Push only the task branch.
11. Open/update a PR targeting the intended integration branch or `main`.
12. Stop before merge unless the owner explicitly authorized this session to merge/integrate.

A chat message, local patch, or unpushed branch is not a visible reservation. An Issue plus pushed task branch/PR is the preferred coordination surface.

## No implicit `main` authorization

The following owner phrases authorize task work but **do not authorize a write or merge to `main`** by themselves:

- `fix bug`
- `update code`
- `implement all`
- `continue all`
- `commit`
- `commit push git`
- `review and fix`
- `update docs`
- `update md`
- `chore`
- `run CI`
- `fix CI`

A session may change `main` only after explicit owner authorization such as `merge all về main`, `bạn là integration coordinator`, `cho phép merge PR này vào main`, or another equally clear instruction naming the merge/integration action.

Authorization is limited to the named PR/batch/task. It does not carry forward automatically to later work.

## Branch discipline

Every normal agent must:

1. base its branch on the latest valid `main` baseline;
2. periodically refresh `origin/main` and inspect relevant concurrent work;
3. keep edits inside the reserved scope;
4. make coherent lane/request-level commits rather than file-by-file noise;
5. never force-push or reset shared `main`;
6. never update the `main` ref directly;
7. never use the GitHub contents API against `main` for docs, claims, chores or code;
8. never merge its own PR unless explicit owner merge authorization was granted;
9. record its branch/commit/PR in the Issue or task handoff.

A pushed branch or open PR is **not** `ALL MERGED TO MAIN`.

## Documentation, Markdown, claims and chores

There is no docs-only exception. These surfaces also stay on task branches until an authorized merge:

```text
docs/**
*.md
docs/agent-work-claims/**
README.md
handoff/status/inbox files
policy files
release-note preparation
non-functional chores
```

If a task contains only documentation/Markdown/chore changes, it still uses a branch and PR.

## Multi-agent integration branch

For a multi-agent owner request, the owner-authorized coordinator should assemble the combined candidate on:

```text
integration/<batch-id>
```

The coordinator must:

1. refresh latest `origin/main`;
2. identify the exact authorized participating Issues/PRs/branches;
3. merge/cherry-pick/rebase required agent branches into the integration branch without silently dropping commits;
4. resolve semantic/API/test conflicts deliberately rather than choosing `ours`/`theirs` blindly;
5. verify no required work remains only on an unmerged branch/PR;
6. run relevant combined-tree validation;
7. inspect the final diff for accidental reversions, duplicate implementations and contract mismatches;
8. freeze and record the integration candidate SHA;
9. merge to `main` only within the owner's explicit authorization;
10. fetch `main` again and record the exact resulting SHA.

Do not assemble a multi-agent batch by independently landing each agent PR on `main` unless the owner explicitly requests that specific integration strategy.

## Definition of `ALL MERGED TO MAIN`

For a specific owner request, state **ALL MERGED TO MAIN** only after an authorized integration reviewer freshly verifies:

- every required Issue/reservation is terminal or explicitly excluded/superseded;
- every required implementation/docs commit is represented in the integrated result and reachable from current `main`;
- no required work exists only on an agent branch, worktree, stash, draft patch or unmerged PR;
- current `main` was refreshed after the authorized landing;
- the combined tree contains the intended behavior without unresolved merge markers, accidental reversions, duplicate competing implementations or known semantic/API/test collisions;
- required remote-safe validation passed or environment-gated evidence is explicitly handed off;
- the exact current `main` SHA is recorded.

Branch deletion, Issue state, PR UI state, or a previous CI run is not sufficient proof.

## Scope changes and handoff

If work expands beyond the registered scope:

1. stop before touching the added implementation surface;
2. refresh `main` and recheck Issues/PRs/reservations;
3. update the task Issue and branch claim/handoff with the added scope;
4. resolve any overlap;
5. continue on the same task branch or a new dedicated branch as appropriate.

Do not push a claim amendment to `main` merely to reserve the expanded scope.

If another agent should continue, leave exact completed state, remaining work, branch/commit/PR references and successor boundary in the Issue/PR/handoff.

## Closing a task

Before an authorized merge, update the Issue/PR with:

- branch name;
- implementation/docs commit SHA(s);
- validation actually executed;
- known LOCAL_ONLY/policy gates;
- intended integration batch.

After the authorized merge, the coordinator may close/update the Issue and, when repository-history Markdown is useful, include the claim close-out in the same authorized integration/docs PR. A normal agent does not push close-out Markdown directly to `main`.

## CI boundary

The automatic V25 cloud dispatcher is path-filtered. Agent branches and PRs do not trigger the final `main` integration release path. After an authorized `main` landing, ordinary docs/Markdown-only changes remain outside the automatic dispatch path unless they also modify an integration-relevant watched file.

Changed paths, not commit-message prefixes, determine whether an automatic dispatch is eligible. A `chore:` commit that changes `scripts/**`, workflows, build props, solution files, source or tests is integration-relevant despite its commit message.

See `CI_POLICY.md` and `.github/workflows/dispatch-v25-cloud-after-main-integration.yml`.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- Normal task authorization never implies `main` merge authorization.
- CI authorization never implies `main` merge authorization.
- `main` merge authorization never implies unrelated manual CI/release authorization.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by the local runbooks.
- GitHub branch protection/rulesets should enforce this policy where possible; track hard-enforcement work in the repository governance issue for `main` protection.

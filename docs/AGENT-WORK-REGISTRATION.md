# QS3D agent work registration and integration

**Owner rule:** normal AI agents/chat sessions treat `origin/main` as read-only. Every task—including source, tests, scripts, workflows, documentation, Markdown, claim/handoff/status and chores—must be done on a dedicated Issue/branch/PR. Only an agent/session explicitly authorized by the repository owner as an integration/merge coordinator may change `main`.

`docs/MAIN-WRITE-AUTHORIZATION.md` is authoritative for `main` write permission. This file is the canonical work-registration and batch-integration protocol. `CI_POLICY.md` is authoritative for CI behavior, including the CI-neutral-only exemption.

## Source of truth for reservations

Use a GitHub Issue as the immediately visible work reservation whenever practical. Historical Markdown claims remain under `docs/agent-work-claims/` and may be updated on the task branch/PR for repository history.

A reservation should identify status (`ACTIVE`, `BLOCKED`, `COMPLETED`, or `RELEASED`), stable agent/session identity, exact baseline `main` SHA, scope/exclusions, expected files/symbols/tests/runtime surfaces, validation plan, task branch/PR, related Issue, and integration batch when known.

`ACTIVE` and `BLOCKED` reservations remain owned until completed, released, superseded, or explicitly reassigned.

## Mandatory sequence for a normal agent

1. Fetch/read current `origin/main` and record the exact SHA.
2. Read `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, this file, relevant Issues/PRs and active/blocking claims.
3. Choose a non-overlapping lane and register/update its Issue.
4. Create a dedicated branch from the latest valid baseline, normally `agent/<agent-id>/<scope>`; use `recovery/<agent-id>/<scope>` for a dedicated CI-repair lane when appropriate.
5. Put every repository change for the task on that branch, including docs/Markdown/claims/chores.
6. Implement only the reserved lane and run relevant local/static/unit/smoke validation.
7. Re-fetch `origin/main`; reconcile safely if it moved without discarding concurrent work.
8. Push the task branch and open/update a PR targeting the intended integration branch or `main`.
9. Record the exact head SHA after the final intended task push.
10. Classify the final task diff. If every changed path is CI-neutral-only under `CI_POLICY.md`/`.github/workflows/ci.yml`, record that classification and relevant lightweight checks; full build CI is not required. Otherwise observe the automatic task CI for that **exact head SHA**.
11. When CI is required and is not `success`, keep the reservation active, diagnose/fix on the same task branch, push a new head SHA and repeat the exact-SHA CI gate.
12. Only after the applicable completion gate is satisfied—`CI_GREEN` for implementation-relevant work, or CI-neutral classification plus relevant lightweight validation for docs/housekeeping-only work—may the agent report the branch task ready/completed for remote-safe scope.
13. Stop before merge unless the owner explicitly authorized this session to merge/integrate.

An Issue alone does not run CI because it has no source tree. CI evidence belongs to the branch/PR commit SHA when CI is required.

## CI-neutral-only exemption

A task is CI-neutral-only only when **all** changed files are in the ignore set configured by `.github/workflows/ci.yml`: `docs/**`, `**/*.md`, `.gitignore`, `.gitattributes`, `.editorconfig`, `LICENSE*`, `NOTICE*`, and GitHub Issue/PR templates.

This is a changed-path rule, not a Conventional Commit rule. A commit named `chore: ...` does not bypass CI if it touches source, tests, project/build files, dependencies, scripts, workflows, packaging, runtime-affecting configuration, or any other non-ignored file. A mixed diff always follows the full exact-SHA CI gate. Workflow changes under `.github/workflows/**` are deliberately not exempt.

## Mandatory completion gate

For any task that is not CI-neutral-only, a normal agent **must not stop as completed**, mark its claim/Issue `COMPLETED`, or state that the task is finished while the required CI for its exact current head SHA is queued, running, cancelled, skipped, failed, timed out, or absent.

`CI_GREEN` means the required automatic task validation completed with `success` for the exact current branch/PR head SHA. Green evidence from another branch, another PR, an older SHA, or current `main` does not satisfy the task gate.

For CI-neutral-only work, `CI_GREEN` is not applicable; a skipped/nonexistent build workflow is not relabeled green. Completion evidence is the verified path classification plus relevant lightweight documentation/metadata checks.

If the lane also requires licensed BricsCAD/native/UI/private-DWG/signing evidence that the automatic GitHub-hosted workflow cannot provide, remote CI success is necessary but not sufficient: the task remains `BLOCKED`/handed off until the required environment-specific gate is satisfied, or the reservation explicitly narrows completion to source-safe scope without claiming native/runtime PASS.

## Branch and PR CI behavior

`.github/workflows/ci.yml` runs automatically for implementation-relevant pushes to `agent/**`, `recovery/**`, `integration/**`, PRs targeting `main`, and pushes to `main`. CI-neutral-only paths are filtered before the full build workflow starts. Multiple agents use the same workflow definition but receive separate workflow runs for their own refs and exact SHAs when CI is required.

When a branch also has an open PR, concurrency may cancel a superseded duplicate run for an older event. The agent must verify that at least the surviving required run for the exact current head SHA completed successfully; cancelled/stale runs are not evidence.

A PR that contains a workflow/source change plus later docs-only commits is still implementation-relevant because the PR diff includes non-neutral files, so PR CI may correctly run even when the latest individual push was docs-only.

## No implicit `main` authorization

Task instructions such as `fix bug`, `update code`, `implement all`, `continue all`, `commit`, `commit push git`, `review and fix`, `update docs`, `chore`, `run CI`, or `fix CI` authorize task work but do not by themselves authorize a direct write/merge to `main`.

A session may change `main` only after explicit owner authorization naming the merge/integration operation for that PR/batch/task. Authorization is scope-specific and does not automatically carry forward.

## Branch discipline

Every normal agent must base work on a current valid `main` baseline, periodically refresh concurrent work, stay inside reserved scope, use coherent request/lane commits, never force-push/reset shared `main`, never update `main` directly, never use contents/ref APIs to bypass the PR path, and never merge its own PR without explicit owner authorization.

All docs/Markdown/claims/chores follow the same branch/PR rule; there is no docs-only direct-main exception. CI exemption does not grant a write/merge exemption.

## Multi-agent integration branch

For a multi-agent owner request, the owner-authorized coordinator should assemble the combined candidate on `integration/<batch-id>`.

The coordinator must refresh current `origin/main`, identify exact participating Issues/PRs/branches, integrate required changes without silently dropping commits, resolve semantic/API/test conflicts deliberately, verify no required work remains only off the integration candidate, run automatic integration-branch CI when implementation-relevant paths changed and any additional combined validation, inspect for accidental reversions/duplicate implementations, freeze the exact integration head SHA, require `CI_GREEN` when applicable, merge to `main` only within explicit authorization, then fetch `main` again and record the exact resulting SHA.

## Definition of ALL MERGED TO MAIN

State **ALL MERGED TO MAIN** only after an authorized integration reviewer freshly verifies every required reservation is terminal or explicitly excluded/superseded, every required change is represented in current `main`, no required work exists only on an agent branch/worktree/stash/unmerged PR, the combined tree has no known merge/semantic collisions, required exact-main remote-safe CI is green when implementation-relevant paths changed, environment-gated evidence is explicitly classified, and the exact current `main` SHA is recorded.

A branch deletion, Issue/PR UI state, or previous CI run is not sufficient proof.

## Release boundary

The automatic task CI is not a release workflow. `.github/workflows/release-v25-cloud.yml` remains a confirmed **main-only release** path and must not be used as the validation workflow for agent branches or ordinary PRs.

After an authorized integration-relevant `main` landing, `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` may dispatch the main-only V25 cloud release path according to `CI_POLICY.md`. That post-main release behavior is separate from the per-agent completion CI gate and is already path-scoped so documentation-only landings do not dispatch V25 cloud release.

## Closing a task

Before PR handoff, update the Issue/PR with branch name, implementation/docs commit SHA(s), exact current head SHA, validation actually executed, automatic CI run/result when CI was required or the CI-neutral-only path classification when it was not, known LOCAL_ONLY/policy gates, and intended integration batch.

A normal agent may close a source-safe reservation only after its applicable completion boundary is satisfied. It still stops before merge unless separately authorized.

## Git, CI and evidence boundaries

- Never force-push `main` or reset it backwards.
- Never silently overwrite another agent's work.
- CI authorization never implies `main` merge authorization.
- CI-neutral exemption never implies `main` write authorization.
- `main` merge authorization never implies unrelated release authorization.
- Remote/static CI is not licensed BricsCAD runtime evidence.
- Local/private evidence stays under gitignored `artifacts/`; commit only sanitized summaries allowed by runbooks.
- GitHub branch protection/rulesets should require the stable task-CI check for implementation PRs to `main` where possible and prevent force-push/deletion. If a ruleset requires a status on docs-only PRs, prefer a lightweight status/ruleset condition rather than the full build workflow.

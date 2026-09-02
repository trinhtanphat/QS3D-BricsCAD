# Green PR Drain Automation Design

## Goal

Automatically serialize same-repository pull requests into `main`: when the exact PR head passes `QS3D Shared Branch and Integration CI`, merge that exact head, then refresh the remaining open same-repository PR branches from the new `main` so they revalidate against the latest base.

## Safety invariants

1. Only react to `workflow_run` events for `QS3D Shared Branch and Integration CI` whose triggering event is `pull_request` and whose conclusion is `success`.
2. Only operate on open, non-draft PRs targeting `main` from `trinhtanphat/QS3D-BricsCAD` itself.
3. Re-fetch the PR and require `pr.head.sha == workflow_run.head_sha`; stale successful runs are skipped.
4. Merge with the expected head SHA so a concurrent push invalidates the merge instead of merging a different commit.
5. Serialize automation with one repository-wide concurrency group and never cancel an in-progress drain.
6. After a successful merge, list remaining open PRs targeting `main`; update only same-repository branches and use each PR's current head SHA as `expected_head_sha`.
7. Treat update-branch conflicts / non-fast applicability as per-PR skips. Never force-push, reset, rewrite, or close a conflicting branch.
8. Fork PRs are read-only and skipped by this automation.
9. Mutations require `QS3D_AUTOMERGE_TOKEN`. Do not fall back to the workflow `GITHUB_TOKEN`, because GitHub suppresses or gates recursive workflow triggering for mutations made with the repository token; the next `synchronize` CI cycle must start normally.
10. The token must be a fine-grained PAT restricted to this repository with only Contents: Read and write and Pull requests: Read and write.

## Architecture

A single workflow, `.github/workflows/green-pr-drain.yml`, listens to completion of the shared CI workflow. It has one mutation job protected by a fixed concurrency key. The job validates the triggering run and PR with GitHub REST API calls, merges the exact head with `PUT /pulls/{number}/merge`, and then refreshes other eligible PRs with `PUT /pulls/{number}/update-branch`.

The workflow uses `gh api` on `ubuntu-latest`, so it introduces no third-party Actions dependency. API responses are parsed with `jq`, which is available on GitHub-hosted Ubuntu runners. The job fails closed if the dedicated secret is absent or if the target merge itself fails; refresh failures are isolated per PR so one conflict does not stop refresh attempts for unrelated PRs.

## Event and data flow

1. A PR to `main` runs `QS3D Shared Branch and Integration CI`.
2. When that run completes successfully, `green-pr-drain.yml` receives `workflow_run.completed`.
3. The job rejects non-`pull_request` runs and requires exactly one associated PR.
4. The job re-fetches the PR and verifies state, base, source repository, draft state, and exact head SHA.
5. The job submits a merge request with `sha=<exact head>` and `merge_method=merge`.
6. If GitHub merges it, `main` advances.
7. The job lists all remaining open PRs with base `main` and updates each eligible same-repository branch with `expected_head_sha`.
8. Each successful update creates a normal PR synchronization event using the dedicated PAT, causing shared CI to run against the new base.
9. The next green PR repeats the cycle until no eligible green PR remains.

## Error handling

- Missing `QS3D_AUTOMERGE_TOKEN`: fail before any mutation and print setup guidance.
- Missing/ambiguous associated PR: skip safely.
- PR closed, draft, wrong base, fork, or head SHA changed: skip safely.
- Merge rejected by ruleset, checks, conflict, or concurrent head movement: fail the job; do not attempt branch refresh because `main` did not advance through this run.
- Update branch returns no change / conflict / validation error: log warning for that PR and continue.
- Unexpected authentication or API failures during branch refresh: log warning and continue to other PRs, while the workflow summary records the failure.

## Bootstrap

Because a `workflow_run` workflow must exist on the default branch before it can orchestrate future CI completions, this carrier PR is merged once through the existing repository rules. After it lands on `main` and `QS3D_AUTOMERGE_TOKEN` exists, subsequent eligible PRs are handled automatically.

## Non-goals

- No bypass of required checks or rulesets.
- No auto-resolution of merge conflicts.
- No mutation of forks.
- No force pushes or history rewriting.
- No BricsCAD runtime behavior changes.

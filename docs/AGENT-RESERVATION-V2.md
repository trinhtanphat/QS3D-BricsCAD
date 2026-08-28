# Agent reservation protocol v2

Reservation v2 closes the pre-PR race where two agents can both create visible carriers, edit overlapping production paths, and only discover the collision when a PR already exists.

## Activation and migration

`docs/agent-reservation-v2.marker` is the activation marker. The first commit that adds that file defines the activation timestamp used by the machine gate.

- An Issue created before the marker is a legacy reservation unless it explicitly opts into `Reservation-Protocol: v2`.
- An Issue created at or after the marker must satisfy reservation v2 before an `agent/**` branch can pass shared branch CI.
- Existing active legacy carriers are grandfathered; do not rewrite, close, rename, or duplicate them merely for migration.
- This repository protocol does not change external ChatGPT schedule cadence, enabled state, task IDs, or account-side orchestration.

## Required visible reservation

Before mutating repository files for a new agent lane, create one GitHub Issue and publish these fields:

```text
Lane-Key: issue-123
Reservation-Protocol: v2
Canonical owner/session: account:<github-login>|session:<globally-distinct-opaque-id>
Canonical carrier: agent/<globally-distinct-branch-owner>/issue-123-<scope>
Ownership-Key: <stable-semantic-ownership-key>
Expected-Paths: path/to/file.cs; path/to/other.py; src/OwnedDirectory/
```

The Issue must remain open while the carrier is active. The branch must contain the same Issue number and its canonical carrier must match exactly.

## Owner identity

Repository ownership must be globally distinguishable from orchestration labels. Labels such as `C01`, `C02`, `C05`, `W1`, `worker`, `controller`, `ChatGPT`, `Codex`, or a model name may describe an external scheduler/role, but they are not valid reservation-v2 branch-owner tokens by themselves.

Use a stable account/session identity in the Issue and a repository-safe branch token that visibly binds to that session identity. The recommended Issue form is:

```text
account:<github-login>|session:<opaque-id>
```

## Ownership-Key

`Ownership-Key` describes the semantic authority being changed, not the Issue number or worker identity. Good keys are stable across retries and carriers for the same behavior, for example:

```text
core.dependency.known-count-overrun
v25.workspace.model-tree-lifecycle
repo.agent-reservation-collision-enforcement
```

Keys such as `issue-123`, `task-123`, or `lane-123` are invalid because they do not identify semantic ownership.

When multiple open v2 Issues claim the same Ownership-Key, the first visible reservation wins by GitHub `created_at`, with Issue number as a deterministic tie-break. Every later claimant must stop as:

```text
DUPLICATE_CARRIER / NO MUTATION
```

## Expected-Paths

`Expected-Paths` is a semicolon-separated list of repository-relative literal files or directory prefixes. A directory prefix ends with `/` and owns descendants. Globs and traversal segments are forbidden.

Examples:

```text
Expected-Paths: src/QS3D.Core/Services/DependencyGraph.cs; tests/QS3D.Core.SmokeTests/DependencyGraphKnownCountContractSmoke.cs
Expected-Paths: src/QS3D.BricsCAD.V25/Workspace/; scripts/preflight-workspace-lifecycle.py
```

Before implementation, choose the narrowest truthful path claims. If another earlier v2 reservation already claims an overlapping file/prefix, the later reservation must stop before mutation. If scope must expand, update the same canonical Issue first, rerun the collision check, and only proceed if the expanded ownership remains canonical.

Branch CI also compares actual changed paths to `Expected-Paths`; undeclared mutations fail closed. In addition, a v2 branch fails when its current changed files overlap an earlier open same-repository `agent/**` or `integration/**` PR.

## Machine enforcement

The required shared `preflight` job runs the authenticated read-only agent collision gate on both `push` and `pull_request` events.

For same-repository `agent/**` carriers it enforces:

1. visible Issue binding from the branch `issue-<number>`;
2. reservation-v2 metadata for post-activation Issues;
3. non-generic branch/session identity;
4. exact Lane-Key and canonical carrier;
5. semantic Ownership-Key uniqueness with first-visible-wins ordering;
6. first-visible-wins Expected-Paths ownership;
7. actual branch changes restricted to Expected-Paths;
8. no changed-file overlap with an earlier open agent/integration PR;
9. the existing PR Lane-Key uniqueness rule.

For `integration/**`, the existing PR Lane-Key uniqueness rule remains active; v2 Issue binding is intentionally scoped to normal `agent/**` task carriers.

The gate uses read-only Issue/PR metadata and never mutates Issues, branches, PRs, schedules, or `main`.

## Collision recovery

When the gate reports another earlier owner:

- do not rename the same fix into a new Issue or Ownership-Key;
- do not create another branch/PR for the same outcome;
- continue the earlier canonical carrier if it is yours;
- otherwise stop the overlapping mutation and choose genuinely non-overlapping work;
- reassignment/supersession still requires the normal repository ownership rules.

A red, queued, stale, draft, behind-main, or slow canonical carrier remains owned until explicitly completed, released, or superseded. Green CI or cleaner history does not create a second ownership right.

# Agent branch CI Actions lookup

This file defines the repository-wide agent procedure for locating and validating GitHub Actions evidence for a canonical task branch. It supplements `CI_POLICY.md` and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`; it does not change workflow triggers, required checks, merge authorization, or release authorization.

## Canonical branch-filtered Actions query

When checking branch CI, prefer the GitHub Actions branch-filter view for the canonical branch:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions?query=branch%3A<URL-encoded-branch-name>
```

Example:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions?query=branch%3Aagent%2Fchatgpt-gpt56sol%2Fpolyline-signed-area-precision-2673
```

The query decodes as follows:

- `%3A` = `:`;
- `%2F` = `/`;
- therefore `branch%3Aagent%2F...` means `branch:agent/...`.

The value after `branch:` is the exact Git branch name. Agents should construct the query from the canonical carrier branch recorded for the Lane-Key, URL-encoding the branch name rather than guessing from a prior run URL or stale chat state.

## Which run counts

1. Open/fetch the branch-filtered Actions result for the exact canonical branch.
2. Select the **latest run for that branch**.
3. Fetch/inspect that run and record at minimum its run ID, event, status/conclusion, `head_branch`, and `head_sha` when observable.
4. Compare `head_branch` with the canonical branch and `head_sha` with the branch's **current exact head SHA**.
5. Accept the run as branch-CI evidence only when both branch and SHA match the current canonical carrier and the required workflow/run has reached the required terminal state.

A green run for an older branch SHA is stale evidence. After any new commit, remediation, reconciliation with `main`, or other head change, the previous green run no longer satisfies the exact-head branch-CI admission gate; use the latest run for the new exact head.

Do not choose a run merely because it is green, newer globally, or visually adjacent in the Actions list. The evidence must belong to the exact canonical branch and exact current head SHA.

## Owner-supplied run URL

When the owner supplies a concrete Actions run URL such as:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions/runs/<run-id>
```

fetch that run directly. Verify its `head_branch` and `head_sha` against the canonical carrier before using it as evidence. A supplied run URL is a lookup shortcut, not an exception to exact-branch/exact-SHA validation.

## Connector/tooling fallback rule

If a generic connector wrapper returns no runs or no classic commit statuses, do not infer that branch CI is absent, successful, or failed when branch-filtered Actions evidence or a direct run URL is available.

Preferred evidence order for branch CI is:

1. owner-supplied exact Actions run URL, when provided;
2. latest run from the exact branch-filtered Actions query;
3. other connector/CLI run lookup that proves the same `head_branch` and `head_sha`;
4. only if none of those surfaces is observable, report the exact observability limitation without inventing run/job/status data.

Empty wrappers are not positive CI evidence.

## Reporting

Branch CI reporting must identify the exact evidence it actually observed, for example:

```text
✅ Branch CI: SUCCESS — run 32085484866 / head 1a9e150d... / branch agent/chatgpt-gpt56sol/polyline-signed-area-precision-2673
```

or, when the latest branch run tests an older SHA:

```text
⏳ Branch CI: STALE — latest observed run is green but tests <old-sha>; current canonical head is <new-sha>
```

Continue to follow the status-marker and pending-CI detail requirements in `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`.
# Agent branch CI Actions lookup

This file defines the repository-wide agent procedure for locating and validating GitHub Actions evidence for a canonical task branch. It supplements `CI_POLICY.md` and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`; it does not change workflow triggers, required checks, merge authorization, or release authorization.

## Two valid lookup modes

Agents may locate branch-CI evidence by either of these routes:

1. **Branch-filtered lookup** — when the run ID is not already known, open/fetch the GitHub Actions view filtered to the exact canonical branch and select the latest run for that branch.
2. **Known direct run lookup** — when a concrete Actions run ID or run URL is already known, fetch that run directly without first listing branch runs.

Neither route weakens the exact-head gate. A run counts only after its metadata is verified against the current canonical carrier and the applicable CI policy.

## Canonical branch-filtered Actions query

When the run ID is not already known, use the GitHub Actions branch-filter view for the canonical branch:

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

For this route:

1. Open/fetch the branch-filtered Actions result for the exact canonical branch.
2. Select the **latest run for that branch**.
3. Fetch/inspect that run and record at minimum its run ID, event, status/conclusion, `head_branch`, `head_sha`, and run attempt when observable.
4. Compare `head_branch` with the canonical branch and `head_sha` with the branch's **current exact head SHA**.
5. Accept the run as branch-CI evidence only when the branch/SHA and all other policy-required fields match the current gate.

## Known direct Actions run URL / run ID

When a concrete run ID is already known, an agent may fetch the run directly instead of first querying the branch Actions list.

Canonical URL shape:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions/runs/<run-id>
```

Example:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions/runs/32088766819
```

A run URL may come from the owner, another trusted GitHub surface, an earlier exact CI observation, or any other reliable lookup that exposes the concrete numeric run ID. It does **not** have to be supplied by the owner before the agent may use it.

If an observed URL contains an accidental duplicate slash before `actions`, for example:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD//actions/runs/32088766819
```

normalize it to the canonical single-slash path (or equivalently extract the numeric run ID) before recording/reporting the lookup. The evidence identity is the GitHub Actions run ID and returned run metadata, not the cosmetic number of slashes in the input URL.

A known direct run is a **first-class lookup route**, but it is not automatically valid evidence. Inspect the returned run and verify all fields required by the current lifecycle gate, including when applicable:

- workflow identity;
- event type, such as automatic `push` for pre-PR branch CI;
- `run_attempt` when the repository requires an automatic attempt-1 run;
- exact `head_branch`;
- exact current `head_sha`;
- terminal status/conclusion;
- required jobs/checks and their conclusions.

For example, run `32088766819` is an automatic `push` run whose returned metadata identifies branch `agent/chatgpt-gpt56sol/exact-head-ci-trigger-2678`, head `605f51a9fae9166d87ea262bd7c32c47784398de`, attempt 1, and terminal `success`. That run is evidence only for the matching exact carrier/head and does not prove CI for another branch or later SHA.

## Exact-head and stale-run rule

A green run for an older branch SHA is stale evidence. After any new commit, remediation, reconciliation with `main`, or other head change, the previous green run no longer satisfies the exact-head branch-CI admission gate.

This applies to both lookup modes:

- branch-filtered route: choose the latest exact-branch run and verify it tests the current head;
- direct-run route: verify the known run's `head_sha` equals the current exact head before accepting it.

Do not choose or reuse a run merely because it is green, newer globally, visually adjacent in the Actions list, or easy to fetch. Evidence must belong to the exact canonical branch and exact current head SHA and satisfy the applicable event/attempt/check requirements.

## Connector/tooling fallback rule

If a generic connector wrapper returns no runs or no classic commit statuses, do not infer that branch CI is absent, successful, or failed when branch-filtered Actions evidence or a known direct run is available.

Preferred evidence order for branch CI is:

1. known exact Actions run URL/run ID, when one is already available and can be verified against the current head;
2. latest run from the exact branch-filtered Actions query when the run ID is not known or the known run is stale/mismatched;
3. other connector/CLI run lookup that proves the same `head_branch`, `head_sha`, event/attempt, and required terminal state;
4. only if none of those surfaces is observable, report the exact observability limitation without inventing run/job/status data.

Empty wrappers are not positive CI evidence.

## Reporting

Branch CI reporting must identify the exact evidence it actually observed, for example:

```text
✅ Branch CI: SUCCESS — run 32085484866 / head 1a9e150d... / branch agent/chatgpt-gpt56sol/polyline-signed-area-precision-2673
```

or, when a directly fetched run tests an older SHA:

```text
⏳ Branch CI: STALE — run <run-id> is green but tests <old-sha>; current canonical head is <new-sha>
```

Continue to follow the status-marker and pending-CI detail requirements in `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`.

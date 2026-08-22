# Agent branch CI Actions lookup

This file defines the repository-wide agent procedure for locating and validating GitHub Actions evidence for a canonical task branch or pull request. It supplements `CI_POLICY.md` and `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`; it does not change workflow triggers, required checks, merge authorization, or release authorization.

## Self-observation invariant

CI evidence is **agent-owned repository work, not owner homework**. An AI agent/chat session must use the available GitHub/Actions surfaces to discover and verify the applicable CI itself. It must not ask the repository owner to find a run URL, check whether CI is green, paste routine Actions metadata, or press refresh when the same evidence is recoverable from repository-native surfaces available to the session.

A session may report CI observability as a tooling blocker only after it has exhausted the mandatory recovery ladder in this document and no safe authorized progress remains. An empty or unsupported generic connector result is never enough by itself to establish that blocker.

## Why CI can appear invisible to an AI session

Different GitHub tool surfaces expose different evidence classes. In particular:

- a connector may expose pull-request workflow runs/jobs while not exposing branch-filtered `push` runs through the same operation;
- a classic commit-status lookup may return no statuses even though GitHub Actions checks exist;
- a generic fetch wrapper may reject or omit a branch-filtered Actions listing while still allowing a **known exact Actions run URL/run ID** to be fetched directly;
- PR metadata, the canonical Issue/PR body, protected PR jobs, and the `PR branch CI admission gate` can expose or lead to branch-CI provenance even when a branch-run listing operation is unavailable;
- one chat session may discover a run ID that another session cannot enumerate independently unless the first session records that provenance on the canonical GitHub carrier.

These are tooling-capability differences, not CI results. Therefore:

```text
empty generic run listing
  != no Actions run exists
  != CI failed
  != CI passed
  != owner must find the run
```

Agents must fail closed on evidence identity while still exhausting all available discovery paths themselves.

## Evidence classes must not be confused

The repository can expose several complementary evidence classes:

1. **branch `push` run** — validates the exact pushed branch SHA;
2. **pull-request run** — validates GitHub's current PR merge candidate and provides protected contexts such as `preflight` and `core`;
3. **PR branch CI admission-gate evidence** — a step inside protected PR `preflight` that can prove the repository located acceptable branch-CI provenance for the current candidate according to the gate implementation;
4. **canonical carrier metadata** — exact run IDs/URLs, branch, SHA, event, attempt, and conclusions recorded in the current Issue/PR body or current trusted carrier discussion.

A successful admission-gate step is a **discovery/provenance surface**. It does not authorize inventing an exact branch-run ID or silently reusing stale evidence. When an exact branch run is required for reporting, debugging, or a policy gate and its run ID can be recovered, fetch that run directly and verify its returned metadata.

For merge eligibility, follow the current `CI_POLICY.md` and `docs/PR-CI-LIFECYCLE.md`: protected current-candidate `preflight` and `core`, strict freshness, mergeability, and expected-head protection are authoritative. Do not manufacture a replacement PR merely because one branch-run listing surface is unavailable.

## Mandatory CI evidence recovery ladder

Before telling the owner that CI cannot be checked, perform these steps in order as applicable. Stop at the first route that yields sufficient exact evidence for the current lifecycle gate; otherwise continue down the ladder.

### 0. Bind the lookup to the current canonical identity

Refresh GitHub state first and record:

- Lane-Key / canonical Issue;
- canonical branch;
- exact current branch head SHA;
- canonical PR number, if one exists;
- current PR head SHA / merge candidate as applicable;
- current `main` when freshness matters.

Never start CI discovery from a stale chat-memory SHA or a historical carrier.

### 1. Known exact Actions run ID / URL

If the current Issue, PR, trusted current comment, earlier exact observation, or another reliable repository surface already contains a concrete Actions run ID/URL, fetch that run directly.

Canonical URL shape:

```text
https://github.com/trinhtanphat/QS3D-BricsCAD/actions/runs/<run-id>
```

Verify the returned metadata required by the applicable gate, including when relevant:

- workflow identity;
- event type (`push` or `pull_request`);
- exact `head_branch`;
- exact `head_sha`;
- `run_attempt` when policy requires it;
- terminal status/conclusion;
- required jobs/checks and their conclusions.

A known direct run is a first-class lookup route. The owner does **not** need to supply the run ID if the repository already records it.

### 2. Exact branch-filtered Actions lookup

When a branch run is needed and no current direct run ID is known, use the GitHub Actions view filtered to the exact canonical branch:

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

For this route:

1. construct the query from the **current canonical branch**, not from chat memory;
2. select the latest run for that exact branch;
3. fetch/inspect the run;
4. compare its `head_branch` and `head_sha` with the current canonical branch/head;
5. accept it only if all fields required by the applicable gate match.

If the current tool wrapper cannot open/list this filtered view, that is only a reason to continue to the next fallback.

### 3. Recover provenance from the canonical PR and Issue

If a canonical PR exists, fetch its current metadata/body and the current canonical Issue body before declaring branch CI unobservable. Search those repository surfaces for:

- Actions run ID or URL;
- branch-CI provenance section;
- exact branch;
- exact SHA;
- event;
- attempt;
- conclusion.

This is especially important across chat sessions: a previous session or coordinator may already have recorded the exact run even when the current connector cannot enumerate branch push runs.

Issue/PR metadata is a discovery source, not a waiver of exact verification. Once a numeric run ID is recovered, return to step 1 and fetch that run directly when the applicable gate requires exact run metadata.

### 4. Inspect the protected PR run and admission gate

When a canonical PR has a protected PR workflow run, fetch that exact `pull_request` run and its jobs. Inspect `preflight`, including the step named **`PR branch CI admission gate`** when present.

If that step is `SUCCESS`, the repository gate itself has found acceptable branch-CI provenance for the candidate according to the committed gate logic. Use that fact to continue discovery rather than asking the owner for a run URL:

1. re-read the current PR body and canonical Issue body for the run ID/provenance the gate is validating;
2. inspect available job-step/log evidence when the tooling exposes it;
3. recover the concrete branch run ID/URL when possible;
4. fetch that exact run directly and verify its metadata when exact branch-run evidence is required.

Do **not** invent the run ID merely because admission succeeded. Conversely, do not claim `CI_UNOBSERVABLE` while a current successful admission gate plus canonical carrier metadata gives a repository-native path to recover the evidence.

If current `CI_POLICY.md` makes protected current-candidate `preflight` + `core` the authoritative merge gate, inability to enumerate a separate branch-run list must not by itself block an otherwise current, green, mergeable authorized PR. Follow the current lifecycle policy rather than an older timestamp-order rule.

### 5. Use other exact connector/CLI run lookup

If available, use another connector operation or authenticated CLI route that can prove the same exact workflow/run identity. The evidence must still bind to the correct branch/head or PR candidate and satisfy the current event/attempt/status requirements.

Do not substitute classic commit statuses for Actions evidence when the repository gate requires Actions metadata.

### 6. Only then classify a tooling observability blocker

`CI_OBSERVABILITY_BLOCKED` / owner-facing `BLOCKED` is permitted only if **all available applicable routes above were actually attempted** and none can produce the evidence required by the current lifecycle gate.

The blocker record must include:

- canonical branch/head and PR candidate identity;
- which lookup routes were attempted;
- exact empty/unsupported/error result that prevented each route;
- the last positive CI/PR evidence that was observable;
- why no current repository-native path can recover the missing evidence;
- why no other safe authorized lifecycle action remains.

Do not collapse `queued`, `running`, `red`, `stale`, or `not yet enumerated by one wrapper` into an observability blocker.

## Exact-head and stale-run rule

A green run for an older branch SHA is stale evidence. After any new commit, remediation, reconciliation with `main`, or other head change, the previous green run no longer proves the new exact branch head.

This applies to every lookup mode:

- branch-filtered route: choose the latest exact-branch run and verify it tests the current head;
- direct-run route: verify the known run's `head_sha` equals the required head;
- PR/Issue recovery route: reject provenance whose recorded head no longer matches;
- admission-gate route: bind the protected run to the **current PR candidate**, not a historical PR run.

Do not choose or reuse a run merely because it is green, newer globally, visually adjacent in Actions, present in an old comment, or easy to fetch.

## Red or pending CI is still agent-owned work

If the exact current run is red and the defect is repository-safe and inside the owned lane, follow `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`: inspect the failing job/step/log evidence, fix the root cause on the same canonical carrier, push, and re-observe the new exact candidate.

If CI is queued/running and the current execution can continue observing it, keep observing the same lifecycle. Do not convert a routine pending gate into owner homework or a terminal report.

Manual dispatch/re-run/cancel remains controlled by `CI_POLICY.md`; self-observation does not grant permission to trigger workflows manually.

## Cross-session provenance write-back

To make CI discoverable by future AI sessions, the session that obtains concrete CI provenance should record it on the canonical GitHub carrier when doing so is within the lane's normal Issue/PR metadata responsibilities.

Prefer current canonical Issue/PR metadata that future sessions already inspect. Record enough to re-verify independently:

```text
CI provenance:
- workflow: <workflow>
- run: <run-id or URL>
- event: <push|pull_request>
- attempt: <n when relevant>
- branch/PR: <identity>
- head: <sha>
- status/conclusion: <terminal result>
```

Do not create a second Issue/PR solely to store CI evidence. Do not treat stale historical comments as stronger than the current authoritative carrier body under `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`. Update current canonical metadata when appropriate so another independent chat session can recover the exact run without relying on conversation memory.

## Connector/tooling fallback rule

If a generic connector wrapper returns no runs or no classic commit statuses, do not infer that branch CI is absent, successful, or failed when any other repository-native evidence route is available.

Preferred evidence order is therefore:

1. known exact Actions run URL/run ID;
2. exact branch-filtered Actions query;
3. current canonical PR/Issue provenance;
4. current protected PR run + `PR branch CI admission gate` as a recovery/discovery surface;
5. other exact connector/CLI lookup;
6. tooling blocker only after the above are exhausted.

Empty wrappers are not positive or negative CI evidence.

## Reporting

CI reporting must identify the exact evidence actually observed, for example:

```text
✅ Branch CI: SUCCESS — run 32085484866 / head 1a9e150d... / branch agent/chatgpt-gpt56sol/polyline-signed-area-precision-2673
```

or, when a directly fetched run tests an older SHA:

```text
⏳ Branch CI: STALE — run <run-id> is green but tests <old-sha>; current canonical head is <new-sha>
```

When protected PR evidence is the current merge gate, report that evidence separately from branch-push evidence rather than conflating the two.

Continue to follow the terminal-first status-marker and self-remediation requirements in `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`.
# Shared CI required-check cancellation boundary

## Failure mode

A task-branch update can emit both `push` and `pull_request(synchronize)` runs for the same commit SHA. GitHub creates check-runs as soon as jobs are materialized. If those two event families share one `cancel-in-progress` concurrency group, the losing run leaves `cancelled` job contexts attached to the candidate SHA.

Protected-main rules require stable `preflight` and `core` contexts. A cancelled duplicate of either required name can therefore block merge even when the surviving run has the same exact SHA and completed those contexts successfully. PR #5962 / commit `59841875ffc6e32ddeabb52dfd604fc6583ac1be38` reproduced this: Shared CI run `34059071502` passed, while losing push run `34059070007` left cancelled `preflight` and `core` contexts and merge was rejected.

## Contract

- Pull-request **code** validation alone owns the protected-branch required check names `preflight` and `core`.
- Branch push validation remains enabled for exact-head evidence, but exposes `branch-preflight` and `branch-core` so it cannot satisfy or poison PR-required contexts.
- `pull_request(edited)` uses `metadata-preflight` / `metadata-core`. Multiple metadata edits can therefore cancel each other on the same SHA without leaving cancelled required contexts.
- Manual dispatch uses `dispatch-preflight` / `dispatch-core`, so manually invoked evidence cannot satisfy the PR ruleset accidentally.
- Push, pull-request code, pull-request metadata, and dispatch use separate cancellation classes. Superseded work still cancels within the same event class.
- Repository plus head-branch identity remains in the concurrency key so fork branches with equal names cannot collide.
- Metadata edits may reuse prior exact-head GREEN evidence only through the existing fail-closed evidence gate.
- Required checks are not disabled, skipped, renamed for PR code validation, or made advisory.

## Verification

Run the auto-discovered source guard directly:

```text
python scripts/preflight-ci-required-check-cancellation.py
```

Then run the aggregate source guards. Hosted verification must confirm that a same-repository push/PR pair exposes `branch-preflight`/`branch-core` plus the canonical PR `preflight`/`core`, with neither event cancelling the other. A metadata edit must expose only metadata-prefixed contexts. Merge remains forbidden until the canonical PR code run is terminal GREEN and protected main has not advanced.

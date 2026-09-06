# Shared CI required-check cancellation boundary

## Failure mode

A task-branch update can emit both `push` and `pull_request(synchronize)` runs for the same commit SHA. GitHub creates check-runs as soon as jobs are materialized. If those two event families share one `cancel-in-progress` concurrency group, the losing run leaves `cancelled` job contexts attached to the candidate SHA.

Protected-main rules require stable `preflight` and `core` contexts. A cancelled duplicate of either required name can therefore block merge even when the surviving run has the same exact SHA and completed those contexts successfully. PR #5962 / commit `59841875ffc6e32e17d1c987ce612768529f787e` reproduced this: Shared CI run `34059071502` passed, while losing push run `34059070007` left cancelled `preflight` and `core` contexts and merge was rejected.

## Contract

- Pull-request code validation owns the protected-branch required check names `preflight` and `core`.
- Branch push validation remains enabled for exact-head evidence, but exposes `branch-preflight` and `branch-core` display names so it cannot satisfy or poison PR-required contexts.
- Push, pull-request code, and pull-request metadata edits use separate cancellation classes. Superseded work still cancels within the same event class.
- Repository plus head-branch identity remains in the concurrency key so fork branches with equal names cannot collide.
- `pull_request(edited)` remains isolated from code validation; it may reuse prior exact-head GREEN evidence only through the existing fail-closed evidence gate.
- Required checks are not disabled, skipped, renamed for PRs, or made advisory.

## Verification

Run the auto-discovered source guard directly:

```text
python scripts/preflight-ci-required-check-cancellation.py
```

Then run the aggregate source guards. Hosted verification must also confirm that a same-repository branch/PR pair no longer leaves cancelled `preflight` or `core` contexts on the exact candidate SHA. Merge remains forbidden until the canonical PR run is terminal GREEN and protected main has not advanced.

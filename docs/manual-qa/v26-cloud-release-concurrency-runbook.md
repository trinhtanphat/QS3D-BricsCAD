# V26 cloud release concurrency qualification

Issue: #5907
Lane-Key: issue-5907

## Safety invariant

The V26 cloud release workflow uses one stable workflow-level concurrency group. It must neither cancel an already-running release transaction nor replace an older pending release dispatch when newer manual dispatches arrive. The active transaction runs to completion, while pending dispatches remain queued and execute FIFO under the same group.

## Hosted qualification

1. Confirm `.github/workflows/release-v26-cloud.yml` retains `group: qs3d-cloud-v26-preview-release`.
2. Confirm the same top-level concurrency mapping declares `cancel-in-progress: false`.
3. Confirm the same mapping declares `queue: max` so multiple pending release requests are retained rather than replaced.
4. Run `python scripts/preflight-v26-cloud-release-concurrency.py` through normal preflight auto-discovery.
5. Confirm the guard rejects regressions to `cancel-in-progress: true`, missing `queue`, and `queue: single`.
6. Confirm the existing release job dependencies, exact-head/source admission, checksum/identity gates, and fail-closed publication behavior remain unchanged.
7. Require fresh exact-head Shared CI GREEN before merge.

## Adversarial scenario

Dispatch V26 release A and let it enter the release transaction. While A is still running, dispatch release B and then release C. A must remain running. B must remain pending when C is submitted; C must not replace or cancel B. After A reaches a terminal state, B must start before C, and each queued run must independently execute every release admission gate against its own exact workflow SHA before publication.

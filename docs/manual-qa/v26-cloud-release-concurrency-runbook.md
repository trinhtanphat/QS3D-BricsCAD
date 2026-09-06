# V26 cloud release concurrency qualification

Issue: #5907
Lane-Key: issue-5907

## Safety invariant

The V26 cloud release workflow uses one stable workflow-level concurrency group and must not cancel an already-running release transaction when a newer manual dispatch arrives. A queued dispatch may run only after the active transaction finishes.

## Hosted qualification

1. Confirm `.github/workflows/release-v26-cloud.yml` retains `group: qs3d-cloud-v26-preview-release`.
2. Confirm the same top-level concurrency mapping declares `cancel-in-progress: false`.
3. Run `python scripts/preflight-v26-cloud-release-concurrency.py` through normal preflight auto-discovery.
4. Confirm the guard rejects a regression to `cancel-in-progress: true`.
5. Confirm the existing release job dependencies and exact-head/source admission remain unchanged.
6. Require fresh exact-head Shared CI GREEN before merge.

## Adversarial scenario

Dispatch V26 release A and let it enter the release transaction. Dispatch release B while A is still running. A must remain running; B must not terminate A. After A reaches a terminal state, B may start under the same stable concurrency group and must independently execute all admission gates.

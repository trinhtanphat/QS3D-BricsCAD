# V26 manual release concurrency runbook

## Scope

This REMOTE_SAFE source-completion package protects manual V26 release dispatches from preemption or pending-run replacement. It does not itself publish a release and does not constitute licensed BricsCAD runtime evidence.

## Required workflow contract

`.github/workflows/release-v26.yml` must keep one stable workflow-level concurrency group:

```yaml
concurrency:
  group: qs3d-manual-v26-release
  cancel-in-progress: false
  queue: max
```

The contract means an in-flight manual V26 release is not cancelled by a newer dispatch, and multiple pending dispatches are retained instead of allowing GitHub's default single-pending replacement behavior.

## Deterministic guard

Run:

```text
python scripts/preflight-v26-manual-release-concurrency.py
```

The guard fails closed when the stable group is removed or renamed, `cancel-in-progress` becomes true or implicit, `queue: max` is absent/changed, or the guarded release job disappears. Its mutation harness verifies rejection of running-release preemption, missing queue retention, and single-pending semantics.

The guard is auto-discovered by `scripts/preflight-all.py`, so ordinary Shared CI validates the contract without dispatching the release workflow.

## Validation matrix

For a candidate SHA:

1. Confirm the dedicated guard passes.
2. Confirm `python scripts/preflight-all.py` passes.
3. Confirm Shared CI `preflight` and `core` are terminal SUCCESS for the exact current candidate.
4. Reconcile latest protected `main` non-force if freshness requires it, then obtain fresh exact-head protected checks.
5. Merge only through the protected PR path using the expected current head SHA.

Do not manually dispatch `release-v26.yml` merely to prove this source contract. Actual release/runtime evidence retains its existing manual, licensed-host, signing, provenance, asset, checksum, tag and rollback requirements.

## Failure handling

If a same-lane feature guard conflicts with this contract, identify the exact assertion before mutation. Extend Reservation-v2 only after collision scanning the exact additional path, then align the stale guard without weakening release ownership, exact-main freshness, provenance, checksum, signing, tag, asset, rollback or error-precedence semantics.

## Acceptance boundary

REMOTE_SAFE acceptance is source + deterministic guard + Shared CI evidence bound to the exact candidate SHA. Never label this work `LOCAL_PASS`; licensed BricsCAD V26 execution is separate.

# CI ready-for-review required-check regeneration

Scope: protected pull-request admission for the shared CI workflow.

## Defect boundary

A draft PR can have exact-head `preflight` and `core` success, then transition to ready for review without changing its head SHA. Protected merge admission can require fresh reviewable-state contexts. Before this fix, Shared CI did not subscribe to `pull_request.ready_for_review`, so no canonical required-check run was emitted and the PR could remain with required contexts reported as `expected` until an unrelated reopen or synchronize event occurred.

## Required behavior

`ready_for_review` is a code-validation event, not a metadata-only event. It must use the ordinary pull-request concurrency class and retain the required job identities `preflight` and `core`. It must perform ordinary source/build scope classification on the exact PR head; it must not use the metadata-edit exact-head evidence reuse path.

`edited` remains metadata-only with `metadata-preflight` / `metadata-core` identities and its separate cancellation domain.

## Deterministic qualification

Run:

```text
python scripts/preflight-ci-ready-for-review-required-checks.py
```

The focused guard is auto-discovered by `scripts/preflight-all.py` and rejects omission of `ready_for_review`, routing it into metadata concurrency/job identities, or loss of the edited-event separation contract.

Hosted qualification requires fresh exact-head Shared CI with required `preflight` and `core` terminal `SUCCESS`. The most important runtime probe is the transition from draft to ready: that event must itself create canonical required contexts without close/reopen or a synthetic source commit.

## Safety boundary

This fix does not weaken exact SHA binding, branch/base semantics, reservation gates, source guards, deterministic smoke, or protected-branch required checks. It regenerates required evidence rather than reusing stale evidence.

# V25 cloud dispatcher concurrency qualification

Scope: `.github/workflows/dispatch-v25-cloud-after-main-integration.yml` concurrency and transaction ownership only. This runbook does not alter V25 product code, package contents, release ordinals, or downstream publication admission.

## Invariants

1. The dispatcher keeps the stable concurrency group `qs3d-v25-cloud-main-integration`.
2. A running dispatcher is never preempted by a newer integration request: `cancel-in-progress: false`.
3. Pending integration requests are retained rather than replaced: `queue: max`.
4. Durable side effects preserve their order: reservation comment -> dispatch-fence comment -> downstream `release-v25-cloud.yml` dispatch.
5. Existing exact-main and release-relevant-drift gates remain authoritative. A queued run that has become stale must self-retire before durable side effects rather than steal ownership from a newer source.
6. Existing incomplete/mismatched reservation/fence state remains fail-closed; this carrier prevents cancellation from creating that half-state during normal dispatcher execution rather than weakening recovery rules.

## Deterministic qualification

Run `python scripts/preflight-v25-cloud-dispatch-concurrency.py`. The auto-discovered guard must PASS on the fixed workflow and its mutation probes must fail for active preemption, removal of `queue: max`, and single-pending replacement semantics.

Review the workflow diff: only concurrency semantics should change in the production workflow. Reservation parsing, final protected-main rebinding, release-relevant drift classification, fence persistence, and downstream dispatch arguments must remain byte-equivalent to the protected-main implementation.

## Adversarial scenarios

- A second main integration arrives while a dispatcher is between reservation and fence: the running owner continues; it is not cancelled.
- Multiple integrations arrive while one dispatcher owns the group: pending requests remain queued. This contract does not claim a specific runner start order; each eventual run re-evaluates exact main and relevant drift before durable side effects.
- A queued source is superseded by release-relevant protected-main changes: existing drift admission must exit before reservation/fence/dispatch.
- A queued source differs from current main only by non-release paths: existing provenance rules decide whether it remains admissible; concurrency does not loosen source identity.
- Existing malformed or conflicting reservation/fence comments: existing fail-closed handling remains unchanged.

## Platform notes

Concurrency is evaluated by GitHub Actions before runner allocation, so the safety invariant is shell/runner neutral. The transaction itself remains on `ubuntu-latest` with the existing Bash quoting and Git pathspec behavior unchanged.

## Exit criteria

Merge only after fresh exact-head required CI is terminal GREEN, reservation/path collision is GREEN, protected main is reconciled, review threads are resolved, and merge uses the verified exact head SHA. Never use a stale GREEN from the TDD or pre-reconcile head.
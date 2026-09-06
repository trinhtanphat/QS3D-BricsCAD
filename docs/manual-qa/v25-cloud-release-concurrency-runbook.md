# V25 cloud release concurrency qualification

Issue: #5913
Lane-Key: issue-5913

## Safety invariant

The V25 cloud preview release workflow must use one stable workflow-level concurrency group, must not cancel an already-running release transaction, and must retain multiple pending manual release dispatches instead of replacing an older pending request when a newer request arrives.

## Automated qualification

1. Confirm `.github/workflows/release-v25-cloud.yml` retains `group: qs3d-cloud-v25-preview-release`.
2. Confirm the same workflow-level concurrency mapping declares `cancel-in-progress: false`.
3. Confirm the mapping declares `queue: max` so pending release requests are retained instead of using a single replaceable pending slot.
4. Run `python scripts/preflight-v25-cloud-release-concurrency.py` through normal `preflight-*.py` auto-discovery.
5. Confirm the guard rejects a missing queue policy, `queue: single`, and `cancel-in-progress: true`.
6. Confirm existing exact-head/source admission, package-integrity, checksum/signature, artifact-identity, and publication gates are unchanged.
7. Require fresh exact-head Shared CI GREEN after every carrier mutation and after reconciliation with current protected main.

## Adversarial hosted scenario

Dispatch V25 cloud release A and let A enter the release transaction. While A is running, dispatch B and then C. A must not be cancelled by B or C. B must remain retained when C is submitted; C must not replace or cancel B. Once capacity becomes available, every retained run that starts must independently execute the full release admission pipeline against its own exact workflow SHA before any publication side effect.

Do not infer a stronger FIFO runner-start guarantee than the concurrency implementation documents. The safety property under qualification is retention/non-replacement plus non-preemption, not scheduler ordering.

## Review checklist

Confirm this carrier changes only the reserved V25 cloud workflow, its focused source guard, and this runbook. Do not add `continue-on-error`, skip a release gate, weaken exact-head checks, or modify C01-C04 product code to satisfy CI. If another lane exposes an unrelated product failure, leave that product path untouched and move the C05 carrier independently.
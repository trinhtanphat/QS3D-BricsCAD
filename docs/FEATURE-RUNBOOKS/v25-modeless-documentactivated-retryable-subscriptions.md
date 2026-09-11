# V25 modeless document-event subscription ownership

Issue: #6377
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring Workflows
Lane-Key: issue-6377
Runtime classification: REMOTE_SAFE for source guards/static review/admitted-reference V25 compile; LOCAL_ONLY for licensed BricsCAD event-accessor fault injection and MDI timing.

## Defect

Start Center, Project Information, and Workspace modeless surfaces subscribe to native BricsCAD document events. Native event accessors are fallible boundaries: an add may register a delegate and then throw, and a remove may fail while the delegate is still rooted. A boolean published only after a successful `+=`, or cleared before a successful `-=`, can therefore forget a live native callback. Later show/load cycles can stack duplicate handlers, while stale callbacks can mutate UI state after the owning surface was hidden or unloaded.

## Required ownership contract

1. Publish conservative may-be-subscribed ownership before each fallible native `+=`.
2. On add failure, immediately attempt compensation, but retain ownership if exact native removal does not succeed.
3. Clear ownership only after the corresponding exact `-=` completes successfully.
4. Fence detach reentrancy so a callback raised during teardown cannot recursively corrupt ownership state.
5. Workspace owns `DocumentActivated` and `DocumentToBeDestroyed` independently; one successful detach must not erase evidence that the other handler may remain registered.
6. Hidden, disposed, unloaded, or otherwise stale callbacks must perform cleanup-only retry and return before changing detached UI state.
7. A later show/load may subscribe only when conservative ownership for that exact event is clear, preventing duplicate handler generations.
8. Preserve existing active-document/no-document refresh behavior and fail-soft modeless UI semantics.

## REMOTE_SAFE verification

Run the dedicated source guard:

`python scripts/preflight-v25-modeless-documentactivated-retryable-subscriptions.py`

Also run the adjacent lifecycle/document-affinity guards listed in the #6377 Reservation-v2 Expected-Paths, then the repository-required preflight and deterministic smoke suite. Build `QS3D.BricsCAD.V25` only against the repository's admitted/locked BricsCAD V25 reference generations. A hosted/static pass or admitted-reference compilation is evidence for source compatibility only; it is not licensed native runtime qualification.

## LOCAL_ONLY qualification

Licensed BricsCAD V25 qualification should inject or reproduce native event-accessor partial failures where possible and verify:

- `DocumentActivated +=` partial registration followed by throw does not lose ownership;
- `DocumentActivated -=` failure retains ownership and is retried from a later cleanup boundary;
- Workspace independently recovers partial add/remove failures for `DocumentActivated` and `DocumentToBeDestroyed`;
- repeated Show/Hide and Loaded/Unloaded cycles do not produce duplicate callbacks;
- stale callbacks after hide/unload perform cleanup only and do not refresh or invalidate detached UI;
- MDI document activation/destruction continues to invalidate or refresh the intended surface once, including active-document and no-document transitions;
- unload/reload and process shutdown leave no QS3D-owned callback residue.

Until those cells run in a real licensed host, classify native runtime evidence as `LOCAL_ONLY / NO_RESULT`. Never infer `LOCAL_PASS` from CI, static inspection, mocks, or compilation.

## Rollback / regression boundary

If a future change removes conservative ownership publication, clears an ownership flag before successful native removal, merges the two Workspace event owners into one ambiguous flag, or permits stale callbacks to mutate detached UI, the dedicated guard must fail. Do not weaken predecessor Start Center or Workspace document-affinity guards merely to accept a changed source shape; reconcile them only when their original behavioral invariant remains enforced.

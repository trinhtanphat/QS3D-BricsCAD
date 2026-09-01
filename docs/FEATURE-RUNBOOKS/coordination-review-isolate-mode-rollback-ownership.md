# Coordination review failed-Isolate mode rollback ownership

Issue: #4609  
Lane-Key: `issue-4609`  
Ownership-Key: `v25.coordination-review.failed-isolate-mode-rollback-retry-ownership`

## Product failure boundary

`TransientReviewSession.Isolate(...)` temporarily changes BricsCAD `OBJECTISOLATIONMODE` before setting the implied selection and queueing `ISOLATEOBJECTS`. The prior mode remains attempt-local until the native isolate queue succeeds. If a later step in that launch throws, the original action failure remains primary while the session synchronously tries to restore the prior mode.

A failed compensation is not proof that the host mode was restored. In that case the exact prior mode is transferred into `_objectIsolationModeBefore` before the original exception is rethrown. `HasIsolation` therefore remains true and existing `RestoreIsolation`, reset, or `Dispose` paths can retry the mode-only debt without queueing `UNISOLATEOBJECTS`. Ownership clears only after `SetSystemVariable("OBJECTISOLATIONMODE", prior)` succeeds. A destroyed document remains the explicit abandon boundary.

## Repository-safe qualification

Run the normal aggregate feature-source guards. Focused contracts are:

- `scripts/preflight-coordination-review-isolation-mode-restore-ownership.py`
- `scripts/preflight-coordination-review-isolate-mode-rollback-ownership.py`

The guards require exact prior-mode capture, result-bearing synchronous compensation, conditional failed-launch ownership transfer, bare rethrow/original-exception priority, successful isolate publication only after native queueing, mode-only retry without `UNISOLATEOBJECTS`, and clear-after-success semantics.

Shared CI must also compile the V25 adapter against the repository's trusted locked BricsCAD V25 references. V26 consumes the linked V25 source through the established parity boundary.

## Licensed boundary

Hosted guards and adapter compilation are source qualification only. They do not prove BricsCAD native runtime behavior and are never `LOCAL_PASS`. Any later licensed qualification must bind evidence to the exact pushed SHA/plugin identity and report only the observed result.

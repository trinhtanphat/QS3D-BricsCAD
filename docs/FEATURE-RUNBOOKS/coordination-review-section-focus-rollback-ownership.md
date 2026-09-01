# Coordination review section-focus rollback ownership

Issue: #4559  
Lane-Key: `issue-4559`  
Ownership-Key: `v25.coordination-review.section-focus-rollback-retry-ownership`

## Product failure boundary

`ApplySectionFocus` changes only transient BricsCAD view state. Before the first native `Editor.SetCurrentView` attempt, the prior `ViewSnapshot` remains attempt-local so ordinary failed applies do not falsely publish persistent section ownership. A native call can fail after host-side work has begun, however, so the synchronous compensation attempt must itself have an observable result.

If restoring the prior view is confirmed, no persistent cleanup debt is retained and the original section-apply exception remains the action result. If restoring the prior view is not confirmed, the prior snapshot is transferred to `_viewBeforeSection` before rethrowing the original apply exception. `RestoreSectionView`, row/document cleanup, or `Dispose` can then retry the exact prior view. Retry ownership clears only after a native restore succeeds. Destroyed-document handling remains the explicit abandon boundary.

## Repository-safe qualification

Run the discovered source guards through the normal aggregate feature-guard workflow. The focused contracts are:

- `scripts/preflight-coordination-review-section-focus-rollback.py`
- `scripts/preflight-coordination-review-section-focus-rollback-ownership.py`

They require attempt-local capture before native apply, conditional ownership transfer only after failed compensation, original-exception priority, clear-after-success retry semantics, and explicit destroyed-document abandonment.

The normal Shared CI must also compile the V25 adapter against trusted locked BricsCAD V25 references. V26 consumes the linked V25 source and therefore receives the same source correction through the existing parity contract.

## Licensed boundary

Hosted/source guards and V25 compilation are not native runtime proof. If this behavior is later exercised in a licensed BricsCAD qualification campaign, bind the evidence to the exact pushed SHA/plugin identity and report only the observed result. This source carrier does not claim `LOCAL_PASS`.
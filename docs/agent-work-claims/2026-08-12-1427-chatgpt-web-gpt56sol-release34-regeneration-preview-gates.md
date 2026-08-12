# Agent work claim — Release #34 regeneration preview gates

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:27 Asia/Ho_Chi_Minh`

## Scope

Reconcile the Release #34 regeneration preview/preflight assertions with already-landed structural freshness hardening. `RegenerationPreviewService.PreviewSubset` snapshots semantic element ownership/reference identity before caller target enumeration and checks freshness before/after preview. `RegenerationEngine.RegenerateDirtySubset` snapshots project element references before caller target enumeration, bounds/canonicalizes targets against that snapshot, and rejects structural drift.

## Files

- `scripts/preflight-regeneration-preview.py`
- `scripts/preflight-regeneration-preview-subset-freshness.py`
- this claim file

## Out of scope

- production `RegenerationPreviewService.cs`
- production `RegenerationEngine.cs`
- dependency impact planner
- BricsCAD adapter/release/runtime behavior

## Acceptance checks

- preview gate pins bounded `CanonicalPreviewTargets(elementIds, sourceElementOwnership.Count)` plus structural freshness;
- engine gate pins bounded `CanonicalTargetIds(elementIds, sourceElements.Length)` and captured-element structure checks;
- stale/change-version, detached preview, rollback, health-diff and canonical malformed-target assertions remain intact;
- structural-freshness smoke remains covered.

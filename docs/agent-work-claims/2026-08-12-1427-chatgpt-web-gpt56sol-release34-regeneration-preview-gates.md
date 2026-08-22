# Agent work claim — Release #34 regeneration preview gates

- Status: `COMPLETED`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 14:27 Asia/Ho_Chi_Minh`
- Completed: `2026-08-12 14:30 Asia/Ho_Chi_Minh`

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

## Implementation

- claim: `7dd64f3bd1106488f7874c8a1ad20966a7be3fd0`
- subset freshness gate: `102cfbf2e0f6172efa71494227378a8762687789`
- aggregate preview gate: `ed7eb74c44957ae1202fabd580d17b4c8cbc304c`
- production structural hardening already present: `d6d3959d8ca04ca16aeed706ca594d2edb3398cb`

## Evidence & limitations

Remote readback confirms both gates now track captured ownership/reference freshness and bounded target helper signatures while preserving detached preview, stale rejection, health/rollback, and malformed-target checks. Production regeneration code was not changed in this lane. No GitHub Actions or licensed BricsCAD runtime was executed.

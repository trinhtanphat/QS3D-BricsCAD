# Work claim — Quantity Rule preview apply mutation tracking

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-rule-preview-apply-tracking`
- Registered: `2026-08-12T00:11:00+07:00`
- Baseline main SHA: `9f4f28d5ed79d3b898c70078eeaeeb345b4fd9ea`
- Priority: P0 — make successful reviewed quantity-rule applies participate in project persistence/version state and keep no-change element applies side-effect free.

## Confirmed defects

`QuantityRulePreviewService.ApplyElement(...)`, `ApplyProject(...)` and `ApplyProjectWithHealthGuard(...)` ultimately call `QuantityRuleEngine.ApplyMatching(...)`, which mutates semantic quantities/provenance but intentionally does not own `ProjectState.Touch()` because regeneration batches own their revision boundary. The preview-apply service currently never supplies that missing project revision advancement. A successful reviewed apply can therefore change persisted semantic output while `ProjectState.ChangeVersion` remains unchanged.

`ApplyElement(...)` also calls `ApplyMatching(...)` even when a fresh element preview contains zero changes. `ApplyMatching` rewrites managed quantities via `SetQuantity`, updating element persistence timestamps despite the reviewed preview being a semantic no-op.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRulePreviewService.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRulePreviewSmoke.cs`
- `scripts/preflight-quantity-rule-preview-apply-tracking.py` (new)
- this claim file for close-out

## Intended contract

- Changed element apply is rollback-safe and advances `ProjectState.ChangeVersion` exactly once.
- Changed project apply / health-guarded project apply advance project revision once for the reviewed batch, not once per rule/element.
- Fresh element preview with no changes returns zero and leaves element/project persistence state unchanged.
- Existing stale-preview, exact-owned-element, health guard and project snapshot rollback semantics remain intact.
- `QuantityRuleEngine.ApplyMatching` remains revision-agnostic for regeneration callers.

## Excluded scope

No rule formula/category policy changes, no quantity calculation redesign, no UI/native changes, no Actions dispatch, and no BricsCAD V25 runtime claim.

## Completion condition

Reviewed quantity-rule apply paths have an explicit atomic project revision owner, no-change element apply is a true no-op, focused smoke/static coverage is on current `main`, and this claim is closed with exact SHAs and truthful validation boundaries.

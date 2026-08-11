# Work claim — Semantic Tag audit-owned ChangeVersion touch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T23:16:00+07:00`
- Baseline main SHA: `4edc480c8e8ad539643eeef33db3c06e23bb95b0`
- Priority: prevent Semantic Tag replace/remove from advancing `ProjectState.ChangeVersion` twice and align the existing semantic-tag lifecycle preflight with current AuditTrail-owned revision semantics.

## Confirmed defect

`AuditTrail.Record(...)` already calls `ProjectState.Touch()`. `SemanticTagBuilder.Build(...)` and `SemanticTagRemovalService.Remove(...)` each call `project.Touch()` again after their audit record, so one logical tag replace/remove advances ChangeVersion twice.

The existing `scripts/preflight-semantic-tags.py` still requires explicit `project.Touch()` in the builder and uses it as the revision marker before CAD commit. That gate is stale under the current AuditTrail contract.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/SemanticTagBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/SemanticTagRemovalService.cs`
- `scripts/preflight-semantic-tags.py`
- this claim file

## Intended contract

- Replace/remove keep the same metadata, ownership validation, audit events, CAD transaction order and rollback behavior.
- `AuditTrail.Record(...)` is the single ChangeVersion advancement.
- Existing lifecycle gate is repaired in place: snapshot -> audit/revision -> CAD commit -> committed flag -> guarded rollback remains mandatory; explicit duplicate Touch is forbidden.
- The gate confirms AuditTrail owns Touch + event append and ProjectStateSnapshot restores AuditEvents + ChangeVersion.

## Excluded scope

- Semantic Tag PICKFIRST, handle canonicalization, native cleanup, rendering, runtime health and command UX already handled by completed lanes
- Material rename, Browser selection, Interchange and other active claims
- global AuditTrail changes
- BricsCAD V25 native/runtime qualification

## Validation plan

- remove only the two redundant explicit Touch calls;
- repair existing semantic-tag lifecycle gate instead of adding a competing gate;
- compare latest main for overlap before PR/merge;
- do not dispatch GitHub Actions.

## Completion condition

Replace/remove use one audit-owned ChangeVersion advancement, the existing semantic-tag lifecycle gate matches that contract, changes are merged to main, and runtime remains LOCAL_ONLY unless locally qualified.

# Work claim — release #31 modeless semantic assignment lifecycle preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-modeless-semantic-assignment-preflight`
- Registered: `2026-08-12T10:36:00+07:00`
- Baseline main SHA: `4c23efdcd3ce26e9185228e7308082808fa929de`
- Priority: release #31 reports `preflight-modeless-semantic-assignment-project-lifecycle.py` failing after Floor assignment preview binding moved behind `RequireBoundProjectForRead`.

## Reserved scope

Reconcile only `scripts/preflight-modeless-semantic-assignment-project-lifecycle.py`. Preserve Family/Material/Zone/Floor production assignment behavior unchanged.

## Canonical evidence

- Family/Material/Zone still read `previewProject` directly through `ProjectContextCoordinator.TryGetReadOnly` before canonical mutation binding.
- Floor `OnAssignClick` now obtains `previewProject = RequireBoundProjectForRead("gán tầng cho selection")`; that helper verifies the active source DWG and exact bound current project before selection planning.
- Floor still captures `expectedProjectId`, resolves preview IDs, rejects empty selection, binds through `ExistingProjectMutationContext.Require`, re-resolves semantic ownership, compares IDs and only then calls `ProjectFloorService.Assign`.

## Excluded scope

No production source edits, no relaxation of empty-selection, ProjectId, ownership freshness or no-bootstrap checks, and no unrelated #31 work.

## Completion condition

The gate recognizes the helper-based Floor preview path while preserving all lifecycle ordering assertions, is pushed to `main`, and this claim is closed with exact evidence.
# Work claim — release #31 modeless project editors preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-modeless-project-editors-preflight`
- Registered: `2026-08-12T10:30:00+07:00`
- Baseline main SHA: `2722ef061f55f651a32aedbae032284db3d04d25`
- Priority: QS3D Cloud V25 Preview Build & Release #31 reports `scripts/preflight-modeless-project-editors.py` failing after Floor/Level mutation guards were encapsulated behind bound-project helpers.

## Reserved scope

Reconcile only `scripts/preflight-modeless-project-editors.py` with the current FloorLevelWindow guard structure. Preserve all project-editor production source unchanged.

## Canonical evidence

- `FloorLevelWindow.RequireBoundProjectForRead(operation)` calls `EnsureBoundDrawingIsActive(operation)`, requires the same read-only `_boundProject`, and returns only that canonical current project.
- `RequireBoundProjectForMutation(operation, mutationContext)` calls the read helper first, then `ExistingProjectMutationContext.Require`, and requires the mutation project to remain reference-equal to both current and bound project.
- Save/Delete/Activate handlers call `RequireBoundProjectForMutation(...)`; Assign calls `RequireBoundProjectForRead(...)` before selection planning and binds/revalidates mutation state later; Inspect calls `EnsureBoundDrawingIsActive(...)` directly.
- The release #31 gate still requires every Floor handler body to contain direct `EnsureBoundDrawingIsActive(...)`, so it rejects the stronger encapsulated guard path.

## Expected surfaces

- `scripts/preflight-modeless-project-editors.py`
- this claim file for close-out

## Excluded scope

- No edits to FloorLevelWindow, Zone/Family/Material/Curtain/Rebar Mesh editors or mutation behavior.
- No weakening of source-DWG checks, canonical project identity or guard-before-mutation ordering.
- No unrelated #31 failures, workflow dispatch, release publication or BricsCAD runtime qualification.

## Validation plan

- Teach the gate that Floor mutation handlers are guarded through `RequireBoundProjectForMutation(...)`, Assign through `RequireBoundProjectForRead(...)`, and Inspect directly through `EnsureBoundDrawingIsActive(...)`.
- Pin helper implementation ordering from active-DWG check to read-only bound-project identity and then mutation binding.
- Preserve all existing non-Floor editor checks unchanged.
- Re-fetch gate before write, read back implementation, verify ancestry and close with exact SHA.

## Completion condition

The gate recognizes the current stronger Floor helper-based guard without weakening modeless editor DWG/project safety, is pushed to `main`, and this claim is closed with exact evidence.
# Work claim — release #31 modeless project editors preflight reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release31-modeless-project-editors-preflight`
- Registered: `2026-08-12T10:30:00+07:00`
- Completed: `2026-08-12T10:32:00+07:00`
- Baseline main SHA: `2722ef061f55f651a32aedbae032284db3d04d25`
- Claim commit: `5d1f255630a9c61ecdd159f67b74ce256fbbd268`
- Implementation commit: `9c26cacbb18b9908098872f9604f5aa3046756e8`
- Priority: QS3D Cloud V25 Preview Build & Release #31 reports `scripts/preflight-modeless-project-editors.py` failing after Floor/Level mutation guards were encapsulated behind bound-project helpers.

## Reserved scope

Reconcile only `scripts/preflight-modeless-project-editors.py` with the current FloorLevelWindow guard structure. Preserve all project-editor production source unchanged.

## Canonical evidence

- `FloorLevelWindow.RequireBoundProjectForRead(operation)` calls `EnsureBoundDrawingIsActive(operation)`, requires the same read-only `_boundProject`, and returns only that canonical current project.
- `RequireBoundProjectForMutation(operation, mutationContext)` calls the read helper first, then `ExistingProjectMutationContext.Require`, and requires the mutation project to remain reference-equal to both current and bound project.
- Save/Delete/Activate handlers call `RequireBoundProjectForMutation(...)`; Assign calls `RequireBoundProjectForRead(...)` before selection planning and binds/revalidates mutation state later; Inspect calls `EnsureBoundDrawingIsActive(...)` directly.

## Completed reconciliation

- Floor handlers are checked against their actual helper-based guard path instead of requiring direct `EnsureBoundDrawingIsActive(...)` in every handler body.
- The gate now separately validates the helper chain: active DWG check before read-only current/bound project identity, then canonical mutation binding and reference equality.
- Existing Zone/Family/Material/Curtain/Rebar Mesh assertions remain unchanged.
- No production editor source was modified.

## Validation

- Current-main readback confirms the gate contains the Floor `handler_guards` map plus read/mutation/active helper integrity checks.
- Remote connector validation is source/readback and ancestry only. GitHub Actions was not dispatched and no full build, smoke, signing, package or licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed by implementation `9c26cacbb18b9908098872f9604f5aa3046756e8`.
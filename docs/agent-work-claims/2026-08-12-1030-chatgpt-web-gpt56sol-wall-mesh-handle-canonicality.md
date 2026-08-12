# Work claim — Wall Mesh generated handle canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-wall-mesh-handle-canonicality`
- Registered: `2026-08-12T10:30:00+07:00`
- Baseline main SHA: `2722ef061f55f651a32aedbae032284db3d04d25`
- Priority: P1 — generated StructuralWall mesh handle metadata must preserve the writer-owned delimiter/spacing contract.
- Task Key: `CORE-WALL-MESH-HANDLE-CANONICALITY`

## Confirmed defect

`StructuralWallMeshSolidBuilder` records every generated bar with `bar.Handle.ToString()` and persists `GeneratedWallMeshHandles` as `string.Join(";", update.Handles)`. `GeneratedWallMeshHealthService` currently splits that metadata and trims every token before validating it. A persisted alias such as `"A; B"` therefore passes handle validity with no health evidence even though the writer never emits surrounding whitespace.

## Non-overlap check

Existing Wall Mesh work covers empty-token validation and other generated-health behavior. Recent claim/commit search found no Wall Mesh handle canonicality lane. Slab Mesh handle canonicality was claimed concurrently by another agent and is explicitly excluded here. Beam Stirrup handle canonicality is also owned by another agent.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs`
- one focused Core smoke regression for Wall Mesh handle token spacing
- this claim file

Do not modify `StructuralWallMeshSolidBuilder`, native ownership/XData, empty-token validity, duplicate/count/source/live-solid semantics, hex-letter casing, persistence format, or BricsCAD runtime code.

## Intended contract

- A non-empty generated Wall Mesh handle token with leading/trailing whitespace emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing invalid/duplicate/count/source-overlap/live-solid and ownership checks continue to operate on the trimmed token.
- Empty tokens retain `INVALID_WALL_MESH_GENERATED_HANDLE` precedence without canonicality noise.
- Lower/upper hex spelling remains accepted; this lane only owns writer-proven whitespace/delimiter canonicality.
- Inspection remains read-only and deterministic.

## Completion condition

Padded Wall Mesh handle tokens are fail-visible without changing existing downstream validation semantics, focused smoke coverage pins padded/canonical/empty/duplicate/lowercase behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.

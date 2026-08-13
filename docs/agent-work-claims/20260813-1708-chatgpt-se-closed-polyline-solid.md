# SE closed-polyline to 3D Solid

- Timestamp: 2026-08-13 17:08 +07:00
- Agent: ChatGPT (GPT-5.6 Sol)
- Workspace/branch: `feat/se-polyline-to-solid`
- Status: Active

## Scope
Implement/complete BricsCAD command `SE` to convert selected closed planar 2D polylines into 3D Solid objects using the currently active structural Family/Type selected in the QS3D workspace panel.

The command will validate closed-planar source geometry, preserve source polylines, associate/capture source geometry into the QS3D semantic project using the active Family/Type, create native 3D solids with category-appropriate extrusion height/thickness/offset, handle multi-selection atomically with rollback where practical, refresh/select generated output, and provide clear command-line/status feedback.

## Target categories
- Architectural Wall
- Beam
- Column
- Slab
- Structural Wall
- Door
- Stair
- Foundation

## Files / areas
- `src/QS3D.BricsCAD.V25/` command/native builder integration
- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj` V25 source-link parity
- `tests/QS3D-BricsCAD.Tests/` focused source/contract coverage where practical
- command/help documentation where applicable

## Constraints
- Reuse current project/active-Family services and existing semantic/generated-geometry ownership policies.
- Do not mutate or erase the selected source polylines on success.
- Do not silently fall back to a different Family when an active Family is present.
- Runtime BricsCAD interaction remains subject to host validation when CI cannot execute BricsCAD itself.

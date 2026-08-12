# Work claim — Shape Rebar generated mode health

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-shape-rebar-mode-health`
- Registered: `2026-08-12T12:26:00+07:00`
- Baseline main SHA: `83b3f93274a60e8de3744cb8ae668ca7de381e5b`
- Priority: P1 — writer-owned Shape Rebar mode metadata must participate in generated-rebar mode health.
- Task Key: `CORE-SHAPE-REBAR-MODE-HEALTH`

## Confirmed defect

`ShapeRebarSolidBuilder.CommitSemanticUpdate(...)` always persists `GeneratedShapeRebarMode = "BBS.ShapePath.SegmentedCylinder"` whenever it persists `GeneratedShapeRebarHandles`. `GeneratedRebarModeHealthService` currently inspects longitudinal rebar plus slab/wall/foundation mesh modes but does not inspect Shape Rebar at all.

As a result, missing, unsupported, or alias Shape Rebar mode metadata can pass generated-rebar mode diagnostics without any mode-specific evidence.

## Non-overlap check

Recent commit searches for `shape rebar mode health` and `GeneratedShapeRebarMode` returned no matching lane. Existing generated-rebar mode semantics/null-health work covers the service generally but the current source still omits Shape Rebar; this claim is limited to that missing provider path.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs`
- one focused Core smoke regression for Shape Rebar mode health
- this claim file

Do not modify longitudinal or mesh mode semantics, Shape Rebar builder/planner, handles/count validation, ownership/native CAD generation, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- If `GeneratedShapeRebarHandles` exists, missing/blank or unsupported `GeneratedShapeRebarMode` emits `GENERATED_REBAR_MODE_METADATA_INVALID` as Warning.
- A stored Shape mode that normalizes case/outer whitespace to `BBS.ShapePath.SegmentedCylinder` but is not exactly that writer-owned token emits `GENERATED_REBAR_MODE_METADATA_NON_CANONICAL` as Error.
- Exact writer-owned Shape mode preserves existing behavior.
- Elements without Shape Rebar handles remain unaffected.

## Completion condition

Missing/unsupported/alias Shape Rebar modes are fail-visible, focused smoke coverage pins those cases plus canonical/no-handles controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.

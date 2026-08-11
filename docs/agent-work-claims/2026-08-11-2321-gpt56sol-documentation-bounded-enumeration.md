# Work claim — Documentation planner bounded enumeration sweep

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-documentation-bounds-20260811-2321`
- Registered: `2026-08-11T23:21:00+07:00`
- Baseline main SHA: `1cdcf77393a67c503f13b0506a14512dc6424665`
- Priority: P2 source-proven bounded-input regression hardening

## Reserved scope

Fix three closely related `QS3D.Core.Documentation` APIs that declare hard maximum counts but currently call `ToList()` before checking those limits, defeating their resource bounds for very large or non-terminating inputs:

- `SemanticViewPlanner.BuildCatalog`: maximum 10000 definitions;
- `SemanticSheetAutoLayoutPlanner.Build`: maximum 10000 layout items;
- `SemanticTitleBlockParameterMapBuilder.Build`: maximum 128 mappings.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticViewPlanner.cs`
- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs`
- `src/QS3D.Core/Documentation/SemanticTitleBlockParameterMapBuilder.cs`
- `tests/QS3D.Core.SmokeTests/SemanticViewSheetPlannerSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No view-filter, sheet packing, title-block mapping semantics or max-count values change.
- No documentation catalog persistence/schema changes.
- No BricsCAD/WPF/runtime changes.
- No GitHub Actions dispatch or workflow edits.

## Validation plan

- Verify claim reachability from current `main`, then re-fetch every source/test blob before implementation.
- Replace each unbounded pre-limit `ToList()` with one-pass bounded enumeration that stops when the first over-bound item is observed.
- Preserve existing null-item and duplicate/identity validation ordering for all inputs within the declared bound.
- Add sentinel enumerable regressions that yield exactly one over-bound item and throw a different exception if any further `MoveNext()` occurs, proving each API does not over-enumerate.
- Source/static readback plus committed smoke coverage only; no local .NET/BricsCAD/Actions PASS claim.

## Coordination

The original title-block mapping and semantic sheet-index claims are completed; automatic sheet layout/view planning are existing older features. No current active claim identified for these exact Core documentation source files. This claim is intentionally limited to the shared bounded-enumeration defect class.

## Completion condition

All three declared count limits are enforced during enumeration, focused sentinel regressions are committed on `main`, current source is re-read, and this claim is marked `COMPLETED` with exact SHAs and actual validation scope.

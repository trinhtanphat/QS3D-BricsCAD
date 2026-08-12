# Work claim — Semantic Sheet definition placement bounds

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:28:00+07:00`
- Baseline main SHA observed: `e22ebecc7ac75e744485542251da295bf4157242`
- Priority: P1 — deterministic Core resource-bound correctness.

## Confirmed defect

`SemanticSheetDefinition` is a public defensive-snapshot constructor that accepts a lazy placement enumerable. It currently materializes placements with unrestricted `new List<SemanticSheetPlacementDefinition>(placements)`, while `SemanticSheetPlanner` already supports at most 128 view placements through `MaxPlacements`. A huge or non-terminating placement source can therefore be consumed without bound before `Build()` reaches the existing capacity.

## Reserved scope

- `src/QS3D.Core/Documentation/SemanticSheetPlanner.cs` — `SemanticSheetDefinition` placement snapshot materialization only, plus minimal shared visibility/helper needed to reuse the existing `MaxPlacements` contract.
- Focused Core smoke regression for lazy over-bound placements and defensive snapshot preservation.
- Focused static preflight and planning note.

## Explicit exclusions

- Placement geometry/overlap/view-identity validation and ordering.
- `BuildCatalog()` / available-view capacities and catalog identity behavior.
- Semantic Documentation store/editor, AutoLayout, Schedule placement, native CAD placement/WPF/UI.
- BricsCAD V25/V26 runtime qualification.

## Implementation plan

1. Re-fetch moving `main` after claim and confirm constructor placement snapshot remains unrestricted.
2. Reuse the existing 128-placement capacity while snapshotting constructor input; reject on placement 129 and never request placement 130.
3. Preserve read-only snapshot behavior and downstream null/view-id/geometry/overlap validation for accepted-size definitions.
4. Add adversarial lazy placement regression with a sentinel after item 129 and a bounded defensive-snapshot case.
5. Add focused static preflight and planning documentation.
6. Refresh moving `main`, verify zero reserved-source overlap, merge only a focused PR with expected-head protection, then close this claim with exact evidence.

## Validation policy

Pure Core resource-bound behavior. GitHub Actions remain manual-only and are not dispatched. Executable smoke/preflight PASS and licensed BricsCAD runtime PASS will not be claimed without actual execution evidence.

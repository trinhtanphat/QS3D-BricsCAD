# Work claim — sheet auto-layout read-only result

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-sheet-auto-layout-readonly`
- Registered: `2026-08-12T09:12:00+07:00`
- Baseline main SHA: `9035da9b36a11e5d6d6673bbddc467f6c4a503e2`
- Priority: `Documentation/Core API integrity discovered during requested continue-all review.`

## Confirmed defect

`SemanticSheetAutoLayoutPlanner.Build()` advertises `IReadOnlyList<SemanticSheetPlan>` but returns the mutable `List<SemanticSheetPlan>` used during planning. Callers can cast the published result to `IList<SemanticSheetPlan>` and mutate the plan collection after deterministic packing/validation.

## Reserved scope

Make the non-empty automatic sheet-layout result genuinely read-only while preserving empty-result behavior, packing, ordering, page numbering, placements, input bounds, and plan object semantics. Add focused regression coverage to the existing auto-layout smoke.

## Expected surfaces

- `src/QS3D.Core/Documentation/SemanticSheetAutoLayoutPlanner.cs` — only the returned result wrapper
- `tests/QS3D.Core.SmokeTests/SemanticSheetAutoLayoutSmoke.cs`

## Coordination

Earlier auto-layout lanes are completed and covered bounded item/available-view enumeration. This follow-up does not modify input bounds, placement math, sheet numbering, title-block reservation, or `SemanticSheetPlanner`.

## Validation plan

- Preserve existing deterministic packing assertions.
- Prove the returned non-empty result rejects indexed replacement, `Add`, and `Remove` through `IList<SemanticSheetPlan>`.
- Prove result contents remain unchanged after rejected mutation attempts.
- Re-read current main/target blobs before every write; no GitHub Actions; no BricsCAD runtime PASS.

## Completion condition

Claim is on main before source changes; implementation and regression commits are SHA-guarded and pushed to main; claim closes `COMPLETED` with exact evidence.

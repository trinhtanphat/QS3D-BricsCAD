# Plan — Semantic Sheet definition placement bounds

## Goal

Preserve `SemanticSheetDefinition` defensive placement snapshots while enforcing the planner's existing 128-placement capacity during lazy constructor enumeration.

## Existing contract

- `SemanticSheetPlanner.MaxPlacements` supports at most 128 view placements per sheet.
- Placement null/view-id/geometry/bounds/overlap validation remains in `Build()`.
- Definition placements are defensive read-only snapshots.

## Defect

The public constructor snapshots `placements` with unrestricted `new List<SemanticSheetPlacementDefinition>(IEnumerable<...>)`. A huge or non-terminating lazy source can be consumed without bound before `Build()` reaches the existing 128-placement capacity.

## Implementation

1. Reuse `SemanticSheetPlanner.MaxPlacements` from the definition constructor.
2. Materialize placements one pass at a time; reject when item 129 is observed and never request item 130.
3. Preserve `ArgumentNullException` for a null enumerable and `AsReadOnly()` snapshot behavior.
4. Preserve downstream placement semantic validation and all catalog/available-view behavior.

## Regression

- Lazy placement source yields 129 placements and throws a sentinel if item 130 is requested.
- Expected failure is the existing `Semantic sheet supports at most 128 view placements.` message.
- A bounded source-mutation case proves the definition remains a defensive snapshot.

## Static guard

Require shared `MaxPlacements`, bounded helper use, guard-before-add ordering, read-only return, and absence of the legacy unrestricted constructor materialization.

## Validation boundary

GitHub Actions remain manual-only and are not dispatched. Remote evidence is source/diff/static-contract review plus committed deterministic smoke/preflight coverage. No BricsCAD V25/V26 runtime PASS is claimed.

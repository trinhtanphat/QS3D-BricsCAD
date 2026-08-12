# Work claim — Wall quantity null opening guard

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12`
- Baseline main SHA: `0151f4b9ff18e9956c4b3d25530cdc0d1bd4c06a`
- Priority: quantity correctness / malformed-input fail-closed behavior

## Confirmed defect

`WallQuantityCalculator.Calculate(...)` accepts an optional enumerable of opening cuts, but silently `continue`s when an enumerated entry is `null`. A malformed opening collection can therefore understate opening area and deduction volume while still returning apparently valid wall quantities. This differs from the Core pattern used by persisted/reporting/planning collections, where malformed null entries fail closed instead of disappearing from calculation.

The collection itself may remain `null` to mean “no openings”; this claim only covers an explicit enumerable that contains a null entry.

## Reserved scope

- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- focused standalone `QS3D.Core.SmokeTests` regression
- `docs/plans/2026-08-12-wall-quantity-null-opening.md`
- this claim file

## Intended contract

1. `openings == null` keeps existing no-opening behavior.
2. A non-null enumerable containing a null entry fails closed with an argument/data-shape error.
3. Valid opening calculations remain unchanged, including clamping total opening area to gross wall area.
4. No native BricsCAD, wall-regeneration, or host-link behavior changes in this lane.

## Non-overlap

- Do not modify `SemanticRegenerators.cs`, opening hosting/cutting, WallPier, XLSX/reporting, or native commands.
- No GitHub Actions dispatch or release publication.

## Closure

Claim before source, planning before implementation, exact current blob re-fetch, focused regression, ancestry `behind_by: 0`, and no unexecuted PASS claims.

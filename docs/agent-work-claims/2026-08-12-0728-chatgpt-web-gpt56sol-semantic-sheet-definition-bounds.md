# Work claim — Semantic Sheet definition placement bounds

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:28:00+07:00`
- Completed: `2026-08-12T07:32:00+07:00`
- Baseline main SHA observed: `e22ebecc7ac75e744485542251da295bf4157242`
- Claim commit: `e3980f5b6d6cadacd18668941f46c2b83694f402`
- PR: `#617`
- Squash merge on `main`: `75bf6d149900be282c5e8edc4b527c915c54426b`
- Priority: P1 — deterministic Core resource-bound correctness.

## Defect closed

`SemanticSheetDefinition` is a public defensive-snapshot constructor that accepts a lazy placement enumerable. It previously materialized placements without a bound, while `SemanticSheetPlanner` already supported at most 128 view placements. Huge or non-terminating placement sources could therefore be consumed without bound before `Build()` reached the existing capacity.

## Implemented

- Constructor placement snapshots now use one-pass bounded enumeration with the existing `MaxPlacements = 128` contract.
- Placement 129 triggers the existing `Semantic sheet supports at most 128 view placements.` error; placement 130 is never requested after oversize is known.
- `MaxPlacements` is shared internally between constructor snapshotting and downstream `Build()` validation.
- Accepted placement collections remain defensive read-only snapshots.
- Downstream null placement, view identity, numeric geometry, paper bounds, overlap, deterministic ordering, catalog and available-view validation remain unchanged.
- Added adversarial placement smoke coverage, defensive snapshot non-regression, isolated registration, focused static preflight and planning documentation.

## Validation evidence

- Current source confirmed the unrestricted constructor snapshot before implementation.
- Branch changed exactly five files; production source diff was +17/-4 in `SemanticSheetPlanner.cs`.
- PR #617 full diff was reviewed before merge.
- Moving-main comparison found 25 concurrent commits after the claim point with zero overlap in the reserved source/lane files; another four commits after PR base were also unrelated.
- Squash merge used expected head `1763c9188188222f5a2b08277557377091be9b89` and succeeded as `75bf6d149900be282c5e8edc4b527c915c54426b`.
- GitHub Actions were not dispatched because repository policy is manual-only.
- Executable smoke/preflight PASS and licensed BricsCAD V25/V26 runtime PASS are not claimed from this connector-only environment.

## Explicit exclusions honored

- Placement geometry/overlap/view-identity validation and ordering.
- `BuildCatalog()` / available-view capacities and catalog identity behavior.
- Semantic Documentation store/editor, AutoLayout, Schedule placement, native CAD placement/WPF/UI.
- BricsCAD V25/V26 runtime qualification.

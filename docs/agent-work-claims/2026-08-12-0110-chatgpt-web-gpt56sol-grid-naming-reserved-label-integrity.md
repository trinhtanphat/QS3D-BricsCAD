# Work claim — Grid naming reserved-label integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-naming-reserved-label-integrity`
- Registered: `2026-08-12T01:10:00+07:00`
- Baseline main SHA observed: `388de3818354b7e0849fc82bca896ea92cb7b49b`
- Priority: P1 — deterministic Core semantic-integrity / mutation-atomicity defect.

## Confirmed defect

`GridNamingService.Renumber(...)` builds a case-insensitive `reservedLabels` set from non-target Grid elements, but ignores the return value of `HashSet.Add`. If two non-target Grids already carry the same trimmed label, the duplicate is silently collapsed and renumbering an unrelated Grid can report success while leaving the project in an ambiguous Grid-label state that `GridNamingHealthService` already classifies as `GRID_LABEL_DUPLICATE` / Error.

The batch cannot repair duplicate labels when every owner of that duplicate is outside the target set, so this path must fail closed before `ProjectState.Touch()` or any target property mutation. Duplicates involving target Grids remain repairable by the batch and must not be over-rejected merely because of their pre-renumber values.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs`
- `tests/QS3D.Core.SmokeTests/GridNamingReservedLabelIntegritySmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/GridNamingReservedLabelIntegritySmokeRegistration.cs` (new)
- `scripts/preflight-grid-naming-reserved-label-integrity.py` (new)
- `docs/plans/2026-08-12-grid-naming-reserved-label-integrity.md` (new)
- this claim file for close-out

## Implementation plan

1. Re-fetch moving `main` after this claim lands and stop if another ACTIVE/BLOCKED claim has begun touching `GridNamingService.cs`.
2. While collecting non-target Grid labels, reject a second owner of the same non-empty trimmed label case-insensitively instead of silently collapsing it.
3. Preserve current repair semantics for duplicates where at least one duplicate owner is in the target batch, plus existing capacity, ID, formatting, collision, ordering and no-op behavior.
4. Add an isolated smoke that proves non-target duplicates reject atomically (`ChangeVersion` and target properties unchanged), while a target/non-target pre-existing duplicate can still be repaired by renumbering the target.
5. Add a focused static preflight that pins the fail-closed `reservedLabels.Add(...)` contract and isolated smoke registration without editing shared registration hotspots.
6. Integrate only after another moving-main overlap check; never force-update `main`.

## Explicit exclusions

No Grid annotation/runtime-health, intersection/spatial geometry, CAD command lifecycle, updater/release, persistence-format or UI changes. No native BricsCAD runtime PASS is implied.

## Completion condition

Unrepairable duplicate non-target Grid labels are rejected before mutation, repairable target-involved duplicates still renumber correctly, focused regression/static coverage is merged on current `main`, and this claim is marked `COMPLETED` with exact commit evidence.

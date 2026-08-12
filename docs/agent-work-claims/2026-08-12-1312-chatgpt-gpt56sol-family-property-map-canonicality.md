# Work claim — Project Family property-map canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-family-property-map-canonicality`
- Registered: `2026-08-12T13:12:00+07:00`
- Baseline main SHA: `14412456376a50d96caaa4ef9f9e29228a41a581`
- Priority: P1 — fail closed before Family property mutation when the existing public property map is already non-canonical and cannot be persisted safely.

## Reserved scope

Harden `ProjectFamilyService.SetProperty` and `ProjectFamilyService.RemoveProperty` so they validate the existing target Family property snapshot before deciding no-op or mutating. Existing blank/padded/non-canonical property keys must fail closed rather than being preserved or compounded by a mutation.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- one focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Template profile validation/persistence
- BulkEdit family/property behavior
- Family property freshness/no-op audit lanes
- Family assignment target-enumeration freshness
- UI/BricsCAD runtime
- changing property-value normalization policy

## Validation plan

- Regression proves `SetProperty` rejects a target Family containing a padded existing property key before project/family/element mutation.
- Regression proves `RemoveProperty` rejects the same malformed existing map even when the requested canonical key would otherwise be treated as missing/no-op.
- Canonical Family property mutation continues to work.
- Read back source/test on current `main` and verify pushed commit ancestry before closeout.

## Coordination

Recent Family-property work covers Template-profile property-key canonicality and removal freshness; this claim is limited to existing-map integrity at `ProjectFamilyService` mutation entry points and does not alter those neighboring contracts.

## Completion condition

Source and focused regression are pushed to `main`, read back on the moving branch, exact implementation/test SHAs are recorded, ancestry is verified, and this claim is marked `COMPLETED`.

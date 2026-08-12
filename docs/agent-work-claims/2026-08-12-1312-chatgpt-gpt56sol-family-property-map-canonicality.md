# Work claim — Project Family property-map canonicality

- Status: `COMPLETED`
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

## Validation

- Claim commit: `ae29cb776aca1188b07ddf5e65dcbef841289725`.
- Implementation commit: `3efc68670a348a471413a9bc50f62895fc6da233`.
- Focused smoke commit: `0516de365ca1b9c32ad6be8c9e75f6f3d9f5e72d`.
- `SetProperty` now rejects blank/padded/non-canonical existing Family property keys before no-op or mutation.
- `RemoveProperty` now rejects the malformed existing map before the old missing-key no-op path.
- Focused smoke covers padded-key Set, padded-key Remove, blank-key Set, no-mutation rejection, and a canonical inherited Set success path.
- Source and smoke were read back from `main`; comparison of `0516de365ca1b9c32ad6be8c9e75f6f3d9f5e72d...main` returned `status: identical`, `ahead_by: 0`, `behind_by: 0` at closeout preparation.
- No GitHub Actions were dispatched. No local .NET/BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

Recent Family-property work covers Template-profile property-key canonicality and removal freshness; this claim remains limited to existing-map integrity at `ProjectFamilyService` mutation entry points and does not alter those neighboring contracts.

## Completion

Remote source-safe scope is complete. Property-value normalization policy, UI/runtime behavior, and local host qualification remain outside this claim.

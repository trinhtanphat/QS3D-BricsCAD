# Core Family member property-map preflight claim

- Status: `ACTIVE`
- Agent: `gpt56sol-family-member-map-preflight-20260813`
- Registered: `2026-08-13`
- Baseline main SHA: `cc8b5a316256e114ec6ffcd9a399e0f8b45d463d`
- Priority: Core model / Family mutation integrity.

## Confirmed defect

`ProjectFamilyService.SetProperty()` and `RemoveProperty()` validate the target Family property map, but after resolving owned Family members they inspect/mutate each `ProjectElement.Properties` map without first validating that map's keys are canonical. A legacy/directly-mutated member containing a padded key such as `" WidthM "` can therefore survive a Family-wide mutation while a new canonical `"WidthM"` entry is added, leaving persistability-invalid state instead of failing closed before `ProjectState.Touch()`.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/FamilyMemberPropertyMapPreflightSmoke.cs`
- this claim file only

## Intended change

Preflight each resolved Family member property map before a real Set/Remove property mutation, reject blank/padded/case-colliding canonical keys before project mutation, preserve existing Family inheritance/override semantics, and add focused atomicity regression coverage.

## Excluded scope

No Family Manager/UI, bulk assignment, persistence schema, Cost/Measurement, MAP/IFC, BricsCAD/native, or other agent-owned surfaces.
# Core Family assignment member property-map preflight claim

- Status: `ACTIVE`
- Agent: `gpt56sol-family-assignment-member-map-preflight-20260813`
- Registered: `2026-08-13`
- Baseline main SHA: `20653d2fac12cde6210cb0af38f0828e8cfa22a9`
- Priority: Core model / Family assignment integrity.

## Confirmed defect

`ProjectFamilyService.Assign()` validates target/previous Family default maps, ownership and enumeration freshness, but pending elements' own `Properties` maps are not canonicality-preflighted before `ProjectState.Touch()` and inherited-default rewrite. A legacy/directly-mutated pending element with padded or blank property keys can therefore be reassigned while retaining malformed keys and receiving canonical target defaults, leaving invalid mixed state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/FamilyAssignmentMemberPropertyMapPreflightSmoke.cs`
- this claim file only

## Intended change

Reuse the member property-map preflight for pending real Family assignments before project mutation, preserve assignment no-op/category/ownership/inheritance semantics, and add focused atomicity regression coverage.

## Excluded scope

No BulkEditService, Family Manager/UI, persistence schema, Cost/Measurement, MAP/IFC, BricsCAD/native, or other agent-owned surfaces.
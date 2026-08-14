# Work claim — Core Family assignment member property-map preflight

- Status: `ACTIVE`
- Agent: `gpt56sol-family-assign-member-map-20260814-0802`
- Registered: `2026-08-14T08:02:00+07:00`
- Baseline main SHA: `a16482b88ec76c2c9942059a003cc09230302c01`
- Priority: Core model / Family assignment integrity.

## Confirmed defect

`ProjectFamilyService.Assign()` validates target/previous Family default maps, ownership, category and target-enumeration freshness, but actual pending elements' own `Properties` maps are not canonicality-preflighted before `ProjectState.Touch()` and inherited-default rewrite. A legacy/directly-mutated pending element with a padded or blank property key can therefore be reassigned while retaining malformed state and receiving canonical target defaults.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/FamilyAssignmentMemberPropertyMapPreflightSmoke.cs`
- this claim file only

## Intended change

Reuse the existing member property-map canonicality preflight for actual pending Family assignments after the existing no-op exit and before project mutation. Preserve already-assigned no-op behavior, category/ownership checks, inherited-default replacement and explicit override semantics. Add focused atomicity regression coverage.

## Excluded scope

No `BulkEditService`, Family Manager/UI, persistence schema, Cost/Measurement, MAP/IFC, Rebar, V25 release/source-handle/native surfaces, or other agent-owned capability.

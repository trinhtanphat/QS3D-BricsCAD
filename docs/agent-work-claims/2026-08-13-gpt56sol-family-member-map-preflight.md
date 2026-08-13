# Work claim — Core Family member property-map preflight

- Status: `COMPLETED`
- Agent: `gpt56sol-family-member-map-preflight-20260813`
- Registered: `2026-08-13`
- Completed: `2026-08-13`
- Baseline main SHA: `cc8b5a316256e114ec6ffcd9a399e0f8b45d463d`
- Priority: Core model / Family mutation integrity.

## Confirmed defect

`ProjectFamilyService.SetProperty()` and `RemoveProperty()` already validated the target Family property map, but after resolving owned Family members they inspected/mutated each `ProjectElement.Properties` map without first validating that member map's key canonicality. A legacy/directly-mutated member containing a padded key such as `" WidthM "` could therefore survive a real Family-wide mutation while a canonical `"WidthM"` entry was added, leaving persistence-invalid state instead of failing closed before `ProjectState.Touch()`.

## Implemented scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`: real Set/Remove mutations now preflight every resolved Family member property map before touching project state. Blank, padded and canonical-colliding member keys fail closed with the member id in the diagnostic.
- Existing no-op behavior is unchanged because member preflight occurs only after the existing Set/Remove no-op exits.
- Existing inherited-instance and explicit-override semantics remain unchanged for canonical member maps.
- `tests/QS3D.Core.SmokeTests/FamilyMemberPropertyMapPreflightSmoke.cs`: focused self-registering smoke covers padded-member Set rejection, padded-member Remove rejection, blank-key rejection, atomic project/member state preservation and ordinary canonical propagation.

## Coordination

- Claim-first commit: `3130f32279190cd1625ea21220c0439de0da29da`.
- Implementation + focused regression commit: `1c622fec7d4467a50bc11e47d3f7d9ede97fa00e`.
- Concurrent IFC-02A work landed after the claim and was preserved by rebuilding the implementation commit on the then-current `main`; no force update was used.
- Current lineage after implementation also preserved IFC-02A completion commit `c334f0c7ab60a2a922c415e578630a8ec2394c01`.

## Excluded scope

No Family Manager/UI, bulk assignment, persistence schema, Cost/Measurement, MAP/IFC behavior, BricsCAD/native code, or other agent-owned surfaces were changed.

## Validation actually executed

- Re-fetched `main` before the claim, after claim publication, before implementation publication, and after concurrent commits.
- Verified the claimed production file blob remained unchanged across the concurrent IFC commit before publishing the fix.
- Read back the exact remote implementation diff: only the reserved production file and dedicated regression file changed.
- Verified implementation commit `1c622fec7d4467a50bc11e47d3f7d9ede97fa00e` is on current `main` lineage.
- GitHub returned no Actions workflow runs and no combined status checks for the direct implementation commit.
- No managed executable smoke/build or licensed BricsCAD/native runtime validation was available/executed in this lane, so none is reported as PASS.

## Completion condition

Satisfied for this bounded Core lane: malformed member property maps now fail closed before Family-wide mutation, canonical Family propagation semantics are preserved, focused regression source is on remote `main`, concurrent work was retained without force-push, and unavailable runtime/native gates remain explicitly unclaimed.
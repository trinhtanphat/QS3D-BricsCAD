# Work claim — Family assignment structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assignment-structural-freshness-20260812-1012`
- Registered: `2026-08-12T10:12:00+07:00`
- Baseline main SHA observed: `8bb3539395046e081330d8570029184645499708`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-STRUCTURAL-FRESHNESS`

## Confirmed defect

Family assignment now guards `ProjectState.ChangeVersion` across caller-owned lazy target enumeration, but `ProjectState.Elements` is itself a publicly mutable `IList<ProjectElement>`. A lazy enumerable can remove or replace a target directly in `project.Elements` without calling `project.Touch()`, leaving `ChangeVersion` unchanged. `ResolveOwnedElements(...)` currently validates ownership only against the pre-enumeration element snapshot, so assignment can proceed and mutate an element that no longer belongs to the project.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — post-enumeration target ownership/category revalidation only
- `tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` — focused auto-discovered regression
- this claim file

## Intended contract

- Preserve the existing ChangeVersion freshness guard.
- After caller target enumeration completes, revalidate each unique target against the current `project.Elements` collection by semantic id and object identity.
- Revalidate target category against the Family category at the same boundary.
- Fail before assignment planning/mutation if a target was removed, replaced, duplicated or became category-incompatible during enumeration.

## Excluded scope

No changes to Zone/Grid/Floor freshness, global ProjectState collection tracking, Family activation/property propagation semantics, CAD/UI/runtime, Actions/build/release.

## Validation plan

Add a lazy target sequence that directly removes the yielded element from `project.Elements` after yielding it, without calling `Touch()`. Require fail-closed behavior with unchanged ChangeVersion, FamilyId, inherited properties and element dirty/timestamp state. Re-fetch moving `main` and exact source blob before write.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.

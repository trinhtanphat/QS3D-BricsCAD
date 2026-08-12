# Work claim — Zone assignment global structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-assignment-global-structural-freshness`
- Registered: `2026-08-12T12:02:00+07:00`
- Baseline main SHA: `311b7d44fa0b8dda2b1aa3901c0aa0ef8461e584`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-ZONE-ASSIGNMENT-GLOBAL-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectZoneService.Assign(...)` globally validates Zone identities and project element identities before enumerating caller-owned targets, then checks `ChangeVersion` and re-resolves only the target Zone and selected target element IDs afterward. `ProjectState.Zones` and `ProjectState.Elements` remain publicly mutable lists, so a lazy target enumerable can directly insert an unrelated duplicate Zone or element identity without calling `project.Touch()`. Because post-enumeration lookup only asks for the target IDs, an unrelated duplicate pair is invisible and assignment can mutate a project that became globally identity-invalid during enumeration.

This violates two completed contracts: target-based Zone operations must reject unrelated duplicate Zone identities, and Zone assignment structural freshness must fail on duplicate project identities introduced during caller enumeration before assignment mutation.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` — post-enumeration global Zone/element identity revalidation only
- `tests/QS3D.Core.SmokeTests/ZoneAssignStructuralFreshnessSmoke.cs` — focused regression extension only
- this claim file for close-out

## Intended contract

- Preserve the existing pre-enumeration global validation and `ChangeVersion` freshness guard.
- After caller target enumeration, revalidate global Zone uniqueness and global project element identity integrity before target ownership checks and changed-set planning.
- Direct no-`Touch()` insertion of an unrelated duplicate Zone ID or unrelated duplicate element ID must fail before `project.Touch()`, target `ZoneId` mutation, or target dirty/timestamp mutation.
- Preserve removal/replacement structural checks, canonical valid assignment, no-op behavior, Zone CRUD semantics, and all unrelated services.

## Excluded scope

- No Floor/Family/Grid changes, no global `ProjectState` collection redesign, no CAD/UI/runtime work, no Recognition/Auto Room/Regeneration/Interchange/Beam Stirrup work.
- No force-push, GitHub Actions dispatch, full-build/executable-smoke PASS, or licensed BricsCAD V25/V26 runtime qualification claim.

## Validation plan

Re-fetch source and existing auto-registered structural smoke after this claim lands. Add the minimum post-enumeration global validation and regressions for unrelated duplicate Zone and element insertion during lazy enumeration, then read back integrated source/test, close this claim with exact SHAs, and verify completion ancestry on current `main`.
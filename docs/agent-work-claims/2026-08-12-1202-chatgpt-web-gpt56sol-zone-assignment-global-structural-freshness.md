# Work claim — Zone assignment global structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-assignment-global-structural-freshness`
- Registered: `2026-08-12T12:02:00+07:00`
- Baseline main SHA: `311b7d44fa0b8dda2b1aa3901c0aa0ef8461e584`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-ZONE-ASSIGNMENT-GLOBAL-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectZoneService.Assign(...)` globally validated Zone identities and project element identities before enumerating caller-owned targets, then checked `ChangeVersion` and re-resolved only the target Zone and selected target element IDs afterward. Because `ProjectState.Zones` and `ProjectState.Elements` are publicly mutable lists, a lazy target enumerable could directly insert an unrelated duplicate Zone or element identity without calling `project.Touch()`. The post-enumeration target-only lookups did not observe those unrelated duplicate pairs, allowing assignment to mutate a project that became globally identity-invalid during enumeration.

This violated two completed contracts: target-based Zone operations must reject unrelated duplicate Zone identities, and Zone assignment structural freshness must fail on duplicate project identities introduced during caller enumeration before assignment mutation.

## Completed implementation

- Preserved the existing pre-enumeration global validation and `ChangeVersion` freshness guard.
- `RequireCurrentAssignmentOwnership(...)` now re-runs global Zone identity validation after target enumeration.
- The same post-enumeration boundary re-resolves the complete project element collection through `ResolveProjectElements(...)`, rejecting null/blank/duplicate semantic element identities before target ownership checks and changed-set planning.
- Target Zone and selected target object-identity checks remain in place after the global integrity checks.
- Existing removal/replacement behavior, canonical valid assignment, no-op behavior and Zone CRUD semantics remain unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ZoneAssignStructuralFreshnessSmoke.cs` remains auto-registered with `ModuleInitializer` and now covers four structural-drift cases:

1. selected target element removed directly during lazy enumeration;
2. target Zone removed directly during lazy enumeration;
3. an unrelated duplicate Zone identity inserted directly during lazy enumeration;
4. an unrelated duplicate semantic element identity inserted directly during lazy enumeration.

The duplicate cases require `InvalidOperationException` before assignment mutation and verify unchanged `ProjectState.ChangeVersion`, target `ZoneId`, target dirty flags and target timestamp; the deliberate external duplicate insertion remains visible.

## Integration evidence

- Claim commit: `621c72f1ee1ae2a5b0d63d1cfa5bc5c7caaf08c2`
- Production fix: `c5d47191a32b402836442de371fc174c21470c2c` (`fix(zone): revalidate global assignment identity`)
- Focused regression: `5cc7cd810d492441c574c6b9fc6898900bb3a673` (`test(zone): guard global structural freshness`)
- Integrated source read-back confirms post-enumeration `ValidateUniqueZoneIds(...)` plus full `ResolveProjectElements(...)` global validation before target ownership checks.
- Integrated smoke read-back confirms the two existing removal cases plus both unrelated duplicate-insertion cases.

## Excluded scope / validation boundary

- No Floor/Family/Grid changes, no global `ProjectState` collection redesign, no CAD/UI/runtime work, no Recognition/Auto Room/Regeneration/Interchange/Beam Stirrup changes.
- No force-push and no GitHub Actions dispatch.
- No full-build/executable-smoke PASS or licensed BricsCAD V25/V26 runtime qualification is claimed from this connector-only lane; validation is repository integration/read-back plus focused regression source coverage.
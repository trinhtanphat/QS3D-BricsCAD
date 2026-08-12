# Work claim — Zone assignment structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-assignment-structural-freshness-20260812-1017`
- Registered: `2026-08-12T10:17:00+07:00`
- Baseline main SHA observed: `5eb917c0b2f9b4de74fca149dcbc47cdc68112b6`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-ZONE-ASSIGNMENT-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectZoneService.Assign(...)` correctly guards `ProjectState.ChangeVersion` during caller-owned lazy target enumeration, but it resolves both the target Zone and project element ownership before that enumeration. `ProjectState.Zones` and `ProjectState.Elements` are publicly mutable lists, so a lazy enumerable can directly remove the target Zone or a yielded target element without calling `project.Touch()`. `ChangeVersion` remains unchanged and the stale pre-enumeration snapshot can then be used to assign a detached element or a missing Zone.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` — post-enumeration Zone/element ownership revalidation only
- `tests/QS3D.Core.SmokeTests/ZoneAssignStructuralFreshnessSmoke.cs` — focused auto-discovered regression
- this claim file

## Intended contract

Preserve existing ChangeVersion freshness behavior, then re-resolve the target Zone and each unique target element against the current project by semantic id and object identity before planning/mutation. Structural removal/replacement/duplicate identities must fail before `project.Touch()`, `ZoneId` mutation or element dirty-state mutation.

## Excluded scope

No changes to Floor/Family/Grid freshness, global ProjectState collection tracking, Zone CRUD/activation semantics, CAD/UI/runtime, Actions/build/release.

## Validation plan

Cover direct no-`Touch()` removal of a yielded target element and direct removal of the target Zone during lazy enumeration. Require fail-closed behavior with unchanged ChangeVersion, ZoneId and element dirty/timestamp state, aside from the deliberate external list removal itself.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.

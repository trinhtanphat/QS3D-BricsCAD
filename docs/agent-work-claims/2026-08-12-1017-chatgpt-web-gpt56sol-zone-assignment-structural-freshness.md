# Work claim — Zone assignment structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-assignment-structural-freshness-20260812-1017`
- Registered: `2026-08-12T10:17:00+07:00`
- Completed: `2026-08-12T10:19:00+07:00`
- Baseline main SHA observed: `5eb917c0b2f9b4de74fca149dcbc47cdc68112b6`
- Claim commit: `30c6510e7b7f8fa4c2a9ed95d05c5ad099915cdc`
- Source fix commit: `c4a3e44b2552848b867e4ad977fe36957cd4f6d8`
- Regression commit: `102f29ecfff03b5fc3c9a3a2d1d2e342e07e4a1f`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-ZONE-ASSIGNMENT-STRUCTURAL-FRESHNESS`

## Confirmed defect

`ProjectZoneService.Assign(...)` guarded `ProjectState.ChangeVersion` during caller-owned lazy target enumeration, but resolved both the target Zone and project element ownership before enumeration. Because `ProjectState.Zones` and `ProjectState.Elements` are publicly mutable lists, a lazy enumerable could directly remove the target Zone or a yielded target element without calling `project.Touch()`. `ChangeVersion` stayed unchanged and the stale pre-enumeration snapshot could then be used to assign a detached element or a missing Zone.

## Completed implementation

- Preserved the existing ChangeVersion freshness guard.
- Added post-enumeration re-resolution of the target Zone by semantic id and object identity.
- Added post-enumeration re-resolution of every unique target element by semantic id and object identity.
- Missing/replaced/duplicate project identities now fail before changed-set planning, `project.Touch()`, `ZoneId` mutation or dirty-state mutation by the assignment.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ZoneAssignStructuralFreshnessSmoke.cs` is auto-discovered through `ModuleInitializer` and covers:

1. yield target element, then remove it directly from `project.Elements` without `Touch()`;
2. yield target element, then remove the target Zone directly from `project.Zones` without `Touch()`.

Both cases require `InvalidOperationException`, unchanged `ProjectState.ChangeVersion`, unchanged target element `ZoneId`, and unchanged element dirty/timestamp state. The deliberate external list removal remains visible, proving no assignment mutation occurred after stale ownership was detected.

Source and regression commits were fetched back from GitHub after write and their diffs matched the reserved scope.

## Excluded scope

No changes to Floor/Family/Grid freshness, global ProjectState collection tracking, Zone CRUD/activation semantics, CAD/UI/runtime, Actions/build/release.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed. Validation in this lane is repository-level source/regression diff readback and static contract review only.

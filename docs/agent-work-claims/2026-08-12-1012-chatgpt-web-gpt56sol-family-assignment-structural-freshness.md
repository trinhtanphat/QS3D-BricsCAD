# Work claim — Family assignment structural freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assignment-structural-freshness-20260812-1012`
- Registered: `2026-08-12T10:12:00+07:00`
- Completed: `2026-08-12T10:16:00+07:00`
- Baseline main SHA observed: `8bb3539395046e081330d8570029184645499708`
- Claim commit: `ee0e4da59a0069dc4a142fa8090fd8a86e44b3c1`
- Source fix commit: `ca792208d6c2a665d55f63ea98455bca5d4197e7`
- Regression commit: `61287613592a2f7a31cc295b20ad2f25c9f587b0`
- Priority: P1 semantic mutation atomicity
- Task Key: `CORE-FAMILY-ASSIGNMENT-STRUCTURAL-FRESHNESS`

## Confirmed defect

Family assignment guarded `ProjectState.ChangeVersion` across caller-owned lazy target enumeration, but `ProjectState.Elements` and `ProjectState.Families` are publicly mutable lists. A lazy enumerable could directly remove a target element or the target Family without calling `project.Touch()`, leaving `ChangeVersion` unchanged. Pre-enumeration ownership checks therefore did not prove that the assignment objects still belonged to the project when planning began.

## Completed implementation

- Preserved the existing revision freshness guards.
- Added post-enumeration re-resolution of the target Family by semantic id and object identity.
- Added post-enumeration re-resolution of every unique target element by semantic id and object identity.
- Revalidates target element category against the current target Family category at the same boundary.
- Missing/replaced/duplicate project identities fail before assignment planning, `project.Touch()`, FamilyId/property mutation, or dirty-flag mutation by the assignment.

## Regression evidence

`tests/QS3D.Core.SmokeTests/FamilyAssignStructuralFreshnessSmoke.cs` is auto-discovered through `ModuleInitializer` and covers two no-revision structural mutations:

1. the lazy target sequence yields an element, then removes it directly from `project.Elements`;
2. the lazy target sequence yields an element, then removes the target Family directly from `project.Families`.

Both regressions require `InvalidOperationException`, unchanged `ProjectState.ChangeVersion`, unchanged target element FamilyId/inherited properties, and unchanged element dirty/timestamp state. The deliberate external list removal remains visible, proving the assignment itself did not add mutation after stale ownership was detected.

Source and regression commits were fetched back from GitHub after write and their diffs matched the reserved scope. `compare_commits` confirmed current `main` remained descended from regression commit `61287613592a2f7a31cc295b20ad2f25c9f587b0` after a later unrelated claim commit.

## Excluded scope

No changes to Zone/Grid/Floor freshness, global ProjectState collection tracking, Family activation/property propagation semantics, CAD/UI/runtime, Actions/build/release.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed. Validation in this lane is repository-level source/regression diff readback, ancestry verification and static contract review only.

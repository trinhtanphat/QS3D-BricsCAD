# Work claim — Family assignment canonical no-op smoke

- Status: `ACTIVE`
- Agent: `gpt56sol-family-assignment-noop-smoke-agent`
- Registered: `2026-08-14T19:47:00+07:00`
- Baseline main SHA: `da2c8c58c5e24a901eb73de23e6c93674241b706`
- Trigger: V25 Preview #181 / run `31801274134`, deterministic Core smoke job `94769593393`

## Evidence

The exact CI annotation reports `ProjectFamilyAssignmentAtomicitySmoke.SemanticallyIdenticalTargetAssignmentIsNoOp()` failing because the fixture assigns `"  target  "` through `ProjectElement.FamilyId`, whose public setter now canonicalizes the stored value to `"target"` before `ProjectFamilyService.Assign()` runs. Historical commit `22bcb9e738672a1680ed2633d8436bce7e070c1b` intentionally defines padded/case-varied references that already resolve to the target Family as true assignment no-ops that preserve stored identity and persistence state. Current `ProjectFamilyService.Assign()` still implements that contract by comparing the trimmed previous Family ID to the target and skipping mutation.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- this claim file

## Excluded scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- all production behavior, workflows, release policy, V25 runtime and unrelated smoke tests

## Planned fix

Keep production behavior unchanged. In the focused smoke, first assert the public FamilyId setter canonicalizes padded input, then inject the intended legacy/raw padded `_familyId` backing value via narrow reflection before invoking `ProjectFamilyService.Assign()`. Preserve the existing zero-change, identity, property, dirty/timestamp and project persistence assertions.

## Validation

- implementation stays on a dedicated agent branch;
- integrate through a dedicated integration branch before final `main` landing;
- verify the source commit is represented on current `main`;
- require the next exact V25 cloud run to advance past this deterministic failure before declaring the lane complete.

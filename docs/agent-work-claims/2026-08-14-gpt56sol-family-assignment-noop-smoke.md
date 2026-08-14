# Work claim — Family assignment canonical no-op smoke

- Status: `RELEASED`
- Agent: `gpt56sol-family-assignment-noop-smoke-agent`
- Registered: `2026-08-14T19:45:00+07:00`
- Baseline main SHA: `45ad998b54af55867116179b1a431bace358baa1`
- Trigger: release #181 / run `31801274134`, deterministic Core smoke annotation from job `94769593393`

## Coordination close-out

This reservation is a duplicate of the earlier visible claim `docs/agent-work-claims/2026-08-14-gpt56sol-family-assignment-canonical-noop-smoke.md`, whose claim-only commit `ae15c9bc66942a53aa27050373b3594d0fbbcf5c` was already reachable from `main` before PR #1331 landed. Do not create or merge a second equivalent implementation from this claim.

The reserved behavior is being handled by implementation commit `61294523490e4e1651ad9e2ace58997a9851f30b` on `agent/gpt56sol-family-assignment-noop-smoke-20260814`, merged to `integration/gpt56sol-family-assignment-noop-smoke-20260814` through PR #1332.

## Original evidence

`ProjectFamilyAssignmentAtomicitySmoke.SemanticallyIdenticalTargetAssignmentIsNoOp()` expects a legacy raw FamilyId value `"  target  "` to survive a semantic no-op assignment. `ProjectFamilyService.Assign()` trims the current relation only for semantic comparison and returns zero before mutation when it already identifies the target. The smoke currently assigns the padded value through `ProjectElement.FamilyId`, whose setter canonicalizes it before the service runs, so the fixture no longer represents the intended legacy raw state.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- this claim file

## Excluded scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- all production behavior, workflows/release policy, V25 runtime and issue #1005

## Resolution

Released as duplicate; the earlier reservation remains authoritative until its integrated result is qualified by fresh V25 CI.

# Work claim — Family assignment canonical no-op smoke

- Status: `ACTIVE`
- Agent: `gpt56sol-family-assignment-noop-smoke-agent`
- Registered: `2026-08-14T19:45:00+07:00`
- Baseline main SHA: `45ad998b54af55867116179b1a431bace358baa1`
- Trigger: release #181 / run `31801274134`, deterministic Core smoke annotation from job `94769593393`

## Evidence

`ProjectFamilyAssignmentAtomicitySmoke.SemanticallyIdenticalTargetAssignmentIsNoOp()` expects a legacy raw FamilyId value `"  target  "` to survive a semantic no-op assignment. `ProjectFamilyService.Assign()` trims the current relation only for semantic comparison and returns zero before mutation when it already identifies the target. The smoke currently assigns the padded value through `ProjectElement.FamilyId`, whose setter canonicalizes it before the service runs, so the fixture no longer represents the intended legacy raw state.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- this claim file

## Excluded scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `src/QS3D.Core/Domain/ProjectElement.cs`
- all production behavior, workflows/release policy, V25 runtime and issue #1005

## Planned fix

Keep production behavior unchanged. Assert the public FamilyId setter canonicalizes padded input, then inject the intended legacy raw `_familyId` only inside the smoke via narrow reflection. Preserve the existing zero-change, raw-value, properties, dirty/timestamp and project-version no-op assertions.

## Integration path

Implementation will use a recovery branch, then a dedicated integration branch/PR, then final reviewed landing to `main` in accordance with `docs/GITHUB-MAIN-PROTECTION.md`.

## Completion condition

The test-only fixture repair is integrated to current `main`, ancestry is verified, and fresh current-main V25 CI advances past this smoke or exposes the next deterministic blocker.
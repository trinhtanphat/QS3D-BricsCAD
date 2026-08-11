# Work claim — Active Family whitespace repair

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-active-family-whitespace-repair`
- Registered: `2026-08-11T22:58:00+07:00`
- Baseline main SHA observed: `227c7259b35879ab76c3b09f4129209560156d4f`
- Priority: P1 — `ProjectFamilyActivationService.ClearIfMissing()` currently returns early for whitespace-only `ActiveFamilyId`. `GetActive()` treats that value as no active Family, but the repair path leaves the stale metadata entry in place, so read and repair semantics disagree and the invalid metadata can remain in memory/persistence metadata.

## Reserved scope

- Make `ClearIfMissing()` remove whitespace-only `ActiveFamilyId` as missing/invalid state while retaining no-op behavior when the key is absent and preserving valid existing active Family references.
- Add deterministic Core smoke coverage and focused static preflight for this repair contract.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyActivationService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationWhitespaceRepairSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationWhitespaceRepairSmokeRegistration.cs` (new)
- `scripts/preflight-project-family-activation-whitespace-repair.py` (new)
- this claim file for close-out

## Excluded scope

- No Qsdb schema/loader validation change, no Family UI/Workspace, no active-Family deletion/reference logic, no CAD/Ribbon/updater/release/GitHub Actions.
- No automatic canonical rewrite of a valid padded Family id; explicit SetActive continues to own canonicalization of valid active identity.

## Validation plan

- Missing key remains no-op and does not Touch.
- Whitespace-only active metadata is removed with exactly one project Touch.
- Valid padded/case-varied active id remains present and resolves to the existing Family without mutation.
- Missing nonblank active id is removed with the existing mutation behavior.
- Focused preflight rejects the previous `missing-or-whitespace => return` condition.

## Completion condition

- `GetActive()` and `ClearIfMissing()` agree that whitespace-only ActiveFamilyId means no active Family, and the repair method actually clears that stale metadata with regression coverage.

# Work claim — Active Family deletion canonical-ID guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-active-family-delete-canonical-id`
- Registered: `2026-08-11T22:29:00+07:00`
- Baseline main SHA observed: `f24e8ce4e936365126887b7856a53f034de24175`
- Priority: P1 — `ProjectFamilyActivationService.GetActive()` canonicalizes a persisted `ActiveFamilyId` by trimming it, but `ProjectFamilyService.Delete()` compares the raw metadata value. A recoverable padded active-family id can therefore be treated as active by reads while deletion incorrectly permits removing that Family and leaves stale activation metadata.

## Reserved scope

- Canonicalize the active Family metadata comparison in `ProjectFamilyService.Delete()` without changing activation semantics or creating a second metadata key.
- Add deterministic Core smoke coverage proving padded/case-varied active ids cannot bypass the active-Family deletion guard while non-active deletion remains unchanged.
- Add a focused static preflight for this exact invariant.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActiveDeleteCanonicalSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActiveDeleteCanonicalSmokeRegistration.cs` (new)
- `scripts/preflight-project-family-active-delete-canonical.py` (new)
- this claim file for close-out

## Excluded scope

- No Workspace/Family UI, Family usage badge, Right Panel, quantity rules/settings, project persistence/session recovery, CAD/native source, Ribbon, updater, release or GitHub Actions.
- No broad rewrite of Family activation or deletion behavior; only the inconsistent raw-vs-canonical active-id comparison.

## Validation plan

- Deterministic smoke: padded active id blocks delete, case-insensitive padded id blocks delete, inactive Family deletion succeeds, active metadata remains unchanged on rejected delete.
- Static preflight requires trim + case-insensitive comparison before deletion and forbids regression to raw metadata comparison.
- Re-fetch current `main` before source write and preserve concurrent winners.

## Completion condition

- Read and mutation paths agree on the same canonical ActiveFamilyId identity, the regression is covered, and this claim is marked `COMPLETED` with pushed evidence.

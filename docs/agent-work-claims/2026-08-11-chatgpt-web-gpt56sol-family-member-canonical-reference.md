# Work claim — Family member canonical reference guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-member-canonical-reference`
- Registered: `2026-08-11T22:37:00+07:00`
- Baseline main SHA observed: `08d065ac766fc23c19df2d3d4a4ecba232c41c3a`
- Priority: P1 — `ProjectElement.FamilyId` is publicly mutable, but `ProjectFamilyService.ResolveFamilyMembers()` compares its raw text to a canonical Family id. A recoverable padded relation can therefore disappear from `ReferenceCount`, Family property propagation, and the deletion safety check even though `ProjectState.FindFamily()` treats trimmed IDs canonically elsewhere.

## Reserved scope

- Canonicalize Family member matching inside `ProjectFamilyService.ResolveFamilyMembers()` by trimming nullable relation text before the existing case-insensitive comparison.
- Add deterministic Core smoke coverage proving padded/case-varied Family relations remain visible to `ReferenceCount` and block Family deletion, while unrelated Family references remain excluded.
- Add a focused static preflight for this exact member-resolution boundary.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyMemberCanonicalReferenceSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/ProjectFamilyMemberCanonicalReferenceSmokeRegistration.cs` (new)
- `scripts/preflight-project-family-member-canonical-reference.py` (new)
- this claim file for close-out

## Excluded scope

- No rewrite of `ProjectElement` relation setters, persistence migration, Workspace/Family UI, quantity settings/rules, CAD/native source, Ribbon, updater, release or GitHub Actions.
- No change to duplicate-element detection or Family assignment semantics beyond member identity matching.

## Validation plan

- Deterministic smoke: padded relation counts as a reference; case-varied padded relation counts; deletion is rejected without mutation; unrelated Family remains zero-reference/deletable.
- Static preflight requires trimmed FamilyId comparison inside `ResolveFamilyMembers()` and rejects the previous raw comparison.
- Re-fetch current `main` before source write and preserve concurrent winners.

## Completion condition

- Family reference counting/property propagation/deletion safety all use canonical relation identity, with regression coverage and a completed pushed claim.

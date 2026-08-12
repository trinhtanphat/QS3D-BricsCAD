# Work claim — Family Activation global duplicate integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-activation-global-duplicate-integrity-20260812-0901`
- Registered: `2026-08-12T09:01:00+07:00`
- Baseline main SHA: `074ebe1a79b7ead9e03aac777e013f6fcfb4b8a2`
- Priority: P1 — active-Family state must not resolve or mutate through globally ambiguous Family identities.
- Task Key: `CORE-FAMILY-ACTIVATION-GLOBAL-DUPLICATE-ID`

## Confirmed defect

`ProjectFamilyActivationService.GetActive`, `SetActive`, and `ClearIfMissing` call `ProjectState.FindFamily(...)` only for the active/target ID. `FindFamily(target)` detects duplicates matching that target but does not reject an unrelated duplicate Family pair. With `F1`/`f1` plus unique `F2`, `SetActive(F2)` can therefore advance project revision and write `ActiveFamilyId=F2` even though the Family identity collection is globally invalid under QSDB/interchange rules. `GetActive` and `ClearIfMissing` likewise can return/retain ordinary active state from the same ambiguous project.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyActivationService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyActivationGlobalDuplicateIntegritySmoke.cs`
- this claim file

## Intended contract

- Family activation reads/mutations preflight all non-null Family IDs for case-insensitive uniqueness before resolving an active/target Family.
- `GetActive`, `SetActive`, and `ClearIfMissing` fail closed on unrelated duplicate Family IDs.
- Existing blank/missing ActiveFamilyId behavior, canonical no-op behavior, valid active switch and missing-active cleanup remain unchanged.
- No changes to ProjectFamilyService, Family Manager UI, persistence/interchange, Floor/Zone services or native BricsCAD code.

## Validation plan

Focused auto-registered Core smoke seeds `F1`/`f1` plus unique `F2`, proves all three APIs reject the ambiguous project and that SetActive/ClearIfMissing do not mutate metadata/version/timestamp. Valid controls preserve GetActive, canonical no-op SetActive, real switch and missing-active cleanup. Re-fetch exact source/claim before writes. No force-push, Actions dispatch, .NET smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.

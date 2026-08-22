# Work claim — recognition mapping defined-category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-recognition-mapping-defined-category-20260811-2218`
- Registered: `2026-08-11T22:18:00+07:00`
- Completed: `2026-08-11T22:23:30+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Claim commit: `4583cdf7728beabce284427e60a020c87697dd9e`
- Scope-narrowing commit: `9ce7a17f25d1c2d7cb07167cd485f47c350d0373`
- Implementation commit: `6038a20622a8eacb005462d9432e0b8e4948ece8`
- Regression-test commit: `e319375122e8f1b34555ea9d078d5eafd00db112`
- Priority: deterministic fail-closed defect found during owner-requested `continue all` source review

## Reserved scope

Require project recognition layer-mapping category values to resolve to a defined `ElementCategory`, including numeric enum text such as `"999"` that `Enum.TryParse` accepts but the enum does not define.

## Implemented

- `ValidateLayerMappings` now requires both successful enum parse and `Enum.IsDefined`.
- `ExactLayerMapping` independently retains the same defined-enum guard before entity compatibility/candidate creation.
- Focused smoke coverage now proves a valid Column project mapping remains usable, a numeric undefined mapping value (`"999"`) fails closed, and the valid mapping can be restored without changing the existing recognition flow.

## Changed surfaces

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file

## Excluded scope

- `tests/QS3D.Core.SmokeTests/ReviewHardeningSmoke.cs` was explicitly released before implementation to reduce shared-file contention.
- No Recognition/B4D native UI/runtime lifecycle or BricsCAD V25 qualification.
- No changes to `RecognitionEngine.cs`, generated ownership, semantic capture, templates/persistence, Direct Draw, reporting, updater, licensing, quantity, documentation or UI lanes.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

Before the fix, `ProjectRecognitionService.ValidateLayerMappings` treated `Enum.TryParse(..., out ElementCategory _)` as sufficient validity. .NET enum parsing accepts numeric strings whose underlying value is not defined, so `999` passed validation. `ExactLayerMapping` could parse the same undefined value and then return `null` through entity-type compatibility, silently falling back to generic recognition rather than rejecting malformed authoritative project mapping state.

## Validation performed

- Initial claim creation raced with concurrent `main` movement and was rejected by GitHub with 409; no claim file was created by that failed attempt.
- Re-synced `main`, rechecked recent recognition claims, and successfully published claim `4583cdf7728beabce284427e60a020c87697dd9e`.
- Verified the claim remained in current-main ancestry (`behind_by=0`) before implementation.
- Narrowed the test reservation from the large shared `ReviewHardeningSmoke.cs` to recognition-focused `ProxyCaptureEligibilitySmoke.cs` in a claim-only commit before modifying test source.
- Re-fetched both reserved source/test blobs from current `main` and used their exact blob SHAs for conflict-safe Contents API writes.
- Compared claim commit to then-current `main` `e319375122e8f1b34555ea9d078d5eafd00db112`: status `ahead`, `ahead_by=32`, `behind_by=0`; both changed product/test files remained present in the diff.
- No GitHub Actions workflow was dispatched or re-run. This remote pass does not claim hosted smoke execution or BricsCAD V25 runtime qualification.

## Outcome

Malformed numeric project layer-mapping categories can no longer be silently ignored and fall through to generic recognition. Undefined `ElementCategory` values now fail closed at the project mapping boundary while defined mappings preserve normal behavior.

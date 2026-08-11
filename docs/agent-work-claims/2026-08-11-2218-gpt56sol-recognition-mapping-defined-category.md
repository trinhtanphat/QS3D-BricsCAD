# Work claim — recognition mapping defined-category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-recognition-mapping-defined-category-20260811-2218`
- Registered: `2026-08-11T22:18:00+07:00`
- Baseline main SHA: `d2e5c2e4d009193970e1a346da5dfd098e274d4d`
- Priority: deterministic fail-closed defect found during owner-requested `continue all` source review

## Reserved scope

Require project recognition layer-mapping category values to resolve to a defined `ElementCategory`, including numeric enum text such as `"999"` that `Enum.TryParse` accepts but the enum does not define.

## Expected surfaces

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- `tests/QS3D.Core.SmokeTests/ReviewHardeningSmoke.cs` is explicitly released from this claim to reduce shared-file contention.
- No Recognition/B4D native UI/runtime lifecycle or BricsCAD V25 qualification.
- No changes to `RecognitionEngine.cs`, generated ownership, semantic capture, templates/persistence, Direct Draw, reporting, updater, licensing, quantity, documentation or UI lanes.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS claim.

## Defect evidence

`ProjectRecognitionService.ValidateLayerMappings` currently treats `Enum.TryParse(..., out ElementCategory _)` as sufficient validity. .NET enum parsing accepts numeric strings whose underlying value is not defined, so a mapping such as `QS3D.LayerMapping.<layer>=999` passes validation. `ExactLayerMapping` parses the same undefined value and can then return `null` through entity-type compatibility, silently falling back to generic recognition instead of rejecting malformed authoritative project mapping state.

## Validation plan

- Require both successful parse and `Enum.IsDefined` at the project mapping boundary.
- Keep the exact mapping path defensive against undefined category values.
- Add focused recognition smoke coverage proving numeric undefined mapping values fail closed while a normal defined project mapping remains usable.
- Re-fetch current `main` and both reserved files before writes; use SHA-guarded writes under concurrent branch movement.

## Coordination

The earlier recognition-category-integrity claim is `COMPLETED`; recent claim search shows no active recognition-mapping reservation. The focused `ProxyCaptureEligibilitySmoke.cs` surface is reused because it already owns project recognition fail-closed cases and is no longer reserved by the completed earlier claim.

## Completion condition

The mapping validation fix and regression are pushed to current `main`, the claim is closed with exact commit SHAs and truthful validation scope, and no GitHub Actions/native runtime qualification is claimed.

# Work claim — measured-solid removal freshness

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-measured-freshness`
- Registered: `2026-08-12T08:31:00+07:00`
- Baseline main SHA: `06a53250217420da17e004c45d29434ceb5636b7`
- Priority: `Correctness follow-up discovered while self-reviewing the completed measured-solid stale-cleanup lane; quantity removal must preserve ProjectElement freshness metadata semantics.`

## Reserved scope

Ensure stale policy-owned measured quantity removal in `MeasuredSolidQuantityPolicy.Apply` updates `ProjectElement.UpdatedUtc` exactly when a removal mutation occurs, matching the existing persistence freshness semantics of `SetQuantity` and other direct collection cleanup paths.

## Expected surfaces

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`
- `tests/QS3D.Core.SmokeTests/MeasuredSolidQuantityAtomicitySmoke.cs` only if focused deterministic coverage can be added safely

## Excluded scope

- measured quantity source extraction or B4D scanning
- gross/net formula ownership
- category regenerators
- global `ProjectElement` mutation APIs
- UI/CAD/runtime behavior

## Validation plan

- Keep validate-before-mutate atomicity unchanged.
- Coalesce one `TouchPersistenceState()` call when one or both stale measured keys are removed.
- Do not touch freshness for a true no-op.
- Re-read current source before SHA-guarded writes; do not run GitHub Actions.

## Completion condition

Claim is on `main` before implementation; the follow-up fix is pushed safely and this claim is closed with exact SHA/evidence.

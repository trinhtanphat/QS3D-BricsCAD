# Work claim — measured-solid removal freshness

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-measured-freshness`
- Registered: `2026-08-12T08:31:00+07:00`
- Completed: `2026-08-12T08:33:30+07:00`
- Baseline main SHA: `06a53250217420da17e004c45d29434ceb5636b7`
- Claim commit: `4bf588ce97ce27b542c1590739b9664e222823a4`
- Implementation commit: `bced52bb8d8ec74571415b0b62fedbff81ce38f8`
- Regression-test commit: `e81427b6b6497cc51386bf5ebf1f5e3f7b3f8aa6`
- Final pushed product/test SHA: `e81427b6b6497cc51386bf5ebf1f5e3f7b3f8aa6`
- Priority: `Correctness follow-up discovered while self-reviewing the completed measured-solid stale-cleanup lane; quantity removal must preserve ProjectElement freshness metadata semantics.`

## Reserved scope

Ensure stale policy-owned measured quantity removal in `MeasuredSolidQuantityPolicy.Apply` updates `ProjectElement.UpdatedUtc` exactly when a removal mutation occurs, matching the existing persistence freshness semantics of `SetQuantity` and other direct collection cleanup paths.

## Implemented

- Coalesced a single `TouchPersistenceState()` after one or both stale policy-owned measured quantities are actually removed.
- Preserved validate-before-mutate atomicity and existing measured-value application behavior.
- Preserved true no-op behavior: when no measured value is set and no stale policy-owned quantity is removed, `Apply` returns false and does not touch freshness.

## Regression coverage

`MeasuredSolidQuantityAtomicitySmoke.RemovalAdvancesFreshnessWithoutNoOpTouch` now proves:

- stale measured quantity removal is reported as handled;
- `UpdatedUtc` advances on removal;
- the next true no-op remains unhandled;
- the no-op leaves `UpdatedUtc` unchanged.

Existing measured source-removal and Earthwork fallback coverage remains intact.

## Excluded scope

- measured quantity source extraction or B4D scanning
- gross/net formula ownership
- category regenerators
- global `ProjectElement` mutation APIs
- UI/CAD/runtime behavior

## Validation performed

- Re-read `ProjectElement.TouchPersistenceState()` and confirmed direct dictionary removal otherwise bypasses the `SetQuantity` freshness update path.
- Source and test writes were SHA-guarded against current `main` blobs.
- No GitHub Actions workflow was dispatched or re-run. No licensed BricsCAD V25 runtime PASS is claimed.

## Outcome

The measured-solid stale-cleanup lifecycle now preserves both value correctness and element freshness semantics, while remaining idempotent on true no-op calls.

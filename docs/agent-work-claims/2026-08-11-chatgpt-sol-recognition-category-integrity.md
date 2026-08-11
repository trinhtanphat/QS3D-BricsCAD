# Work claim — recognition category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-recognition-category-integrity-20260811-2152`
- Registered: `2026-08-11T21:52:51+07:00`
- Baseline main SHA: `b33459e74ce1229e4b42cfe78190c5d3e6063ef7`
- Priority: evidence-driven Core recognition defect found during owner-requested repository review

## Reserved scope

Harden Core recognition so undefined `ElementCategory` enum values cannot enter a `RecognitionRule` or survive mutable `RecognitionCandidate` validation into capture-readiness / auto-accept decisions.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file for completion metadata

## Excluded scope

- No BricsCAD V25 adapter/UI/runtime changes.
- No generated native ownership redesign or changes to the completed generated-source-recognition lane.
- No persistence, interchange, reporting, quantity settings, Direct Draw, ribbon, updater, rebar parser, or geometry work.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

`RecognitionCandidate.Category` is mutable while `RecognitionResult.ValidateCandidates` currently validates only confidence. `RecognitionBatch` revalidates candidates and then uses `RecognitionResult.IsCaptureReady`; for a non-`ProxyEntity`, `EntitySnapshotCaptureEligibility.IsReady` returns ready before any category switch. Therefore an undefined enum value can remain eligible and be auto-accepted instead of failing closed. `RecognitionRule` also accepts undefined enum values at construction.

## Validation plan

- reject undefined categories at `RecognitionRule` construction;
- reject a candidate whose mutable category is changed to an undefined enum before batch construction;
- preserve existing measured ProxyEntity and normal non-proxy recognition behavior;
- source/static review only in this remote environment; do not claim BricsCAD V25 runtime validation.

## Coordination

Keep the patch narrowly within the listed Core recognition/test surfaces. Re-fetch latest `main` and both source files before implementation; if another ACTIVE/BLOCKED claim reserves either surface, stop rather than overlap.

# Work claim — Recognition confidence fail-closed readiness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `7070431ab0acee3c8dd1494bf8ef2821b19c50b0`
- Priority: evidence-driven remote-safe Core readiness hardening

## Reason

`RecognitionResult` validates candidate confidence values only in its constructor (and when a `RecognitionBatch` is built), while `RecognitionCandidate.Confidence` remains publicly mutable. A caller can construct a valid result, later set the top candidate confidence to `NaN`, and then `RequiresReview` evaluates both `NaN < 0.82` and a `NaN` margin comparison as false. With an otherwise capture-ready snapshot, malformed confidence can therefore fail open as `RequiresReview == false`.

## Reserved scope

Make direct recognition readiness evaluation revalidate the current candidate collection before deciding `RequiresReview`, so post-construction candidate mutation cannot bypass the established finite `[0,1]` confidence contract. Preserve candidate mutability, scoring, ordering, thresholds, batch behavior, capture eligibility, evidence, and normal valid results. Add a dedicated CAD-independent regression smoke.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `tests/QS3D.Core.SmokeTests/RecognitionResultConfidenceIntegritySmoke.cs`
- this claim file

## Excluded scope

- No changes to recognition scoring weights/rules, project layer mappings, Proxy capture policy, B4D/native UI, semantic capture, or BricsCAD V25 runtime.
- No change to valid candidate confidence values or thresholds.
- No GitHub Actions dispatch.

## Validation plan

- Construct a valid capture-ready `RecognitionResult`, mutate the top candidate confidence to `NaN`, and assert `RequiresReview` fails closed by throwing the existing confidence-validation exception instead of returning false.
- Repeat with an out-of-range confidence above 1.
- Confirm a valid high-confidence single candidate remains not-review-required and a valid low-confidence candidate remains review-required.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The earlier recognition category/mapping claims are `COMPLETED`, and their recorded scope explicitly excluded `RecognitionEngine.cs`. No current recognition-engine claim was found.

## Completion condition

Current `main` revalidates current candidate confidence before direct readiness evaluation, contains focused regression coverage, and this claim is marked `COMPLETED`.

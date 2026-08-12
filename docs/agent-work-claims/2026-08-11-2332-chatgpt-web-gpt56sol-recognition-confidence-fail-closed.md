# Work claim — Recognition confidence fail-closed readiness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `7070431ab0acee3c8dd1494bf8ef2821b19c50b0`
- Priority: evidence-driven remote-safe Core readiness hardening

## Reason

`RecognitionResult` validated candidate confidence values only in its constructor (and when a `RecognitionBatch` was built), while `RecognitionCandidate.Confidence` remained publicly mutable. A caller could construct a valid result, later set the top candidate confidence to `NaN`, and then `RequiresReview` evaluated both `NaN < 0.82` and a `NaN` margin comparison as false. With an otherwise capture-ready snapshot, malformed confidence could therefore fail open as `RequiresReview == false`.

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

## Completion

- Implementation commits:
  - `d77510f2c65074916c0c8c4b041a2124ee8e7353` — revalidate current candidates at the start of direct `RequiresReview` evaluation.
  - `a69f02c97d9673c04661beb69c516a6376902e81` — add post-construction `NaN`/out-of-range confidence regression plus valid high/low readiness coverage.
- Final observed `main` before claim close: `9a02aedfce071796c474b670f08364eda7837997`.
- Validation actually performed:
  - re-fetched `RecognitionResult.RequiresReview` from current `main` and confirmed it invokes the existing candidate validator before threshold/margin/capture-readiness logic;
  - re-fetched the dedicated smoke and confirmed mutated `NaN` and `1.01` confidence fail closed while valid `0.95` and `0.50` preserve existing readiness semantics;
  - candidate mutability, scoring weights, ordering, thresholds and capture policy were otherwise unchanged;
  - did not execute repository `dotnet` tests because this hosted session has no usable .NET SDK checkout;
  - did not dispatch or rerun GitHub Actions.
- BricsCAD V25 local gate impact: none; this is CAD-independent Core recognition-readiness integrity hardening.

## Completion condition

Satisfied: current `main` revalidates current candidate confidence before direct readiness evaluation, contains focused regression coverage, and this claim is released as `COMPLETED`.

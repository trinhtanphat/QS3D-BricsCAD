# Work claim — RecognitionResult confidence projection fail-closed

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-result-confidence-projection-20260812-0850`
- Registered: `2026-08-12T08:50:00+07:00`
- Baseline main SHA: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`
- Priority: evidence-driven remote-safe Recognition integrity during owner-requested continue-all audit

## Confirmed defect

`RecognitionCandidate.Confidence` remains publicly mutable after a `RecognitionResult` is constructed. Existing hardening validates current candidates in `RequiresReview` and `RecognitionBatch` partitions, but the public `RecognitionResult.Confidence` and `Margin` projections still read the mutable confidence values directly. A caller can therefore mutate a candidate to `NaN`/`Infinity` after construction and receive a non-finite public result instead of the fail-closed behavior already established for recognition readiness/partitioning.

## Reserved scope

- Make `RecognitionResult.Confidence` and `RecognitionResult.Margin` validate current candidates before exposing confidence-derived values.
- Preserve zero/one-candidate semantics and valid confidence arithmetic.
- Add focused Core smoke coverage for post-construction `NaN`/`Infinity` mutation plus valid projections.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Recognition scoring, thresholds, rule terms, category compatibility, batch partition logic, capture eligibility, snapshot scanning, UI/native runtime or persistence.
- No GitHub Actions or LOCAL_ONLY qualification.

## Validation plan

- A valid two-candidate result preserves expected top confidence and margin.
- Mutating the top candidate confidence to `NaN` makes both `Confidence` and `Margin` fail closed.
- Mutating the runner-up confidence to positive infinity makes `Margin` fail closed.
- Existing `RequiresReview` and batch behavior remains unchanged.

## Completion condition

Focused source and regression are merged to current `main`, remote source/test are re-read, and this claim is closed `COMPLETED` with the integration SHA.
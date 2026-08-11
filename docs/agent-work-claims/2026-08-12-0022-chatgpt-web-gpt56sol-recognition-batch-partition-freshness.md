# Work claim — Recognition batch partition freshness

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:22:00+07:00`
- Baseline main SHA: `d7eb1dda291328e8c24eb2ec18fba564898120a8`
- Priority: evidence-driven remote-safe recognition fail-closed correctness

## Reason

`RecognitionBatch` validates candidate state and computes `AutoAccepted` / `ReviewRequired` once in its constructor, but the contained `RecognitionCandidate` objects remain mutable. After batch construction a caller can lower or corrupt a candidate confidence while the cached partition still reports the result as auto-accepted. This makes the public batch classification stale and can fail open relative to the current candidate state.

## Reserved scope

Make batch partition access revalidate current candidates and classify against the batch's original thresholds at access time, so valid confidence/category mutations are reflected and malformed current candidate state fails closed. Preserve recognition scoring, thresholds, result ordering, capture-readiness semantics, input bounds and public property types. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` (`RecognitionBatch` only)
- `tests/QS3D.Core.SmokeTests/RecognitionBatchPartitionFreshnessSmoke.cs`
- this claim file

## Excluded scope

- No changes to recognition rules/scoring, `RecognitionResult` confidence validator, entity capture policy, project layer mappings, CAD/native/UI behavior, or BricsCAD V25 runtime.
- No deep immutability redesign of `RecognitionCandidate`.
- No GitHub Actions dispatch.

## Validation plan

- Build a batch with an initially auto-accepted result, lower the current candidate confidence, and assert it moves to `ReviewRequired` rather than remaining cached in `AutoAccepted`.
- Mutate current confidence to `NaN` after construction and assert partition access fails closed through existing candidate validation.
- Confirm an unchanged high-confidence result remains auto-accepted under the original batch thresholds.
- Re-fetch current `main` and exact source blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Recent recognition work bounds enumerable inputs and the earlier confidence fail-closed lane is completed. No current/recent claim was found for stale `RecognitionBatch` partition classification.

## Completion condition

Current `main` cannot expose stale auto-accept/review partitions after candidate mutation, focused regression coverage is present, and this claim is marked `COMPLETED`.

# Work claim — Recognition batch partition freshness

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:22:00+07:00`
- Completed: `2026-08-12T00:31:00+07:00`
- Baseline main SHA: `d7eb1dda291328e8c24eb2ec18fba564898120a8`
- Priority: evidence-driven remote-safe recognition fail-closed correctness

## Reason

`RecognitionBatch` validated candidate state and computed `AutoAccepted` / `ReviewRequired` once in its constructor, but the contained `RecognitionCandidate` objects remained mutable. After batch construction a caller could lower or corrupt a candidate confidence while the cached partition still reported the result as auto-accepted. This made the public batch classification stale and could fail open relative to the current candidate state.

## Reserved scope

Make batch partition access revalidate current candidates and classify against the batch's original thresholds at access time, so valid confidence/category mutations are reflected and malformed current candidate state fails closed. Preserve recognition scoring, thresholds, result ordering, capture-readiness semantics, input bounds and public property types. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` (`RecognitionBatch` only)
- `tests/QS3D.Core.SmokeTests/RecognitionBatchPartitionFreshnessSmoke.cs`
- this claim file

## Excluded scope

- No changes to recognition rules/scoring, `RecognitionResult` confidence validator, entity capture policy, project layer mappings, CAD/native/UI behavior, or BricsCAD V25 runtime.
- No deep immutability redesign of `RecognitionCandidate`.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `fa8e10ac67af9fb7bfd1314066eaac763f255177` — retain original batch thresholds and recompute auto-accepted/review partitions from validated current candidate state on access.
- Regression commit: `1a2de1cc37d6124083dad202284268c207866dfa` — verify high-confidence auto-accept, reclassification after confidence drops below the original threshold, restoration after confidence rises, and fail-closed partition access after `NaN` mutation.
- Final observed `main` before close: `66f9da3ee1f3cd33c549d266ac8921c2f8c05cde`.
- Validation actually performed:
  - re-fetched current `RecognitionBatch` source and confirmed partition access uses current candidate validation and the original constructor thresholds;
  - fetched the implementation commit diff and confirmed product changes are confined to `RecognitionBatch` (apart from contents-API EOF newline representation);
  - re-fetched the focused smoke source and confirmed both valid reclassification and malformed-current-state cases are covered;
  - the first smoke create attempt hit a normal concurrent-main `409`; current head was re-fetched and the file was created without force;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25 runtime PASS is claimed.

## Coordination

Recent recognition work bounds enumerable inputs and the earlier confidence fail-closed lane is completed. No current/recent claim was found for stale `RecognitionBatch` partition classification.

## Completion condition

Satisfied: current `main` cannot expose stale auto-accept/review partitions after candidate mutation, focused regression coverage is present, and this claim is released as `COMPLETED`.

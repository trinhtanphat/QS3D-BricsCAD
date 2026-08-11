# Work claim — Recognition batch partition multiplicity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:48:00+07:00`
- Baseline main SHA: `c6d7f7ee9a6ef4a0ad3583c5ae9e12e32111c6ce`
- Priority: evidence-driven remote-safe recognition partition correctness

## Reason

`RecognitionBatch.Results` preserves the materialized input sequence, including duplicate `RecognitionResult` references, and `AutoAccepted` preserves that same multiplicity through `Where`. `ReviewRequired`, however, currently uses LINQ `Except`, whose set semantics remove duplicate review-required results. The public constructor does not declare or enforce uniqueness, so a valid input sequence containing the same review-required result more than once can produce partitions whose cardinalities no longer account for all `Results` entries.

## Reserved scope

Make review-required classification use the same per-entry predicate semantics as auto-accepted classification so order and multiplicity are preserved. Keep current-candidate revalidation, original batch thresholds, scoring, capture-readiness semantics, input bounds and public property types unchanged. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` (`RecognitionBatch` only)
- `tests/QS3D.Core.SmokeTests/RecognitionBatchPartitionMultiplicitySmoke.cs`
- this claim file

## Excluded scope

- No changes to recognition scoring/rules, candidate mutability, confidence validation, entity capture policy, project mapping, CAD/native/UI behavior, or BricsCAD runtime.
- No uniqueness requirement added to batch inputs.
- No GitHub Actions dispatch.

## Validation plan

- Construct a batch containing the same low-confidence `RecognitionResult` reference twice and assert both entries remain in `Results` and `ReviewRequired`.
- Construct a mixed batch with duplicate accepted and duplicate review-required entries and assert partition counts/order account for every input entry.
- Preserve the freshness behavior added by the preceding completed lane: current candidate state is revalidated and the original thresholds remain authoritative.
- Re-fetch current `main` and exact source blob before writes; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

The preceding recognition batch partition-freshness claim is `COMPLETED` and intentionally left result multiplicity unchanged. No current/recent claim was found for duplicate-result partition semantics.

## Completion condition

Current `main` preserves input multiplicity/order across `Results`, `AutoAccepted` and `ReviewRequired`, focused regression coverage is present, and this claim is marked `COMPLETED`.

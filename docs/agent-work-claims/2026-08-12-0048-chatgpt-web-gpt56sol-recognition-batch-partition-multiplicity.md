# Work claim — Recognition batch partition multiplicity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:48:00+07:00`
- Completed: `2026-08-12T00:51:00+07:00`
- Baseline main SHA: `c6d7f7ee9a6ef4a0ad3583c5ae9e12e32111c6ce`
- Priority: evidence-driven remote-safe recognition partition correctness

## Reason

`RecognitionBatch.Results` preserves the materialized input sequence, including duplicate `RecognitionResult` references, while the old `ReviewRequired` implementation used LINQ `Except`. Because `Except` has set semantics, duplicate review-required entries were collapsed even though the public constructor did not require unique results. This made the partition cardinalities inconsistent with the input sequence.

## Reserved scope

Make review-required classification use the same per-entry predicate semantics as auto-accepted classification so order and multiplicity are preserved. Keep current-candidate revalidation, original batch thresholds, scoring, capture-readiness semantics, input bounds and public property types unchanged. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` (`RecognitionBatch` only)
- `tests/QS3D.Core.SmokeTests/RecognitionBatchPartitionMultiplicitySmoke.cs`
- this claim file

## Excluded scope

- No changes to recognition scoring/rules, candidate mutability, confidence validation, entity capture policy, project mapping, CAD/native/UI behavior, or BricsCAD runtime.
- No uniqueness requirement added to batch inputs.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `49a4d3a18626c38731bdfbd40146b542cb1d9332` — replace set-based review partitioning with per-entry predicate partitioning shared by both public partitions.
- Regression commit: `7805f954b9648176ca0e8aa32d0bf0128d62f2ba` — cover duplicate review-only input and mixed duplicate accepted/review input, including reference identity/order checks.
- Final observed `main` before close: `a006b701de90662fa8adfdbf5da94dd315f52441`.
- Validation actually performed:
  - re-fetched current `RecognitionBatch` and confirmed both partitions use per-entry filtering after current-candidate revalidation;
  - re-fetched the dedicated smoke and confirmed duplicate cardinality plus ordering/reference checks are present;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

The preceding recognition batch partition-freshness claim is `COMPLETED` and intentionally left result multiplicity unchanged. No current/recent claim was found for duplicate-result partition semantics.

## Completion condition

Satisfied: current `main` preserves input multiplicity/order across `Results`, `AutoAccepted` and `ReviewRequired`, focused regression coverage is present, and this claim is released as `COMPLETED`.

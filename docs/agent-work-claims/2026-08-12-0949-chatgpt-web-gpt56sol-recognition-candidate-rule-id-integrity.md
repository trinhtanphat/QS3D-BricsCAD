# Work claim — Recognition candidate RuleId integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Baseline main SHA: `3385893ba7133f5d505fd13f496b1760c069d1cb`
- Claim commit: `a41dbc76dc1e8f7454c7ebea95eba952b5dadc9e`
- Source fix: `427c5ee9c61810b19f04549878e7933ec7b0380b`
- Regression smoke: `1f4d7691de8b0c94658713a0a636dd9bfcf7929f`
- Priority: P1 Core recognition result determinism

## Confirmed defect

`RecognitionRule` requires a nonblank trimmed identity and `RecognitionEngine` produces candidates from canonical unique rule ids. `RecognitionCandidate.RuleId`, however, remains public-mutable. `RecognitionResult` previously validated only candidate null/category/confidence, so public construction or post-construction mutation could leave a candidate with blank/padded/noncanonical RuleId or duplicate another candidate RuleId. Dynamic reranking then used those malformed ids as its deterministic confidence tie-break, allowing invalid identity state to alter top-candidate/review semantics.

## Implemented contract

- `RecognitionResult.ValidateCandidates(...)` now requires every candidate RuleId to be nonblank;
- RuleId must already be canonical with no leading/trailing whitespace;
- RuleIds in one result must remain unique using `StringComparer.OrdinalIgnoreCase`, matching recognition-rule identity semantics;
- constructor validation and every current-candidate projection fail closed after invalid post-construction mutation because `TopCandidate`/`Margin`/review paths rerun the validator;
- `RecognitionBatch` partitions also fail closed because they validate current candidates before partitioning;
- scoring weights, rule terms, thresholds, capture eligibility, candidate object mutability and public `Candidates` order are unchanged.

## Regression coverage

`RecognitionCandidateRuleIdIntegritySmoke` is isolated and auto-registered with a module initializer. It covers:

- blank and whitespace RuleIds rejected at `RecognitionResult` construction;
- padded/noncanonical RuleId rejected at construction;
- case-insensitive duplicate RuleIds rejected at construction;
- post-construction blank RuleId fails closed through `TopCandidate`, `Margin`, `AutoAccepted` and `ReviewRequired`;
- post-construction duplicate RuleId fails closed through result projections and batch partitioning.

## Validation

- Exact source diff readback confirms only candidate RuleId required/canonical/unique checks were added in `RecognitionEngine.cs`.
- Exact regression commit readback confirms focused constructor + mutation coverage only.
- Compared source fix `427c5ee9c61810b19f04549878e7933ec7b0380b` to observed current `main` `8f1ed34bb4d92e957a33cef49cd9fc51dcae4d5b`: `behind_by=0`, `ahead_by=11`; no concurrent commit in that range modified `src/QS3D.Core/Recognition/RecognitionEngine.cs`.
- No GitHub Actions were dispatched. No executable .NET smoke/full build PASS and no licensed BricsCAD V25/V26 runtime PASS are claimed.

## Completion

`COMPLETED`: Recognition result identity is now fail-closed both at construction and after public candidate mutation, preserving deterministic reranking and batch semantics.

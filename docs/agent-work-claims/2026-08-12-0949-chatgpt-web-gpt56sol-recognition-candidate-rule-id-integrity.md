# Work claim — Recognition candidate RuleId integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Baseline main SHA: `3385893ba7133f5d505fd13f496b1760c069d1cb`
- Priority: P1 Core recognition result determinism

## Confirmed defect

`RecognitionRule` requires a nonblank trimmed identity and `RecognitionEngine` produces candidates from those canonical unique rule ids. `RecognitionCandidate.RuleId`, however, remains public-mutable, while `RecognitionResult` validation currently checks only candidate null/category/confidence. Public construction or post-construction mutation can therefore leave a candidate with blank/padded/noncanonical RuleId, or create duplicate RuleIds, and current dynamic reranking still uses those malformed ids as its deterministic confidence tie-break.

Because `RecognitionResult.TopCandidate`, `Margin`, review/capture projections and `RecognitionBatch` partitions all depend on current candidate ranking, malformed mutable candidate identities must fail closed rather than silently alter ranking/acceptance semantics.

## Reserved scope

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` — candidate identity validation only.
- `tests/QS3D.Core.SmokeTests/RecognitionCandidateRuleIdIntegritySmoke.cs` — focused auto-registered Core smoke.
- this claim file for close-out.

## Intended contract

- candidate RuleId must remain nonblank and canonical (`RuleId == RuleId.Trim()`);
- RuleIds within one result must remain unique using the same `OrdinalIgnoreCase` identity semantics used for recognition rules;
- constructor validation and all dynamic result/batch projections fail closed after invalid post-construction RuleId mutation;
- valid mutable confidence reranking remains unchanged;
- no change to scoring weights, rule terms, thresholds, capture eligibility or public candidate ordering.

## Coordination

Recent Recognition confidence-validation and candidate-reranking lanes are already completed. Current recent claims reserve drawing units, measured-solid lifecycle, quantity summary Follow3D, sheet layout, revisions, Family Activation, Grid and Quantity Rule provenance; none reserve Recognition candidate identity validation.

## Validation boundary

Remote source/test diff readback and ancestry only. Do not claim GitHub Actions, executable .NET smoke/full build, or licensed BricsCAD V25/V26 runtime PASS unless actually run.

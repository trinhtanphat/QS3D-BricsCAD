# Recognition candidate reranking after mutation

- Status: ACTIVE
- Agent: ChatGPT Web / GPT-5.6 Sol
- Scope: Keep `RecognitionResult` top-candidate projections consistent when public `RecognitionCandidate.Confidence` values are mutated after result construction.
- Evidence: `RecognitionEngine.Suggest()` initially orders candidates by descending confidence then `RuleId`, while `RecognitionCandidate.Confidence` remains publicly mutable. Current `RecognitionResult.TopCandidate`, `Margin`, `SuggestedCategory`, capture readiness and batch acceptance continue to trust the original candidate positions after a valid confidence mutation, so a promoted runner-up can leave the result reporting the old top candidate and a stale/negative margin.
- Plan: Preserve the public `Candidates` snapshot order, dynamically derive the current top two using the same deterministic ranking as `RecognitionEngine.Suggest()`, and add focused Core smoke coverage for post-construction reranking plus unchanged public candidate order.
- Reserved files:
  - `src/QS3D.Core/Recognition/RecognitionEngine.cs`
  - `tests/QS3D.Core.SmokeTests/RecognitionCandidateRerankingSmoke.cs`
- Excluded: scoring weights, thresholds, rule definitions/terms, capture eligibility policy, batch multiplicity, persistence, CAD runtime.

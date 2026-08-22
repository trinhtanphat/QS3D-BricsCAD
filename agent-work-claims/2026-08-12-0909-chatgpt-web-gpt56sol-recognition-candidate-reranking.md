# Recognition candidate reranking after mutation

- Status: COMPLETED
- Agent: ChatGPT Web / GPT-5.6 Sol
- Scope: Keep `RecognitionResult` top-candidate projections consistent when public `RecognitionCandidate.Confidence` values are mutated after result construction.
- Evidence: `RecognitionEngine.Suggest()` initially orders candidates by descending confidence then `RuleId`, while `RecognitionCandidate.Confidence` remains publicly mutable. The previous `RecognitionResult` projections trusted the original candidate positions after valid confidence mutation, so a promoted runner-up could leave the result reporting the old top candidate and a stale/negative margin.
- Resolution: Preserve the public `Candidates` snapshot order while dynamically deriving the current top two using the same deterministic confidence/`RuleId` ranking. `TopCandidate`, confidence, margin, category/evidence, capture/review projections and `RecognitionBatch` partitions now follow the current valid candidate ranking.
- Source: `54c3b4a26f2d77e78ba46574b21b181df3a37799`
- Regression smoke: `7fc6c0f44b7257f1bd734d42fcafe3e31270c1f7`
- Validation: source/test commit diff readback only; no GitHub Actions, executable Core smoke, full build, or BricsCAD runtime PASS claimed.
- Reserved files:
  - `src/QS3D.Core/Recognition/RecognitionEngine.cs`
  - `tests/QS3D.Core.SmokeTests/RecognitionCandidateRerankingSmoke.cs`
- Excluded: scoring weights, thresholds, rule definitions/terms, capture eligibility policy, batch multiplicity, persistence, CAD runtime.

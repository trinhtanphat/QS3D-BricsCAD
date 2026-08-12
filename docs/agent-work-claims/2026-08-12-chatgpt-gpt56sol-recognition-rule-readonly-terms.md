# Work claim — recognition rule read-only terms

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-recognition-rule-readonly`
- Registered: `2026-08-12T09:02:00+07:00`
- Completed: `2026-08-12T09:06:00+07:00`
- Baseline main SHA: `4601218af86e01f1909cc7bf688bc87315e59e88`
- Claim commit: `68054e5f4db548d8ca2a69f1ca32ca3c40e83e13`
- Implementation commit: `d064d6ed20dd9ceed5e22b0720546b5e44da485d`
- Regression-test commit: `4e326e2bc12f7653d70ef480cab6bf8186795ef9`
- Final observed main during verification: `e67c22582f8092ec31e4a55ffdc9a0c8d7854f49`
- Priority: `Core recognition configuration integrity discovered during requested continue-all review.`

## Confirmed defect

`RecognitionRule` normalized and validated its constructor term collections and exposed them as `IReadOnlyList<string>`, but `NormalizeTerms` returned the mutable `List<string>` produced by `ToList()`. A caller could cast `LayerTerms`, `TextTerms`, or `EntityTypes` back to a mutable list and change rule semantics after construction without re-running normalization/bounds validation.

## Implemented

- `RecognitionRule.NormalizeTerms` now wraps the normalized, de-duplicated `List<string>` in `AsReadOnly()` before publication.
- Existing term ordering, normalization, case-insensitive de-duplication and bounded materialization are unchanged.
- Candidate confidence, result projections, scoring, batch partitions, capture eligibility, mappings and native/UI workflows are untouched.

## Regression coverage

`RecognitionBoundedEnumerationSmoke.RuleTermsAreReadOnlySnapshots` now proves:

- layer/text/entity terms retain their canonical normalized values;
- mutating the original constructor source lists after rule construction cannot alter the rule;
- mutation through the returned `IList<string>` surface rejects both indexed replacement and `Add` with `NotSupportedException`;
- normal Beam recognition still succeeds after the immutability hardening;
- all pre-existing enumerable-bound regressions remain intact.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` — only `RecognitionRule.NormalizeTerms` ownership/immutability
- `tests/QS3D.Core.SmokeTests/RecognitionBoundedEnumerationSmoke.cs`

## Coordination

The immediately preceding Recognition confidence-projection lane was `COMPLETED` and explicitly excluded rule terms. This completed lane remained outside candidate confidence, result projections, scoring, batch partitions, capture eligibility, mappings, and native/UI workflows.

## Validation performed

- Re-read current main after both product and regression publication despite concurrent main movement.
- Current source still contains `ToList().AsReadOnly()` in `NormalizeTerms`.
- Current smoke still invokes `RuleTermsAreReadOnlySnapshots` and includes source-list isolation plus mutation-rejection checks.
- Product and test writes used exact current blob SHAs; no force-push or concurrent-work overwrite occurred.
- No GitHub Actions workflow was dispatched or rerun.
- No local .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote source-only lane.

## Outcome

Recognition rule term collections now satisfy their advertised read-only contract and cannot be mutated after normalization/validation. The lane is closed with no dangling ownership.

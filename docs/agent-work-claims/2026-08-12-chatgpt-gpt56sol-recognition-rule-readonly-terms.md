# Work claim — recognition rule read-only terms

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-recognition-rule-readonly`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `4601218af86e01f1909cc7bf688bc87315e59e88`
- Priority: `Core recognition configuration integrity discovered during requested continue-all review.`

## Confirmed defect

`RecognitionRule` normalizes and validates its constructor term collections, exposes them as `IReadOnlyList<string>`, but `NormalizeTerms` currently returns the mutable `List<string>` produced by `ToList()`. A caller can cast `LayerTerms`, `TextTerms`, or `EntityTypes` back to a mutable list and change rule semantics after construction without re-running normalization/bounds validation.

## Reserved scope

Make all three constructor-owned normalized term collections genuinely read-only while preserving ordering, de-duplication, normalization, scoring, and input bounds. Add focused regression to the existing bounded-enumeration smoke.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` — only `RecognitionRule.NormalizeTerms` ownership/immutability
- `tests/QS3D.Core.SmokeTests/RecognitionBoundedEnumerationSmoke.cs`

## Coordination

The immediately preceding Recognition confidence-projection lane is `COMPLETED` and explicitly excluded rule terms. This claim does not modify candidate confidence, result projections, scoring, batch partitions, capture eligibility, mappings, or native/UI workflows.

## Validation plan

- Preserve current normalized term values and case-insensitive recognition behavior.
- Prove constructor source collection mutation cannot affect the rule.
- Prove returned term collections reject mutation when exposed through `IList<string>`.
- Keep existing bounded-enumeration regressions intact.
- Re-read latest main and current blobs before each write; no GitHub Actions; no licensed BricsCAD runtime PASS.

## Completion condition

Claim is on main before source changes; source/test commits are pushed with SHA guards; claim closes `COMPLETED` with exact evidence.

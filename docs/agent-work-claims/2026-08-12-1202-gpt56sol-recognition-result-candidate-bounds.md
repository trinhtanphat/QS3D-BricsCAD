# Work claim — RecognitionResult candidate enumeration bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-result-candidate-bounds-20260812-1202`
- Registered: `2026-08-12T12:02:00+07:00`
- Baseline main SHA observed before registration: `e782ab6760b0f6cde9a09ecc04ca973095df86ca`
- Priority: evidence-driven remote-safe recognition input integrity

## Confirmed defect

`RecognitionInputBounds.MaxRules` limits a recognition engine to 10,000 rules, and `RecognitionResult` already requires candidate `RuleId` values to be unique. A valid result produced by an engine therefore cannot contain more than 10,000 candidates. However, the public `RecognitionResult(EntitySnapshot, IReadOnlyList<RecognitionCandidate>)` constructor currently validates the caller list with an unbounded `foreach` and then materializes it with `ToList()`. A custom `IReadOnlyList` can report a small `Count` while exposing an unbounded enumerator, and an oversized ordinary list is fully traversed instead of failing at the established engine rule ceiling.

## Reserved scope

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`, limited to bounded materialization of `RecognitionResult` candidate input before semantic validation.
- one focused `QS3D.Core.SmokeTests` regression file.
- this claim file.

## Intended contract

- Reuse the existing `RecognitionInputBounds.Materialize(...)` helper and `MaxRules` ceiling; do not invent a new numerical policy.
- Fast-reject known candidate collections above 10,000 without enumerating them.
- Stop arbitrary/lazy candidate enumeration at the 10,001st item and fail closed.
- Preserve current candidate null/rule-id/category/confidence validation, ordering/projection semantics, mutable-candidate revalidation, scoring and batch behavior.

## Coordination

A concurrently active authoritative-layer-mapping claim reserves `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`; this lane does not modify that file or layer-mapping semantics. Recent recognition candidate integrity/reranking/confidence lanes are completed and do not reserve candidate cardinality/materialization.

## Validation boundary

Focused source/readback regression only in this connector session. No GitHub Actions dispatch, local .NET build/smoke PASS, or licensed BricsCAD V25/V26 runtime PASS will be claimed without execution.

## Completion condition

Current `main` bounds `RecognitionResult` candidate enumeration using the already-established recognition rule ceiling, focused regression evidence is present, moving-main overlap is rechecked, and this claim is closed `COMPLETED` with exact integration evidence.

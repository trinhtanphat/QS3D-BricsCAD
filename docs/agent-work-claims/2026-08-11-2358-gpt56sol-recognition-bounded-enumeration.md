# Work claim — recognition bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-bounded-enumeration-20260811-2358`
- Registered: `2026-08-11T23:58:00+07:00`
- Baseline main SHA: `5bea0add9450dcab6378a736be98d8ad5b13ef9b`
- Priority: evidence-driven Core availability/integrity hardening during owner-requested `continue all`

## Reserved scope

Bound public Recognition `IEnumerable` inputs so rules, rule terms, snapshots and batch results fail closed at explicit cardinality limits instead of fully materializing unbounded/infinite streams.

## Expected surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- focused `tests/QS3D.Core.SmokeTests/*Recognition*Bound*` regression files
- this claim file for close-out

## Concrete defects

1. `RecognitionRule.NormalizeTerms()` currently calls `ToList()` after a lazy pipeline with no raw-input cardinality cap. A caller can supply an unbounded/infinite term stream and prevent construction from terminating.
2. `RecognitionEngine` materializes custom rules with unbounded `ToList()`.
3. `RecognitionBatch` materializes result streams with unbounded `ToList()`; both `RecognitionEngine.SuggestBatch` and `ProjectRecognitionService.SuggestBatch` feed lazy snapshot projections into this boundary, so unbounded snapshot streams can trigger unbounded recognition work and memory growth.

## Contract

- At most 10,000 custom recognition rules.
- At most 10,000 raw terms per rule term collection.
- At most 250,000 snapshots/results per recognition batch, aligned with the existing large semantic-element planning ceiling.
- Oversized `ICollection<T>` inputs fail from `Count` before enumeration where possible; arbitrary `IEnumerable<T>` inputs use the repo-standard `Take(max + 1)` fail-closed pattern.

## Explicit exclusions

- No recognition scoring thresholds, category compatibility, confidence semantics or capture policy changes.
- No V25/native entity enumeration or UI changes.
- No persistence/interchange/quantity/updater/licensing/release/Actions/LOCAL_PASS work.

## Validation plan

- Ordinary finite rule/term/batch inputs retain behavior.
- Oversized rule and term collections fail deterministically.
- Oversized batch result and snapshot collections fail before downstream scoring/enumeration when count is available.
- Lazy over-limit enumerables stop at the cap sentinel instead of fully materializing.
- Re-fetch/compare `main`, publish through a feature branch/PR without force-push, and re-read remote `main` after integration.

## Completion condition

Recognition public enumerable boundaries are cardinality-bounded with focused regression coverage integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.

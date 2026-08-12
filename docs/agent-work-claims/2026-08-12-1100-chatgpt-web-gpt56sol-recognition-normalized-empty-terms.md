# Work claim — Recognition rule normalized-empty term integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:00:00+07:00`
- Baseline main SHA: `1ac6de9fbc302e8230c0cb7fbb58c306a1bc607e`
- Priority: evidence-driven remote-safe recognition configuration integrity

## Reason

`RecognitionRule.NormalizeTerms` filters blank input before normalization but does not reject nonblank terms that normalize to an empty token. For example, an `entityTypes` entry of `"---"` becomes `""`. Because the resulting collection is non-empty, the scoring logic treats it as an explicit entity-type restriction, yet the empty token can match no normal entity type, silently turning the rule into a dead rule. Project layer mapping validation already fails closed when a nonblank pattern normalizes to empty.

## Intended scope

Reject nonblank recognition terms that normalize to empty while preserving existing behavior for truly blank input (ignored), valid normalized terms, case-insensitive deduplication, term bounds, default rules, scoring weights and candidate/result semantics.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without actual supported runtime execution.

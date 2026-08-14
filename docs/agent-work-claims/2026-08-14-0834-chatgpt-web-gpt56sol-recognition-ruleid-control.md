# Work claim — Recognition RuleId control-character canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-ruleid-control-20260814`
- Registered UTC: `2026-08-14T01:34:00Z`
- Baseline main SHA: `86f92bfb43b008141173d25524a670b189eaa74b`
- Priority: `P1 foundation hardening`

## Verified defect

The current Recognition identifier guards reject blank, surrounding-whitespace and duplicate candidate RuleIds, but still accept internal control characters such as `rule\nsecond`. `RecognitionRule` likewise trims an id and retains internal control characters. Those values participate in candidate ordering, evidence/presentation and diagnostic text, so a malformed identifier can cross the canonical Recognition boundary even though neighboring canonical identity contracts reject control characters.

## Reserved scope

- `src/QS3D.Core/Recognition/RecognitionEngine.cs` — RuleId canonicality only
- one new focused self-registering Core smoke regression
- this claim file

## Bounded implementation

- reject control characters in the canonical `RecognitionRule.Id` produced by the constructor;
- reject control characters in `RecognitionCandidate.RuleId` whenever `RecognitionResult` validates current candidates, including post-construction mutation;
- preserve existing rule-id trim behavior for `RecognitionRule`, case-insensitive duplicate semantics, ranking, confidence, entity-type, authoritative project-layer mapping, rule terms and all host/UI behavior;
- do not modify Recognition scoring/business heuristics, layer mapping semantics, capture eligibility, V25/native code, persistence or unrelated identity contracts.

## Validation plan

Focused smoke will cover constructor rejection for internal control characters, candidate construction rejection and post-construction mutation fail-closed behavior while preserving an ordinary canonical id. No GitHub Actions will be dispatched. Managed/native PASS will only be reported if actually executed. No force-push.

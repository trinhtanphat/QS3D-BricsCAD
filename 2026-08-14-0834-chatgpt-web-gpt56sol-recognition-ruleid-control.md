# Work claim — Recognition RuleId control-character canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-recognition-ruleid-control-20260814`
- Registered UTC: `2026-08-14T01:34:00Z`
- Completed UTC: `2026-08-14T01:39:00Z`
- Baseline main SHA: `86f92bfb43b008141173d25524a670b189eaa74b`
- Priority: `P1 foundation hardening`

## Verified defect

The Recognition identifier guards rejected blank, surrounding-whitespace and duplicate candidate RuleIds, but still accepted internal control characters such as `rule\nsecond`. `RecognitionRule` likewise trimmed an id while retaining internal control characters. Those values participate in candidate ordering, evidence/presentation and diagnostic text, so a malformed identifier could cross the canonical Recognition boundary even though neighboring canonical identity contracts reject control characters.

## Completed implementation

- `198ca8766054c7fe9abc88214b05745942a74863` — atomically updated `src/QS3D.Core/Recognition/RecognitionEngine.cs` and added `tests/QS3D.Core.SmokeTests/RecognitionRuleIdControlCharacterSmoke.cs` on current `main`.
- `RecognitionRule` now rejects control characters after preserving its existing surrounding-whitespace trim behavior.
- `RecognitionResult` candidate validation now rejects control characters in `RecognitionCandidate.RuleId`, including after post-construction mutation.
- Existing case-insensitive duplicate semantics, ranking, confidence, authoritative project-layer mappings, scoring, rule terms, capture eligibility and host/UI behavior remain unchanged.

## Validation recorded

- claim-first ownership was published to `main` at `966861a0cc507b55520093f772edaea23e9decb4` before source/test work;
- current-main overlap was rechecked before publish; concurrent commits touched Selection/QSDB claims only and did not touch Recognition;
- branch self-review from claim to implementation showed exactly three source-line additions plus one focused smoke file;
- implementation was reconciled on current `main` and published with a non-forced fast-forward;
- remote `main` was re-fetched at `198ca8766054c7fe9abc88214b05745942a74863`, and both source guards plus the self-registering smoke were re-fetched from that commit;
- smoke covers rule-constructor rejection of newline/tab controls, preservation of existing RuleId trim behavior, candidate construction rejection, and fail-closed post-construction mutation through result/batch reads;
- no GitHub Actions were dispatched, no managed runtime/native BricsCAD execution was performed, and no runtime/native PASS is claimed;
- no force-push.

## Scope exclusions preserved

No Recognition scoring/business heuristics, layer mapping semantics, capture eligibility, entity-type policy, V25/native code, persistence or unrelated identity contracts were modified.

# Work claim — Recognition rule normalized-empty term integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:00:00+07:00`
- Completed: `2026-08-12T11:04:00+07:00`
- Baseline main SHA: `1ac6de9fbc302e8230c0cb7fbb58c306a1bc607e`
- Priority: evidence-driven remote-safe recognition configuration integrity

## Reason

`RecognitionRule.NormalizeTerms` filtered blank input before normalization but did not reject nonblank terms that normalize to an empty token. For example, an `entityTypes` entry of `"---"` became `""`. Because the resulting collection was non-empty, scoring treated it as an explicit entity-type restriction, yet the empty token could match no normal entity type, silently turning the rule into a dead rule. Project layer mapping validation already fails closed when a nonblank pattern normalizes to empty.

## Changed scope

Nonblank recognition terms that normalize to empty now fail closed. Truly blank input is still ignored; valid normalized terms, case-insensitive deduplication, term bounds, default rules, scoring weights and candidate/result semantics remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `tests/QS3D.Core.SmokeTests/RecognitionRuleNormalizedEmptyTermSmoke.cs`
- this claim file

## Completion

- Claim commit: `aed0fa9d7228d43804c6b5fc55e447f762b722ba`.
- Implementation commit: `654c4a5ea7bbe140a934568643b654fff63438e9` — route nonblank rule terms through `NormalizeRequiredTerm` and reject normalized-empty tokens.
- Regression commit: `f5fa5567ffab86eb79c91c757241d7f06fbbb46c` — reject punctuation-only layer/text/entity-type terms and preserve blank skipping plus valid normalization/deduplication.
- Validation actually performed:
  - exact implementation diff was re-read and contained only the normalization call-site plus helper;
  - current source was re-fetched and contains the fail-closed helper;
  - dedicated smoke source was re-fetched and checked for invalid + valid controls;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

No active or completed normalized-empty recognition-term lane was found before registration. Recent recognition work covered candidate rule-id integrity and batch freshness, which are disjoint.

## Completion condition

Satisfied: current `main` rejects nonblank recognition rule terms that normalize to empty, preserves valid term behavior, focused regression coverage is present, and this claim is released as `COMPLETED`.

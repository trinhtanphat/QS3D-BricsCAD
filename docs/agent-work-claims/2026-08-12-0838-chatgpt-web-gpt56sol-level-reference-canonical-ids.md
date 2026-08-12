# Work claim — Level Reference canonical IDs

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-level-reference-canonical-ids`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `0abbd050176357572a4b165bf7cec408326bca16`
- Priority: P1 — persisted Bottom/Top Level references must not be silently normalized by diagnostics.
- Task Key: `CORE-LEVEL-REFERENCE-CANONICAL-IDS`

## Confirmed defect

`ProjectFloorService.AssignBottomLevel(...)` / `AssignTopLevel(...)` persist exact canonical `floor.Id` values. `LevelReferenceHealthService` previously trimmed stored `BottomLevelId` and `TopLevelId` values before validation. Directly mutated or malformed persisted values such as `" L1 "` therefore resolved as valid `L1` instead of producing health evidence. A whitespace-only stored reference was similarly normalized to empty/missing.

## Implemented fix

- Diagnostics now preserve the raw stored Bottom/Top Level reference text long enough to compare it with its trimmed form.
- Padded/whitespace-only Bottom references emit `BOTTOM_LEVEL_REFERENCE_NON_CANONICAL` with `HealthSeverity.Error`.
- Padded/whitespace-only Top references emit `TOP_LEVEL_REFERENCE_NON_CANONICAL` with `HealthSeverity.Error`.
- Trimmed IDs are still used for the existing missing/ambiguous/range checks, so established lookup behavior remains intact while malformed persisted spelling is fail-visible.
- Native-integration pending diagnostics remain suppressed when a canonicality error is already present for the element.

## Regression coverage

`tests/QS3D.Core.SmokeTests/LevelReferenceCanonicalIdHealthSmoke.cs` covers:

- padded Bottom Level ID;
- padded Top Level ID;
- whitespace-only Top Level ID;
- canonical Bottom/Top references do not emit the new canonicality errors.

## Integration evidence

- Claim registration: `2ecb42affc613707e5b25d1760411738be8d6701`.
- Source fix: `0c7416c7dcaaac41dfb9749296e37eb4670a14aa`.
- Focused Core smoke: `bc5aadcd20081f60c76ebfcec5458053fd4289bd`.
- Source and smoke were read back from current `main` after concurrent commits.
- Comparison from smoke commit `bc5aadcd20081f60c76ebfcec5458053fd4289bd` to then-current `main` `e86a5a71637882f00a0f6db7d7d8941a2eca0800` was `ahead`, `ahead_by=3`, `behind_by=0`, with the smoke commit as merge base.

## Validation boundary

Committed deterministic Core smoke coverage plus source/readback/ancestry review. No GitHub Actions were dispatched, no full local .NET build PASS is claimed, and no licensed BricsCAD V25 runtime PASS is claimed.

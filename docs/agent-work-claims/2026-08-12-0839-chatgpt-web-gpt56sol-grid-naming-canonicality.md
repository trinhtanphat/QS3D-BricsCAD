# Work claim — Grid Naming persisted canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web/gpt56sol-grid-naming-canonicality`
- Registered: `2026-08-12T08:39:00+07:00`
- Baseline main SHA: `deef1042c1078f2982d03a31907c4001c586f9e2`
- Priority: P1 — persisted Grid naming metadata must match writer-owned canonical spelling.
- Task Key: `CORE-GRID-NAMING-CANONICALITY`

## Confirmed defect

`GridNamingService.Renumber(...)` writes exact `GridLabel` text and writes `GridSequenceIndex` with invariant `int.ToString(...)`. `GridNamingHealthService` previously trimmed both stored values before validating them and accepted sequence strings such as `"01"` as integer `1`. Malformed or externally edited persisted Grid metadata could therefore use spellings the writer never emits and still pass naming health.

## Implemented fix

- `GridLabel` retains raw persisted text for canonicality comparison; leading/trailing whitespace now emits `GRID_LABEL_NON_CANONICAL` with `HealthSeverity.Error`.
- `GridSequenceIndex` is parsed for its existing numeric/range contract, then compared exactly with `sequenceIndex.ToString(CultureInfo.InvariantCulture)`.
- Padded or leading-zero numeric spellings now emit `GRID_SEQUENCE_NON_CANONICAL` with `HealthSeverity.Error`.
- Existing empty-label, label-length, duplicate-label, invalid-sequence/range and sequence-without-label behavior remains intact.
- Inspection remains read-only.

## Regression coverage

`tests/QS3D.Core.SmokeTests/GridNamingCanonicalityHealthSmoke.cs` covers:

- padded Grid label `" A "`;
- padded sequence `" 1 "`;
- leading-zero sequence `"01"`;
- canonical `"A"` / `"1"` control behavior.

## Integration evidence

- Claim registration: `93d09da17a6e38751fe680692622ccb9f57b8763`.
- Source fix: `06f0c7d74a95da191f8a162363364536f8e93eec`.
- Focused Core smoke: `2a9ef7111e599b708f18076cd51dd24ba633e4e8`.
- Source and smoke were read back from current `main` after concurrent commits.
- Comparison from smoke commit `2a9ef7111e599b708f18076cd51dd24ba633e4e8` to current `main` was `ahead`, `ahead_by=4`, `behind_by=0`, with the smoke commit as merge base.

## Validation boundary

Committed deterministic Core smoke coverage plus source/readback/ancestry review. No GitHub Actions were dispatched, no full local .NET build PASS is claimed, and no licensed BricsCAD V25 runtime PASS is claimed.

# Work claim — RevisionMath signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-revisionmath-signed-zero-20260813-2015`
- Registered: `2026-08-13T20:15:00+07:00`
- Baseline main SHA: `579f2cded8c18a5581509157ad5e4db88db72939`
- Priority: revision numeric canonicality hardening
- Claim-only commit: `e51d19df145f576b9f3f2e12a68d01fa926076c4`
- Source fix commit: `50ba32a6d0df94ae8433b643158a6fd67cdedfc6`
- Regression commit: `28c4f1dcc6b00ebd5434d68cb66956c9aec90344`

## Completed change

`RevisionMath.Finite(...)` now preserves its existing non-finite rejection while canonicalizing every accepted numeric zero to `+0d`. `Add(...)`, `Subtract(...)`, and `Percent(...)` likewise canonicalize a zero arithmetic result after retaining the existing overflow/divide-by-zero behavior and thresholds.

The focused existing `RevisionRegressionSmoke` now exercises public revision paths and verifies the sign bit rather than numeric equality alone:

- `RevisionService.Capture(...)` must store a quantity supplied as `-0d` as canonical positive zero;
- `QuantityRevisionRow.Delta` for `After=-0d` and `Before=+0d` must return canonical positive zero;
- `BitConverter.DoubleToInt64Bits(...) == 0L` guards the IEEE sign bit.

## Scope preserved

No edits were made to `RevisionService.cs`, `QuantityRevisionReport.cs`, `RevisionSnapshotStore.cs`, schema/XML validators, comparison tolerance, percentage denominator threshold, quantity keys, UI, native BricsCAD code, CST, MAP, or cross-repo platform work.

## Validation actually performed

- post-claim refresh confirmed the claim remained on current `main` with no competing `RevisionMath` / `RevisionRegressionSmoke` reservation;
- source commit and regression commit were published separately;
- compare `50ba32a6d0df94ae8433b643158a6fd67cdedfc6..28c4f1dcc6b00ebd5434d68cb66956c9aec90344` showed only the focused smoke plus an unrelated Zone/Floor claim-file change between the two commits;
- exact remote readback confirmed `RevisionMath.cs` blob `b6daed45f602a943063609e0dd5fe8e56e118487` and `RevisionRegressionSmoke.cs` blob `d390cf80926689e369c56410c3be7ba31b697d5f` contain the intended changes;
- no GitHub Actions were dispatched, no managed smoke executable or .NET build was run, and no BricsCAD/native runtime was executed; therefore no managed/native PASS is asserted.

## Completion

Completed. The reservation is closed and the two source/test files are free for future non-overlapping claims.
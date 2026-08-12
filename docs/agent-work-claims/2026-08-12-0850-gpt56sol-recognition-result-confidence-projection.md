# Work claim — RecognitionResult confidence projection fail-closed

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-recognition-result-confidence-projection-20260812-0850`
- Registered: `2026-08-12T08:50:00+07:00`
- Actual claim parent SHA: `a53bb86dd12adc37d672938d479f665ff29542a8`
- Feature branch base SHA: `4c88546c99c6894d41ad496748de763030896213`
- Priority: evidence-driven remote-safe Recognition integrity during owner-requested continue-all audit

## Confirmed defect

`RecognitionCandidate.Confidence` remains publicly mutable after a `RecognitionResult` is constructed. Existing hardening validated current candidates in `RequiresReview` and `RecognitionBatch` partitions, but the public `RecognitionResult.Confidence` and `Margin` projections still read mutable confidence values directly. A caller could mutate a candidate to `NaN`/`Infinity` after construction and receive a non-finite public result instead of the fail-closed behavior already established for recognition readiness/partitioning.

## Implemented fix

- `RecognitionResult.Margin` now validates current candidates before exposing confidence-derived arithmetic.
- `RecognitionResult.Confidence` now validates current candidates before returning the top confidence.
- Valid zero/one/two-candidate scalar semantics, scoring, thresholds, batch partitioning and capture behavior remain unchanged.
- Added focused module-initialized smoke coverage for valid projection values, post-construction top-candidate `NaN`, and runner-up positive infinity.

## Integration evidence

- Source branch commit: `2822c1af4bd1524da72cdc0c5be3654dbc268384`.
- Regression branch commit: `3e0ca41eb64f1183ffc6679b4a37d1991393ef49`.
- PR: `#669`.
- PR head reviewed: `bd685ba0f378b3bfd8391f7670e2d39d26d0be75`.
- Squash integration commit on `main`: `8c6b6cc39e64f79c2fa97fa7963f45ead0dee48f`.
- Remote `main` was re-read after integration and confirms both validating getters and the focused smoke source.

## Coordination / validation boundary

The original claim write raced with moving `main`; the actual claim parent was the unrelated Preview Review composite-key claim `a53bb86d...`. Before integration, moving-main comparisons and direct source readback confirmed no concurrent changes to `RecognitionEngine.cs`. Exact PR patch contained only `RecognitionEngine.cs` plus the focused smoke. No GitHub Actions were dispatched, no local .NET build PASS is claimed, and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Satisfied. The lane is released.

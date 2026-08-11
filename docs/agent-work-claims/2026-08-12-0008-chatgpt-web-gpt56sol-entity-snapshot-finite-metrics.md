# Work claim — EntitySnapshot finite metric integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `9e12cb7f1145659c84ed8fac4d033c8832007a68`
- Priority: evidence-driven remote-safe Core model hardening

## Confirmed defect

`EntitySnapshot` exposes public nullable metric setters for length, area, surface area and volume. These setters currently accept `double.NaN` and `double.PositiveInfinity`/`NegativeInfinity`, allowing a public Core model instance to carry non-finite measurement state. Some downstream recognition paths defensively ignore non-finite values, but the model itself does not enforce the finite-number invariant and other consumers can observe or propagate malformed metrics.

## Reserved scope

Require every non-null `EntitySnapshot` metric assignment to be finite while preserving existing `null`, zero, negative and ordinary finite values.

## Expected surfaces

- `src/QS3D.Core/Model/EntitySnapshot.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotFiniteMetricsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/EntitySnapshotFiniteMetricsRegistration.cs`
- this claim file

## Excluded scope

- No RecognitionEngine, capture eligibility or recognition scoring changes.
- No native BricsCAD adapter changes.
- No positivity/business-rule changes: only NaN/Infinity are rejected.
- No GitHub Actions dispatch.

## Validation plan

- Preserve constructor identity fields and nullable metric defaults.
- Accept representative finite values including zero and negative values to avoid inventing new business semantics.
- Reject NaN, +Infinity and -Infinity on all four metric properties.
- Use a dedicated module initializer to avoid shared smoke registry contention.
- Re-fetch the target blob immediately before product write and review exact pushed diffs/ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Completion condition

EntitySnapshot cannot retain non-finite measurement values through its public setters, focused smoke coverage is present on `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.
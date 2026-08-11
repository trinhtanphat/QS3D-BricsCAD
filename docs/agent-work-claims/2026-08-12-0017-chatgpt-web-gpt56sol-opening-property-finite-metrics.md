# Work claim — OpeningPropertySet finite metric integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:17:00+07:00`
- Baseline main SHA: `07c986cc4419eae81d11adf505b4586f7247c030`
- Priority: evidence-driven remote-safe Core domain integrity

## Confirmed defect

`OpeningPropertySet` exposes three public `double` auto-properties (`OpeningWidthM`, `OpeningHeightM`, `SillOffsetM`) that currently accept `double.NaN` and infinities. This allows malformed non-finite geometric measurements to be retained at a public Core domain boundary.

## Reserved scope

Require every assignment to the three opening metric properties to be finite. Preserve all existing finite values, including zero and negative values; this lane does not introduce dimensional positivity or placement business rules.

## Expected surfaces

- `src/QS3D.Core/Domain/OpeningPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/OpeningPropertySetFiniteMetricsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/OpeningPropertySetFiniteMetricsRegistration.cs`
- this claim file

## Excluded scope

- No physical-opening boolean/cutter/native service changes.
- No opening host, target-state, identity, placement, family or regeneration policy changes.
- No positivity/minimum-size engineering policy.
- No GitHub Actions dispatch.

## Coordination

Recent opening claims reserve V25 boolean audit-owned revision, cut target ownership, host enumeration and target-state surfaces. None reserves this Core DTO file or finite setter invariant. `ProjectElement.cs` is also deliberately excluded because another active lane reserves `SetQuantity` there.

## Validation plan

- Preserve default zero values and ordinary finite values.
- Preserve finite negative values rather than invent new business semantics.
- Reject NaN, +Infinity and -Infinity for width, height and sill offset.
- Verify failed assignments leave the prior finite value unchanged.
- Use a dedicated module initializer to avoid shared smoke registration contention.
- Re-fetch target blob immediately before product write and review exact pushed diffs/ancestry.
- No .NET/V25 runtime PASS will be claimed unless actually executed.

## Completion condition

`OpeningPropertySet` cannot retain non-finite metric values through public setters, focused smoke coverage is on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs.
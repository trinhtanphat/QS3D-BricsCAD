# Work claim — MTR-05 fact payload conflict

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr05-fact-payload-conflict-20260813-2305`
- Registered: `2026-08-13T23:05:00+07:00`
- Baseline main SHA: `24e604107a5d03dde70d234b2a61d1443e5a2313`
- Priority: `P0` quantity-trust integrity.

## Reserved scope

Reject conflicting `MeasurementTraceFact` payloads for one ordinal `(Name, SourceIdentity)` evidence identity. `Value` and `Unit` remain payload. Preserve facts whose name or source identity differs.

## Expected surfaces

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Excluded scope

- Adjustment identity/payload policy.
- Quantity formulas, projections, persistence, reports, UI, BricsCAD/native behavior.
- Other current agent claims.

## Validation plan

- Refresh `main` and claims after this claim-only commit.
- Add regressions for same fact identity with differing value and differing unit.
- Preserve exact-duplicate rejection and positive cases for different names/sources.
- Read back pushed files and inspect status; no Actions or native PASS claims.

## Coordination

The earlier exact-duplicate MTR-05 claim is `COMPLETED`; current source still accepts same `(Name, SourceIdentity)` with different payload. No visible current claim reserves these two MeasurementTrace surfaces.

## Completion condition

The conflicting fact payload state fails closed on current `main`, focused regression coverage is pushed, and this claim is closed `COMPLETED`.
# Work claim — UnitScale finite underflow integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-unit-scale-underflow-20260812-1322`
- Registered: `2026-08-12T13:22:00+07:00`
- Baseline main SHA: `1f031e97fa8eac275b118037dce790990f3a4d21`
- Priority: P1 — finite non-zero unit quantities must not silently collapse to exact zero.

## Confirmed defect

`UnitScale.Scale(...)` rejects non-finite input and non-finite multiplication results, but accepts IEEE-754 underflow to `0d`. For example, a finite non-zero quantity such as `double.Epsilon` converted through a sub-unit scale can multiply to exact zero. Downstream quantity/takeoff code then sees a valid finite zero rather than a failed conversion, silently erasing a non-zero measurement.

This is not a new domain minimum or capacity policy: it only detects the arithmetic case where a finite non-zero input loses all magnitude during the existing multiplication.

## Reserved scope

- `src/QS3D.Core/Units/UnitScale.cs`
- one new focused Core smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Preserve exact zero input as zero.
- Preserve every finite multiplication that remains representable, including subnormal non-zero results.
- Reject a finite non-zero input when multiplication underflows to exact zero.
- Preserve existing NaN/Infinity input rejection and non-finite-result overflow rejection.
- Do not change unit factors, enum mappings, project unit metadata, CAD/native behavior, or introduce a minimum quantity threshold.

## Validation boundary

Focused source-safe smoke/readback only. No GitHub Actions, full local .NET build/smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.
# Work claim — Rebar arithmetic finite underflow integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-math-underflow-20260812-1326`
- Registered: `2026-08-12T13:26:00+07:00`
- Baseline main SHA: `34637af83161a538d9cb2af81ea5a86ac6f41022`
- Priority: P1 — positive rebar arithmetic must not silently collapse to exact zero.

## Confirmed defect

`RebarMath.Multiply(...)` and `RebarMath.Divide(...)` reject non-finite results but accept IEEE-754 underflow to exact zero. This can silently erase a mathematically positive rebar quantity. Public `RebarWeight.KilogramsPerMeter(...)` demonstrates both paths: an extremely small but finite positive diameter can underflow during `diameter * diameter`, or can retain a subnormal square that then underflows during division by 162, returning zero unit weight instead of failing closed.

The same divide primitive is also used by spacing-based schedule quantity calculation, where silently changing a positive ratio to zero can alter the subsequent ceiling/count result.

This lane does not introduce a rebar minimum diameter, spacing or length; it only rejects complete loss of positive magnitude in existing arithmetic.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarMath.cs`
- one new focused Core smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Preserve multiply/divide when a zero numerator/factor legitimately yields zero.
- Preserve finite non-zero results, including representable subnormals.
- Reject multiplication when both factors are non-zero and the result underflows to exact zero.
- Reject division when the numerator is non-zero and the result underflows to exact zero.
- Preserve existing nonnegative/positive operand validation and non-finite overflow behavior.
- Do not change rebar domain minima, parser syntax, formulas, unit-weight formula or CAD/native behavior.

## Validation boundary

Focused source-safe public `RebarWeight` smoke/readback only. No GitHub Actions, full local .NET build/smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.
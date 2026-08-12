# Work claim — Rebar arithmetic finite underflow integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-math-underflow-20260812-1326`
- Registered: `2026-08-12T13:26:00+07:00`
- Completed: `2026-08-12T13:29:00+07:00`
- Baseline main SHA: `34637af83161a538d9cb2af81ea5a86ac6f41022`
- Priority: P1 — positive rebar arithmetic must not silently collapse to exact zero.

## Confirmed defect

`RebarMath.Multiply(...)` and `RebarMath.Divide(...)` rejected non-finite results but accepted IEEE-754 underflow to exact zero. This could silently erase a mathematically positive rebar quantity. Public `RebarWeight.KilogramsPerMeter(...)` demonstrated both paths: an extremely small but finite positive diameter could underflow during `diameter * diameter`, or retain a subnormal square that then underflowed during division by 162, returning zero unit weight instead of failing closed.

The same divide primitive is used by spacing-based schedule quantity calculation, where silently changing a positive ratio to zero can alter the subsequent ceiling/count result.

This lane does not introduce a rebar minimum diameter, spacing or length; it only rejects complete loss of positive magnitude in existing arithmetic.

## Completed implementation

- Claim commit: `e67be10edbf0c90eb881e149f0fcc154b5eb3923`.
- Source fix: `2764aa2ba9cf79d8248da908edeffb35936cb128`.
- Focused smoke: `926647d25c3db0cc3c7a6848eb331c546adf7073`.
- Read back source blob from moving `main`: `28f692055ef06393410dafdb08f5ff2d5c05b49b`.
- Read back smoke blob from moving `main`: `37540f9e0583b7d77e73a17859500367b6785a7d`.

## Final contract

- Multiply/divide still preserve legitimate zero from a zero numerator/factor.
- Finite non-zero results remain accepted, including a representative subnormal unit weight.
- Multiplication now rejects exact-zero underflow when both factors are non-zero.
- Division now rejects exact-zero underflow when the numerator is non-zero.
- Existing nonnegative/positive operand validation and non-finite overflow behavior remain unchanged.
- Rebar domain minima, parser syntax, formulas, unit-weight formula and CAD/native behavior were not changed.

## Validation boundary

Focused source-safe public `RebarWeight` smoke/readback only. No GitHub Actions were dispatched. No full local .NET build/smoke PASS or BricsCAD V25/V26 runtime PASS is claimed.
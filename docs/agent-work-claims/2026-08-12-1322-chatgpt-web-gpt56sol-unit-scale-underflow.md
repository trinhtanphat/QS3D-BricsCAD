# Work claim — UnitScale finite underflow integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-unit-scale-underflow-20260812-1322`
- Registered: `2026-08-12T13:22:00+07:00`
- Completed: `2026-08-12T13:24:00+07:00`
- Baseline main SHA: `1f031e97fa8eac275b118037dce790990f3a4d21`
- Priority: P1 — finite non-zero unit quantities must not silently collapse to exact zero.

## Confirmed defect

`UnitScale.Scale(...)` rejected non-finite input and non-finite multiplication results, but accepted IEEE-754 underflow to `0d`. A finite non-zero quantity such as `double.Epsilon` converted through a sub-unit scale could therefore become exact zero, allowing downstream quantity/takeoff code to treat lost magnitude as a valid zero measurement.

This is not a new domain minimum or capacity policy: it only detects the arithmetic case where a finite non-zero input loses all magnitude during the existing multiplication.

## Completed implementation

- Claim commit: `6067399efbe4a815023fbba07ccc7a46b4224988`.
- Source fix: `649b6072a366091dc96b0e5d4e08389fd32e2fcc`.
- Focused smoke: `c419215e1038046284d22953e9f0161effa2fe15`.
- Read back source blob from moving `main`: `6dcdf8c0ddce23760eb02b077c565035696c7789`.
- Read back smoke blob from moving `main`: `6b41f6dcb3fc2787e4779ce7e11b34f483a218df`.

## Final contract

- Exact zero input remains zero.
- Every finite multiplication that remains representable is preserved, including a representative subnormal non-zero result.
- A finite non-zero input whose multiplication underflows to exact zero now fails closed with `OverflowException`.
- Existing NaN/Infinity input rejection and non-finite-result overflow rejection remain unchanged.
- Unit factors, enum mappings, project unit metadata and CAD/native behavior were not changed; no minimum quantity threshold was introduced.

## Validation boundary

Focused source-safe smoke/readback only. No GitHub Actions were dispatched. No full local .NET build/smoke PASS or BricsCAD V25/V26 runtime PASS is claimed.
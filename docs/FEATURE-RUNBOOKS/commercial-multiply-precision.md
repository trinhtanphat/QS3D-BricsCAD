# Commercial exact multiplication precision

Lane-Key: issue-5844

## Defect

`CommercialGuard.Multiply` historically delegated to native checked `decimal` multiplication. That detects arithmetic overflow, and an additional guard detected nonzero products rounded all the way to zero, but a nonzero exact product can also require scale greater than 28 and be rounded to a different nonzero `decimal`. Commercial amounts must not silently lose precision.

A concrete case is `0.0000000000000000000000000001m * 1.5m`: the exact value is `1.5e-28`, which requires decimal scale 29 and therefore cannot be represented exactly.

## Contract

- Multiplication decodes both decimal operands to exact integer coefficients and scales, multiplies coefficients, and adds scales.
- Materialization may reduce scale only by removing trailing decimal zeros from the exact coefficient.
- If scale still exceeds 28, or the exact coefficient cannot fit the decimal coefficient without loss, multiplication throws `OverflowException` instead of publishing a rounded amount.
- Exactly representable scale-28 products remain accepted.
- Products whose combined scale exceeds 28 but is exactly reducible through trailing zeros remain accepted.
- Existing commercial overflow behavior remains fail-closed.

## Deterministic evidence

`CommercialMultiplyPrecisionSmoke` exercises the production surface through `EstimatingLine.Amount`: a non-representable product must reject, exact and reducible controls must preserve the exact amount, and a true overflow must still reject. `scripts/preflight-commercial-multiply-precision.py` pins the exact-coefficient implementation and forbids regression to native `checked(left * right)` inside `CommercialGuard.Multiply`.

Runtime classification: REMOTE_SAFE / NOT_APPLICABLE. This is managed Core commercial arithmetic correctness; licensed BricsCAD runtime evidence is not required and must not be claimed.

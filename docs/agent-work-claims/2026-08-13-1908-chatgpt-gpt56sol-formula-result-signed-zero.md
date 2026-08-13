# Work claim — Formula evaluator signed-zero result canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260813-1908`
- Registered: `2026-08-13T19:08:21+07:00`
- Baseline main SHA: `47b975d0976057ae6e10303d4f15ae1e59d21b95`
- Priority: P0 calculation correctness / deterministic numeric canonicality.

## Reserved scope

Canonicalize zero-valued results returned by `ExpressionEvaluator.Evaluate(...)` to IEEE `+0d`, while preserving the existing grammar, finite checks, arithmetic-underflow failures, function semantics, reference parsing and variable binding behavior. Add focused bit-level regression coverage in the existing formula finite-safety smoke.

Reserved files:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`

## Source evidence

On current main, `Parser.Parse()` returns `EnsureFinite(value, ...)` unchanged. Unary negation therefore makes the valid expression `-0` return IEEE negative zero; multiplication/division such as `0 * -1` and `0 / -1` can do the same. Existing finite-safety smoke uses numeric `Near(0d, ...)`, which treats `-0d == +0d` and cannot detect the sign bit. Recent canonical numeric contracts in Core normalize representable zero rather than preserve a negative-zero bit pattern.

## Excluded scope

- no grammar/token/reference-parser changes;
- no arithmetic-underflow, overflow, non-finite, function or rounding-policy changes;
- no quantity/business formulas, MeasurementTrace, Cost/Estimate, persistence, UI or native BricsCAD changes;
- no overlap with the active MTR-04 adjustment-rule-association lane;
- no GitHub Actions, packaging, release, installed-reference build or native runtime qualification.

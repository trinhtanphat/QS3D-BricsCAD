# Work claim — Formula evaluator signed-zero result canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260813-1908`
- Registered: `2026-08-13T19:08:21+07:00`
- Completed: `2026-08-13T19:29:00+07:00`
- Baseline main SHA: `47b975d0976057ae6e10303d4f15ae1e59d21b95`
- Priority: P0 calculation correctness / deterministic numeric canonicality.

## Reserved scope

Canonicalize zero-valued results returned by `ExpressionEvaluator.Evaluate(...)` to IEEE `+0d`, while preserving the existing grammar, finite checks, arithmetic-underflow failures, function semantics, reference parsing and variable binding behavior. Add focused bit-level regression coverage in the existing formula finite-safety smoke.

Reserved files:

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
- `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`

## Source evidence

On the claimed baseline, `Parser.Parse()` returned `EnsureFinite(value, ...)` unchanged. Unary negation therefore made the valid expression `-0` return IEEE negative zero; multiplication/division such as `0 * -1` and `0 / -1` could do the same. Existing finite-safety smoke used numeric `Near(0d, ...)`, which treated `-0d == +0d` and could not detect the sign bit.

## Completed implementation

- Claim commit: `a5a4acaa70ecaf93c61c09606002ff65e76f7169`.
- Production fix: `fbaeb0eca9feb1f828018431f949f0097bd6be57` (`fix(formulas): canonicalize zero results`).
- Regression: `ffc9164d9945e178462a23efdd08ea940b37736e` (`test(formulas): guard signed-zero results`).
- `ExpressionEvaluator.Evaluate(...)` now canonicalizes a zero-valued completed parser result to `+0d` at the public result boundary.
- Bit-level smoke coverage checks `-0`, `0 * -1`, `0 / -1`, and a variable bound to `-0d` using `BitConverter.DoubleToInt64Bits`.
- Production remote blob readback: `be30fbfd0af13aa4b6efd161321682f20b0c8ef5`.
- Smoke remote blob readback: `f4f284d832801938c25fa02ede96b24656753978`.

## Excluded scope

- no grammar/token/reference-parser changes;
- no arithmetic-underflow, overflow, non-finite, function or rounding-policy changes;
- no quantity/business formulas, MeasurementTrace, Cost/Estimate, persistence, UI or native BricsCAD changes;
- no GitHub Actions, packaging, release, installed-reference build or native runtime qualification.

## Validation actually executed

- Refreshed current `main` after claim publication and verified the claim commit remained an ancestor while concurrent changes did not touch the reserved Formula files.
- Refreshed `main` after production and regression commits; before closeout the remote head was exactly `ffc9164d9945e178462a23efdd08ea940b37736e`.
- Read back both production and smoke blobs from that remote head and verified the intended minimal source change and bit-level regression contents.
- Probed local managed toolchain: `dotnet`, `csc`, `mcs`, and `msbuild` are unavailable in this environment. No managed smoke/build PASS is claimed.
- No GitHub Actions were dispatched and no BricsCAD native/runtime PASS is claimed.

## Completion condition

Completed: claim-first ownership was published, the minimal production fix and focused regression were pushed to `main`, remote contents were read back, and this claim is closed without leaving an `ACTIVE`/`BLOCKED` reservation.

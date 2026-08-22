# Work claim — QuantityMath arithmetic underflow

- Status: `COMPLETED`
- Agent: `ChatGPT web / GPT-5.6 Sol`
- Registered: `2026-08-12T15:46:00+07:00`
- Baseline main SHA: `79dbf834335d7b8b43c276b21eb1adc086a20ede`
- Priority: Proven Core quantity arithmetic integrity defect: finite non-zero multiplication/division could underflow to exact zero and silently publish a false zero quantity.

## Reserved scope

Harden `QuantityMath.Multiply()` and `QuantityMath.Divide()` so exact-zero results produced by finite non-zero arithmetic fail closed, while preserving legitimate zero operands, representable subnormal results, existing overflow handling, and denominator validation.

## Expected surfaces

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Excluded scope

- `MeasuredSolidQuantityPolicy` persisted numeric-literal parsing.
- Curtain/Grid/Geometry arithmetic helpers, formulas, Rebar math, persistence, reporting, native CAD/runtime, release/preflight workflows.

## Implementation

- Claim publication: `b942ff6174cd3681e2afaa2a064b3c8c13791fd1`.
- Product guard: `2f83c7b4464a7e41b7c2da17ed98097824259c53`.
- Focused regression: `13794a8d7da5c530b3b55c569bba6949646acae5`.
- Smoke registration: `94ca58368a2fb7a1d87e347091dda23ee57a5df6`.
- Implementation head before claim close: `94ca58368a2fb7a1d87e347091dda23ee57a5df6`.

## Validation actually executed

- Re-read live `QuantityMath.cs` at `94ca58368a2fb7a1d87e347091dda23ee57a5df6`: multiplication and division now reject exact-zero underflow while preserving existing finite/overflow/denominator guards.
- Re-read live `QuantityMathUnderflowSmoke.cs`: regression covers multiplication underflow, division underflow, legitimate zero arithmetic, and representable `double.Epsilon` results.
- Re-read live `SmokeTestRegistration.cs`: `QuantityMathUnderflowSmoke.Run()` is registered in `RunAll()`.
- Executable .NET smoke/build: `NOT RUN` because no repository worktree/network clone is available in this environment.
- GitHub Actions / BricsCAD runtime: `NOT RUN` / not dispatched; no runtime PASS claimed.

## Coordination

The measured-solid numeric-literal-underflow lane completed before this claim and explicitly excluded `QuantityMath` arithmetic. Curtain multiplication-underflow work was already completed and served only as precedent. No overlapping active claim was introduced between publication and implementation.

## Remaining gates

- Full executable smoke/build and native runtime qualification remain governed by repository CI/runtime policy; none is claimed by this Core-only fix.

## Completion condition

Satisfied: the product guard and focused regression are pushed to `main`, smoke registration is live, and this claim is closed `COMPLETED` with the evidence actually obtained.

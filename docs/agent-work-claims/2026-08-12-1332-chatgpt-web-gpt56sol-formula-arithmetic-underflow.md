# Agent Work Claim

- Agent: `ChatGPT web / GPT-5.6 Sol`
- Status: `ACTIVE`
- Started at: `2026-08-12T13:32:00+07:00`
- Scope: Harden CAD-independent formula arithmetic so multiplication/division of finite non-zero operands that IEEE-754 underflow to exact zero fail closed instead of silently persisting a false zero quantity. Preserve legitimate arithmetic where at least one mathematical operand is already zero.
- Primary files:
  - `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
  - `tests/QS3D.Core.SmokeTests/FormulaArithmeticUnderflowSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
  - `docs/agent-work-claims/2026-08-12-1332-chatgpt-web-gpt56sol-formula-arithmetic-underflow.md`
- Tests intended:
  - Non-zero finite multiplication that collapses to exact zero is rejected.
  - Non-zero finite division that collapses to exact zero is rejected.
  - Legitimate zero multiplication/division remain valid.
- Dependencies:
  - Builds on completed formula finite-safety and variable-name-normalization lanes; no overlap with their completed scopes.
- Notes:
  - Pure Core/netstandard-compatible change; no BricsCAD host/native runtime, UI, Ribbon, Direct Draw, persistence, revision, rebar, grid, floor, or generated-handle surfaces are in scope.
  - GitHub Actions are not to be dispatched by this lane; validation is remote source/regression readback unless a supported local Core runtime is available.

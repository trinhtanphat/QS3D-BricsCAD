# Agent Work Claim

- Agent: `ChatGPT web / GPT-5.6 Sol`
- Status: `COMPLETED`
- State: `COMPLETED`
- Started at: `2026-08-12T15:05:00+07:00`
- Completed at: `2026-08-12T15:13:00+07:00`
- Baseline main SHA: `a0cb5739a3da282f02b4ae625a406e81919cdfe8`
- Task Key: `CORE-FORMULA-NUMERIC-LITERAL-UNDERFLOW`
- Scope: Harden CAD-independent formula numeric-literal parsing so a syntactically non-zero decimal/scientific token that `double.TryParse` underflows to exact zero is rejected instead of silently becoming zero. Preserve literal zero spellings, representable subnormal/normal values, existing non-finite rejection, and the completed multiplication/division underflow contract.
- Primary files:
  - `src/QS3D.Core/Formulas/ExpressionEvaluator.cs`
  - `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs`
  - this claim file
- Regression contract:
  - `1e-4000` is rejected with an underflow-specific formula error.
  - `0` and `0e-4000` remain valid exact zero literals.
  - `5e-324` remains valid as representable `double.Epsilon`.
  - Existing multiplication/division underflow handling remains unchanged.
- Dependencies:
  - Builds on completed formula finite-safety and arithmetic-underflow lanes; does not reopen multiplication/division handling.
- Notes:
  - Pure Core/net8 smoke surface; no BricsCAD host/native runtime, UI, persistence, QSDB, SourceHandle, generated-handle, revision, rebar, grid, floor, or release workflow scope.
  - No GitHub Actions, full .NET build, executable smoke run, or BricsCAD runtime PASS is claimed.

## Completion evidence

- Claim registration: `ea5dcd41423301f7630e46f8d3d8d7cebd7b1af4`
- Initial source fix: `1ceb7e046aa3d41bfc78eda3c50442c709e0ead8`
- Focused smoke: `678c6a547b3fa31fcb5c9016856200c16ae8e7a1`
- Significand-only correction: `9cdc9925c03693f74abcf9a9738660e355f71856`
- Pull request: `#938`
- Squash merge to `main`: `d11dd6ab9a8b7cd4583f75ceb45f0c103c3556cb`
- Current-main source readback blob: `a1f925d5a9fcda0a1d9329b4cac3db18f1e56855`
- Current-main smoke readback blob: `b77d72170dad7f17060946689ef1f9068a270959`
- Ancestry/readback: `d11dd6ab9a8b7cd4583f75ceb45f0c103c3556cb` was identical to current `main` at closeout readback.

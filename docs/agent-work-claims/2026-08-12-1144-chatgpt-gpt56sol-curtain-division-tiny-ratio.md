# Work claim — Curtain layout tiny-ratio division floor

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-curtain-division-tiny-ratio-20260812-1144`
- Registered: `2026-08-12T11:44:00+07:00`
- Priority: P1 Core geometry correctness

## Confirmed defect

`CurtainWallLayoutPlanner.DivisionCount(...)` computes `Math.Ceiling(ratio - 1e-12d)` and then rejects results below one. For valid finite positive inputs whose ratio is in `(0, 1e-12]`, the tolerance subtraction lowers the ceiling to zero, so a curtain span that requires exactly one supported division is rejected. A concrete counterexample is `spanM=1d`, `maximumM=2e12d` (`ratio=5e-13`), which must resolve to one division rather than fail integer-range validation.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs`, only `DivisionCount(...)`
- one focused Core smoke/regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

Preserve the existing positive/finite input validation, near-integer `1e-12` tolerance, integer-range protection, `MaxGridDivisions`, and panel-count limits. After applying the existing ceiling tolerance, clamp a valid positive finite ratio to a minimum of one division. Do not change curtain area calculations, rectangular-area overflow behavior, frame deductions, native builders, health diagnostics, persistence, or runtime workflows.

## Validation boundary

Add focused source-safe regression coverage pinning `span=1`, `maximum=2e12` through `Plan(...)` to one column/row as applicable, plus preservation of an existing near-integer tolerance control if the current smoke pattern permits. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
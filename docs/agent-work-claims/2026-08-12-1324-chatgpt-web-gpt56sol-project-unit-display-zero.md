# Work claim — ProjectUnitPolicy canonical display zero

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-unit-display-zero-20260812-1324`
- Registered: `2026-08-12T13:24:00+07:00`
- Completed: `2026-08-12T13:26:42+07:00`
- Baseline main SHA: `6067399efbe4a815023fbba07ccc7a46b4224988`
- Claim commit: `3749c9d802ad3da39c011d956c0fb0e1152ad98c`
- Implementation commit: `bc0334a22e8234ec638406c28a4be2c53eff23cc`
- Regression-test commits: `caca8a0b88fefbc793b910b27c02413bf6ac44b0`, `df848e474aeb308e2e10fa9343dc6d576f93cfc2`
- Priority: P2 — display rounding must not preserve an IEEE negative-zero sign bit.

## Confirmed defect

`ProjectUnitPolicy.RoundForDisplay(...)` returned `Math.Round(...)` directly. A finite negative value whose magnitude rounded to zero, or an explicit IEEE negative zero, could therefore retain the negative-zero sign bit at the project display-rounding boundary.

## Implemented

- Preserve existing finite-input validation, configured precision, and `MidpointRounding.AwayFromZero` behavior.
- Canonicalize every rounded zero to positive IEEE zero via `rounded == 0d ? 0d : rounded`.
- Preserve non-zero rounded results unchanged.
- Regression covers a small negative value rounding to zero, explicit negative zero constructed from its IEEE sign bit, and ordinary positive/negative rounding.

## Reserved scope

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `tests/QS3D.Core.SmokeTests/ProjectUnitDisplayZeroSmoke.cs`
- this claim file

## Coordination

The concurrent `UnitScale` finite-underflow claim owns `src/QS3D.Core/Units/UnitScale.cs` and a separate focused smoke. This work did not touch that file or its arithmetic-conversion contract.

## Validation performed

- Re-fetched `ProjectUnitPolicy.cs` after registering the claim and confirmed its blob had not changed under concurrent work before source publication.
- Re-fetched the source and smoke from `main` after publication and verified the canonical-zero implementation and focused regression remained present.
- The regression was strengthened to construct explicit negative zero by IEEE sign bit rather than relying on source-literal constant folding.
- No GitHub Actions workflow was dispatched or re-run. No hosted/local .NET PASS or BricsCAD V25/V26 runtime PASS is claimed without execution.

## Outcome

Project display rounding now has a single canonical zero representation while preserving all existing non-zero rounding semantics. Scope is released.

# Work claim — ProjectUnitPolicy canonical display zero

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-unit-display-zero-20260812-1324`
- Registered: `2026-08-12T13:24:00+07:00`
- Baseline main SHA: `6067399efbe4a815023fbba07ccc7a46b4224988`
- Priority: P2 — display rounding must not preserve an IEEE negative-zero sign bit.

## Confirmed defect

`ProjectUnitPolicy.RoundForDisplay(...)` returns `Math.Round(...)` directly. A finite negative value whose magnitude rounds to zero, or an explicit `-0d`, can therefore produce IEEE `-0.0`. This method is the project display-rounding boundary, so the same visual zero can retain two binary signs and downstream fixed-decimal formatting can surface a non-canonical negative zero such as `-0.000`.

## Reserved scope

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `tests/QS3D.Core.SmokeTests/ProjectUnitDisplayZeroSmoke.cs`
- this claim file

## Intended contract

- Preserve current finite-input validation, configured decimal precision, and `MidpointRounding.AwayFromZero` behavior.
- Canonicalize every rounded zero to positive IEEE zero.
- Preserve all non-zero rounded values unchanged.
- Do not modify `UnitScale.cs`, unit factors, unit enums, project metadata, CAD/native resolution, or any BricsCAD runtime behavior.

## Coordination

The current `UnitScale` finite-underflow claim owns `src/QS3D.Core/Units/UnitScale.cs` and a separate focused smoke. This claim does not touch that file or its arithmetic-conversion contract.

## Validation boundary

Focused source/readback regression only. No GitHub Actions dispatch, hosted/local .NET PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.

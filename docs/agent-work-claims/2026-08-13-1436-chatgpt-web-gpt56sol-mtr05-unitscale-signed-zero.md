# Work claim — MTR-05 UnitScale signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr05-unitscale-signed-zero-20260813-1436`
- Registered: `2026-08-13T14:36:00+07:00`
- Baseline main SHA: `15eef2ae8a2aaad865fe8b642d3e62ef5ab72f98`
- Priority: `MTR-05 / P0 continuous hardening` — canonical unit conversion results must not retain IEEE negative zero when equivalent quantity zero is already canonicalized elsewhere

## Confirmed defect

`ProjectUnitPolicy.RoundForDisplay(...)` explicitly canonicalizes every rounded zero to positive `0d`, and the canonical `MeasurementTrace` numeric contract likewise collapses signed zero. `UnitScale.Scale(...)`, however, returned the multiplication result directly. With an explicit IEEE negative-zero input and any positive unit scale, the public conversion APIs could therefore return negative zero even though it is numerically equal to zero.

This created an avoidable representation split inside the same units/measurement foundation. The existing UnitScale finite-underflow lane was already `COMPLETED` and did not cover signed zero.

## Reserved scope

Canonicalize exact-zero results returned by `UnitScale.Scale(...)` to positive `0d` after the existing finite/overflow/underflow guards.

The change preserves:

- all unit factors and enum mappings;
- existing NaN/Infinity input rejection;
- existing non-finite result rejection;
- existing finite non-zero underflow-to-zero rejection;
- representable subnormal non-zero results;
- ordinary signed non-zero conversions.

## Expected surfaces

- `src/QS3D.Core/Units/UnitScale.cs`
- `tests/QS3D.Core.SmokeTests/UnitScaleUnderflowSmoke.cs`
- this claim file

## Excluded scope

- No changes to `ProjectUnitPolicy`, drawing-unit resolution, quantity rules, MeasurementTrace/Snapshot/Delta/Inspector, reports or persistence.
- No changes to native BricsCAD V25/V26 adapters or LOCAL qualification.
- REV-03A remains excluded.
- LOCAL-003/Curtain/native runtime work remains excluded.
- No GitHub Actions and no BricsCAD native PASS claim.

## Implementation

- Claim-only registration: `575f86deaec7c47632846f5a2b1cb93ea85553f6`.
- Source fix: `72cd3315b76d8cba2a833e27d0d3533e98d0e5db` — `UnitScale.Scale(...)` now returns positive `0d` for any exact-zero result only after the pre-existing non-zero-underflow guard.
- Focused regression: `0126d3eb21cd64f69bdcfda66a33b28f08866c19` — existing ModuleInitializer smoke now constructs IEEE negative zero via `BitConverter.Int64BitsToDouble(long.MinValue)` and checks positive-zero bits across `ToMeters`, `FromMeters`, `ToSquareMeters`, and `ToCubicMeters`; it also retains underflow/subnormal/ordinary conversion checks and adds an ordinary negative conversion guard.
- A concurrent Workspace host-clipping preflight commit landed between the source and test commits; it was inspected and is non-overlapping with Units.

## Validation actually executed

- Re-fetched current `main` after registration and again after implementation.
- Re-fetched the exact current `UnitScale.cs` blob from remote and verified the existing input/non-finite/underflow guards remain unchanged and zero canonicalization occurs only at the final return.
- Re-fetched the exact current `UnitScaleUnderflowSmoke.cs` blob from remote and verified bit-level positive-zero assertions plus preservation checks are present.
- Executable managed smoke/build: `NOT_RUN` in this connector-only environment; source inspection is not reported as a test PASS.
- GitHub Actions: `NOT_RUN` / not dispatched.
- BricsCAD native qualification: `NOT_APPLICABLE` to this pure Core representation fix and no native PASS is claimed.

## Coordination

- Previous `UnitScale finite underflow integrity` claim is `COMPLETED`; this lane is a separate representation-canonicality invariant over the same two files.
- Existing canonical display-zero work is `COMPLETED` and provided supporting Units invariant evidence; this lane did not modify display rounding.
- Workspace UI, LOCAL-003, Curtain and REV-03A ownership remained non-overlapping and excluded.

## Completion condition

Satisfied: the claim-first UnitScale signed-zero fix and focused regression are present on current `main`, existing arithmetic guards remain intact, remote source/test were re-fetched, and executable/native evidence is reported only at the level actually executed.

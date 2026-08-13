# Work claim — MTR-05 UnitScale signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr05-unitscale-signed-zero-20260813-1436`
- Registered: `2026-08-13T14:36:00+07:00`
- Baseline main SHA: `15eef2ae8a2aaad865fe8b642d3e62ef5ab72f98`
- Priority: `MTR-05 / P0 continuous hardening` — canonical unit conversion results must not retain IEEE negative zero when equivalent quantity zero is already canonicalized elsewhere

## Confirmed defect

`ProjectUnitPolicy.RoundForDisplay(...)` explicitly canonicalizes every rounded zero to positive `0d`, and the canonical `MeasurementTrace` numeric contract likewise collapses signed zero. `UnitScale.Scale(...)`, however, returns the multiplication result directly. With an explicit IEEE negative-zero input and any positive unit scale, the public conversion APIs can therefore return negative zero even though it is numerically equal to zero.

This creates an avoidable representation split inside the same units/measurement foundation and can leak a sign bit into future deterministic formatting/fingerprinting callers. The existing UnitScale finite-underflow lane is `COMPLETED` and does not cover signed zero.

## Reserved scope

Canonicalize exact-zero results returned by `UnitScale.Scale(...)` to positive `0d` after the existing finite/overflow/underflow guards.

The change must preserve:

- all unit factors and enum mappings;
- existing NaN/Infinity input rejection;
- existing non-finite result rejection;
- existing finite non-zero underflow-to-zero rejection;
- representable positive/negative subnormal non-zero results;
- ordinary signed non-zero conversions.

## Expected surfaces

- `src/QS3D.Core/Units/UnitScale.cs`
- `tests/QS3D.Core.SmokeTests/UnitScaleUnderflowSmoke.cs` — extend the existing focused UnitScale arithmetic smoke with explicit IEEE negative-zero checks
- this claim file

## Excluded scope

- No changes to `ProjectUnitPolicy`, drawing-unit resolution, quantity rules, MeasurementTrace/Snapshot/Delta/Inspector, reports or persistence.
- No changes to native BricsCAD V25/V26 adapters or LOCAL qualification.
- REV-03A remains `ACTIVE` and is explicitly excluded.
- LOCAL-003/Curtain/native runtime work remains excluded.
- No GitHub Actions and no BricsCAD native PASS claim.

## Validation plan

- Re-fetch current `main` after this claim-only commit and recheck recent claims/commits for Units overlap.
- Extend the existing ModuleInitializer smoke to construct negative zero from its IEEE sign bit and assert `ToMeters`, `FromMeters`, `ToSquareMeters`, and `ToCubicMeters` return positive-zero bits.
- Preserve the existing underflow, representable-subnormal and ordinary conversion assertions.
- Re-fetch source and smoke from pushed `main` and inspect exact blobs before closeout.
- Executable managed smoke/build remains `NOT_RUN` unless a real .NET execution path is available; source review is not reported as PASS.

## Coordination

- Previous `UnitScale finite underflow integrity` claim is `COMPLETED`; this lane is a separate representation-canonicality invariant over the same two files.
- Existing canonical display-zero work is `COMPLETED` and provides supporting Units invariant evidence; this lane does not modify display rounding.
- Current Workspace UI, LOCAL-003, Curtain and REV-03A ownership are non-overlapping and excluded.

## Completion condition

The claim-first UnitScale signed-zero fix plus focused regression is present on current `main`, existing arithmetic guards remain intact, remote source/test are re-fetched, and this claim is updated to `COMPLETED` with exact pushed SHAs and validation actually executed.

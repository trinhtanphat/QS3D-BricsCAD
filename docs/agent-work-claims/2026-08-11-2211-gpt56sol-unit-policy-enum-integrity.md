# Work claim — unit-policy enum integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-unit-policy-enum-integrity-20260811-2211`
- Registered: `2026-08-11T22:11:28+07:00`
- Baseline main SHA: `10438bbc3b2c9e6ba53011d37cac3c2bf2e3f65e`
- Priority: evidence-driven Core invariant hardening during owner-requested `continue all`

## Reserved scope

Harden the CAD-independent unit-policy boundary so undefined enum values cannot create an invalid `ProjectUnitPolicy` instance or be persisted as an invalid quantity-unit binding source.

## Expected surfaces

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file for close-out

## Explicit exclusions

- No BricsCAD V25 adapter/runtime/UI changes.
- No `QS3DUNITS` command lifecycle, project-context, save/reopen, or LOCAL-001 qualification changes.
- No unit conversion-factor changes or INSUNITS mapping expansion.
- No updater/licensing, Build3D, Xref, rebar, health dependency, documentation-editor, persistence/interchange, or other currently claimed lanes.
- No GitHub Actions dispatch or release work.

## Validation plan

- `ProjectUnitPolicy` constructor rejects undefined `LengthUnit` immediately instead of allowing an invalid object that fails only on later conversion.
- `DrawingUnitResolutionPolicy.BindQuantityUnit` rejects undefined `DrawingUnitResolutionSource` before any metadata mutation.
- Focused smoke coverage verifies both fail-closed paths and verifies invalid binding source leaves the supplied metadata unchanged.
- Re-fetch current `main` before the coherent implementation commit, preserve concurrent changes, then re-read the pushed source/test from current `main`.

## Coordination

Current active neighboring claims observed on `main` cover Build3D preflight selection, semantic documentation canonical-ID editing, updater/licensing, Xref, rectangular rebar, and bulge tessellation. This lane is restricted to `QS3D.Core/Units` enum integrity and the existing unit-resolution smoke.

## Completion condition

Both invalid-enum entry points fail at their public boundary without partial state, regression coverage is present on current `main`, and this claim is marked `COMPLETED` with the exact implementation/final SHA and validation actually performed.

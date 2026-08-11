# Work claim — ProjectUnitPolicy enum integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-project-unit-policy-enum-integrity-20260811-2206`
- Registered: `2026-08-11T22:06:48+07:00`
- Baseline main SHA: `401f49c08d58f6e7596689aba01e8fc5e6b22c5e`
- Priority: evidence-driven remote-safe Core invariant defect found during owner-requested `continue all` review

## Reserved scope

Harden `ProjectUnitPolicy` so an undefined `LengthUnit` enum value cannot be stored in a policy object and survive until a later conversion call. Preserve all currently defined LengthUnit mappings and display-rounding behavior.

## Expected surfaces

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- No BricsCAD V25/native INSUNITS resolution or interactive unit workflow changes.
- No Direct Draw, Plan-to-3D, persistence/QSDB, reporting/export, updater, rebar, documentation, quantity-deduction, Xref or UI work.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

`ProjectUnitPolicy` validates `displayDecimals` but currently assigns `drawingUnit` without validating that the enum value is defined. `ToDrawingUnit` rejects an invalid enum only later when conversion is attempted. This permits an invalid policy object to exist and expose `DrawingUnit`/`RoundForDisplay` successfully, violating the fail-closed unit-policy invariant already enforced by `DrawingUnitResolutionPolicy.SetProjectOverride`, `TryResolve`, and quantity compatibility APIs.

## Validation plan

- Reject undefined `LengthUnit` values in the `ProjectUnitPolicy` constructor.
- Add deterministic Core smoke coverage for constructor rejection.
- Preserve valid conversion/rounding behavior and existing invalid display-decimal rejection.
- Re-fetch current `main` and both reserved files before implementation and again before final push.

## Coordination

This claim owns only the CAD-independent `ProjectUnitPolicy` constructor invariant and one existing Core smoke surface. It does not reserve native unit lifecycle or any active feature lane.

## Completion condition

A coherent implementation + regression batch is pushed to current `main`, the claim is marked `COMPLETED` with exact SHAs and actual validation limits, and ancestry/content are rechecked without running GitHub Actions.

# Work claim — ProjectUnitPolicy enum integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-project-unit-policy-enum-integrity-20260811-2206`
- Registered: `2026-08-11T22:06:48+07:00`
- Completed: `2026-08-11T22:15:11+07:00`
- Baseline main SHA: `401f49c08d58f6e7596689aba01e8fc5e6b22c5e`
- Claim commit: `4a993ce9e9ebaef9d6aad552ac93173210416f6e`
- Implementation commit: `9336938914be2963ad0a780f65ea61c9ecf7dda2`
- Regression-test commit: `f674b91cd6948a11786d3bd3ed88084bd20f7b88`
- Priority: evidence-driven remote-safe Core invariant defect found during owner-requested `continue all` review

## Reserved scope

Harden `ProjectUnitPolicy` so an undefined `LengthUnit` enum value cannot be stored in a policy object and survive until a later conversion call. Preserve all currently defined LengthUnit mappings and display-rounding behavior.

## Implemented

- `ProjectUnitPolicy` now rejects undefined `LengthUnit` values at construction instead of permitting an invalid policy object until first conversion.
- `DrawingUnitResolutionSmoke` now locks constructor rejection for invalid enum values, retains invalid display-decimal rejection, and verifies valid Centimeter conversion/display behavior.

## Changed surfaces

- `src/QS3D.Core/Units/ProjectUnitPolicy.cs`
- `tests/QS3D.Core.SmokeTests/DrawingUnitResolutionSmoke.cs`
- this claim file for close-out metadata

## Excluded scope

- No BricsCAD V25/native INSUNITS resolution or interactive unit workflow changes.
- No Direct Draw, Plan-to-3D, persistence/QSDB, reporting/export, updater, rebar, documentation, quantity-deduction, Xref or UI work.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

Before the fix, `ProjectUnitPolicy` validated `displayDecimals` but assigned `drawingUnit` without validating that the enum value was defined. `ToDrawingUnit` rejected an invalid enum only later when conversion was attempted. This permitted an invalid policy object to exist and expose `DrawingUnit`/`RoundForDisplay` successfully, violating the fail-closed unit-policy invariant already enforced by `DrawingUnitResolutionPolicy.SetProjectOverride`, `TryResolve`, and quantity compatibility APIs.

## Validation performed

- Re-fetched current `main` and both reserved files repeatedly before writes; neither reserved file changed under concurrent work.
- Verified the claim commit remained an ancestor of current `main` with `behind_by=0` before implementation.
- Two coherent Git-object fast-forward attempts were intentionally rejected by GitHub because `main` moved during publication; no force push was used and neither detached commit was attached to `main`.
- Per the repository exception allowing split integration when concurrent movement makes request-level batching unsafe, source and regression were then written through SHA-guarded Contents API commits.
- Confirmed `main` reached regression commit `f674b91cd6948a11786d3bd3ed88084bd20f7b88` immediately after the second write.
- No GitHub Actions workflow was dispatched or re-run. This remote pass does not claim BricsCAD V25 runtime or hosted CI execution.

## Coordination

This claim owned only the CAD-independent `ProjectUnitPolicy` constructor invariant and one existing Core smoke surface. It did not reserve native unit lifecycle or any active feature lane.

## Outcome

Undefined project unit-policy enum values now fail closed at object construction, while defined-unit conversion/display behavior remains covered by deterministic Core smoke source.

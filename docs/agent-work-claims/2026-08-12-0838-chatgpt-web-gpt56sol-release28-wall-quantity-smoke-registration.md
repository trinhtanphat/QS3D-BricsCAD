# Work claim — release #28 Wall quantity opening-bound smoke registration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `15e4c9556ac5c5d137610742a99ec984f64e3fd5`
- Priority: QS3D Cloud V25 Preview Build & Release #28 exposed a real Core smoke coverage gap: `WallQuantityOpeningBoundSmoke.Run()` exists but is not registered by `SmokeTestRegistration.RunAll()`.

## Reserved scope

Register the existing `WallQuantityOpeningBoundSmoke.Run()` in the canonical Core smoke suite so the opening enumeration bound regression actually executes during deterministic Core smoke runs. Preserve the existing test body and production `WallQuantityCalculator` behavior unchanged.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file for close-out

## Excluded scope

- No changes to `WallQuantityOpeningBoundSmoke.cs` unless an independent defect is proven.
- No changes to `WallQuantityCalculator`, the 10,000-opening bound, finite-value semantics or quantity arithmetic.
- No edits to `scripts/preflight-smoke-registration.py`; the existing gate correctly detected this omission.
- No unrelated smoke-suite registrations, V25/V26 runtime work, installer/signing/release changes, or GitHub Actions dispatch.

## Validation plan

- Re-fetch current `main` before the implementation write and verify no new overlapping claim landed.
- Add exactly one `WallQuantityOpeningBoundSmoke.Run();` registration in `SmokeTestRegistration.RunAll()`.
- Read back `SmokeTestRegistration.cs` and confirm exactly one registration is present.
- Preserve `preflight-smoke-registration.py` unchanged as the regression authority that all static `Run()` smoke classes are registered/invoked.
- No licensed BricsCAD runtime PASS will be claimed remotely.

## Coordination

The active Start Center diagnostic lane owns `StartCenterCommands.cs` and `scripts/preflight-start-center.py` only and does not overlap this Core smoke registration. This claim intentionally avoids all other run #28 failures while concurrent agents continue moving `main`.

## Completion condition

The claim-only commit is an ancestor of current `main`, the existing Wall quantity opening-bound smoke is registered exactly once in the canonical suite, the implementation commit is pushed to `main`, and this claim is closed with the actual implementation SHA and validation boundary.

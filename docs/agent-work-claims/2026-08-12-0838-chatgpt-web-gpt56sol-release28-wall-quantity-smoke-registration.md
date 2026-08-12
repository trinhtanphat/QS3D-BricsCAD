# Work claim — release #28 Wall quantity opening-bound smoke registration

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:38:00+07:00`
- Completed: `2026-08-12T08:40:00+07:00`
- Baseline main SHA: `15e4c9556ac5c5d137610742a99ec984f64e3fd5`
- Priority: QS3D Cloud V25 Preview Build & Release #28 exposed a real Core smoke coverage gap: `WallQuantityOpeningBoundSmoke.Run()` existed but was not registered by `SmokeTestRegistration.RunAll()`.

## Reserved scope

Register the existing `WallQuantityOpeningBoundSmoke.Run()` in the canonical Core smoke suite so the opening enumeration bound regression actually executes during deterministic Core smoke runs. Preserve the existing test body and production `WallQuantityCalculator` behavior unchanged.

## Completed implementation

Implementation commit: `9f677318d49973b618327cd02fa2d5ca1f288871` (`test(quantity): register wall opening bound smoke`).

`SmokeTestRegistration.RunAll()` now invokes `WallQuantityOpeningBoundSmoke.Run()` exactly once, immediately with the other quantity smoke coverage. No production Wall quantity code, opening bound, finite-value policy, test body or feature preflight script was changed.

## Validation performed

- Verified the claim-only commit `c3a1ba2074cd499ce764c13b495b44389b5c6929` remained an ancestor of moving `main` before implementation.
- Re-read current `SmokeTestRegistration.cs` before the write and confirmed the registration was still absent.
- Read back current `SmokeTestRegistration.cs` after the implementation and confirmed `WallQuantityOpeningBoundSmoke.Run();` is present in `RunAll()`.
- Re-read `scripts/preflight-smoke-registration.py`; it remains unchanged and continues to fail when any runnable `*Smoke.cs` class is unregistered/unreferenced.

## Validation boundary

- Run #28 remains tied to `fbd5edf8c14c3c7547ac040172450e31add73cff`; this newer fix was not executed by that run.
- No new GitHub Actions workflow was dispatched from this lane.
- No full Core smoke execution, licensed BricsCAD V25 runtime, signing, installer or release PASS is claimed here.
- Other run #28 feature-gate failures remain separate lanes and must be reconciled against current `main` because concurrent agents have already moved the branch substantially beyond the run SHA.

## Excluded scope preserved

- `WallQuantityOpeningBoundSmoke.cs` unchanged.
- `WallQuantityCalculator` unchanged.
- `scripts/preflight-smoke-registration.py` unchanged.
- No unrelated smoke registrations or release/runtime changes.

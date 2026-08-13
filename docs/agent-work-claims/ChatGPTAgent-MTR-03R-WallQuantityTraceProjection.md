# Agent Work Claim

- Owner / Assistant: ChatGPT
- Status: `ACTIVE`
- Task: MTR-03R Wall Quantity trace projection — project canonical `WallQuantityCalculator.Calculate()` wall net-area/net-volume outputs into `MeasurementTrace` without duplicating wall formula math.
- Paths:
  - `src/QS3D.Core/Services/WallQuantityCalculator.cs`
  - `tests/QS3D.Core.SmokeTests/WallQuantityMeasurementTraceSmoke.cs`
  - `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
  - `docs/agent-work-claims/ChatGPTAgent-MTR-03R-WallQuantityTraceProjection.md`
- Base commit: `96c6c960e29a1720790988f46cf55ccaca359a7d`
- Planned first-commit target: `main`
- Overlap check: refreshed recent `main`, visible claim reservations, MTR history, Wall source, and smoke registration. No ACTIVE/BLOCKED collision found on Wall trace projection. Historical MTR-03 explicitly excluded wall takeoff; no MTR-03R or Wall measurement-trace implementation/history was found.
- Notes: claim-only first. Projection must call canonical `Calculate()` exactly once, must not re-enumerate the caller's openings or duplicate Wall formulas, and must trace the canonical clamped opening deduction for both net area and net volume.
- Verification: pending remote source/test/registration readback. No GitHub Actions or managed/native runtime PASS will be claimed without toolchain execution.
- Updated at (UTC): 2026-08-13T10:12:26Z

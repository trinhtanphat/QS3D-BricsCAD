# Curtain path projection overflow hardening

- Status: COMPLETED
- Agent: chatgpt-gpt56sol-curtain-path-projection-20260812-0032
- Timestamp: 2026-08-12T00:32:00+07:00
- Baseline main SHA: 1834d8ed76213726ee70f8f60e1799ca133dc38c
- Priority: P1
- Exact scope: Harden large finite-coordinate arithmetic in `src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs` so mathematically finite point projections do not fail because intermediate squared/dot products overflow. Also harden the frame-piece center-station midpoint if the direct `(overlapStart + overlapEnd) / 2` form is still present in the claimed source snapshot. Add or extend deterministic Core smoke coverage for these cases.
- Expected surfaces: `src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs`; `tests/QS3D.Core.SmokeTests/CurtainPathFramePlannerSmoke.cs`; this claim file.
- Implementation: `de02fb0253f9caeeddf312a76ab93817ac161562` normalizes segment/point vectors before projection ratio arithmetic, clamps finite out-of-segment ratios without constructing overflowing squared/dot intermediates, validates point deltas, and replaces the direct station midpoint with a delta-half midpoint. `a0712e426221edec771e94606de6df5253d02eca` adds deterministic smoke coverage for a `1e200` projection and a `1.6e308` path/frame midpoint.
- Validation: GitHub commit diffs were inspected after publication. Independent arithmetic evaluation of the regression values produced projection ratio `0.5`, finite midpoint `1.3e308`, and a far-point endpoint clamp of `1.0`. No local .NET compiler/test runner or BricsCAD host is available in this environment, so compilation and native/runtime execution are not claimed.
- Handoff: Complete for the claimed remote source-only scope. Native BricsCAD acceptance remains outside this lane.

# Curtain path projection overflow hardening

- Status: ACTIVE
- Agent: chatgpt-gpt56sol-curtain-path-projection-20260812-0032
- Timestamp: 2026-08-12T00:32:00+07:00
- Baseline main SHA: 1834d8ed76213726ee70f8f60e1799ca133dc38c
- Priority: P1
- Exact scope: Harden large finite-coordinate arithmetic in `src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs` so mathematically finite point projections do not fail because intermediate squared/dot products overflow. Also harden the frame-piece center-station midpoint if the direct `(overlapStart + overlapEnd) / 2` form is still present in the claimed source snapshot. Add or extend deterministic Core smoke coverage for these cases.
- Expected surfaces: `src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs`; `tests/QS3D.Core.SmokeTests/CurtainPathFramePlannerSmoke.cs`; this claim file.
- Handoff: Remote source-only lane. No BricsCAD runtime validation or local .NET build is available in this environment. Refresh `main` before source/test commits and do not overwrite concurrent work.

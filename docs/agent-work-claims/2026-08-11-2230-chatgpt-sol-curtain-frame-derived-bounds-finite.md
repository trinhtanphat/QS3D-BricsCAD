# Agent Work Claim

- Status: ACTIVE
- Agent: chatgpt-gpt56sol-curtain-frame-bounds-20260811-2230
- Timestamp: 2026-08-11T22:30:00+07:00
- Baseline `main` SHA: `f6af488dfea115c7d064eff6bdce406db9c83d93`
- Priority: P1 deterministic correctness / fail-closed geometry
- Exact scope: Harden `CurtainFrameOpeningPlanner` and `CurtainOpeningRect` against non-finite derived bounds produced by otherwise finite frame/opening coordinates, extents, or clearance arithmetic; add focused smoke regression coverage.
- Expected surfaces:
  - `src/QS3D.Core/Geometry/CurtainFrameOpeningPlanner.cs`
  - `tests/QS3D.Core.SmokeTests/CurtainFrameOpeningSmoke.cs`
  - this claim file for completion/handoff
- Handoff: Re-read current `main` before implementation, preserve existing behavior for valid finite geometry, fail closed before subtraction when derived frame/opening bounds are non-finite, and do not claim BricsCAD runtime validation from the remote environment.

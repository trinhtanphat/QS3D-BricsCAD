# Agent Work Claim

- Agent: `chatgpt-gpt56-sol-wall-pier-path-underflow`
- Slice: `WallPierPathProfilePlanner positive arithmetic underflow`
- Scope: `Fail closed when multiplication of positive finite path-profile quantities underflows to literal zero, especially footprint area × height for volume, instead of publishing zero derived quantities from positive geometry. Preserve footprint topology, canonical area handling, ordinary Rectangular/Chamfered results, and existing overflow guards.`
- Allowed paths:
  - `src/QS3D.Core/Geometry/WallPierPathProfilePlanner.cs`
  - `tests/QS3D.Core.SmokeTests/WallPierPathProfileUnderflowSmoke.cs`
  - `docs/agent-work-claims/chatgpt-wall-pier-path-underflow-2026-08-12.md`
- Shared files: `none`
- Dependencies: `WallFootprintEngine`, `PolylineMetrics`
- Validation owner: `chatgpt-gpt56-sol-wall-pier-path-underflow`
- Status: `COMPLETED`

## Implemented contract

`WallPierPathProfilePlanner.Multiply` now preserves the existing finite overflow checks and additionally throws when two non-zero finite operands multiply to literal zero. Positive path geometry therefore cannot publish a zero volume/lateral quantity solely because the product fell below the representable range.

## Regression evidence

The focused auto-registered `WallPierPathProfileUnderflowSmoke` covers:

- a positive, representable footprint whose `area × height` would underflow to zero;
- a smaller-scale control whose positive area, volume, and lateral area remain representable and must still succeed.

The regression is isolated from the shared `WallPierProfileSmoke` to avoid concurrent whole-file replacement.

## Landing evidence

- Claim: `09c55330533587078af2cd42d1cff90ef4e87648`
- Source fix: `301d870b1c33e974f60e7fc323138e1099d0fd64`
- Regression: `8a4bcad1afaa9c5df593445e8a06cdfa32310567`
- Source blob readback: `ef08e967ccfb9bc53df5c26a60f4111e8f648c6c`

## Validation boundary

Remote commit diff/readback confirms the source and focused auto-registered smoke are present. The regression commit has no combined CI status attached. No GitHub Actions/full build or licensed BricsCAD runtime was executed for this lane, so no executable runtime PASS is claimed.

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
- Test transfer: `Add a focused auto-registered smoke proving positive finite path geometry cannot return zero volume after multiplication underflow, while a tiny-but-representable control remains valid. Do not dispatch GitHub Actions.`
- Status: `ACTIVE`

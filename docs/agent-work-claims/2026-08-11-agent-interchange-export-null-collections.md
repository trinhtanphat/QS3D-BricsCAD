# Agent Work Claim — Interchange exporter null semantic collections

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: COMPLETED
- Scope: Harden `ProjectInterchangeJsonExporter.Build(ProjectState)` so malformed `Zones`, `Floors`, or `Families` collections containing `null` fail closed with explicit interchange-domain errors instead of throwing `NullReferenceException` from pre-export ordering/serialization.
- Claimed Files:
  - `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`
  - this claim file
- Branch: `agents/interchange-export-null-collections-fix-20260811`
- Started At: 2026-08-11 23:58 +07:00
- Last Updated: 2026-08-12 00:10 +07:00
- Completed At: 2026-08-12 00:10 +07:00
- Implementation: PR #558, squash commit `c7444768b329e7d3bd95b9a5905aecfb217f8c1a`.
- Validation: exact PR diff reviewed (2 files, 27 additions, 0 deletions); focused Core smoke coverage added for null Zone/Floor/Family entries; GitHub Actions were not dispatched per repository policy; no BricsCAD V25/runtime PASS claimed or required for this pure-Core lane.
- Result: malformed semantic collections now fail through explicit `InvalidDataException` export errors before deterministic ordering instead of escaping as `NullReferenceException`.

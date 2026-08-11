# Agent Work Claim — Interchange exporter unique semantic identities

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: ACTIVE
- Scope: Harden `ProjectInterchangeJsonExporter.Build(ProjectState)` so duplicate case-insensitive Zone/Floor/Family/Element semantic IDs are rejected before export instead of emitting a snapshot that downstream validated readers/import planners reject as ambiguous.
- Claimed Files:
  - `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`
  - this claim file
- Branch: `agents/interchange-export-unique-identities-20260811`
- Started At: 2026-08-12 00:14 +07:00
- Last Updated: 2026-08-12 00:14 +07:00
- Local Dependencies: None; pure-Core semantic export contract. No BricsCAD V25 runtime PASS is claimed.
- Validation Plan: focused smoke cases for case-insensitive duplicate Zone/Floor/Family/Element IDs; exact diff review; valid deterministic export remains unchanged; no GitHub Actions dispatch.
- Coordination: do not edit concurrent Start Center or other active lanes; re-sync `main` before source changes.

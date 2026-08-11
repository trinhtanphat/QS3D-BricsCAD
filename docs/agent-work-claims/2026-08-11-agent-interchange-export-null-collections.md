# Agent Work Claim — Interchange exporter null semantic collections

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: ACTIVE
- Scope: Harden `ProjectInterchangeJsonExporter.Build(ProjectState)` so malformed `Zones`, `Floors`, or `Families` collections containing `null` fail closed with explicit interchange-domain errors instead of throwing `NullReferenceException` from pre-export ordering/serialization.
- Claimed Files:
  - `src/QS3D.Core/Export/ProjectInterchangeJsonExporter.cs`
  - `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`
  - this claim file
- Branch: `agents/interchange-export-null-collections-20260811`
- Started At: 2026-08-11 23:58 +07:00
- Last Updated: 2026-08-12 00:06 +07:00
- Local Dependencies: None for this pure-Core export contract. No BricsCAD V25 runtime PASS is claimed.
- Validation Plan: add focused smoke cases for null Zone/Floor/Family entries, review exact diff, preserve valid deterministic export ordering and existing element semantic-reference validation. GitHub Actions remain undispatched per repository policy.
- Coordination: Re-sync `main` and open PRs before source changes; do not edit concurrent Start Center, Quantity Setup, Grid naming, Wall Junction, Auto Layout, or other active claims.

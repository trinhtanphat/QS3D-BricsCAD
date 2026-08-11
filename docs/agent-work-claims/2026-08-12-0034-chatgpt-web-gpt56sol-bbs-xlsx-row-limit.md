# Agent Work Claim — BBS XLSX worksheet row limit

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: ACTIVE
- Scope: Harden `XlsxRebarScheduleExporter.Export(...)` so BBS exports fail closed before file mutation when row count would exceed the XLSX worksheet row limit, instead of building an oversized/invalid worksheet in memory.
- Claimed Files:
  - `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
  - `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs`
  - this claim file
- Branch: `agents/bbs-xlsx-row-limit-20260812`
- Started At: 2026-08-12 00:34 +07:00
- Last Updated: 2026-08-12 00:34 +07:00
- Local Dependencies: None; pure-Core XLSX format boundary. No BricsCAD V25 runtime PASS is claimed.
- Validation Plan: add a focused smoke case using a synthetic `IReadOnlyList<RebarScheduleRow>` with an oversized `Count` whose indexer throws, proving rejection happens before row enumeration/materialization or target-file replacement; preserve ordinary BBS XLSX output.
- Coordination: re-sync `main` and active claims before source edits; do not touch unrelated Rebar layout/adapter/UI lanes.

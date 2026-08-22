# Agent Work Claim — BBS XLSX worksheet row limit

- Agent: ChatGPT remote agent
- Owner: OpenAI ChatGPT
- Status: COMPLETE
- Scope: Harden `XlsxRebarScheduleExporter.Export(...)` so BBS exports fail closed before file mutation when row count would exceed the XLSX worksheet row limit, instead of building an oversized/invalid worksheet in memory.
- Claimed Files:
  - `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
  - `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs`
  - this claim file
- Branch: `agents/bbs-xlsx-row-limit-fix-20260812`
- Started At: 2026-08-12 00:34 +07:00
- Completed At: 2026-08-12 00:40 +07:00
- Last Updated: 2026-08-12 00:40 +07:00
- Local Dependencies: None; pure-Core XLSX format boundary. No BricsCAD V25 runtime PASS is claimed.
- Validation: focused smoke coverage uses a synthetic oversized `IReadOnlyList<RebarScheduleRow>` whose indexer/enumerator throw, proving rejection happens from `Count` before row access or target-file replacement; ordinary BBS XLSX behavior remains unchanged.
- Result: PR #584 was rebased against concurrent `main` changes and squash-merged as `38fb4bb143a6d7b704d9c85e590a7e7e8a6f4d86`.
- Coordination: claimed exporter/test files are released for other agents after this completion record merges.

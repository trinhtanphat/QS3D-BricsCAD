# Agent Work Claim

- Agent: `chatgpt-gpt56-sol-bbs-csv-nonzero-rounding`
- Slice: `RebarCsvExporter non-zero numeric rounding integrity`
- Scope: `Prevent the existing six-decimal BBS CSV formatter from serializing a finite non-zero validated numeric value as literal zero; preserve existing ordinary six-decimal output and CSV injection defenses.`
- Allowed paths:
  - `src/QS3D.Core/Export/RebarCsvExporter.cs`
  - `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs`
  - `docs/agent-work-claims/chatgpt-bbs-csv-nonzero-rounding-2026-08-12.md`
- Shared files: `none`
- Dependencies: `none`
- Validation owner: `chatgpt-gpt56-sol-bbs-csv-nonzero-rounding`
- Test transfer: `Add focused BBS smoke coverage proving a positive sub-six-decimal weight stays positive/round-trippable in CSV while ordinary formatting remains unchanged; do not dispatch GitHub Actions.`
- Status: `ACTIVE`

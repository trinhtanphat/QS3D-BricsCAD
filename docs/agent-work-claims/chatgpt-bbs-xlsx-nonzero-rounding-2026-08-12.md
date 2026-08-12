# Agent Work Claim

- Agent: `chatgpt-gpt56-sol-bbs-xlsx-nonzero-rounding`
- Slice: `XlsxRebarScheduleExporter non-zero numeric rounding integrity`
- Scope: `Prevent the existing eight-decimal BBS XLSX numeric formatter from serializing a finite non-zero value as literal zero; preserve existing ordinary eight-decimal worksheet numeric output, row validation, styles, and atomic publication.`
- Allowed paths:
  - `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
  - `tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs`
  - `docs/agent-work-claims/chatgpt-bbs-xlsx-nonzero-rounding-2026-08-12.md`
- Shared files: `none`
- Dependencies: `none`
- Validation owner: `chatgpt-gpt56-sol-bbs-xlsx-nonzero-rounding`
- Test transfer: `Add focused BBS smoke coverage proving a finite positive sub-eight-decimal value remains non-zero/round-trippable in sheet XML while ordinary formatting remains unchanged; do not dispatch GitHub Actions.`
- Status: `ACTIVE`

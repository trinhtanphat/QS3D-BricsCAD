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
- Status: `COMPLETED`

## Implemented contract

`AppendNumber` retains the existing invariant `0.########` representation for ordinary worksheet values. If a finite non-zero value would be formatted as literal `0`, it falls back to invariant round-trip representation so XLSX publication does not silently erase a validated numeric value.

## Regression evidence

`BbsRegressionSmoke` now exports a row containing `4e-9` across the BBS numeric data columns, reads `xl/worksheets/sheet1.xml`, parses cells G2:L2 and requires the original non-zero value to round-trip. Diameter/quantity controls E2/F2 remain `16` and `1` semantically.

## Landing evidence

- Claim: `db628cf2f0a2277f20003df98a0cc7d5dc52c414`
- Source fix: `c63f7fd7d3f73cb35472ec5b0915c16379eae963`
- Regression: `d760080383a9913b26937ba57829eeac52b88ac1`
- Source blob readback: `d72b00d6d7224c629a6248715aae5eeb7689fe9f`
- Regression blob readback: `433c5a628aab2cddf97b97ad514d1b15b995ad7e`

## Validation boundary

Remote commit diff/readback confirms the source and focused auto-registered smoke are present. No GitHub Actions/full build or licensed BricsCAD runtime was executed for this lane, so no executable runtime PASS is claimed.

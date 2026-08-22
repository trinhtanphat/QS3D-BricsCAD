# Work claim — BBS XLSX nonnegative numeric boundary

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bbs-xlsx-nonnegative-20260812-1103`
- Registered: `2026-08-12T11:03:00+07:00`
- Baseline main SHA: `b6e9aa70bf433e8bfd560ff99ee18539d8250dae`
- Priority: P1 export data integrity

## Confirmed defect

`RebarScheduleBuilder` produces positive bar quantities and finite nonnegative physical dimensions, lengths, weights and waste percentages, but the public `XlsxRebarScheduleExporter.Export(...)` boundary currently validates those row numbers only for NaN/Infinity. A caller-created or subsequently mutated `RebarScheduleRow` can therefore serialize negative diameter, quantity, cutting/total length, unit/net/total weight or waste percentage into an otherwise valid BBS workbook.

## Reserved scope

- `src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs`
- one focused new Core smoke under `tests/QS3D.Core.SmokeTests/` for negative BBS XLSX values
- this claim file for close-out

## Contract

- Reject negative BBS worksheet numeric quantities before path resolution, directory creation or temp-file creation.
- Require `Quantity` to be strictly positive, matching `RebarScheduleBuilder` output semantics.
- Require `DiameterMm` to be strictly positive, matching parsed rebar notation / unit-weight semantics.
- Require cutting length, total length, unit weight, net weight, waste percentage and total weight to remain finite and nonnegative.
- Preserve zero where it is a valid lower bound for derived weight/length/waste fields and preserve all existing row/text/snapshot/XML/package/atomic-replace behavior.

## Exclusions

- No changes to `RebarScheduleBuilder`, notation parsing, rebar math/layout planners, fabrication qualification, CAD commands/UI or other XLSX exporters.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Validation plan

Add deterministic Core smoke coverage proving each negative/invalid physical row value is rejected before any export directory/file is created, plus a valid control row. Re-read the integrated source and smoke on current `main` after commit.

## Coordination

The BBS row-snapshot claim is completed at `5818bcec8d0331f2a28ec43de8cb0da976815d4c`; no BBS XLSX nonnegative claim/fix was found in current commit history. The active ED2 snapshot lane owns `XlsxQuantityExporter.cs`, not this BBS exporter.

## Completion condition

Source guard and focused smoke are integrated on current `main`, read back after merge/write, and this claim is marked `COMPLETED` with exact commit evidence and validation limits.

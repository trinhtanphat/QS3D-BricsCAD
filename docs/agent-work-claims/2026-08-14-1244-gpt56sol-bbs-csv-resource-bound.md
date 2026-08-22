# Work claim — BBS CSV resource bound

- Status: `COMPLETED`
- Agent: `gpt56sol-bbs-csv-resource-bound-20260814-1244`
- Registered: `2026-08-14T12:44:00+07:00`
- Completed: `2026-08-14T12:48:00+07:00`
- Workstream: `REB / export-resource integrity`
- Priority: `P1`
- Baseline: `e6230682ee8ef0dea0abe44ffd35a5a0cfec9087`
- Claim commit: `4b7354221e5fd26e03c0b3d4d4d2765f6152eb63`
- Claim scope refinement: `263ffb0ffb3fe01cac4b2fad92d00427356072fc`
- Source: `a5bc3a175d98598dd37b3b7aba74710b391a8913`
- Regression: `1a57170f2f50e781b3431cc4df463d4ea977f28d`

## Confirmed defect

`RebarCsvExporter.ToCsv(IEnumerable<RebarScheduleRow>)` accepted arbitrary/lazy public input and appended every row to one `StringBuilder` with no row ceiling. This left the BBS CSV public boundary open to unbounded enumeration/memory growth. The same Core export subsystem already fail-closes an arbitrary/lazy rebar-procurement CSV at a finite public row bound, and the BBS XLSX exporter likewise has an explicit worksheet row ceiling; BBS CSV was the remaining unbounded projection.

## Completed change

- Added a 10,000-row ceiling at the public BBS CSV renderer boundary, aligned with the existing Core rebar-procurement CSV resource ceiling.
- The 10,001st yielded row is rejected with `ArgumentOutOfRangeException` before row validation or serialization, so a lazy/infinite source cannot grow the `StringBuilder` beyond the configured row bound.
- Preserved CSV schema/order, existing numeric validation and formatting, spreadsheet-formula hardening, and atomic destination replacement.
- Added a dedicated self-registering `BbsCsvResourceBoundSmoke` instead of editing the broad shared `BbsRegressionSmoke.cs`.

## Reserved scope

- `src/QS3D.Core/Export/RebarCsvExporter.cs`
- `tests/QS3D.Core.SmokeTests/BbsCsvResourceBoundSmoke.cs`
- `docs/agent-work-claims/2026-08-14-1244-gpt56sol-bbs-csv-resource-bound.md`

## Regression coverage

Focused smoke source pins three behaviors:

1. exactly 10,000 valid rows remain accepted;
2. row 10,001 fails closed with `ArgumentOutOfRangeException`;
3. an infinite counting enumerable stops at exactly 10,001 `MoveNext` calls.

## Excluded scope

- no rebar quantity/fabrication math or schedule semantics;
- no procurement CSV/report/optimizer changes;
- no BBS XLSX behavior changes;
- no V25/V26/native builders or Level-placement claim files;
- no MAP/IFC/persistence changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation

Remote GitHub readback on live `main` SHA `b162ce6a9254f9c5861bd8bcb76e0b798ecee989` confirmed the source blob contains the 10,000-row boundary and the focused smoke blob contains all three regression cases. GitHub compare confirmed live `main` is ahead of source commit `a5bc3a175d98598dd37b3b7aba74710b391a8913` with that commit as merge-base, and ahead of regression commit `1a57170f2f50e781b3431cc4df463d4ea977f28d` with that commit as merge-base.

Executable .NET/native validation was **not run** in this environment: there is no local `gh`, no GitHub DNS checkout and no executable .NET/native runner available here. No GitHub Actions were dispatched and no BricsCAD/native PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, bounded source fix, focused regression source, live-main ancestry/readback verification and explicit validation boundary are present on `main`.

# Work claim — REB-03 procurement CSV resource bound

- Status: `ACTIVE`
- Agent: `gpt56sol-rebar-procurement-csv-bound-20260814-0823`
- Registered: `2026-08-14T08:23:00+07:00`
- Baseline main SHA: `38a9e6669986554171b83a3d7fed033aeb9c4bb4`
- Priority: `P1` export/resource integrity on the completed REB-03A projection.

## Confirmed defect

`RebarProcurementReportBuilder.Build()` fails closed above 10,000 canonical optimisation results, but the public `RebarProcurementCsvExporter.ToCsv(IEnumerable<RebarProcurementSummary>)` enumerates arbitrary input without any ceiling while accumulating the entire output in a `StringBuilder`. A caller can therefore bypass the canonical report bound by supplying a repeated/lazy enumerable of existing public summary rows, producing unbounded enumeration/memory growth in the export projection.

## Reserved scope

- `src/QS3D.Core/Export/RebarProcurementCsvExporter.cs`
- new `tests/QS3D.Core.SmokeTests/RebarProcurementCsvBoundSmoke.cs`
- this claim file only

## Intended change

Apply the same 10,000-row ceiling at the CSV public boundary, checking the count before serialising the next row so an unbounded/lazy source fails closed after a bounded number of MoveNext calls. Preserve CSV schema, row formatting/order, formula hardening, numeric validation and atomic file behavior. Do not add duplicate-identity/business validation in the renderer and do not change canonical optimisation/report math.

## Excluded scope

- no `RebarProcurementReport.cs`, cutting optimiser/demand/math, BBS/XLSX, MAP/IFC, Reporting/BQ, persistence, V25/V26/native or current active claim edits;
- no GitHub Actions/native qualification.

## Validation plan

Add a focused self-registering smoke that obtains a real canonical summary, proves exactly 10,000 repeated rows remain accepted, proves the 10,001st row fails with `ArgumentOutOfRangeException`, and uses a counting lazy enumerable to pin bounded enumeration. Re-fetch remote diff/lineage and report executable validation only if actually available.

## Coordination

REB-03A claim is `COMPLETED`. Current live changes since the audit baseline touch quantity-report family-category integrity and V25 only; MAP-03C and IFC-02D remain active but are outside this scope.

## Completion condition

Claim-first reservation, minimal public CSV resource bound, focused regression, remote readback/ancestry and explicit validation boundary are present on `main`, then this claim is closed `COMPLETED`.

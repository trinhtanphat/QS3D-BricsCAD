# Work claim — REB-03A rebar procurement/waste report projection

- Status: `COMPLETED`
- Agent: `gpt56sol-rebar-procurement-report-20260814-0830`
- Baseline main SHA: `d5ab24f28cb4c30034eacec32055ed0e4ab58363`
- Priority: `REB-03`; dependencies REB-01A and REB-02A completed first.

## Confirmed gap

QS3D already had BBS CSV/XLSX export, but no consumer of the canonical REB-01 procurement quantities / REB-02 cutting result. Procurement stock count, kerf and off-cut therefore had no canonical report projection. REB-03 requires report/output to consume the canonical optimisation result rather than maintain independent cutting math.

## Implemented scope

- `src/QS3D.Core/Rebar/RebarProcurementReport.cs`
  - immutable `RebarProcurementSummary` projected directly from `RebarCuttingOptimizationResult`;
  - carries algorithm id, group/grade/diameter/stock identity, required cut count/length, allowance, canonical kerf/off-cut/procurement quantities;
  - derives waste length and length-to-weight projection with the existing canonical `RebarWeight` service;
  - exposes demand/procurement/waste weight and waste percent without recomputing stock allocation;
  - deterministic report builder with case-insensitive unique group identity, stable ordering and an explicit 10,000-result bound.
- `src/QS3D.Core/Export/RebarProcurementCsvExporter.cs`
  - CSV serialises the canonical summary only; no packing, kerf, off-cut or procurement recomputation exists in the exporter;
  - round-trip numeric formatting, spreadsheet-formula text hardening and atomic file replace follow existing export conventions.
- `tests/QS3D.Core.SmokeTests/RebarProcurementReportSmoke.cs`
  - canonical optimiser-to-report quantity projection;
  - deterministic group ordering;
  - duplicate group identity rejection;
  - CSV uses projected quantities and protects formula-leading group/grade text;
  - null-row rejection.
- `tests/QS3D.Core.SmokeTests/RebarCuttingOptimizerSmoke.cs`
  - invokes the REB-03 focused smoke from the already registered REB pipeline, avoiding another edit to the hot shared smoke registry.

## Coordination / commits

- Claim-first: `b8f64592c608374d891338e5dce798dfb9d43299`.
- Canonical procurement report projection: `7cda3d92fd5b65ceb9bee865586f4fd3c79595a9`.
- CSV projection: `70c3e25f62c9144d82f20cfaa28d5fee8c9a1d7f`.
- Focused report/CSV smoke: `0631730d1e2ab7e9b358a5859decafac26a56122`.
- Smoke-chain registration through existing REB-02 smoke: `604b8662be656e5a87fd8b43769526b1debec7b5`.

## Excluded scope

Existing BBS CSV/XLSX schema changes, XLSX procurement workbook, persistence/schema, project-level automatic demand aggregation from all semantic rebar elements, CAD/BricsCAD host UI, remnant inventory purchasing and pricing are not part of this bounded lane.

## Validation actually executed

- Refreshed `main` and searched for REB-03 overlap before claiming; no competing REB-03 commit/claim was found.
- Claim-only commit was published before source/test writes.
- Verified the existing BBS/export layer before implementation and kept it unchanged; the new exporter consumes `RebarProcurementSummary` rather than schedule rows or raw cut requirements.
- Read-back/self-review confirmed no cutting allocation or kerf/off-cut computation is duplicated in the CSV exporter; only canonical summary formatting is performed there.
- Live `main` at the final source/test checkpoint was exactly `604b8662be656e5a87fd8b43769526b1debec7b5`.
- GitHub exposes zero combined status checks for that SHA; no Actions were dispatched.
- The execution environment exposes no `dotnet`, `csc` or `mcs`, so no executable managed build/smoke is reported as PASS. No licensed BricsCAD/native validation was executed.

## Completion condition

Satisfied for this bounded REB-03 Core/export-static lane: canonical cutting results now have a deterministic procurement/waste report and CSV path, report/export does not own independent cutting math, regression coverage is in the existing smoke chain, the claim is closed, and unavailable runtime/native gates remain explicitly unclaimed.

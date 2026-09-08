# Commercial QS Product Surface Design

## Context

`main` already contains the Commercial QS modeless workspace for Variation, IPC and Final Account and, via #6126, Core authorities for Tender/Procurement and CVR/Forecast. The remaining product gap is to expose those authorities in the existing workspace and converge export/audit behavior without creating parallel arithmetic or storage systems.

## Product boundary

The feature remains a BricsCAD V25/V26 Windows x64 plugin surface. V26 reuses the V25 UI assembly/surface already referenced by its host project; this design does not introduce a separate V26 commercial window. Hosted CI may prove source, Core and compile behavior, but it does not constitute licensed BricsCAD runtime evidence.

## Architecture

### Existing authorities retained

- `CommercialVariationRegister`, `InterimPaymentCertificateService`, `FinalAccountService` remain post-contract settlement authorities.
- `TenderProcurementService` remains procurement evaluation/award authority and reuses `TenderEvaluationService` for bid arithmetic and ranking.
- `CommercialCostControlService` remains CVR/forecast arithmetic and lifecycle authority.
- `AuditTrail` attached to canonical `ProjectState` remains the only audit store.
- `ProjectContextCoordinator` remains the BricsCAD host bridge for canonical project state.

No UI or exporter may recompute monetary values owned by these Core services.

### Commercial workspace

Extend `CommercialQsWindow` rather than adding another window. The existing Variation, IPC and Final Account tabs remain intact. Add:

1. **Tender / Procurement** tab
   - Inputs: package id/description/currency/package revision, tender requirements, mandatory compliance requirements, bids, compliance responses.
   - The adapter builds a closed `TenderProcurementPackage`, calls `TenderProcurementService.Evaluate`, binds deterministic commercial/compliance rows, and shows `RecommendedBidId`.
   - Award input accepts award id, selected bid id and fresh award revision; `TenderProcurementService.Award` is the only award gate.

2. **CVR / Forecast** tab
   - Inputs: period id/currency/revision, original budget, approved variation net change, committed cost, actual cost, accrued cost, earned value and forecast cost to complete.
   - The adapter builds `CommercialControlPeriod` and calls `CommercialCostControlService.Evaluate`.
   - Lifecycle buttons freeze, reopen and revise forecast only through `CommercialCostControlService`, always requiring a fresh revision. Reopen additionally requires a canonical reason.
   - The UI displays revised budget, cost-to-date, committed exposure, forecast final cost, forecast variance, earned value and CVR margin from Core result properties.

All parse failures and Core rejections are fail-closed and surfaced in the existing status strip.

## Reporting/export convergence

Create `QS3D.Core.Export.CommercialQsWorkbook` as a deterministic, write-only XLSX projection of validated commercial results. The UI supplies already-validated Core objects and never constructs workbook rows itself.

Workbook schema version: `QS3D_COMMERCIAL_QS_V1`.

Sheets, in fixed order:

1. `META` — schema version and generated sections.
2. `VARIATIONS` — variation identity, description, proposed/approved amounts, status, currency and revision.
3. `IPC` — certificate totals and currency.
4. `FINAL_ACCOUNT` — final contract value, amount due, recovery due, unreleased retention and currency.
5. `TENDER` — package identity/revision, each bid's evaluated total/rank/completeness/compliance, recommendation and optional award identity/revision.
6. `CVR` — period identity/revision/status and all `CommercialCostControlResult` metrics.

The exporter uses invariant decimal formatting, deterministic row ordering, safe XML escaping, atomic replacement and XLSX package validation following existing Core export patterns. A missing section produces a header-only sheet; export requires at least one validated commercial section overall.

## Audit convergence

Create `QS3D.Core.Commercial.CommercialWorkflowAudit` as the canonical formatter/recorder for commercial audit actions. It accepts an existing `AuditTrail` and validated Core results; it does not own persistence.

Canonical actions:

- `commercial.variation.validated`
- `commercial.ipc.created`
- `commercial.final-account.reconciled`
- `commercial.tender.evaluated`
- `commercial.tender.awarded`
- `commercial.cvr.evaluated`
- `commercial.cvr.frozen`
- `commercial.cvr.reopened`
- `commercial.cvr.forecast-revised`
- `commercial.report.exported`

Each event binds the relevant entity id as `ElementId`; revision-bearing actions include revision identity in deterministic detail and use a canonical correlation id derived from the entity/revision when applicable. The BricsCAD adapter obtains the canonical project through `ProjectContextCoordinator.GetOrCreate(document)` and records through `AuditTrail.ForProject(project)`. Recording marks the project dirty via existing AuditTrail behavior, but the commercial window does **not** auto-save the sidecar. Normal QS3D save remains the persistence boundary.

## Source and regression protection

- Core smoke tests prove workbook generation is deterministic, contains all fixed sheets, preserves revision/provenance values and uses service-produced monetary values.
- Core smoke tests prove audit actions, entity ids and revision identities are canonical and deterministic.
- `preflight-commercial-qs-workspace.py` requires Tender/CVR tabs, Core service calls, Core workbook export and audit authority calls, and forbids adapter-side commercial arithmetic/report row generation.
- Existing full smoke runner registration remains the single deterministic Core test entry point.
- V25 compile validation must build the WPF surface; V26 remains covered by its existing reuse of V25 UI source/assembly contracts.

## Native evidence boundary

Merging this source carrier requires fresh/current protected `preflight` + `core` SUCCESS. Licensed BricsCAD V25/V26 interactive load/open/calculate/export/save/reopen evidence remains a separate qualification lane. This carrier must not label hosted or compile evidence as `LOCAL_PASS`.

## Acceptance

The design is complete when the same Commercial QS window exposes Variation, IPC, Final Account, Tender and CVR; export is owned by Core XLSX projection; commercial actions flow into the existing AuditTrail without auto-save; deterministic tests/source guards pass; and the carrier merges through the protected PR path with fresh exact-head evidence.
# Commercial QS Product Surface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the existing Commercial QS workspace with Tender/Procurement, CVR/Forecast, deterministic Core XLSX reporting and canonical project audit events.

**Architecture:** Keep all monetary/lifecycle authority in `QS3D.Core`. The BricsCAD window parses inputs, calls existing services, binds returned results, delegates workbook generation to a new Core exporter, and delegates audit formatting/recording to a new Core commercial audit helper using the existing `AuditTrail`. V26 continues to reuse the V25 UI surface.

**Tech Stack:** C#/.NET 8 Core, C# BricsCAD V25 host/WPF, deterministic smoke executable, Python source guards, OpenXML-compatible XLSX ZIP/XML writer using existing repository primitives.

**Spec:** `docs/superpowers/specs/2026-09-08-commercial-qs-product-surface-design.md`

## Global Constraints

- `QS3D.Core` remains the arithmetic/domain authority; UI/export layers must not duplicate tender, settlement or CVR arithmetic.
- Extend `CommercialQsWindow`; do not create a second commercial product surface.
- Use the existing `ProjectContextCoordinator` + `AuditTrail`; do not create a second audit store.
- Do not auto-save the QS3D sidecar after audit writes.
- No paid service or paid cloud dependency.
- Hosted CI/compile evidence must not be represented as licensed BricsCAD V25/V26 runtime PASS.
- All writes stay on canonical branch `agent/gpt56sol-20260908-commercial-ui-reporting/issue-6131-commercial-product-surface`; merge only through the protected PR path.

---

### Task 1: RED contracts for reporting and audit

**Files:**
- Create: `tests/QS3D.Core.SmokeTests/CommercialQsWorkbookSmoke.cs`
- Create: `tests/QS3D.Core.SmokeTests/CommercialWorkflowAuditSmoke.cs`
- Modify: `tests/QS3D.Core.SmokeTests/SmokeTestEntryPoint.cs`
- Modify: `scripts/preflight-commercial-qs-workspace.py`

**Interfaces:**
- Consumes: existing `CommercialVariationRegister`, `InterimPaymentCertificate`, `FinalAccountResult`, `TenderProcurementEvaluation`, `TenderAwardDecision`, `CommercialCostControlResult`, `AuditTrail`.
- Produces test expectations for `CommercialQsWorkbook.Export(...)` and `CommercialWorkflowAudit.Record*` methods.

- [ ] **Step 1: Write the failing workbook smoke**

Create a smoke that builds validated Variation/IPC/Final Account/Tender/CVR objects using existing Core services, calls:

```csharp
CommercialQsWorkbook.Export(path, new CommercialQsWorkbookSnapshot(
    variationRegister,
    ipc,
    finalAccount,
    tenderPackage,
    tenderEvaluation,
    tenderAward,
    cvrResult));
```

Assert that two exports from identical inputs are byte-identical, that the package contains fixed sheets `META`, `VARIATIONS`, `IPC`, `FINAL_ACCOUNT`, `TENDER`, `CVR`, and that revision ids and service-produced totals occur in worksheet XML.

- [ ] **Step 2: Write the failing audit smoke**

Create an in-memory `ProjectState`, `AuditTrail.ForProject(project)`, validated tender/CVR objects, then call concrete APIs:

```csharp
CommercialWorkflowAudit.RecordTenderEvaluation(audit, evaluation);
CommercialWorkflowAudit.RecordTenderAward(audit, award);
CommercialWorkflowAudit.RecordCvrEvaluation(audit, cvrResult);
CommercialWorkflowAudit.RecordReportExport(audit, "QS3D_COMMERCIAL_QS_V1", "commercial.xlsx");
```

Assert canonical actions, entity ids, revision ids in details, canonical non-empty correlation ids for revision-bearing events, and stable event ordering.

- [ ] **Step 3: Register both smokes in `SmokeTestEntryPoint.cs`**

Add:

```csharp
CommercialQsWorkbookSmoke.Run();
CommercialWorkflowAuditSmoke.Run();
```

- [ ] **Step 4: Strengthen the source guard to require the future surface**

Require `TENDER`, `CVR / FORECAST`, `TenderProcurementService`, `CommercialCostControlService`, `CommercialQsWorkbook.Export`, `CommercialWorkflowAudit`, and remove/forbid `WriteCommercialCsv`/UI CSV row construction. Also forbid assignments to `EvaluatedTotal`, `ForecastFinalCost`, `ForecastVariance`, `CvrMargin`, settlement totals and other service-owned monetary outputs in UI code.

- [ ] **Step 5: Verify RED**

Run:

```text
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
python scripts/preflight-commercial-qs-workspace.py
```

Expected: smoke build/test fails because `CommercialQsWorkbook` / `CommercialWorkflowAudit` do not exist; source guard fails because Tender/CVR/Core workbook/audit UI tokens are absent and legacy UI CSV export remains.

- [ ] **Step 6: Commit the RED tests/guard**

Commit message: `test(qs): define commercial product surface contracts #6131`.

### Task 2: Commercial audit authority

**Files:**
- Create: `src/QS3D.Core/Commercial/CommercialWorkflowAudit.cs`
- Test: `tests/QS3D.Core.SmokeTests/CommercialWorkflowAuditSmoke.cs`

**Interfaces:**
- Consumes: `QS3D.Core.Audit.AuditTrail` and validated commercial result objects.
- Produces: static `RecordVariation`, `RecordIpc`, `RecordFinalAccount`, `RecordTenderEvaluation`, `RecordTenderAward`, `RecordCvrEvaluation`, `RecordCvrTransition`, `RecordReportExport` methods.

- [ ] **Step 1: Implement canonical action/detail/correlation formatting**

Use invariant formatting and entity/revision identity only; never recalculate monetary results. Revision-bearing correlation ids use deterministic tokens such as `commercial:<entity-id>:<revision-id>` after existing canonical validation.

- [ ] **Step 2: Run the audit smoke**

Run the full smoke executable and confirm `CommercialWorkflowAuditSmoke` passes while workbook smoke remains the only new RED failure.

- [ ] **Step 3: Commit**

Commit message: `feat(qs): add canonical commercial workflow audit #6131`.

### Task 3: Deterministic Commercial XLSX exporter

**Files:**
- Create: `src/QS3D.Core/Export/CommercialQsWorkbook.cs`
- Test: `tests/QS3D.Core.SmokeTests/CommercialQsWorkbookSmoke.cs`

**Interfaces:**
- Produces `CommercialQsWorkbookSnapshot` and `CommercialQsWorkbook.Export(string path, CommercialQsWorkbookSnapshot snapshot)`.
- Workbook schema is `QS3D_COMMERCIAL_QS_V1`; fixed sheet order is `META`, `VARIATIONS`, `IPC`, `FINAL_ACCOUNT`, `TENDER`, `CVR`.

- [ ] **Step 1: Implement snapshot validation**

Require at least one validated section. Enforce coherent currency/revision identities through the source object invariants; do not create substitute arithmetic.

- [ ] **Step 2: Implement deterministic workbook projection**

Follow existing repository XLSX patterns: invariant decimal strings, sorted variation/bid rows, inline strings, XML escaping, fixed worksheet order, atomic temp-file replacement, `XlsxPackageValidator.Validate` before publication.

- [ ] **Step 3: Verify GREEN for both new Core smokes**

Run the full smoke executable twice. Confirm all tests pass on both runs and generated workbook bytes are stable inside the smoke.

- [ ] **Step 4: Commit**

Commit message: `feat(qs): add deterministic commercial workbook #6131`.

### Task 4: Tender and CVR product UI

**Files:**
- Modify: `src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml`
- Modify: `src/QS3D.BricsCAD.V25/UI/CommercialQsWindow.xaml.cs`
- Modify: `src/QS3D.BricsCAD.V25/CommercialQsCommands.cs`
- Modify: `scripts/preflight-commercial-qs-workspace.py`

**Interfaces:**
- Consumes Core `TenderProcurementService`, `CommercialCostControlService`, `CommercialQsWorkbook`, `CommercialWorkflowAudit`.
- Keeps existing Variation/IPC/Final Account handlers and fields functional.

- [ ] **Step 1: Add Tender / Procurement tab**

Add structured input areas for package metadata, requirements, compliance requirements, bids, compliance responses, plus evaluation/award actions and a read-only result grid/summary. Parse pipe-delimited rows consistently with the existing workspace input style. Call only `TenderProcurementService.Evaluate` and `.Award` for evaluation/award outputs.

- [ ] **Step 2: Add CVR / Forecast tab**

Add period/budget/cost/earned-value/forecast fields, evaluate/freeze/reopen/revise controls, revision/reopen-reason inputs and read-only metric summary. Call only `CommercialCostControlService` lifecycle/evaluation methods.

- [ ] **Step 3: Replace UI CSV generation with Core workbook export**

Change the header action to `EXPORT XLSX`; save as `.xlsx`; build `CommercialQsWorkbookSnapshot` from the already validated in-memory results and call `CommercialQsWorkbook.Export`. Delete `WriteCommercialCsv`, `Csv`, `EscapeCsv` and UI report-row assembly.

- [ ] **Step 4: Record canonical audit events**

For each successful validated action, obtain the canonical project using `ProjectContextCoordinator.GetOrCreate(_document)`, then record through `AuditTrail.ForProject(project)` + `CommercialWorkflowAudit`. Do not call `ProjectContextCoordinator.Save` from these handlers.

- [ ] **Step 5: Update command status text**

Report `Variation • IPC • Final Account • Tender • CVR • XLSX` when opening the workspace.

- [ ] **Step 6: Run source guard and Core smoke**

Run:

```text
python scripts/preflight-commercial-qs-workspace.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Expected: PASS for both.

- [ ] **Step 7: Commit**

Commit message: `feat(qs): complete commercial Tender CVR workspace #6131`.

### Task 5: Full candidate verification and protected integration

**Files:** all reserved paths above only.

- [ ] **Step 1: Run focused verification again on exact branch head**

```text
python scripts/preflight-commercial-qs-workspace.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
```

All commands must exit 0.

- [ ] **Step 2: Push/open canonical PR**

PR targets `main`, references `Fixes #6131`, repeats Reservation-v2 metadata and explicitly states that licensed V25/V26 runtime evidence is not claimed.

- [ ] **Step 3: Diagnose/fix protected CI on the same carrier**

Require exact-head protected `preflight` + `core` SUCCESS. Fix any current-lane failures only in reserved paths. Never weaken guards to manufacture green.

- [ ] **Step 4: Reconcile main drift non-force if required**

If `main` advances, verify changed paths do not semantically overlap, build a normal merge/reconciliation commit preserving main changes plus this carrier, fast-forward the branch without force, and require fresh exact-head protected checks.

- [ ] **Step 5: Merge with optimistic head binding**

When current, collision-clean, mergeable and green, merge the same PR through protected GitHub PR merge using the exact expected head SHA.

- [ ] **Step 6: Verify merged main and release reservation**

Refresh `main`, confirm the merge commit contains the expected carrier tree, close #6131 completed, and delete the merged branch when practical. Report `MERGED_MAIN` with PR, protected run and resulting main SHA. Keep licensed-native V25/V26 qualification explicitly pending unless separately evidenced.
# BIM3D-QS customer Excel + reverse-trace plan

Issue / Lane-Key: #3296 / `issue-3296`
Canonical branch: `agent/chatgpt-gpt56sol/customer-excel-trace-3296`
Baseline main: `ca8f22416d63c841a4a7d19ddc34f3538d4a8d80`
Owner/session: `chatgpt / gpt56sol-20260820-qs-excel-trace`

## 1. Session requirements consolidated

### [ĐÃ XÁC NHẬN]

- The customer-facing product stays inside `QS3D-BricsCAD`; BricsCAD owns the live DWG database/viewports and QS3D owns semantic model, quantity/reporting, review and export workflows.
- The QS golden path is BIM3D-first: author/capture model -> verify 3D -> calculate quantity -> review/locate -> export Excel -> trace results back to CAD.
- Preserve the existing semantic/provenance identity instead of inventing a second authority: QS3D Element ID + CAD Handle + Drawing Fingerprint.
- Keep the quantity tab internal ID `QS3D_QTY`, but present the compact customer-facing tab as `QS3D`.
- Provide visible `Xuất Excel` and `Excel → CAD` actions.
- Customer workbook layout for this lane: `DGKL`, `COP_PHA`, `CHI_TIET`, `TRACE_MODEL`.
- `DGKL` is grouped quantity output; `COP_PHA` is a formwork-oriented grouped view; `CHI_TIET` is one semantic element per row; `TRACE_MODEL` is the explicit business-row-to-model provenance map.
- Reverse locate must support one-element detail rows and multi-element aggregate rows, then select/zoom the resolved live CAD objects.
- Wrong drawing, stale/missing semantic elements, provenance drift, partial Handle resolution, malformed/ambiguous trace rows and unsupported workbook states fail closed before PICKFIRST is changed.
- Unsupported quantity metrics remain blank; a genuine measured zero remains numeric zero.
- Existing ED2 export/locate compatibility remains available; this lane adds the customer workbook rather than replacing the canonical quantity engine.

### [SUY LUẬN]

- A dedicated business workbook command is preferable to changing ED2 semantics because existing source guards and external users may rely on `QS3DED2` / `QS3DEXCELLOCATE`.
- Aggregate reverse locate should revalidate the current project from Element IDs first and compare the complete canonical Handle set against workbook provenance before changing selection.
- `TRACE_MODEL` should use deterministic trace keys derived from business-sheet identity so the business sheets do not need to expose raw Handle/fingerprint columns to normal users.

### [CHƯA RÕ]

- Licensed BricsCAD V25 interactive visual acceptance (native Ribbon text/layout, Open/Save dialogs, PICKFIRST highlight/zoom behavior) cannot be proven by remote/source CI and remains under #72 / LOCAL_ONLY.
- Any future clash/duplicate workbook sheets (`CLASHES`, `DUPLICATES`, `RULES`) are a separate coordination lane; this quantity-export lane does not invent clash persistence.

### [ĐỀ XUẤT]

- Keep `QS3DED2` as the compatibility/export-evidence path and add `QS3DEXCEL` as the customer workbook path.
- Keep `QS3DEXCELLOCATE` for ED2/legacy compatibility and add `QS3DEXCELTRACE` for the four-sheet customer workbook; the visible `Excel → CAD` button points to the customer command.
- Reuse `ProjectQuantityReportBuilder.Detail/Group` and evidence flags exactly; the workbook layer only projects and validates output.

## 2. Current-main truth

Already present and reused:

- `ProjectQuantityReportBuilder.Detail/Group` for canonical report projections.
- `QS3DED2` scoped export with detached regeneration and live-Handle validation.
- `XlsxQuantityExporter.ExportEd2` with `CHI_TIET` / `TONG_HOP` parity checks and evidence-aware blank cells.
- `XlsxHandleReader` and `ExcelLocateResolutionService` with strict modern ED2 identity/fingerprint/Handle validation.
- `QS3DEXCELLOCATE` detail locate + zoom.
- Compact BLT-style quantity Ribbon augmentation.

Proven remaining gap for #3296:

1. no customer workbook with `DGKL`, `COP_PHA`, `CHI_TIET`, `TRACE_MODEL`;
2. no hardened reader for row-level `TRACE_MODEL` provenance;
3. existing modern locator is intentionally one-element ED2 `CHI_TIET` only, so grouped business rows cannot locate all underlying elements;
4. compact quantity Ribbon still labels the export as `.blte2` and does not expose the customer reverse-trace action;
5. internal tab title is still `ĐỊNH LƯỢNG`, not the requested visible `QS3D`.

## 3. Implementation design

### Core workbook exporter

Add `QsCustomerWorkbookExporter` in `QS3D.Core.Export`.

- Input: canonical `detailRows` + `summaryRows`.
- Validate one-element detail identity, aggregate scope parity, finite/non-negative evidence-aware values, non-empty Handle provenance and one Drawing Fingerprint.
- Write the four required worksheets atomically.
- Preserve evidence semantics by omitting unsupported numeric cells.
- Emit deterministic `TRACE_KEY` values and one `TRACE_MODEL` row for every visible business row.

### Core trace reader

Add `QsCustomerWorkbookTraceReader`.

- Accept only `DGKL`, `COP_PHA`, `CHI_TIET` as business-row sources.
- Resolve the workbook/worksheet relationships rather than assuming physical sheet numbers.
- Read literal `TRACE_KEY`, then find exactly one matching `TRACE_MODEL` entry.
- Return sheet, row, Element IDs, Handle set and Drawing Fingerprint.
- Reject missing/duplicate/ambiguous/formula-backed critical cells and malformed identity tokens.
- Require exactly one Element ID for `CHI_TIET`; permit many for aggregate sheets.

### BricsCAD V25 commands

Add focused customer Excel commands.

- `QS3DEXCEL`: reuse current project, detached regeneration and canonical Detail/Group projection; support Selection/Floor/Zone/All using the existing ED2 selection helper; block stale Handles; save customer `.xlsx`.
- `QS3DEXCELTRACE`: choose workbook + business sheet + row; resolve customer trace; revalidate Drawing Fingerprint, every semantic Element ID, exact canonical project Handle set and complete live CAD resolution; only then set PICKFIRST and run `QS3DZOOMSELECTED`.

Extend `ExcelLocateResolutionService` with a multi-element customer-trace entry point while preserving the existing strict ED2 one-element method.

### Ribbon

- Internal ID remains `QS3D_QTY`.
- Visible tab title becomes `QS3D`.
- Compact export button becomes `Xuất Excel` -> `QS3DEXCEL`.
- Add `Excel → CAD` -> `QS3DEXCELTRACE`.
- Existing ED2 command paths remain registered and source-guarded for compatibility.

## 4. Regression / source guards

- Add `CustomerWorkbookTraceSmoke` to the canonical Core smoke harness.
- Assert workbook contains exactly the four expected business sheets.
- Assert detail trace resolves one element and aggregate trace resolves all underlying IDs/Handles.
- Assert unsupported evidence leaves the corresponding quantity cell absent while true zero remains numeric.
- Assert malformed/unknown business sheet/trace row fails closed.
- Add `scripts/preflight-customer-excel-trace.py` to guard command names, sheet contract, trace identity and Ribbon routing.
- Run existing ED2 round-trip guard unchanged to prove compatibility.

## 5. CI / integration sequence

1. Commit planning on the canonical task branch.
2. Implement Core exporter/reader + smoke.
3. Implement V25 command/resolver + Ribbon routing + focused preflight.
4. Push the canonical branch and observe automatic exact-head branch CI.
5. Remediate any source/preflight/Core/V25 failure on the same branch.
6. Refresh current `main`; reconcile safely if required.
7. Open one PR with `Lane-Key: issue-3296` metadata.
8. Require current protected `preflight` + `core` success and mergeability/freshness.
9. Merge the same PR under standing owner authorization.
10. Refresh and record the landed `main` SHA; update #3296 to `MERGED_MAIN`.

## 6. Explicit non-goals

- no second quantity/calculation engine;
- no generic Workspace refactor owned by #3113;
- no 4D/5D schedule/cost/claims expansion;
- no IFC/RVT expansion;
- no unrelated NETLOAD/startup-log performance lane;
- no claim of licensed BricsCAD V25 runtime PASS from remote CI;
- no clash/duplicate issue persistence in this lane.

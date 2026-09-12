# Benchmark parity acceptance matrix

This matrix records source-safe regression evidence for the six benchmark groups used by the QS3D benchmark-parity program. It is an integration/acceptance index, not a claim that hosted CI replaces licensed BricsCAD or customer-environment qualification.

| Benchmark group | Source-safe acceptance evidence on `main` | Automatically exercised boundary | Remaining / separate boundary |
| --- | --- | --- | --- |
| RIB CostX | calibrated 2D takeoff, PDF/raster sheet ingestion, takeoff-package revision UX, source-handle revision provenance, workbook live-link contracts, quantity/cost/export regression | `BenchmarkParitySuiteSmoke`, including `Qs2DTakeoffWorkflowSmoke`, `QsTakeoffPackageUxSmoke`, and `QsLiveWorkbookApiSmoke`; #6549/#6550 source-handle provenance is integrated on current `main` | licensed-host CAD interaction and full CostX desktop UX equivalence remain separate; hosted smoke is source-safe evidence only |
| Cubicost | concrete/formwork calculation and reviewed-quantity downstream workflow into existing estimate/tender/procurement boundaries | `ConcreteAndFormwork`, `QsCubicostQuantBimSmoke`, and separately registered `QsCubicostDownstreamSmoke` | customer-model/native-host qualification and proprietary desktop UX remain separate |
| Autodesk Takeoff / Forma | calibrated 2D measurement, PDF/raster sheet ingestion, revision/package workflow and source traceability | `CalibratedTwoDimensionalTakeoff`, `Qs2DTakeoffWorkflowSmoke`, `QsTakeoffPackageUxSmoke` | browser/cloud/vendor-service equivalence is not claimed by host-neutral Core smoke |
| Solibri | strict IFC QA gating and guarded downstream execution before quantity workflows | `QaGateBlocksInvalidModel`, `QsQaGate2Smoke`, `IfcWorkbench` | complete proprietary ruleset coverage and native Solibri review/UI equivalence require separate evidence if pursued |
| Trimble | supplier governance, commitments, PO/delivery tracking, field progress, actual-vs-commitment control and deterministic project-controls/ERP interchange | `ConstructionLifecycle`, `QsTrimbleConstructionLifecycleSmoke` | external ERP/vendor SDK execution remains an outer-adapter concern; hosted CI is not external-system certification |
| QuantBIM | host-neutral IFC/QTO workflow, traceable takeoff, IFC STEP ingestion, renderer-neutral scene contract and viewport-host orchestration | `QsCubicostQuantBimSmoke`; dedicated `QsQuantBimSceneSmoke` and `QsQuantBimViewportHostSmoke` module initializers execute when the smoke assembly loads | concrete standalone Windows renderer/shell, native drawing engine, packaging and standalone runtime qualification belong to sibling `QS3D-CAD` under `docs/PRODUCT-BOUNDARY.md`; they are not a shipping requirement of `QS3D-BricsCAD` |

## Live Workbook and Integration API acceptance

`QsLiveWorkbookApiSmoke` is deliberately invoked by the consolidated `BenchmarkParitySuiteSmoke`. This prevents the existing Live Workbook v2 / API v1 test from becoming dead regression coverage merely because the file itself has no module initializer.

The smoke covers the following source-safe contracts:

- BIM-element and drawing-handle workbook bindings with revision identity;
- deterministic dependency cascades and recalculation ordering;
- fresh/refreshed/stale/missing/conflict/error states and publish-blocking behavior;
- trace evidence for source and dependent bindings;
- collision-safe workbook/cell/source identities;
- API authentication and scope authorization;
- versioned API route coverage, JSON DTO responses and ETag/conditional-read behavior;
- workbook-refresh authorization and workbook identity mismatch/conflict handling;
- project snapshot data that includes quantity, BOQ, estimate, classification, QA, revision/snapshot/diff and tender/procurement surfaces.

The older `WorkbookLiveLinkEngine` and `QsIntegrationRouteCatalog` checks remain in the suite as backward-compatibility regression. The v2/API v1 smoke supplements them; it does not replace or fork those contracts.

## Acceptance rules

1. Hosted/Core smoke may prove deterministic source-safe behavior only. It must never be described as licensed BricsCAD `LOCAL_PASS` evidence.
2. A benchmark item is not considered fully closed solely because this matrix has a green source-safe row. Native host, vendor service, UI, file-format, customer-model or external-system qualification remains separate whenever the current repository actually owns that environment boundary.
3. Product-family scope is authoritative: `QS3D-BricsCAD` remains a BricsCAD V25/V26 plugin; standalone Windows/CAD shell, renderer and native desktop packaging belong to sibling `QS3D-CAD`. Do not create a standalone `QS3D.exe` carrier in this repository merely to chase benchmark UI parity.
4. Reservation-v2 ownership must be respected. The CostX source-handle provenance carrier #6549/#6550 is integrated; active Cubicost numeric/identity hardening #6551/#6554 owns its separate production/test/guard paths.
5. A smoke file that is neither registered, called by a registered suite, nor a module initializer is not acceptance evidence.
6. New benchmark regressions should extend the canonical implementation/test surface rather than introducing parallel engines for the same domain.
7. Exact-head Shared CI and Hybrid Coordinator must both be terminal green on a branch that is current-main-safe before merge.

## Coordinator status after CostX provenance integration

Trimble construction lifecycle/project-controls is source-safe complete. CostX source-handle provenance #6549/#6550 is integrated. This #6552 carrier closes the cross-cutting regression hole in which Live Workbook v2 / Integration API v1 smoke existed but was not executed by the consolidated benchmark suite.

For this repository, QuantBIM acceptance stops deliberately at reusable host-neutral IFC/QTO, evidence, scene and viewport-orchestration contracts. `docs/PRODUCT-BOUNDARY.md` explicitly assigns the standalone desktop executable, concrete standalone renderer/native drawing host and its runtime evidence to sibling `QS3D-CAD`; absence of a standalone desktop project under this repository's `src/` is therefore not a `QS3D-BricsCAD` parity defect.

The six-group program should remain evidence-driven: after #6552 lands, continue from current `main`, respect active sibling carriers such as #6551/#6554, and select the next unresolved benchmark gap only when it belongs to the `QS3D-BricsCAD`/shared-Core boundary defined by current product governance.

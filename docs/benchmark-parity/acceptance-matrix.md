# Benchmark parity acceptance matrix

This matrix records source-safe regression evidence for the six benchmark groups used by the QS3D benchmark-parity program. It is an integration/acceptance index, not a claim that hosted CI replaces licensed BricsCAD or customer-environment qualification.

| Benchmark group | Source-safe acceptance evidence on `main` | Automatically exercised boundary | Remaining / separate boundary |
| --- | --- | --- | --- |
| RIB CostX | calibrated 2D takeoff, takeoff-package revision UX, workbook live-link contracts, quantity/cost/export regression | `BenchmarkParitySuiteSmoke`, including `Qs2DTakeoffWorkflowSmoke`, `QsTakeoffPackageUxSmoke`, and `QsLiveWorkbookApiSmoke` | native PDF/raster/CAD interaction and licensed-host UX remain separate; active source-handle provenance carrier #6549/#6550 owns its reserved paths |
| Cubicost | concrete/formwork calculation and downstream quantity workflow | `ConcreteAndFormwork` plus `QsCubicostQuantBimSmoke` | customer-model/native-host qualification remains separate |
| Autodesk Takeoff / Forma | calibrated 2D measurement, revision/package workflow and source traceability | `CalibratedTwoDimensionalTakeoff`, `Qs2DTakeoffWorkflowSmoke`, `QsTakeoffPackageUxSmoke` | browser/cloud/vendor-service equivalence is not claimed by host-neutral Core smoke |
| Solibri | strict IFC QA gating and IFC quantity workbench behavior | `QaGateBlocksInvalidModel`, `QsQaGate2Smoke`, `IfcWorkbench` | native Solibri interoperability/UI equivalence requires explicit external qualification if pursued |
| Trimble | construction lifecycle and deterministic project-controls/ERP interchange | `ConstructionLifecycle`, `QsTrimbleConstructionLifecycleSmoke` | external ERP/vendor SDK execution remains an outer-adapter concern |
| QuantBIM | IFC/QTO workflow, standalone scene/viewport carriers and deterministic selection/navigation contracts | `QsCubicostQuantBimSmoke`; dedicated scene/viewport module-initializer smokes run independently where registered | rendered/native UI fidelity and licensed BricsCAD-host behavior remain separate |

## P0 Live Workbook and Integration API acceptance

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
2. A benchmark item is not considered fully closed solely because this matrix has a green row. Native host, vendor service, UI, file-format, customer-model or external-system qualification remains separate whenever the feature depends on those environments.
3. Active Reservation-v2 ownership must be respected. In particular, #6549/#6550 currently owns the CostX/Autodesk-style takeoff source-handle revision-provenance paths; this acceptance carrier does not modify them.
4. New benchmark regressions should extend the canonical implementation/test surface rather than introducing parallel engines for the same domain.

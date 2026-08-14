# BLT3D research → QS3D implementation truth — 2026-08-14

> **Purpose:** current-source overlay for the dated BLT3D/Gemini research archive and the 2026-08-12 advisory workstream queue.  
> **Authority:** current QS3D source/tests/product boundary win over this overlay; the research archive remains provenance, not verified competitor truth.  
> **Native boundary:** `SOURCE_IMPLEMENTED` never means licensed BricsCAD V25/V26 runtime qualification.

## Status vocabulary

- `SOURCE_IMPLEMENTED` — a bounded Core/source contract matching the research lane exists in current source.
- `FOUNDATION_PRESENT` — the core building block exists, but broad category migration/UX/runtime coverage is not proven complete.
- `PARTIAL_OR_OPEN` — useful source capability exists, but the lane is intentionally broader than current proof.
- `LOCAL_ONLY` — remaining acceptance requires a licensed/native BricsCAD host or representative private/runtime evidence.
- `ENGINEERING_REQUIRED` — a governing standard/business rule/engineering input must be approved before implementation can be truthful.
- `OUT_OF_SCOPE_OR_SEPARATE_PRODUCT` — not a default implementation lane for `QS3D-BricsCAD`.
- `ARCHIVE_ONLY` — research/provenance material; not a code TODO by itself.

## Verified foundational source map

The following mapping was checked against current repository source during the 2026-08-14 audit. It exists to stop agents from recreating already-established domains merely because the dated queue still describes them as future work.

### MTR — explainable measurement

Status: **FOUNDATION_PRESENT / core contracts implemented**.

Direct evidence:

- `src/QS3D.Core/Measurement/MeasurementTrace.cs` — canonical trace, facts, adjustments, rule identity/version tokens, units, rounding policy, warnings/assumptions and deterministic canonical serialization.
- `src/QS3D.Core/Measurement/MeasurementTraceInspector.cs` — read-only “why this quantity?” projection model.

Interpretation:

- MTR-01 is `SOURCE_IMPLEMENTED` at the Core contract layer.
- MTR-02 has a real rule identity/version foundation inside the trace contract; this does **not** prove every historical/current quantity service has been migrated to versioned trace provenance.
- MTR-03 remains a category-by-category coverage question. Do not create a second quantity engine; verify each current quantity path before claiming a missing migration.
- MTR-04 has a Core inspector foundation; host/UI/native completeness still depends on current source and runtime evidence.
- MTR-05 remains continuous hardening, not a finite “implement once” ticket.

### REV — measurement snapshot / revision quantity delta

Status: **SOURCE_IMPLEMENTED at the Core foundation described by REV-01/02/03**.

Direct evidence:

- `src/QS3D.Core/Measurement/MeasurementSnapshot.cs`
- `src/QS3D.Core/Measurement/MeasurementSnapshotDelta.cs`
- `src/QS3D.Core/Measurement/MeasurementSnapshotDeltaReason.cs`

These files provide frozen deterministic trace snapshots, added/removed/changed/unchanged quantity deltas, and evidence-bounded delta reason classification. This closes the original “create the foundational Core contracts” reading of REV-01/02/03. Broader persistence/UI/revision-product workflows must still be checked separately before being called complete.

### MAP — measurement/work-item mapping + coverage

Status: **SOURCE_IMPLEMENTED at the Core mapping/coverage foundation**.

Direct evidence:

- `src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs`
- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverage.cs`
- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageMatrix.cs`
- `src/QS3D.Core/Mapping/MeasurementWorkItemCoverageReport.cs`

Interpretation:

- MAP-01 mapping identity/resolution foundation exists.
- MAP-02 deterministic coverage evaluation exists.
- MAP-03 report projection exists at the Core/report-model layer.
- This does not imply every semantic category, customer classification system, or native UI has complete mapping content.

### CST — rates / estimate / revision cost

Status: **SOURCE_IMPLEMENTED at the bounded Core commercial foundation**.

Direct evidence:

- `src/QS3D.Core/Cost/RateBook.cs`
- `src/QS3D.Core/Cost/EstimateLine.cs`
- `src/QS3D.Core/Cost/EstimateLineFreshness.cs`
- `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs`
- `src/QS3D.Core/Cost/FrozenEstimateProjection.cs`

Interpretation:

- CST-01 through CST-04 have source-side Core counterparts for rate identity/resolution, frozen estimate lines, freshness, revision cost impact and frozen projection.
- Company-specific rate data, contractual rules, taxes, escalation, procurement or ERP policy are **not** implied by these contracts and must not be guessed.

## Broader research/workstream classification

### NAT — native semantic editing

Status: **PARTIAL_OR_OPEN + LOCAL_ONLY for final host acceptance**.

The repository already contains source-edit/reconcile and category-specific authoring/edit safeguards, but native MOVE/ROTATE/STRETCH/grip/Undo/document-switch/save-reopen semantics are host-sensitive. Open product/local issues explicitly preserve this boundary. A remote agent may fix a proven source defect, but must not mark native behavior PASS without exact-host evidence.

### PERF — large-model/native qualification

Status: **FOUNDATION_PRESENT + LOCAL_ONLY for representative host performance**.

Managed harness/evidence infrastructure exists, while representative licensed BricsCAD project/runtime timing and memory remain runtime evidence rather than a static-source completion claim.

### QSC — QS rule checker

Status: **PARTIAL_OR_OPEN**.

QS3D already has substantial Semantic Health / Release Check infrastructure. The research idea must build on that system rather than introduce a parallel validator. A dedicated declarative QS commercial/measurement rule profile is not considered globally complete by this audit merely because health diagnostics exist.

### TKO — 2D/3D takeoff convergence

Status: **PARTIAL_OR_OPEN**.

Quick Takeoff, B4D recognition and semantic capture paths exist. The broad research goal of one fully converged count/length/perimeter/area/work-item provenance model across every 2D/3D workflow is not declared complete here. Verify one primitive/workflow at a time.

### IFC / BCF

Status: **PARTIAL_OR_OPEN**.

The repository has mature semantic interchange infrastructure, but the dated research workstream specifically asks for IFC GlobalId/classification/QTO round-trip and BCF review/provenance acceptance. Do not equate generic semantic interchange with complete IFC/BCF product qualification unless the exact contracts and tests exist on current `main`.

### REB — rebar/BBS specialist depth

Status: **PARTIAL_OR_OPEN; ENGINEERING_REQUIRED for code/standard-specific detailing**.

QS3D already contains broad Rebar 3D/BBS source capability. Stock cutting/waste/procurement optimisation and lap/splice/anchorage rules require separate verified contracts. Numeric structural/detailing behavior must not be invented from competitor research.

### MEP

Status: **PARTIAL_OR_OPEN / product-expansion decision required per sub-domain**.

Do not build a full routing/fabrication platform merely because it appears in research. Any plugin-side MEP QS lane must start from one explicit measurable system and reuse semantic identity, trace, mapping, revision and reporting foundations.

### CIV / earthwork

Status: **PARTIAL_OR_OPEN**.

Earthwork capability exists in QS3D, but the research list covers deeper existing/design surfaces, cut/fill, trench/backfill/swell/shrink/haul and revision traceability. Verify current earthwork source before opening any narrow missing lane; do not create an independent civil quantity engine.

### EXT — cloud/field/AI/ERP/ESG/DfMA/FM/city-scale systems

Status: **OUT_OF_SCOPE_OR_SEPARATE_PRODUCT by default**.

These research ideas are explicitly scope-control markers. They are not “unfinished QS3D-BricsCAD code”. An owner-approved product/API boundary is required before implementation, and a separate repo/service is often the correct destination.

## Research archive cleanup conclusion

The archive is considered **clean as research provenance**, not “fully implemented feature-by-feature”. The correct interpretation is:

1. Preserve the archived Gemini/public-source material and its deduplicated prompt/response accounting.
2. Do not delete historical research merely because a corresponding QS3D source foundation now exists.
3. Do not read the dated workstream queue as a live unimplemented checklist without this status overlay and a current-source check.
4. Do not implement every future concept. `LOCAL_ONLY`, `ENGINEERING_REQUIRED`, and `OUT_OF_SCOPE_OR_SEPARATE_PRODUCT` are valid final classifications until their prerequisites change.
5. For any remaining `PARTIAL_OR_OPEN` item: verify current source → verify product need → inspect current `ACTIVE`/`BLOCKED` claims → publish one narrow claim → implement regression-backed work.

## 2026-08-14 audit disposition

- Research archive/index: **retained**; provenance is useful and duplicate content should not be reintroduced.
- Foundational MTR/MAP/REV/CST queue wording: **superseded by current source for the Core foundations listed above**.
- Native/runtime items: **not falsely closed**.
- Standards/business-policy items: **not guessed**.
- External-product concepts: **not pulled into this plugin**.
- Any future agent treating the dated research queue as current truth must first reconcile against this map and current source.

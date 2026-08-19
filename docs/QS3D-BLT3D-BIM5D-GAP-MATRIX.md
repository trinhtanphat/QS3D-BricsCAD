# QS3D vs BLT3D / 5D BIM current-main gap matrix

**Issue / Lane-Key:** #3103 / `issue-3103`  
**Audit baseline:** `main@fd2b27973f1ceea3d54842f211262e0234c78ef9`  
**Date:** 2026-08-19  
**Scope:** current-main implementation audit plus clean-room comparison against #3098, #3099, #3101 and #3102. This document does not implement the discovered gaps.

## 1. Audit rules

This matrix deliberately separates three kinds of evidence:

1. **Current-main implementation truth** — only source that exists on the audit baseline can establish `already present`, `partial` or `missing` implementation.
2. **Reference/workflow evidence** — #3098 and #3099 describe BLT3D / 5D-BIM public workflow evidence. They are benchmarks, not proof of QS3D implementation.
3. **Architecture/domain guidance** — #3101 and #3102 define the normalized 3D -> 4D -> 5D progression and interoperability boundaries. Both explicitly say that their documents do not themselves implement the described adapters/domain model.

Status vocabulary:

- **ALREADY PRESENT** — current `main` contains a concrete implementation that substantially satisfies the capability.
- **PARTIAL** — useful implementation exists, but an important relationship, lifecycle, persistence, mapping, or end-to-end workflow is absent.
- **MISSING** — no current-main implementation was found for the minimum capability gate.
- **NOT DESIRED / OUT OF PRODUCT BOUNDARY** — the capability conflicts with the locked repository shipping/host boundary or with the conservative support wording in the current architecture.

No row below turns competitor marketing into a QS3D requirement.

## 2. Executive conclusion

QS3D is **not starting from a basic takeoff-only core**. Current `main` already contains strong foundations for model-linked quantity provenance, grouped/detail quantity reports, BLT-compatible quantity-calculation presets, versioned cost-code/rate resolution, quantity-to-rate estimating, commercial adjustments, revision cost impact, progress-claim valuation, generic audit events, IFC round-trip quantity evidence and BCF/export infrastructure.

The dominant functional gap versus the normalized 5D workflow is **4D orchestration**, not quantity arithmetic or basic pricing:

```text
current QS3D strength
model/source identity -> quantity trace -> cost code/rate -> estimate/revision cost

missing normalized bridge
quantity/model scope -X-> activity/schedule revision -X-> time-aware cost/progress allocation
```

Therefore current QS3D can truthfully describe a substantial **model-based QTO + quantity-cost / estimating foundation**. It should not describe the whole product as an **integrated 4D/5D chain** until explicit activity identity, quantity-to-activity allocation, schedule revision provenance and cross-domain propagation exist.

## 3. Prioritized capability / gap matrix

| Priority | Capability | Current-main status | Current-main evidence | BLT3D / 5D reference signal | Gap / decision |
|---|---|---|---|---|---|
| P0 | Source/model-linked quantity identity | **ALREADY PRESENT** | `src/QS3D.Core/Measurement/MeasurementTrace.cs` stores semantic identity, source identity, quantity key, input facts, adjustments, unit, rule identity/version, warnings and assumptions. `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` carries element IDs, source handles and drawing fingerprint into quantity rows. | #3098 says model-based QTO needs stable identity/provenance; #3099 documents BLT3D model/result review as vendor workflow evidence. | Keep this as the source-of-truth layer. Do not let cost or schedule overwrite geometric/quantity identity. |
| P0 | Deterministic quantity calculation / BLT-familiar calculation workflow | **ALREADY PRESENT** | `src/QS3D.Core/Reporting/QuantityCalculationBltCompatibilityPreset.cs` defines BLT-compatible area/length/count/automatic presets and calculation/report defaults. Reporting source includes deduction gates, rule sets, diagnostics and project quantity builders. | #3099 establishes public BLT3D `Settings -> Calculate -> Results -> Recalculate` workflow, not private implementation details. | No generic “build a quantity engine” gap. Future work should target missing disciplines/rules individually, with fixtures. |
| P0 | Quantity report traceability | **ALREADY PRESENT** | `ProjectQuantityReportBuilder` supports grouped/detail report rows, validates identity, preserves `ElementIds`, source handles and drawing fingerprint, and rejects stale selection state. | #3098/#3099 emphasize result review, detail reports and trace back to source/model. | Extend existing provenance rather than create a second report identity model. |
| P0 | Cost codes + unit rates + effective dates + currency | **ALREADY PRESENT** | `src/QS3D.Core/Cost/RateBook.cs` implements `CostCode`, rate item ID/version, unit, currency, effective UTC and deterministic as-of resolution. | #3098 identifies cost codes/rates as a 5D foundation; #3099 only proves BLT unit-price/result-value wording, not a comparable versioned rate model. | Current QS3D is stronger than the minimal public BLT evidence here. No first-class-rate-book gap. |
| P0 | Model quantity -> priced estimate binding | **ALREADY PRESENT** | `src/QS3D.Core/Cost/EstimateLine.cs` resolves an exact `MeasurementTrace` from a snapshot, resolves the rate by cost code/unit/currency/date, separates measured quantity from explicit commercial adjustment, and calculates final amount. | #3101 says 5D cost must link back to versioned quantity scope; #3098 calls for quantity x rate traceability. | Keep estimate creation anchored to immutable measurement/rate evidence. |
| P0 | 4D activity/task identity | **MISSING** | Audit found no source-level `ActivityId`/activity contract in current `main`; current cost workflows do not provide a first-class activity/task identity. | #3101 defines the truthful 4D gate as first-class activity/task IDs plus schedule time and explicit quantity-to-activity mapping. | Add a small host-neutral activity-reference contract; do not build a full scheduler in the same lane. |
| P0 | Quantity/model -> activity allocation | **MISSING** | No current-main source establishes a quantity-fact/element-to-activity allocation relationship. | #3101 defines `QuantityActivityAllocation` as the bridge that makes 4D explicit. | Highest-value structural gap. One future lane should add allocation identity, quantity/unit and provenance only. |
| P0 | Schedule revision / external schedule provenance | **MISSING** | No current-main source found for schedule revision identity, external activity source, planned start/finish mapping or orphaned schedule diagnostics. | #3101 requires schedule revision provenance; authoritative scheduling may remain external. | Add read-only/lightweight external schedule metadata and diagnostics before any scheduling logic. |
| P1 | Progress quantity state | **PARTIAL** | `src/QS3D.Core/Cost/AdvancedCostManagement.cs` contains `ProgressContractItem`, `ProgressClaimLine` and `ProgressClaimService`; it distinguishes previous-certified and claimed-this-period quantities and caps certification by contract quantity. | #3101 separates measured/installed/accepted/claimed/certified states and recommends dated progress snapshots. | Current progress exists inside claim valuation, but there is no explicit dated activity-linked progress snapshot baseline. |
| P1 | Progress claim valuation | **PARTIAL** | `ProgressClaimService` calculates certified quantity, rejected quantity, remaining quantity, certified value, retention and net certified value. | #3101 requires historical/datestamped states for a complete progress/claim workflow; current #3105 UX documentation on `main` also treats progress claims as a user workflow. | Core valuation exists. Missing pieces should be scoped to claim identity/date/history/persistence/evidence rather than reimplementing arithmetic. |
| P1 | Change propagation: quantity -> cost | **ALREADY PRESENT** | `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs` compares matched estimate revisions and separates measured-quantity, commercial-adjustment, unit-rate, quantity-driven and rate-driven cost deltas with exact reconciliation. | #3098/#3101 require revision delta and cost impact. | Strong current implementation for quantity/cost impact. |
| P1 | Change propagation: quantity -> activity -> cost/progress | **PARTIAL** | Quantity-to-cost revision impact exists, but the 4D bridge is absent, so affected activity allocations/schedule revisions cannot be derived from model/quantity change. | #3101 explicitly defines the desired propagation chain old/new quantity -> affected activities -> affected cost allocations -> variance. | After the P0 activity/allocation contracts land, add an impact projection service; do not silently mutate historical baselines. |
| P1 | Generic audit trail | **ALREADY PRESENT** | `src/QS3D.Core/Audit/AuditTrail.cs` records UTC action, element ID, detail, actor and correlation ID, validates persisted history and returns snapshots. Measurement trace provides calculation-level provenance separately. | #3098/#3101 require auditable quantity/cost relationships. | Preserve both generic event audit and deterministic measurement/cost evidence; they solve different audit questions. |
| P1 | End-to-end 4D/5D auditability | **PARTIAL** | Quantity trace, audit events and cost-revision evidence exist; activity/schedule identity and allocation evidence do not. | #3101 requires common stable identities across quantity, activity and cost for `integrated 4D/5D`. | Add provenance fields to each new 4D mapping instead of adding a parallel audit subsystem. |
| P1 | Quantity/report output | **ALREADY PRESENT** | `src/QS3D.Core/Reporting/` contains grouped/detail quantity report builders, BLT-compatible quantity calculation presets/rules/diagnostics and discipline schedules. `src/QS3D.Core/Export/` contains multiple XLSX/export paths. | #3099 documents BLT summary/detail/floor reporting and Excel/PDF/Word vendor workflow. | Reporting foundation is present; exact competitor format parity is neither required nor established. |
| P1 | Unified quantity + schedule + cost/progress report | **PARTIAL** | Quantity and cost/progress services exist independently; no explicit schedule allocation exists to support a single provenance chain. | #3098/#3101 describe report -> cost -> activity -> quantity -> source navigation for mature 5D. | Add a projection/report only after P0 mapping contracts exist. |
| P1 | IFC/openBIM quantity evidence | **PARTIAL** | `src/QS3D.Core/Export/` includes `IfcRoundTripExchangeResult.cs`, `IfcRoundTripProjection.cs` and `IfcRoundTripQuantityEvidence.cs`, alongside BCF exchange infrastructure. | #3102 defines IFC as the preferred open semantic interchange path and explicitly distinguishes architecture from shipping support. | There is concrete IFC round-trip evidence infrastructure, but do not infer a complete IFC importer/schema coverage matrix from these classes alone. |
| P1 | DWG host interoperability | **ALREADY PRESENT within repository boundary** | The locked `docs/PRODUCT-BOUNDARY.md` defines BricsCAD as owner of native DWG database/document/selection/transactions and QS3D as the hosted plugin. #3102 formalizes active DWG as native-host integration. | BLT3D publicly advertises DWG input; QS3D has a different host architecture. | Keep BricsCAD-native DWG handling in host adapters; do not duplicate a standalone DWG engine in Core. |
| P2 | DXF / external DWG normalized import adapter | **PARTIAL** | #3102 now specifies the adapter boundary on `main`, but that document explicitly says architecture is not proof that every adapter is shipping. Current audit did not identify one complete normalized Core contract proving all loss/unit/provenance diagnostics. | #3099 says BLT3D publicly advertises DWG/DXF input; fidelity is unverified. | Implement adapter slices only with unit/source/loss diagnostics and fixtures. |
| P2 | Revit interoperability | **PARTIAL / architecture only for safe routes** | #3102 defines safe routes as Revit -> IFC -> QS3D or a separate Revit API bridge -> neutral payload; it does not claim direct RVT parsing. | #3099 records vendor “Revit integration/import” wording but explicitly says route/fidelity is ambiguous. | Do not advertise direct RVT parsing. A future bridge is a separate runtime/integration lane. |
| P2 | Direct proprietary `.rvt` parser inside this repo | **NOT DESIRED / UNSUPPORTED** | #3102 explicitly says this architecture does not promise direct proprietary RVT parsing. `PRODUCT-BOUNDARY.md` keeps proprietary host types out of vendor-neutral Core. | BLT vendor wording is not evidence of implementation route. | Keep unsupported unless separately licensed, implemented and qualified under a new explicit decision. |
| P2 | Standalone CAD/DWG engine or `QS3D.exe` from this repo | **NOT DESIRED / OUT OF PRODUCT BOUNDARY** | `docs/PRODUCT-BOUNDARY.md` locks this repository to a BricsCAD V25/V26 hosted plugin; standalone CAD belongs to `QS3D-CAD`. | Competitor packaging does not change QS3D repository boundaries. | No implementation lane should attempt this here. |
| P2 | User workflow: model -> QTO -> cost -> progress/report | **PARTIAL** | All major pieces except the 4D bridge exist in separate current-main services/docs: measurement/reporting, rate/estimate, progress claim, audit, export. | #3099 shows a compact BLT select/calculate/review/recalculate/report loop; #3101 defines the larger normalized 3D/4D/5D chain. | Prioritize orchestration and provenance, not another parallel set of calculation services. |

## 4. What is already stronger than a superficial BLT3D parity checklist

The public BLT3D evidence in #3099 establishes a useful workflow envelope, but it does not establish internal contracts for all of the following. Current QS3D already has concrete source for several of them:

- rule/version-aware measurement trace;
- explicit source and semantic identities;
- commercial adjustment quantity with mandatory reason;
- rate item ID/version/effective date/currency;
- deterministic quantity-to-rate resolution;
- exact revision cost reconciliation split into quantity-driven and rate-driven effects;
- progress claim certification arithmetic with retention;
- generic audit actors/correlation IDs;
- source handles/drawing fingerprint in quantity reporting;
- IFC round-trip quantity evidence types.

This matters because the next implementation sequence should close **relationship gaps**, not duplicate already-implemented primitives.

## 5. Highest-value missing relationship: the 4D bridge

The smallest safe bridge consistent with #3101 is:

```text
Quantity/model identity
    |
    v
ActivityReference
- activityId
- externalScheduleId/source?
- name?
- scheduleRevisionId
- plannedStart/plannedFinish?   # optional imported/read-only context
    |
    v
QuantityActivityAllocation
- quantity identity
- activityId
- allocated quantity + unit
- provenance / mapping rule
```

The first implementation should **not** include critical-path scheduling, resource leveling, calendar engines, Gantt editing, or a live Primavera/MS Project connector. Those are separate capabilities and would make the first lane too large to review safely.

## 6. Gap-to-future-lane backlog

Every item below is deliberately small enough to become its own future Issue/Lane-Key. None is authorized as implementation by #3103 itself.

### GAP-3103-A — activity reference contract

**Goal:** add a host-neutral activity reference with stable ID, external source/revision metadata and optional planned dates.  
**Why:** closes the minimum activity-identity gate without building a scheduler.  
**Validation:** deterministic identity, UTC/date policy, duplicate/conflict tests, no BricsCAD types in Core contract.

### GAP-3103-B — quantity-to-activity allocation

**Goal:** map an existing quantity/source identity to one or more activity references with allocated quantity/unit and mapping provenance.  
**Why:** this is the actual missing 3D/QTO -> 4D relationship.  
**Validation:** no over-allocation unless explicitly allowed by policy; orphan/duplicate diagnostics; source quantity remains immutable.

### GAP-3103-C — schedule revision + orphan diagnostics

**Goal:** represent imported/read-only schedule revision metadata and report unmapped quantity scopes / missing activities.  
**Why:** makes external schedule context auditable without pretending QS3D owns the authoritative scheduler.  
**Validation:** revision/source identity preserved; fail closed on duplicate activity IDs.

### GAP-3103-D — dated progress snapshot separate from claims

**Goal:** introduce a dated progress state tied to quantity/activity scope before commercial certification.  
**Why:** current `ProgressClaimService` starts from contract/claim quantities; a neutral progress snapshot is needed for 4D/5D traceability.  
**Validation:** measured/installed/accepted/claimed/certified states are not conflated.

### GAP-3103-E — quantity/activity/cost impact projection

**Goal:** project a quantity revision delta through existing activity allocations into affected cost rows without mutating prior baselines.  
**Why:** extends the already-present `EstimateRevisionCostImpact` across the missing 4D bridge.  
**Validation:** old/new snapshots reproducible; affected and orphaned mappings explicit.

### GAP-3103-F — unified provenance report

**Goal:** produce a read-only report projection from source element -> measurement trace -> activity allocation -> rate/estimate -> progress/change.  
**Why:** makes the integrated chain inspectable before adding more commercial features.  
**Validation:** every displayed value exposes source/version identity; report generation must detect stale mappings.

### GAP-3103-G — interoperability adapter implementation slices

**Goal:** turn #3102 architecture into separately testable adapters/diagnostics for one exchange path at a time (for example IFC quantity facts first, then DXF/external-DWG metadata).  
**Why:** #3102 is architecture, not proof of complete shipping adapters.  
**Validation:** units, source revision, source IDs and lossy mapping diagnostics are mandatory; no direct-RVT claim.

### GAP-3103-H — progress claim lifecycle/persistence audit

**Goal:** audit whether current claim valuation needs first-class claim ID/date/status/history/evidence persistence after the neutral progress model exists.  
**Why:** arithmetic exists; lifecycle completeness is the actual unknown.  
**Validation:** no rewrite of prior certified state; persistence and report round-trip have explicit evidence.

## 7. Reference-lane reconciliation

### #3098 — BLT3D / BIM5D benchmark

Use as the broad capability benchmark and evidence-quality policy. It explicitly assigns #3103 responsibility for determining current QS3D implementation truth.

### #3099 — BLT3D verified feature/action inventory

Now merged to `main`. It strengthens the public evidence for BLT3D import, settings, calculate/results/recalculate and report/export workflow, while explicitly refusing to invent BLT-specific command-line tokens or unverified 4D/claim capabilities.

### #3101 — normalized quantity -> 4D -> 5D domain progression

Now merged to `main`. Its merge commit explicitly says the lane aligned the domain progression **without source implementation**. It provides the minimum truthful gates used here:

- model-based QTO needs stable source identity and measurement provenance;
- 4D needs explicit activity identity/time/mapping/revision;
- 5D-oriented cost needs cost items/codes, rates, versions, quantity-cost mapping and deltas;
- integrated 4D/5D needs common identities and cross-domain provenance.

Current source satisfies much of the QTO and cost gate, but not the 4D gate.

### #3102 — DWG/DXF/IFC/Revit interoperability architecture

Now merged to `main`. It defines safe integration modes and the adapter boundary, while explicitly warning that an architecture-supported path is not automatically a shipping feature. Its conservative Revit wording is adopted by this matrix.

## 8. Product-boundary decisions

Per current `docs/PRODUCT-BOUNDARY.md`:

- this repository remains a BricsCAD V25/V26 hosted plugin;
- BricsCAD owns live/native DWG database, viewport, document lifecycle and transactions;
- vendor-neutral Core contracts must not leak proprietary host types;
- standalone CAD belongs to `QS3D-CAD`;
- clean-room BLT/BLT3D material is workflow/UX reference only.

Consequences for this gap plan:

1. The missing 4D bridge should be host-neutral domain data, not a new desktop host.
2. External schedule/cost/model integrations must be explicit adapters with source/revision provenance.
3. Direct proprietary RVT parsing is not inferred from competitor wording.
4. Interoperability work must not recreate a standalone DWG engine inside Core.

## 9. Recommended sequencing

Given current-main implementation truth, the most efficient sequence is:

```text
P0  ActivityReference
 -> QuantityActivityAllocation
 -> ScheduleRevision/orphan diagnostics

P1  Dated neutral ProgressSnapshot
 -> quantity/activity/cost impact projection
 -> unified provenance report

P2  adapter implementation slices from #3102
 -> claim lifecycle/persistence audit
```

Do **not** restart with generic quantity, rate-book, estimate or claim arithmetic work: those primitives already exist and should be extended only where a concrete gap is proven.

## 10. Final current-main classification

At `main@fd2b27973f1ceea3d54842f211262e0234c78ef9`:

- **Model-based QTO:** already present with strong provenance primitives.
- **BLT-familiar calculate/review/report foundation:** already present, without claiming proprietary parity.
- **Quantity-cost / model-based estimating:** already present.
- **Versioned cost code/rate foundation:** already present.
- **Quantity-to-cost revision impact:** already present.
- **4D activity/schedule linkage:** missing.
- **Progress/claim valuation:** partial; arithmetic exists, neutral dated/activity-linked progress lifecycle is incomplete.
- **Integrated 4D/5D propagation:** partial because the 4D bridge is missing.
- **Reporting/auditability:** strong for quantity/cost; partial end-to-end until schedule mappings exist.
- **Interoperability:** partial; real exchange/evidence infrastructure exists and #3102 now defines the safe architecture, but architecture must not be marketed as implemented format fidelity.
- **Direct RVT parser / standalone CAD engine in this repo:** unsupported/out of boundary.

That makes **explicit, auditable quantity-to-activity mapping** the highest-priority structural gap for the next implementation lane.

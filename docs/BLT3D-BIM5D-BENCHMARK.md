# BLT3D / BIM5D benchmark for QS3D

**Research lane:** #3098  
**Baseline:** `main@20ecc6c7cc692bf01a4b1c29ed5d98220336b491`  
**Date:** 2026-08-19  
**Scope:** clean-room workflow/product research only. This document does not authorize copying proprietary code/assets and does not change the locked `QS3D-BricsCAD` product boundary.

## Executive summary

1. **BLT3D is a current named BLT SOFTWARE product.** The current BLT vendor page describes BLT3D as a 3D quantity-calculation product with BIM support and states Revit/IFC integration. The same page also advertises import of DWG, DXF, IFC and Revit data, quantity calculation, result review and export/reporting. These are **vendor claims**, not independently runtime-verified facts.
2. The same vendor page currently lists a **BLT product family**: `BLT3D`, `BLTQS 2D`, `Neopro`, and `BLT-GEO`. It does **not** currently list a separate product named `BIM5D`; therefore QS3D documentation must not claim that “BLT BIM5D” is a distinct BLT edition unless stronger primary evidence is found.
3. **5D BIM is primarily an industry workflow concept, not the name of one universal application.** A common convention is 3D model information + 4D schedule/sequence + 5D cost. Autodesk describes 4D as schedule/sequencing and 5D as cost data linked to the model. Cost-management workflows then extend into budgets, contracts, change orders, payments and forecasting.
4. There is also a **separate public open-source project literally named `BIM5D Application`** (`kanechan25/bim5d_software`). Its README describes a Revit-API/C#/WinForms/SQL/VBScript quantity-and-cost workflow with model-data export, rebar processing and BOQ production. This is a useful public workflow reference, but there is no evidence found here that it is affiliated with BLT SOFTWARE.
5. Glodon markets **Cubicost 5D BIM** as an integrated quantity/cost platform with TAS/TRB/TME/TBQ modules. Its official pages describe model-based quantity takeoff, rebar, MEP, billing/pricing, local measurement rules, model traceability, reports and import of IFC/RVT in selected workflows. This is a useful reference for the functional envelope of a mature digital-QS workflow.
6. For this repository, BLT3D/BIM5D material is a **clean-room workflow/UX benchmark only**. `QS3D-BricsCAD` remains a BricsCAD V25/V26 hosted plugin under `docs/PRODUCT-BOUNDARY.md`; no competitor packaging or Revit-host architecture becomes a requirement here.

## Evidence quality rules

Use the following evidence levels when turning this benchmark into requirements:

| Level | Meaning | Allowed use |
|---|---|---|
| A | Open standard / authoritative platform documentation | Architecture and terminology baseline |
| B | Current vendor product documentation | Competitive capability claim, explicitly labeled as vendor claim |
| C | Public source repository / public sample | Workflow inspiration and implementation-pattern study subject to license and clean-room review |
| D | Case study / secondary material | Supporting context only; do not use as the sole basis for a hard requirement |
| Unknown | Forum/social/unverifiable statement | Do not turn into a product claim |

No benchmark item is equivalent to runtime validation of a competitor product.

## 1. BLT SOFTWARE family

Current source: <https://www.thangblt.com/>

The vendor page currently presents these products:

| Product | Vendor positioning | QS3D-relevant signal | Evidence caution |
|---|---|---|---|
| **BLT3D** | 3D quantity calculation, BIM support, Revit/IFC integration | 3D/model-linked takeoff; object-driven review; model import/interchange | Vendor claim; no independent runtime verification in this research lane |
| **BLTQS 2D** | 2D takeoff from AutoCAD/PDF | Drawing-based QS remains valuable alongside 3D/BIM | Vendor claim |
| **Neopro** | Higher-end edition with AI-assisted item recognition, estimating/reporting | Recognition/automation is a competitive direction, but should be evidence-driven | Vendor claim; “AI” scope is not independently verified |
| **BLT-GEO** | Geotechnical/foundation/earthwork/terrain quantities | Separate domain lane rather than mixing all geometry into the building-QS core | Vendor claim |

### BLT3D details currently claimed by the vendor

The current page states or shows the following BLT3D/BLT SOFTWARE workflow elements:

- Windows package targeting AutoCAD 2024+ / ObjectARX and .NET 8;
- model/drawing import including DWG, DXF, IFC and Revit wording;
- configurable units/calculation settings;
- object selection/grouping/classification;
- automatic quantity calculation;
- results review and recalculation;
- reports/export including Excel/PDF/Word wording;
- storage/project retrieval;
- current page version wording `2.1.0`.

Treat every item above as **what the current vendor website says**, not as a guarantee of actual format fidelity, API architecture, quantity-rule correctness, performance, or compatibility.

### Important BLT/BIM5D naming conclusion

As of this research date, the current BLT vendor product list does **not** expose a separately named `BIM5D` product. Therefore:

- `BLT3D` and `BIM 5D` must not be treated as synonyms;
- “BLT has BIM5D” remains **unconfirmed** unless a primary BLT source explicitly says so;
- it is safe to say that BLT3D is marketed with BIM-related quantity functionality, while 5D BIM is a broader model + time + cost workflow concept.

## 2. What 5D BIM means

Authoritative references:

- Autodesk 5D BIM overview: <https://www.autodesk.com/solutions/5d-bim>
- Autodesk Cost Management overview: <https://help.autodesk.com/cloudhelp/ENU/BIM360D-Cost-Management/files/BIM360D_Cost_Management_about_cost_management_html.html>
- buildingSMART technical standards overview: <https://technical.buildingsmart.org/>
- buildingSMART IFC 4.3 documentation: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/content/introduction.htm>

Canonical QS3D terminology and data-flow refinement for this section is maintained by issue #3101 in `docs/BIM5D-QUANTITY-SCHEDULE-COST-MODEL.md`.

A practical QS3D interpretation is:

```text
3D model / element identity
        |
        v
quantity facts + classifications
        |
        v
4D activity / schedule / sequence
        |
        v
5D cost codes / rates / resources
        |
        v
progress + forecast
        |
        v
BOQ / claim / variance / report
```

### 3D

For QS purposes, “3D” should not mean geometry alone. A useful quantity model needs stable element identity, type/classification, units, geometry-derived measurements, source provenance and enough semantic information to explain a quantity result.

### 4D

The commonly used 4D layer links model elements or quantity scopes to project activities, sequencing and time. QS3D should treat this as a separate schedule link, not as a mutation of geometric truth.

### 5D

The commonly used 5D layer adds cost information to the model/quantity/schedule context. A useful implementation can include:

- cost code / WBS mapping;
- unit rates and resources;
- budget and estimate versions;
- quantity x rate calculations;
- change and variance impact;
- progress valuation / claims;
- time-phased forecast or cash-flow views;
- traceable reporting back to model elements and quantity calculations.

“5D” should not become a marketing label until these relationships are represented explicitly and auditable.

## 3. OpenBIM/interoperability implications

buildingSMART defines IFC as an open international standard for sharing BIM data and publishes related standards/services such as BCF, IDS and bSDD. For QS3D this suggests an adapter boundary rather than hard-coding one authoring system into quantity logic.

Recommended separation:

```text
BricsCAD native DWG/API ------------------+
                                           |
IFC/openBIM adapter -----------------------+--> normalized element/quantity facts
                                           |
external authoring-system exchange/API ----+
```

Key requirements for any import/exchange lane:

- preserve source identity and revision provenance;
- normalize units explicitly;
- distinguish native geometry from imported/exchanged geometry;
- record classification/property loss rather than silently inventing data;
- keep IFC/entity/property interpretation behind an adapter;
- never claim direct proprietary-format support merely because another product advertises it.

## 4. Public `BIM5D Application` reference project

Public source: <https://github.com/kanechan25/bim5d_software>

The README describes a project aimed at construction quantity calculation and cost estimation. It states that the project uses Revit API, C#, WinForms, SQL and VBScript and is split into three parts:

1. core quantity/calculation software;
2. an add-in attached to Autodesk software;
3. database-management tooling.

The documented workflow exports building data to JSON/CSV/XLSX, imports/processes that data, performs rebar calculations, and finally aggregates quantities into BOQ output classified by category/level.

### What QS3D may learn from it

- explicit separation between host extraction and downstream quantity/cost processing;
- intermediate exchange artifacts can make workflows debuggable;
- rebar needs discipline-specific rules rather than generic volume-only handling;
- category/level grouping is central to QS review;
- final BOQ should preserve traceability to upstream model data.

### What QS3D must not infer

- that this project defines the industry standard for “5D BIM”;
- that its Revit-specific architecture applies to BricsCAD;
- that public source may be copied without checking license/compatibility;
- that it is related to BLT SOFTWARE.

## 5. Glodon Cubicost as a 5D/QS reference envelope

Official references:

- 5D BIM Digital Cost Management Solution: <https://www.glodon.com/en/solutions/5d-bim-digital-cost-management-solution-7>
- TAS & TRB: <https://www.glodon.com/en/products/tas-%26-trb-1>

Glodon currently describes Cubicost as a 5D BIM digital-cost platform with these major modules:

- **TAS** — architecture/structure quantity takeoff;
- **TRB** — rebar quantity takeoff;
- **TME/TMEC** — MEP quantity takeoff;
- **TBQ** — billing/pricing, estimating and cost management.

Its product pages emphasize:

- model/drawing-based quantity takeoff;
- local measurement rules and automatic deductions;
- 3D traceability of quantity results;
- revision/change recalculation;
- classification and extraction filters;
- configurable reports;
- IFC/RVT import in selected workflows;
- integrated quantity-price/cost workflows.

Again, these are vendor capabilities/claims used to define a **competitive workflow envelope**, not a request to clone Cubicost.

## 6. ACCA PriMus IFC as an openBIM 5D reference

Reference: <https://www.accasoftware.com/en/5d-bim-software>

ACCA positions PriMus IFC as IFC-based 5D BIM software for quantity takeoff, with measurement rules linked to BIM entities. The useful QS3D signal is that a 5D workflow can be built around **open IFC data + explicit measurement rules + visual/model traceability**, rather than requiring one proprietary authoring host.

## 7. Capability matrix for QS3D planning

This table is a research map, **not a claim that current QS3D already implements or lacks each item**. Current-main truth must be established by issue #3103.

| Capability | BLT3D / BLT family signal | 5D-BIM reference signal | QS3D design question |
|---|---|---|---|
| 2D takeoff | BLTQS 2D | Common pre-BIM workflow | Keep strong drawing-based fallback? |
| 3D/model takeoff | BLT3D | Core 3D/5D workflow | Are quantity facts linked to stable model identity? |
| Architecture/structure QTO | BLT3D general claim | Cubicost TAS | Do rules cover discipline-specific deductions/classification? |
| Rebar QTO | Not established from BLT page | Cubicost TRB; public BIM5D reference | Are hooks/laps/bends/rules auditable? |
| MEP QTO | Not established from BLT page | Cubicost TME/TMEC | Separate future discipline lane? |
| Cost codes/rates | Vendor page mentions unit price/settings | 5D cost layer; Cubicost TBQ | Need first-class cost-code/rate versioning? |
| Schedule/activity links | Not established from BLT page | 4D convention | Need model/quantity -> activity mapping? |
| Progress segmentation | Not established from BLT page | Common construction 4D/5D workflow | Need quantity scope snapshots by segment/date? |
| Progress claims | Not established from BLT page | 5D/QS workflow | Need claim version, certification and audit trail? |
| Change propagation | Recalculate workflow claimed | Cubicost model-revision recalculation | Can revisions show delta and provenance? |
| Model traceability | 3D review wording | Cubicost visual quantity checking | Can every reported quantity navigate to source elements? |
| IFC/openBIM | BLT3D advertises IFC | buildingSMART/ACCA | Adapter with explicit unit/property mapping? |
| Revit integration | BLT3D advertises Revit wording | public BIM5D is Revit-API based; Cubicost advertises RVT import | Exchange/API boundary without turning this repo into a Revit add-in? |
| Reporting | BLT page advertises report export | 5D cost/report workflows | Reports tied to source revision and calculation version? |
| AI recognition | Neopro advertises AI assistance | Vendor-specific emerging feature | Only after deterministic rules + confidence/audit model exist |
| Geotechnical/earthwork | BLT-GEO | Separate specialist domain | Separate module/lane rather than overloading building QTO? |

## 8. QS3D architecture principles derived from the benchmark

### 8.1 Keep geometry/quantity truth separate from cost and time

A model element can produce quantity facts. Cost and schedule should reference those facts by stable identity and version, not overwrite the geometry calculation itself.

### 8.2 Make quantity results explainable

A quantity result should carry enough information to answer:

- source object(s);
- source drawing/model revision;
- rule used;
- input dimensions/properties;
- deductions/additions;
- unit conversion;
- classification/WBS mapping;
- result version.

### 8.3 Treat rates and budgets as versioned business data

Rates change independently from geometry. Recalculating cost with a new rate set must not rewrite historical quantity snapshots or certified claims.

### 8.4 Treat progress/claims as snapshots, not mutable totals

A useful 5D/QS workflow needs dated progress states, approved quantities, previous claims, current claim, cumulative value and variance. Historical claims should remain reproducible.

### 8.5 Use adapters for external models

The BricsCAD host adapter, IFC/openBIM adapter and any future external-authoring integration should normalize into vendor-neutral contracts before quantity/cost logic consumes the data.

### 8.6 Preserve the repository product boundary

Per `docs/PRODUCT-BOUNDARY.md`:

- this repository ships a BricsCAD V25/V26 plugin;
- BLT-like or BIM5D-like means workflow familiarity/reference only;
- a standalone CAD application belongs to `QS3D-CAD`;
- vendor-neutral domain abstractions may migrate toward `QS3D-Platform` deliberately.

## 9. Suggested implementation sequence after research closes

This is sequencing guidance, not pre-authorized implementation scope. For the canonical terminology/data-flow rationale, see `docs/BIM5D-QUANTITY-SCHEDULE-COST-MODEL.md` (issue #3101).

### P0 — quantity truth and provenance

- stable element/source identity;
- explicit units;
- deterministic quantity rules;
- calculation explanation/trace;
- revision/delta model.

### P1 — 4D schedule foundation

- activity/schedule links;
- activity/WBS identity;
- planned/actual time fields;
- dependency/sequence representation;
- quantity/model-to-activity allocations;
- schedule revision provenance.

### P2 — 5D cost foundation

- WBS/cost codes;
- rate/resource sets with versions;
- quantity/activity-to-cost mapping;
- estimate/budget snapshots;
- time-phased cost and variance reporting.

### P3 — progress/claims/forecasting

- project segmentation;
- dated progress snapshots;
- measured/installed/accepted/claimed/certified state separation;
- progress valuation and claims;
- change impact and forecast reporting.

### P4 — interoperability and advanced automation

- IFC/openBIM import contracts;
- mapping diagnostics and provenance;
- external schedule/cost exchange/API adapters where legally/licensably appropriate;
- recognition/rule suggestions/anomaly detection only after deterministic contracts exist;
- never hide confidence, source, or deterministic fallback.

## 10. Multi-agent self-claim backlog

The owner requested that follow-up work be split so agents may choose tasks themselves. These Issues are intentionally **unassigned**. An agent must self-claim by updating the selected Issue to `Status: ACTIVE` with the required lane/carrier metadata before any mutation.

- #3099 — BLT3D verified feature + command inventory
- #3101 — BIM5D 3D/4D/5D quantity-cost-schedule model
- #3102 — DWG/DXF/IFC/Revit interoperability mapping
- #3103 — current QS3D vs BLT3D/5D-BIM gap matrix
- #3104 — QS3D 5D domain architecture design
- #3105 — estimating/progress-claim/reporting UX specification

Rules:

1. one Issue = one stable Lane-Key;
2. one ACTIVE owner/session per Lane-Key;
3. one canonical branch and one canonical PR;
4. no agent should inspect or take over another ACTIVE lane beyond minimal collision metadata;
5. implementation work discovered by research must become new focused Issues rather than being silently added to a research lane;
6. do not merge to `main` without explicit owner authorization and fresh required CI.

## 11. Open questions that require evidence, not guessing

- Is there a historical BLT product/version explicitly branded `BIM5D`? Current vendor page does not prove it.
- What is BLT3D's actual public command surface, and is any complete command reference officially published?
- What exactly does BLT's “Revit” wording mean: native file parsing, API integration, exported/intermediate data, or marketing shorthand?
- Which measurement standards/local rules are supported and how are deductions explained?
- How does BLT3D persist object identity across drawing/model revisions?
- Which BLT3D capabilities are already present in current QS3D, partially present, or intentionally outside the BricsCAD product boundary?

Those questions are assigned to the research/gap Issues above; do not turn them into assumptions in source code.

## Source ledger

Primary/authoritative/open-standard sources:

- BLT SOFTWARE: <https://www.thangblt.com/>
- Autodesk 5D BIM: <https://www.autodesk.com/solutions/5d-bim>
- Autodesk Cost Management: <https://help.autodesk.com/cloudhelp/ENU/BIM360D-Cost-Management/files/BIM360D_Cost_Management_about_cost_management_html.html>
- buildingSMART technical standards: <https://technical.buildingsmart.org/>
- buildingSMART IFC 4.3: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/content/introduction.htm>
- Glodon 5D BIM Digital Cost Management: <https://www.glodon.com/en/solutions/5d-bim-digital-cost-management-solution-7>
- Glodon TAS & TRB: <https://www.glodon.com/en/products/tas-%26-trb-1>
- ACCA PriMus IFC: <https://www.accasoftware.com/en/5d-bim-software>
- Public BIM5D reference project: <https://github.com/kanechan25/bim5d_software>

## Maintenance rule

When a follow-up Issue establishes stronger evidence, update this benchmark by replacing weaker wording rather than stacking contradictory claims. Keep the distinction between **standard**, **vendor claim**, **public reference implementation**, **QS3D inference**, and **verified current-main behavior** explicit.
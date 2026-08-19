# Global QS / QTO / 5D BIM software landscape — 2026

**Research lane:** #3111  
**Baseline:** `main@a323d25a9f5720eb99b2533d9605298366dd3735`  
**Research date:** 2026-08-19  
**Scope:** clean-room market/workflow research for `QS3D-BricsCAD`; this document does not authorize copying proprietary code, binaries, assets, UI layouts, measurement rules, data sets or trade secrets.

## 1. Purpose and research boundary

This note maps the **current public software landscape that materially overlaps with BLT3D and QS3D workflows**: drawing takeoff, model-based quantity takeoff, BIM/openBIM quantities, estimating, BOQ, rates, cost planning, revision/change control, rebar/MEP quantities, 4D schedule/progress, 5D cost, and AI-assisted takeoff.

It is intentionally broad, but it does **not** claim literal exhaustiveness. The construction-software market contains regional products, private/internal tools, reseller-only products, vertical applications, renamed products and offerings with limited public documentation. The useful standard here is: cover the major and technically relevant public products that can inform QS3D strategy, then keep the source ledger updateable.

The earlier [`BLT3D-BIM5D-BENCHMARK.md`](BLT3D-BIM5D-BENCHMARK.md) remains the focused BLT3D/BIM5D note. This document expands from that local reference to the worldwide product landscape.

## 2. Evidence labels

| Label | Meaning | How QS3D may use it |
|---|---|---|
| **STANDARD** | Open standard / authoritative standards documentation | Architecture and interoperability baseline |
| **VENDOR** | Current official product/vendor documentation | Competitive capability signal; still a vendor claim unless independently tested |
| **OPEN SOURCE** | Public source repository / public software documentation | Architecture/API/reference study, subject to license review |
| **RESEARCH** | Academic/public research prototype | Future-direction signal, not a production-product claim |
| **QS3D INFERENCE** | A product/design conclusion drawn for QS3D | Must be tested against current `main` before becoming an implementation requirement |

No vendor marketing statement in this document is equivalent to runtime verification.

## 3. Market taxonomy

The software landscape is easier to reason about as overlapping layers rather than one competitor list.

```text
A. 2D digital takeoff / estimating
   PDF / CAD -> count / length / area / volume -> BOQ / estimate

B. 3D / BIM quantity takeoff
   model elements -> properties / geometry -> rules -> quantities -> traceable result

C. 5D commercial management
   quantities -> WBS / cost code -> rate/resource -> budget/estimate/change/payment/forecast

D. 4D / progress
   elements / scopes -> activities -> schedule -> progress -> earned/claimed quantities/cost

E. Discipline-specialist QTO
   rebar / MEP / electrical / steel / earthwork / trade-specific rules

F. BIM authoring / openBIM information sources
   Revit / BricsCAD BIM / Archicad / ALLPLAN / Tekla / IFC -> structured quantity evidence

G. AI / agentic takeoff
   drawings/models -> detection/reasoning/action -> reviewed measurements / reports / estimates
```

A strong QS platform increasingly combines several of these layers, but QS3D should keep their contracts separable so geometry truth, quantity rules, cost data and progress history do not become one opaque mutable state.

## 4. Global product landscape

### 4.1 BLT SOFTWARE — Vietnam reference family

| Product | Public positioning | Relevance to QS3D |
|---|---|---|
| **BLT3D** | 3D quantity calculation with BIM-oriented workflow; vendor wording includes DWG/DXF/IFC/Revit and reporting | Direct local workflow/UX benchmark for model-based QTO |
| **BLTQS 2D** | 2D takeoff from CAD/PDF | Confirms 2D takeoff remains important even beside BIM |
| **Neopro** | Higher-end estimating/reporting/AI-oriented BLT offering | AI/automation direction; claims require separate verification |
| **BLT-GEO** | Geotechnical/earthwork/foundation quantity workflow | Specialist-domain signal; do not overload the building-QTO core |

Focused details and evidence cautions are maintained in `BLT3D-BIM5D-BENCHMARK.md`.

### 4.2 Glodon — Cubicost / QuantifAI

| Product | Public positioning | Relevance |
|---|---|---|
| **Cubicost TAS** | BIM architecture/structure quantity takeoff, localized measurement rules, visible expressions, reports, model/revision workflows | One of the closest references for rule-driven building QTO |
| **Cubicost TRB** | BIM-based rebar quantity takeoff with local rules, checking and reporting | Strong rebar quantity benchmark |
| **Cubicost TME / TMEC** | MEP quantity takeoff | Specialist MEP reference |
| **Cubicost TBQ** | BOQ/billing/pricing and cost workflow integrated with quantity modules | Quantity-to-price/BOQ reference |
| **QuantifAI** | Announced by Glodon in 2026 as next-generation AI-powered quantity takeoff | Important emerging AI-QTO benchmark |

**QS3D inference:** Cubicost demonstrates the value of keeping discipline engines separate while presenting one coordinated QS workflow. Localized measurement rules, visible calculation expressions and revision-aware model checking are especially relevant concepts.

### 4.3 RIB Software — CostX / Candy / Presto / iTWO / RIB 4.0

| Product | Public positioning | Relevance |
|---|---|---|
| **RIB CostX** | 2D and BIM takeoff plus estimating workbooks; drawing/model-linked quantities and revision comparison | Very close direct benchmark for QS3D QTO + estimating |
| **RIB Candy** | Estimating, planning, BOQ, resources, cash flow, forecasting, valuations, EVM and subcontract management | Strong quantity + time + commercial-control reference |
| **RIB Presto** | BIM-integrated estimating, bidding and project management; cost/time/execution workflows | Major Spain/Latin America reference for BIM-oriented cost management |
| **RIB iTWO** | BIM/project controls and integrated 5D-oriented workflows | Enterprise 5D reference |
| **RIB 4.0** | Integrated project/enterprise platform with model, estimate, cost control and analytics integrations | Enterprise architecture reference |
| **RIB BuildSmart** | Cost management and enterprise accounting | Downstream commercial/ERP boundary reference |

**QS3D inference:** CostX is a useful near-term workflow comparator; Candy/iTWO/Presto are more useful for the later schedule, valuation, subcontract and forecast layers.

### 4.4 Autodesk — Takeoff / Cost Management / Revit

| Product | Public positioning | Relevance |
|---|---|---|
| **Autodesk Takeoff** | Cloud 2D/3D takeoff within Autodesk Construction Cloud | Cloud QTO/collaboration benchmark |
| **Autodesk Cost Management** | Budgets, contracts, change orders, payments and forecasting | 5D/commercial-management reference |
| **Autodesk Revit** | BIM authoring with schedules and material takeoff schedules | Important upstream model/quantity source and comparator for authoring-native QTO |

Revit material-takeoff schedules can expose model quantities, but authoring-native takeoff has category/element semantics and limitations that differ from a dedicated QS engine. QS3D should avoid assuming that “BIM property exists” means “contract measurement quantity is correct”.

### 4.5 Bentley — SYNCHRO

| Product | Public positioning | Relevance |
|---|---|---|
| **SYNCHRO 4D** | 4D construction planning/simulation with model splitting, work areas, WBS and model-based QTO | Strong schedule/model linkage benchmark |
| **SYNCHRO Perform** | Field progress/productivity and earned-value-oriented reporting | Progress evidence reference |
| **SYNCHRO Cost** | QTO/constructible components, WBS/cost codes, estimates, changes, payment applications and actual-vs-plan | Strong 4D/5D commercial workflow reference |

**QS3D inference:** activity links, progress records and cost codes should reference immutable/stable quantity scopes rather than mutate geometry-derived quantity truth.

### 4.6 BEXEL Manager

BEXEL Manager publicly positions itself as an integrated BIM environment spanning model federation, validation, quantity takeoff, cost classification, BOQ, zones, scheduling and 4D/5D workflows.

Useful benchmark concepts:

- model federation and validation before QTO;
- quantity takeoff as a reusable structured layer;
- cost classification / CBS and BOQ mapping;
- zones and schedule integration;
- one model context across 3D/4D/5D views.

### 4.7 ACCA — PriMus family

| Product | Public positioning | Relevance |
|---|---|---|
| **PriMus IFC** | IFC-based 5D BIM quantity takeoff with measurement rules, visual model traceability, WBS and revision updates | Strong openBIM/QTO reference |
| **PriMus TAKEOFF** | CAD/DWG/DXF/PDF/image takeoff | 2D companion-workflow reference |
| **PriMus** | Estimating / cost management | BOQ/estimating reference |
| **PriMus KRONO** | Scheduling and financial/project control | 4D/commercial adjunct |

**QS3D inference:** a high-quality openBIM quantity workflow can be built around explicit IFC mapping + measurement rules + visual traceability without requiring the quantity core to depend on one authoring vendor.

### 4.8 Nomitech CostOS

CostOS is positioned as enterprise cost estimating with GIS/2D/3D BIM takeoff, BOQs, price schedules, BIM/IFC integration, parametric assemblies, multiuser workflows and schedule integrations such as Primavera P6 / Microsoft Project.

Particularly relevant signals:

- one estimate can combine multiple quantity-source types;
- BIM and 2D takeoff can feed one cost structure;
- parametric assemblies and cost libraries are first-class;
- schedule/estimate integration belongs behind explicit mappings.

### 4.9 Procore — Estimating / Takeoff

Procore Estimating/Takeoff currently combines digital takeoff and estimating inside the Procore ecosystem. Public documentation includes 2D takeoff, 3D takeoff from Revit files, automated counting, assemblies, cost data, overlays and estimate generation.

**QS3D inference:** users increasingly expect the takeoff result to flow directly into an estimate rather than exporting quantities and manually rebuilding the estimate elsewhere.

### 4.10 ConstructConnect family — On-Screen Takeoff / PlanSwift / Quick Bid / QuoteSoft

| Product | Public positioning | Relevance |
|---|---|---|
| **On-Screen Takeoff (OST)** | Established PDF/DWG digital takeoff; current 4.x generation and AI Takeoff Boost integration | Mature 2D takeoff UX reference |
| **PlanSwift** | Visual takeoff plus assemblies, labor/material calculations and AI Takeoff Boost | Direct 2D/QTO benchmark |
| **Quick Bid** | Estimating/bid workflow | Downstream estimate reference |
| **QuoteSoft** | Trade-specific estimating/takeoff | Specialist-trade signal |
| **Takeoff Boost** | AI-assisted takeoff across ConstructConnect products | AI acceleration benchmark |

### 4.11 STACK

STACK is a cloud takeoff and estimating platform. Current public positioning includes count, linear, area and volume measurements, assemblies/estimating, project collaboration, live totals, AutoCount and AI-assisted workflows.

Useful benchmark concepts:

- cloud collaboration on one takeoff source;
- reusable assemblies/templates;
- measurement tags/groups;
- visible link between drawing markups, quantities and estimate totals.

### 4.12 Bluebeam Revu

Bluebeam Revu remains an important document-centric QS reference rather than a full 5D BIM platform. It supports measurement markups (count/length/area/volume), custom columns/formulas and Quantity Link to Excel.

**QS3D inference:** even sophisticated BIM users value fast, auditable 2D measurement tools. A BIM/QS product should not force every quantity workflow through 3D modeling.

### 4.13 Trimble estimating/takeoff portfolio

Trimble maintains a broad discipline-specific portfolio rather than one universal estimator.

| Product / family | Public positioning | Relevance |
|---|---|---|
| **WinEst** | Database-driven estimating and quantity takeoff | General estimating reference |
| **eTakeoff / Modelogix integrations** | Digital takeoff / historical estimating workflow around WinEst | Takeoff + historical intelligence |
| **B2W Estimate** | Heavy-civil estimating | Infrastructure/heavy-civil specialist reference |
| **Accubid** | Electrical estimating | Electrical specialist reference |
| **AutoBid Mechanical** | Mechanical estimating | MEP specialist reference |
| **LiveCount** | Digital takeoff in Trimble estimating workflows | Measurement front-end reference |
| **Viewpoint Estimating** | Estimating with takeoff/quantity workflows and change-order context | Contractor/ERP-oriented reference |
| **Tekla PowerFab** | Steel fabrication management/estimating ecosystem | Fabrication/commercial specialist reference |

### 4.14 Buildsoft Cubit Estimating

Cubit Estimating combines drawing takeoff and estimating, with multiple result types, plan tracing/counting and BIM quantity inputs. Current releases also market AI Auto Area / symbol-count assistance.

Useful QS3D signal: a takeoff system benefits from explicit **measurement/result types** rather than reducing all quantities to untyped numbers.

### 4.15 InEight Estimate

InEight Estimate targets enterprise/capital-project estimating, WBS structures, estimate libraries, benchmarking and quantity/cost inputs. Public materials also describe connections to 2D takeoff workflows.

Relevant for QS3D mainly as a later-stage enterprise estimating/cost-control reference rather than a UI clone target.

### 4.16 Cleopatra Enterprise

Cleopatra Enterprise publicly combines cost estimating, BIM, scheduling, work packaging, project controls, benchmarking and cost control. Its BIM workflow turns model information into BOQ/cost data.

**QS3D inference:** long-term 5D value comes from retaining traceability when quantities move into estimating, schedule/work package and cost-control layers.

### 4.17 NEVARIS Build / Success X

NEVARIS Build is a current European AVA/estimating/project-control platform covering tendering, award, billing, calculation, controlling and BIM. The current BIM package emphasizes openBIM/IFC, a BIM viewer, model-to-LV/BOQ workflows, quantity/cost determination and change tracking. NEVARIS 2026.1 further advertises early cost derivation directly from BIM models, combining modeled and non-modeled positions.

Strong QS3D signals:

- openBIM quantity/cost workflows can be integrated with tender/AVA processes;
- modeled and non-modeled cost positions must coexist;
- model changes should be traceable into tender/cost consequences.

### 4.18 RIB Presto regional 5D workflow

Presto deserves separate regional attention because RIB positions it strongly in Spain/Latin America and publishes `Cost-It` workflows between Revit and Presto, BIM-oriented project control, planning and 4D tutorials.

**QS3D inference:** regional construction-cost products often encode local tender/measurement conventions that a global core should represent through configuration/adapters, not hard-coded assumptions.

## 5. BIM authoring and model-information sources

These are not all direct QS3D competitors, but they define the upstream quantity evidence that dedicated QS software must consume or reconcile.

### 5.1 BricsCAD BIM

BricsCAD BIM is the most important host-side reference for this repository.

Current public documentation includes:

- BIM classification and data extraction;
- quantity-related BIM properties;
- IFC2x3/IFC4 import/export;
- property sets and IFC base quantities;
- model data that can be extracted/reported.

**Repository boundary:** `QS3D-BricsCAD` should use BricsCAD's native DWG/model/database/selection/view lifecycle, while QS3D owns semantic quantity, project, reporting and controlled generated-geometry workflows.

### 5.2 Autodesk Revit

Revit schedules/material takeoff can query modeled element/material quantities and organize/filter/report them. It is a key upstream BIM source and a reference for object/property-driven schedules.

However, dedicated QS rules, contract measurement, deductions, revision comparison, audit history and cross-model cost logic remain separate concerns.

### 5.3 Graphisoft Archicad

Archicad's Interactive Schedules can generate model-based element/component/surface lists and export/report data. Current component quantity fields include geometric quantities such as net/gross/conditional volume, area and mass.

Useful reference: model schedule editing/reporting should remain tightly tied to identifiable source objects.

### 5.4 ALLPLAN 2026

ALLPLAN 2026 provides quantity takeoff/costing in multiple editions and markets precise/verifiable quantities for modeled and non-modeled objects. Current releases include customizable BOM/quantity export, reinforcement/precast detail workflows and price data on precast elements. In April 2026, ALLPLAN also launched **Steel Genie**, an AI-powered structural-steel estimating tool that converts structural drawings into quantities and estimating-level 3D models.

Useful QS3D signals:

- support both modeled and non-modeled cost objects;
- reinforcement/precast need discipline-aware quantity semantics;
- AI can create estimating-level structured geometry, but generated evidence must remain reviewable.

### 5.5 Tekla Structures

Tekla is a strong discipline reference for structural/rebar/fabrication quantities. Public documentation exposes concrete and reinforcement quantities, bar counts/weights/properties, reports and model hierarchy.

**QS3D inference:** rebar quantity logic should be its own auditable domain with bar identity, shape, hook/lap/bend rules, grouping and weight/length calculations rather than a generic `volume x density` shortcut.

### 5.6 Vectorworks Architect / Design Suite 2026

Vectorworks worksheets are integrated with model/drawing data and can produce material quantity takeoffs, schedules, cost/supply lists and calculations. The 2026 product family continues openBIM/data/reporting workflows.

Useful reference: in-model worksheets can become a flexible reporting surface when formulas and object selection criteria are explicit.

## 6. openBIM / IFC analysis and quantity tooling

### 6.1 Solibri

Solibri Information Takeoff (ITO) extracts model properties/quantities/classification/location data into configurable reports with 3D visualization. Current Solibri versions also support formulas and model comparison workflows useful for quantity-delta review.

**QS3D inference:** quantity reports are more trustworthy when users can select a line/result and visually locate the contributing model objects, then compare revisions.

### 6.2 ORCA AVA / IFC Manager

ORCA AVA is a notable German-speaking AVA/QS platform with IFC-based quantity workflows. Current IFC Manager documentation describes analysis of model geometry/properties and transfer of quantities into AVA/cost structures.

This is another example of an openBIM quantity path linking IFC evidence to local tender/measurement processes.

### 6.3 IfcOpenShell / Bonsai / Ifc5D

IfcOpenShell is an especially valuable **OPEN SOURCE** reference because it exposes IFC parsing/authoring utilities plus cost and quantity APIs. Its ecosystem includes Bonsai and utilities such as Ifc4D/Ifc5D.

Useful clean-room architecture ideas:

- quantities and costs can be represented as first-class IFC-linked entities;
- cost schedules/items and quantity references can be manipulated through explicit APIs;
- open-source implementation can inform adapter/test design only after license review; it is not a reason to copy code into QS3D blindly.

## 7. AI / agentic takeoff generation

AI takeoff is now a distinct product category rather than a small checkbox.

### 7.1 Kreo / Caddie

Kreo currently markets cloud AI takeoff/estimating. In 2026 its **Caddie** feature became an agentic takeoff operator: the user describes a task, reviews a proposed plan, and the agent can execute measurements, reports, annotations and exports inside the product.

This is strategically important for QS3D because it demonstrates a better control model than opaque “AI result appears” automation:

```text
user intent
  -> AI proposes plan
  -> user approves
  -> tool executes deterministic product actions
  -> measurements/reports remain inspectable
```

### 7.2 Togal.ai

Togal.ai markets AI-powered automated drawing takeoff that detects/measures/compares quantities from plans. Accuracy and speed numbers are vendor claims and must not be treated as independent benchmarks.

### 7.3 Beam AI

Beam AI markets cloud automated takeoff and estimating, including quantity extraction, structured Excel/PDF outputs, rates/markups and human-reviewed QA. Performance/accuracy claims remain vendor claims.

### 7.4 Countfire

Countfire specializes in electrical/mechanical and related trade estimating. Current product workflow includes automated symbol counting across PDFs, linear measurements, takeoff-to-estimate quantity transfer, pricing from previous work, revision/specification comparison and cloud collaboration.

**QS3D inference:** trade-specific automation can outperform generic automation when the domain vocabulary, symbol semantics and validation rules are explicit.

### 7.5 Groundplan

Groundplan is cloud takeoff/estimating across construction trades. It supports count/length/area measurements, formulas, worksheets/BOQ, plan revisions, exports/integrations and `Count Assist` symbol automation.

### 7.6 Glodon QuantifAI

Glodon's 2026 AI product announcements include `QuantifAI` as a next-generation quantity-takeoff direction. This is strategically important because it suggests major traditional 5D vendors are moving from rule-driven BIM QTO toward AI-assisted QTO rather than treating the two approaches as mutually exclusive.

### 7.7 ALLPLAN Steel Genie

Steel Genie by ALLPLAN, announced in April 2026, targets structural-steel estimating by analyzing structural drawing sets, detecting members and generating quantities plus estimating-level 3D models.

This is a strong signal for discipline-specific AI: the output is not only a number; it can be a structured intermediate model that supports review and downstream estimating.

## 8. Research frontier: vision agents + BIM MCP

These are **RESEARCH** references, not established commercial-product capabilities.

### Handoff-H1

Published August 15, 2026, Handoff-H1 describes an orchestrated vision-agent system for material quantity takeoff from construction blueprints, combining purpose-built computer vision, tool-using agents and a persistent construction knowledge/project layer.

The useful architectural signal is the separation of:

1. visual primitive extraction;
2. tool execution/reasoning;
3. persistent project/knowledge state;
4. benchmarked final quantity evidence.

### MCP4IFC

MCP4IFC is an open research framework connecting LLMs to IFC operations through Model Context Protocol tooling. It is relevant to QS3D's future AI integration because it treats model actions as explicit tools instead of giving an LLM unrestricted hidden model mutation.

**QS3D inference:** any future QS3D AI/MCP layer should expose bounded, auditable tools with deterministic validation, authorization and provenance rather than direct arbitrary write access to project state.

## 9. Capability comparison by product class

Legend: `✓` = a central public capability; `~` = partial/adjacent/vendor-dependent; blank = not a central public capability in this scan. This is a **market map**, not runtime certification.

| Product / family | 2D QTO | 3D/BIM QTO | IFC/openBIM | Estimate/BOQ | 4D/schedule | Cost/progress | Rebar/MEP specialist | AI/agentic |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| BLT3D / BLTQS / Neopro | ✓ | ✓ | ~ | ✓ |  | ~ | ~ | ~ |
| Cubicost TAS/TRB/TME/TBQ | ~ | ✓ | ~ | ✓ | ~ | ✓ | ✓ | ~ |
| Glodon QuantifAI | ✓ | ~ |  | ~ |  |  | ~ | ✓ |
| RIB CostX | ✓ | ✓ | ~ | ✓ |  | ~ | ~ | ~ |
| RIB Candy | ✓ | ~ | ~ | ✓ | ✓ | ✓ | ~ |  |
| RIB Presto / iTWO / 4.0 | ~ | ✓ | ✓/~ | ✓ | ✓ | ✓ | ~ | ~ |
| Autodesk Takeoff + Cost | ✓ | ✓ | ~ | ✓ | ~ | ✓ | ~ | ~ |
| Revit | ~ | ✓ | ✓/~ | ~ |  |  | ~ | ~ |
| Bentley SYNCHRO | ~ | ✓ | ✓/~ | ✓ | ✓ | ✓ | ~ | ~ |
| BEXEL Manager | ~ | ✓ | ✓ | ✓ | ✓ | ✓ | ~ | ~ |
| ACCA PriMus IFC | ~ | ✓ | ✓ | ✓ | ~ | ✓ | ~ | ~ |
| Nomitech CostOS | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ~ | ~ |
| Procore Takeoff/Estimating | ✓ | ✓ | ~ | ✓ | ~ | ~ | ~ | ✓/~ |
| OST / PlanSwift | ✓ |  |  | ✓ |  |  | trade templates | ✓/~ |
| STACK | ✓ |  |  | ✓ |  | ~ | trade templates | ✓/~ |
| Bluebeam Revu | ✓ |  |  | ~ |  |  |  |  |
| Trimble estimating portfolio | ✓ | ~ | ~ | ✓ | ~ | ✓/~ | ✓ | ~ |
| Buildsoft Cubit | ✓ | ~ | ~ | ✓ |  | ~ | ~ | ✓/~ |
| InEight Estimate | ~ | ~ | ~ | ✓ | ~ | ✓ | ~ | ~ |
| Cleopatra Enterprise | ~ | ✓ | ~ | ✓ | ✓ | ✓ | ~ | ~ |
| NEVARIS Build | ~ | ✓ | ✓ | ✓ | ~ | ✓ | ~ | ~ |
| ALLPLAN | ✓/~ | ✓ | ✓ | ✓ | ~ | ~ | ✓ | ✓/~ |
| Tekla Structures | ~ | ✓ | ✓ | ~ | ~ |  | ✓ | ~ |
| Vectorworks | ~ | ✓ | ✓ | ~ |  |  | ~ | ~ |
| Solibri |  | ✓ | ✓ | ~ |  | ~ | ~ |  |
| ORCA AVA | ~ | ✓ | ✓ | ✓ | ~ | ✓ | ~ | ~ |
| IfcOpenShell / Bonsai / Ifc5D |  | ✓ | ✓ | ✓/~ | ✓/~ | ~ | ~ | research/tooling |
| Kreo / Caddie | ✓ | ~ |  | ✓ |  | ~ | trade-neutral | ✓ |
| Togal.ai | ✓ |  |  | ~ |  |  | trade-neutral | ✓ |
| Beam AI | ✓ |  |  | ✓ |  | ~ | multi-trade | ✓ |
| Countfire | ✓ |  |  | ✓ |  | ~ | electrical/MEP | ✓ |
| Groundplan | ✓ |  |  | ✓ |  | ~ | multi-trade | ✓/~ |

## 10. What the market consistently values

Across otherwise very different products, several patterns repeat.

### 10.1 Visual traceability

Strong QTO products let a user move from a quantity/result back to the drawing markups or model elements that produced it. This is more important than merely producing a fast total.

### 10.2 One quantity source, many downstream views

The same measured/model-derived quantity should feed:

- grouped quantity reports;
- detailed takeoff;
- BOQ items;
- cost codes/rates;
- estimate versions;
- schedule/activity scopes;
- progress/claim records;
- revision/change reports.

Recomputing each downstream view independently creates inconsistency.

### 10.3 Revision/change handling is a core feature

CostX, Solibri, Countfire, Groundplan, Cubicost and many enterprise platforms emphasize revisions/comparison. QS3D should eventually treat **delta between source revisions** as a first-class quantity event, not only “recalculate everything and overwrite”.

### 10.4 Rules must be inspectable

Localized measurement rules, formulas, assemblies, templates and custom calculations appear throughout the market. QS3D should make every nontrivial rule identifiable, versioned and explainable.

### 10.5 2D and 3D coexist

The market does not show BIM eliminating 2D takeoff. CostX, Procore, ACCA, Nomitech, ConstructConnect and others keep both workflows because tender information frequently arrives as mixed PDF/CAD/BIM packages.

### 10.6 Discipline specialization matters

Rebar, MEP, electrical, steel, earthwork and general building quantities have different semantics. One generic measurement engine can provide primitives, but specialist rules should remain separate modules/services.

### 10.7 AI is moving from suggestion to action

Kreo Caddie and current AI-QTO products point toward **tool-using automation**. The valuable pattern is not unrestricted autonomy; it is automation that executes inspectable measurement/report actions under user control.

## 11. Highest-value references for QS3D specifically

The following products are the most strategically useful clean-room references for `QS3D-BricsCAD`.

### Tier 1 — direct workflow references

1. **BLT3D / BLTQS 2D** — familiar Vietnam QS workflow and command/UX reference.
2. **RIB CostX** — mature combined 2D + BIM takeoff + estimating.
3. **Glodon Cubicost** — localized measurement rules, discipline modules, 3D traceability and BOQ/cost integration.
4. **ACCA PriMus IFC** — openBIM/IFC 5D quantity workflow.
5. **Procore Takeoff / STACK / PlanSwift / OST** — fast practical digital takeoff and estimate handoff.

### Tier 2 — 4D/5D architecture references

1. **RIB Candy / iTWO / Presto** — estimate + planning + valuation/forecast/commercial workflow.
2. **Bentley SYNCHRO** — model/activity/progress/cost linking.
3. **BEXEL Manager** — federated 3D/4D/5D model workflow.
4. **Nomitech CostOS** — enterprise estimate + BIM + schedule integrations.
5. **NEVARIS Build / Cleopatra / InEight** — regional/enterprise cost and project-control patterns.

### Tier 3 — source-model / specialist references

1. **BricsCAD BIM** — actual native host and IFC/data source for this repository.
2. **Revit / Archicad / ALLPLAN / Vectorworks** — authoring-native schedules and model quantities.
3. **Tekla Structures** — rebar/structural/fabrication quantities.
4. **Solibri** — IFC information takeoff, validation and revision comparison.
5. **IfcOpenShell/Bonsai/Ifc5D** — open-source IFC/cost architecture study.

### Tier 4 — AI direction

1. **Kreo Caddie** — approved-plan agent that executes actual takeoff actions.
2. **Glodon QuantifAI** — AI direction from a large traditional 5D vendor.
3. **Togal.ai / Beam AI** — automated plan takeoff with review/output workflow.
4. **Countfire / Groundplan** — domain-specific symbol/count automation.
5. **ALLPLAN Steel Genie** — drawing-to-structured-steel-estimating model.
6. **Handoff-H1 / MCP4IFC** — research frontier for vision agents and bounded model tools.

## 12. QS3D product implications

These are **QS3D INFERENCE** items, not claims about current implementation. Issue #3103 remains responsible for checking current `main` before any “missing/present” statement is made.

### P0 — quantity truth and provenance

Before chasing AI or 5D branding, QS3D should have:

- stable source/model element identity;
- explicit units and conversions;
- rule/version identity;
- contributing dimensions/properties;
- deductions/additions;
- deterministic result;
- source drawing/model revision;
- visual/source navigation;
- calculation explanation.

### P1 — unified 2D + 3D project

A competitive QS workflow should be able to mix:

- native BricsCAD model quantities;
- selected CAD geometry;
- 2D drawing measurements;
- imported/openBIM evidence;
- manually entered/non-modeled BOQ quantities;

without losing provenance or forcing every source into fake 3D geometry.

### P2 — revision and delta model

A revision-aware quantity engine should answer:

```text
What changed?
Which source objects changed?
Which quantities changed?
Which BOQ/cost items are affected?
Which previous estimate/claim used the older result?
```

Historical snapshots must remain reproducible.

### P3 — cost/WBS/rate foundation

Quantity and cost should be linked but independent:

```text
QuantityFact(versioned)
  -> WorkItem / WBS / CostCode mapping
  -> RateSet(versioned)
  -> EstimateSnapshot
```

Changing a rate must not rewrite historical geometry/quantity evidence.

### P4 — 4D/progress/claim layer

Later 5D work should add:

- activity links;
- zones/work packages;
- baseline vs actual progress;
- dated quantity/progress snapshots;
- previous/current/cumulative claims;
- change/variation impact;
- forecast / earned-value style reporting where appropriate.

### P5 — discipline engines

Maintain focused quantity contracts for:

- architecture/structure;
- rebar;
- MEP/electrical;
- curtain/facade;
- room finishes;
- earthwork/geotechnical;
- steel/fabrication where product scope allows.

### P6 — openBIM adapters

IFC/openBIM should normalize into vendor-neutral quantity evidence with explicit:

- source file/model identity;
- GlobalId/object identity;
- entity/classification;
- property-set provenance;
- unit normalization;
- geometric vs declared quantity distinction;
- mapping diagnostics and loss reporting.

### P7 — bounded AI assistant / agent

The strongest architecture direction from current AI products is:

```text
natural-language intent
  -> proposed action plan
  -> user authorization
  -> bounded QS3D tools
  -> deterministic validation
  -> visible source annotations/results
  -> audit record
```

Do not allow an LLM to silently invent final quantities, overwrite project facts or bypass semantic/range/version checks.

## 13. Product-boundary guardrails for this repository

This research does not change `docs/PRODUCT-BOUNDARY.md`.

`QS3D-BricsCAD` remains:

- a BricsCAD V25/V26 hosted plugin;
- dependent on BricsCAD for native DWG database/view/editor/selection lifecycle;
- owner of QS3D semantic/project/quantity/reporting workflows inside that host;
- separate from standalone `QS3D-CAD`;
- able to share vendor-neutral contracts progressively through `QS3D-Platform` when deliberately migrated.

Competitor standalone packaging, Revit add-ins, proprietary format parsers or cloud architectures are **references**, not automatic requirements for this repository.

## 14. Relationship to existing self-claim research lanes

This global landscape should feed the already-created lanes rather than duplicate them:

- **#3099** — BLT3D verified feature + command inventory;
- **#3101** — verified BIM5D 3D/4D/5D model;
- **#3102** — DWG/DXF/IFC/Revit interoperability mapping;
- **#3103** — current QS3D vs reference-product gap matrix;
- **#3104** — QS3D 5D domain architecture;
- **#3105** — estimating/progress-claim/reporting UX.

Those Issues remain independently self-claimable. This lane does not take ownership of them.

## 15. Source ledger

### BLT / baseline

- BLT SOFTWARE: <https://www.thangblt.com/>
- Local benchmark: [`BLT3D-BIM5D-BENCHMARK.md`](BLT3D-BIM5D-BENCHMARK.md)

### Glodon

- Cubicost / 5D BIM: <https://www.glodon.com/en/solutions/5d-bim-digital-cost-management-solution-7>
- Cubicost TAS/TRB: <https://www.glodon.com/en/products/tas-%26-trb-1>
- Glodon 2026 digital/AI product news: <https://www.glodon.com/en/news>

### RIB

- RIB CostX: <https://www.rib-software.com/en/rib-costx>
- RIB Candy: <https://www.rib-software.com/en/rib-candy>
- RIB Presto: <https://www.rib-software.com/en/rib-presto>
- RIB iTWO resources: <https://www.rib-software.com/en/client-resources/rib-itwo>
- RIB product portfolio: <https://www.rib-software.com/en>

### Autodesk

- Autodesk Takeoff help: <https://help.autodesk.com/view/BUILD/ENU/?guid=Takeoff_Overview>
- Autodesk 5D BIM: <https://www.autodesk.com/solutions/5d-bim>
- Autodesk Cost Management: <https://help.autodesk.com/cloudhelp/ENU/BIM360D-Cost-Management/files/BIM360D_Cost_Management_about_cost_management_html.html>
- Revit material takeoff schedules: <https://help.autodesk.com/view/RVT/2026/ENU/>

### Bentley

- SYNCHRO: <https://www.bentley.com/software/synchro/>

### BEXEL

- BEXEL Manager: <https://bexelmanager.com/>

### ACCA

- PriMus IFC: <https://www.accasoftware.com/en/5d-bim-software>
- PriMus TAKEOFF: <https://www.accasoftware.com/en/quantity-takeoff-software>
- PriMus family: <https://www.accasoftware.com/en/cost-estimating-software>

### Nomitech

- CostOS: <https://www.nomitech.com/costos/>

### Procore

- Procore Estimating / Takeoff: <https://www.procore.com/preconstruction/estimating>

### ConstructConnect

- On-Screen Takeoff: <https://www.constructconnect.com/products/on-screen-takeoff>
- PlanSwift: <https://www.constructconnect.com/products/planswift>
- ConstructConnect estimating portfolio: <https://www.constructconnect.com/>

### STACK

- STACK Takeoff & Estimate: <https://www.stackct.com/>

### Bluebeam

- Bluebeam Revu quantity takeoff: <https://support.bluebeam.com/>

### Trimble

- Trimble estimating/takeoff portfolio: <https://construction.trimble.com/en/solutions/estimating-and-takeoff>
- WinEst: <https://construction.trimble.com/en/products/winest>

### Buildsoft

- Cubit Estimating: <https://www.buildsoft.com.au/cubit-estimating/>

### InEight

- InEight Estimate: <https://ineight.com/products/estimate/>

### Cleopatra

- Cleopatra Enterprise: <https://cleopatraenterprise.com/>

### NEVARIS

- NEVARIS Build: <https://www.nevaris.com/produkte/build/>
- NEVARIS Build BIM: <https://www.nevaris.com/produkte/build/bim/>
- NEVARIS 2026.1 BIM/cost update: <https://www.nevaris.com/?presse=nevaris-2026-1-intelligente-integration-fuer-durchgaengige-bauprozesse>

### BIM authoring / model sources

- BricsCAD BIM: <https://help.bricsys.com/>
- Autodesk Revit: <https://help.autodesk.com/view/RVT/2026/ENU/>
- Graphisoft Archicad 29: <https://help.graphisoft.com/AC/29/INT/>
- ALLPLAN 2026: <https://www.allplan.com/us_en/system/releasenotes/2026/allplan-2026/>
- ALLPLAN quantity/cost editions: <https://www.allplan.com/us_en/package-overview/compare-allplan-features-2026/>
- ALLPLAN Steel Genie: <https://www.allplan.com/press-reports/press-report/allplan-introduces-steel-genie-ai-powered-estimating-software-for-structural-steel/>
- Tekla Structures: <https://support.tekla.com/>
- Vectorworks 2026 worksheets: <https://app-help.vectorworks.net/2026/eng/VW2026_Guide/Worksheets/Concept_Worksheet_overview.htm>

### openBIM / model analysis

- buildingSMART technical standards: <https://technical.buildingsmart.org/>
- IFC 4.3 documentation: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/>
- Solibri Information Takeoff: <https://help.solibri.com/>
- ORCA AVA: <https://www.orca-software.com/>
- IfcOpenShell: <https://ifcopenshell.org/>
- IfcOpenShell documentation: <https://docs.ifcopenshell.org/>

### AI / agentic takeoff

- Kreo: <https://www.kreo.net/>
- Kreo Caddie documentation: <https://help-takeoff.kreo.net/en/articles/7895351-caddie-ai>
- Togal.ai: <https://www.togal.ai/>
- Beam AI: <https://www.ibeam.ai/>
- Countfire: <https://www.countfire.com/>
- Groundplan: <https://groundplan.com/>

### Research frontier

- Handoff-H1: <https://arxiv.org/abs/2608.15032>
- MCP4IFC: <https://arxiv.org/abs/2511.05533>

## 16. Maintenance rule

This landscape is dated evidence, not a permanent truth table. Product names, ownership, packaging, AI claims and feature boundaries will change. Future updates should:

1. prefer current official sources;
2. record the research date;
3. separate vendor claims from independently verified facts;
4. remove/replace stale claims rather than layering contradictory statements;
5. never turn competitor marketing into an implementation requirement without checking QS3D product boundary, user need and current source reality.

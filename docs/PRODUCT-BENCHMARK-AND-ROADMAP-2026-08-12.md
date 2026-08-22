# QS3D Product Benchmark, Business-Logic Gaps, and Roadmap — 2026-08-12

> **Status:** Advisory / non-canonical product and architecture note.
>
> This document captures the complete product-review discussion from the 2026-08-12 ChatGPT session. It records a dated assessment, benchmark lessons, business-logic gaps, and recommended roadmap. It is **not** implementation-completion truth and it does **not** promote managed-code coverage to native BricsCAD production readiness.
>
> Canonical repository truth remains in:
>
> - `docs/PRODUCT-BOUNDARY.md`
> - `docs/IMPLEMENTATION-STATUS.md`
> - `docs/PLAN.md`
> - runtime/native qualification evidence
> - current work claims under `docs/agent-work-claims/`
>
> If this note conflicts with current source, canonical docs, tests, or native evidence, **current repository truth wins**.

## 1. Executive thesis

QS3D should **not** become “Revit inside BricsCAD.” Its strongest position is a specialist **QS semantic engine for BricsCAD/DWG**: model semantics, measurement logic, explainable quantities, revision intelligence, estimating, rebar/BBS, QA, documentation, and interoperability on top of a mature CAD host.

The review conclusion was that QS3D is no longer merely a collection of takeoff commands. The repository already contains enough semantic, geometry, quantity, reporting, rebar, revision, health, documentation, and interchange infrastructure that the next value step is not indiscriminate breadth. The highest-leverage work is to make quantities **trustworthy, explainable, editable, versioned, revision-aware, cost-aware, interoperable, and native-runtime qualified**.

Product principle:

> **One canonical quantity truth, one explainable measurement path, one cost truth, stable identity across revisions, and explicit evidence for every production-readiness claim.**

Recommended product chain:

```text
DWG / PDF / IFC
        ↓
Recognize / Capture / Direct Draw
        ↓
Semantic Model
Family + Instance + Floor + Zone + Host + Material
        ↓
Model Health / QS Rules
        ↓
Measurement Facts
        ↓
Measurement Rules
        ↓
Explainable Quantities
        ↓
Classification / Work Item Mapping
        ↓
BOQ / BQ / BBS / Schedules
        ↓
Rates / Estimate
        ↓
Revision Quantity Delta + Cost Delta
        ↓
DWG Tables / Excel / IFC / BCF / Interchange
```

## 2. Dated assessment scorecard from the review session

These scores are a **subjective product/architecture assessment snapshot**, not automated repository metrics and not native qualification results.

| Area | Review score |
|---|---:|
| Core architecture / separation | **8.5 / 10** |
| Semantic BIM model | **8.0 / 10** |
| Geometry → semantic → regeneration | **8.0 / 10** |
| QS quantity business logic | **7.5 / 10** |
| Architectural / structural workflow | **7.5 / 10** |
| Rebar / BBS | **7.0 / 10** |
| Native CAD editing experience | **6.0 / 10** |
| IFC / openBIM / interoperability | **5.5 / 10** |
| Estimating / rates / 5D cost | **4.5 / 10** |
| Collaboration / enterprise workflow | **3.5–4.0 / 10** |
| Overall product maturity from source review | **~7 / 10** |

Interpretation: the strongest areas are the deterministic Core direction and semantic/quantity foundation. The largest product gaps are not basic modeling commands; they are native edit UX, large-model/runtime qualification, explainable measurement semantics, estimating/cost, openBIM production workflows, and collaboration.

## 3. Working source-of-truth model used in the review

The session used the following architecture model to reason about QS3D. Verify it against current canonical docs before implementing migrations or persistence changes.

```text
DWG geometry / native entities
        ↓ geometry/source handles
Semantic Project / .qsdb
        ↓ family / instance / relationships
Dependencies + regeneration
        ↓
Canonical calculated quantities
        ↓
BQ / BOQ / BBS / schedules / Excel / caches / interchange projections
```

Working principles:

- **DWG** is the native geometry/source environment.
- **`.qsdb` / semantic project state** is the semantic business-model source rather than a report cache.
- Family/instance, floor/zone, host/opening, material, provenance, and related semantic identities should be explicit domain data.
- Quantities should be deterministic derived results of canonical semantic/geometry inputs and measurement rules.
- BQ/BOQ/BBS/schedules/XLSX/cache outputs should be **derived projections**, not independent hidden quantity engines.
- A renderer/exporter must not silently invent deductions, conversions, rounding, or cost logic.
- Stable identity must survive reporting, revision comparison, and interchange wherever the underlying business object remains the same.

This is the basis for the recurring review rule: **do not fix a reporting problem by adding a second business-logic path inside reporting**.

## 4. Current capability inventory observed in the review

The review found a broad product surface. Presence here means the capability or its building blocks were observed in source/docs during the review; it does **not** mean every path is native-qualified on every BricsCAD version.

### 4.1 Project and semantic model

- Workspace / Project lifecycle.
- Zone / Level / Floor concepts.
- Family / Type / Instance concepts.
- Material and semantic properties.
- Stable metadata, source handles, ownership/provenance hardening.
- Dependency and regeneration patterns.
- Preview/diff/safe mutation patterns.
- Project persistence and revision-related infrastructure.

### 4.2 Architectural and structural modeling

- Direct Draw / semantic creation workflows.
- Wall.
- Beam.
- Column.
- Slab / Floor.
- Foundation.
- Openings / Door-related workflows.
- Auto Host / host-opening relationships and boolean-cut related behavior.
- Room / finish workflows.
- Curtain-related modeling.
- Grid and structural/documentation geometry.

### 4.3 Quantity, reporting, and documentation

- Quantity-oriented Core services and `QuantityRules` concepts.
- Measured geometry / solid quantity paths.
- BQ / BOQ-oriented outputs.
- Schedules and reports.
- ED2/BQ-related workflow surfaces discussed in the review.
- XLSX/Excel projections.
- Documentation / annotation / drawing-production workflows.
- Revision/audit/diff foundations.

### 4.4 Rebar

- Rebar-specific domain logic rather than only generic solids.
- Rebar quantity logic.
- BBS/reporting foundations.
- Structural/rebar workflows substantial enough to benchmark against Tekla and Cubicost TRB rather than generic CAD.

### 4.5 Health, validation, and interchange

- Semantic health / validation services.
- Release/preflight/health concepts.
- Semantic interchange, import/export, preview/diff, provenance and remapping foundations.
- Existing work toward stable source identity across model/report/interchange.

### 4.6 Important qualification caveat

Managed build/test success and repository implementation depth are not equivalent to native BricsCAD production qualification. Native-dependent commands, adapters, drawing effects, transactions, custom objects, persistence/reopen behavior, and host-version behavior require named runtime evidence.

Product/status reporting should distinguish at least:

1. implemented in source;
2. covered by deterministic managed tests/smokes;
3. adapter/integration exercised;
4. native-host qualified on a named BricsCAD version/environment;
5. production-ready according to an explicit acceptance gate.

## 5. How to benchmark QS3D correctly

AutoCAD, BricsCAD, BLT3D, Revit, CostX, Cubicost, Tekla, Solibri, and Autodesk Takeoff are not the same product category. QS3D should benchmark each for the capability it is actually good at.

| Benchmark group | Products / references | What QS3D should learn |
|---|---|---|
| CAD host / drafting UX | AutoCAD, BricsCAD | Native selection, grips, MOVE/ROTATE/STRETCH behavior, speed, DWG ergonomics |
| BIM semantic authoring | Revit, BricsCAD BIM, Archicad | Family/type/instance semantics, relationships, schedules, native editing |
| QS / QTO / estimating | BLT3D, Glodon Cubicost TAS/TRB/TME/TBQ, RIB CostX, Autodesk Takeoff | Measurement rules, deductions, trace-back, 2D/3D takeoff, estimating, revision impact |
| Structural / rebar | Tekla Structures, Cubicost TRB | Rebar semantics, BBS, shape/grouping, fabrication-oriented quantity intelligence |
| QA / federation / openBIM | Solibri, Navisworks | Rule-based checking, issue workflows, federation, clash/BCF patterns |
| Open-source IFC reference | IfcOpenShell / Bonsai | IFC identity, relationships, QTO, classifications, BCF, IDS, cost/QTO integration |

The most relevant direct product benchmark set for QS3D is:

> **Cubicost → CostX → BLT3D → Revit → Tekla → Solibri → Autodesk Takeoff → BricsCAD BIM → IfcOpenShell/Bonsai**

AutoCAD remains primarily a **CAD UX benchmark**, not the closest QS competitor.

## 6. Full competitor/reference lessons from the session

This section preserves the capability-level lessons discussed in the session. It is not an instruction to clone competitors.

### 6.1 AutoCAD

Use AutoCAD as the reference for predictable native CAD editing and user muscle memory:

- selection behavior;
- MOVE / ROTATE / STRETCH;
- grips;
- command feedback;
- large-DWG responsiveness;
- minimal friction between inspection and editing.

QS3D should preserve those expectations while keeping semantic provenance and quantities consistent.

### 6.2 BricsCAD / BricsCAD BIM

BricsCAD is both the host and a BIM benchmark. The review identified strengths to learn from:

- native DWG runtime maturity;
- IFC/openBIM workflows;
- BIM classifications/properties;
- quantity/schedule extraction;
- native BIM editing.

QS3D should **not** duplicate the whole BricsCAD BIM platform. Its differentiation should be deeper QS semantics, measurement explanations, BOQ mapping, revision/cost intelligence, and specialist rebar/QS workflows.

### 6.3 Revit

Revit is the semantic authoring benchmark for:

- category / family / type / instance;
- level/host relationships;
- materials/parameters;
- schedules and material takeoff;
- native parametric edit/regeneration.

QS3D already follows several useful semantic patterns. The major UX gap identified in the session is the seamless preservation of provenance/dependencies/quantities during normal edit operations.

Target behavior example:

```text
MOVE beam
  → semantic position changes
  → joins/dependencies invalidate
  → affected geometry regenerates
  → quantities update
  → BQ/revision delta updates
```

without requiring the user to understand an internal synchronization mechanism.

### 6.4 BLT3D

BLT3D was identified as a close Vietnam-market benchmark because it is quantity-focused and works with 3D/BIM workflows rather than being only a CAD platform.

The important comparison is not command count. QS3D should compete on:

- in-DWG semantic modeling plus takeoff;
- structural/architectural quantity rules;
- traceability from quantity to model;
- revision awareness;
- local QS workflows;
- eventually cost/rate integration.

### 6.5 Glodon Cubicost

Cubicost is arguably the most important business-logic benchmark discussed. Key patterns:

- local measurement-rule profiles;
- automatic deductions;
- quantity trace-back to model;
- visible calculation expressions;
- architecture/structure quantities;
- dedicated reinforcement workflows;
- MEP and billing/cost ecosystem in the broader product family;
- revision-oriented work.

QS3D implication: deepen `QuantityRules` into **versioned measurement semantics** rather than adding more report-specific formulas.

### 6.6 RIB CostX

CostX represents the next layer beyond model quantity:

```text
Drawing / Model
    ↓
Quantity
    ↓
Estimate / Rate
    ↓
Cost
    ↓
Revision impact
```

QS3D should therefore avoid stopping at Excel export. A small, clean native cost domain is a strategic next step once quantity truth is stable.

### 6.7 Autodesk Takeoff

Useful patterns:

- unified 2D and 3D takeoff;
- takeoff types / classification;
- formulas;
- package/inventory workflow;
- version-controlled source documents;
- unit-cost linkage.

QS3D implication: do not require every job to start from a perfect 3D semantic model. A future 2D takeoff path should be able to coexist with and potentially upgrade into semantic objects.

### 6.8 Tekla Structures

Tekla is the stronger reference for detailed structural/rebar semantics and reporting. QS3D does not need Tekla-level fabrication authoring to gain value, but should learn from:

- bar grouping and identity;
- shape codes;
- bends/hooks;
- laps/splices/couplers;
- anchorage/development length;
- mesh and accessories;
- BBS consistency;
- revision-aware reinforcement reporting.

### 6.9 Solibri

Use Solibri as the QA/reference model for:

- declarative model checks;
- evidence-rich findings;
- property/classification validation;
- IDS-style requirements;
- BCF issue workflow;
- quantity/model review.

QS3D should evolve existing Semantic Health into a **QS-specific rule engine** before attempting a full clash/federation platform.

### 6.10 IfcOpenShell / Bonsai

IfcOpenShell/Bonsai are valuable open references for:

- IFC2x3/IFC4/IFC4.3 handling;
- IFC identity and relationship graphs;
- validation;
- classifications;
- QTO;
- BCF;
- IDS;
- model diff;
- 4D/5D-related data structures;
- open implementation patterns around cost/QTO.

Use them as architectural references, not as code to copy blindly.

### 6.11 External benchmark references already used in the first review note

- Glodon Cubicost TAS: <https://www.glodon.com/en/products/cubicost-tas-8>
- RIB CostX: <https://www.rib-software.com/en/rib-costx>
- Autodesk Takeoff: <https://construction.autodesk.com/products/autodesk-takeoff/>
- Solibri: <https://www.solibri.com/>

## 7. Product maturity matrix

| Capability | Current posture from this review | Desired target | Priority |
|---|---|---|---|
| Semantic identity / source provenance | Strong foundation; active hardening | Stable identity across model/report/revision/interchange | P0 |
| Native semantic editing | Important gap vs CAD/BIM authoring UX | MOVE/ROTATE/STRETCH/grip-safe semantic edits with regeneration | **P0** |
| Large-model performance | Not sufficiently qualified for production confidence | Versioned performance budget + large real-project qualification | **P0** |
| Geometric quantity extraction | Broad managed implementation | Deterministic + host-qualified measurement basis | P0 |
| Measurement semantics | Existing rule concepts | Versioned standards/rules/deductions/rounding | P0 |
| Quantity explainability | Fragmented information, no universal trace contract | Every reportable quantity has machine + human trace | P0 |
| Classification → BOQ mapping | Foundations exist but should be first-class | Explicit classification/work-item mapping and coverage | P0–P1 |
| Revision quantity delta | Identity/diff foundations exist | First-class added/removed/changed quantity ledger | P0–P1 |
| Rate / cost estimating | No dedicated end-to-end cost domain identified in review | Rate book + estimate snapshot + revision cost impact | P1 |
| 2D + 3D takeoff convergence | 3D/semantic direction stronger than 2D workflow | PDF/DWG 2D takeoff that can coexist with semantic model | P2 |
| Rule-based QS QA | Semantic health/validation exists | Declarative checker profiles with evidence/safe fixes | P2 |
| IFC/openBIM round-trip | Interchange foundations exist | Stable identity/property/QTO/classification round-trip | P2 |
| BCF/issue workflow | Not a complete first-class loop in this review | Findings linked to model identities and exchangeable issues | P2 |
| Collaboration / cloud | Limited vs enterprise platforms | Only add where concrete team workflow requires it | P2–P3 |
| MEP QS | Gap for broad QS coverage | Specialist module only after core quantity architecture is mature | P3 |
| Civil / earthwork depth | Some direction exists; not primary benchmark strength | Deepen when validated by real QS workflows | P3 |

## 8. Business-logic gap #1 — Explainable measurement semantics

This was the highest-priority business-logic recommendation.

Every number should answer:

> **“Why is this quantity exactly this value?”**

Candidate domain concepts:

- `MeasurementStandard`
- `MeasurementRuleSet`
- `MeasurementRuleVersion`
- `MeasurementContext`
- `MeasurementFact`
- `QuantityExpression`
- `DeductionRule`
- `OpeningTreatment`
- `RoundingRule`
- `WasteRule`
- `AggregationRule`
- `MeasurementTrace`
- `MeasurementSnapshot`

A trace should contain where applicable:

- semantic/source identity;
- geometry/property inputs;
- gross basis;
- deductions/additions and reasons;
- net quantity;
- rule ID/version;
- units/conversion path;
- rounding policy;
- normalized expression;
- warnings/assumptions/fallbacks;
- related source elements.

Example discussed in the session:

```text
Wall W-103
Gross Area        = 28.40 m²

Deduction:
Door D-17         = -2.10 m²
Window W-09       = -1.80 m²
Column overlap    = -1.03 m²

Net Wall Area     = 23.47 m²

Measurement Rule = VN-WALL-MASONRY-v3
Sources           = W-103, D-17, W-09, C-04
```

The product should expose **Quantity + provenance + formula + rule + deductions + revision**, not just `Quantity = 23.47`.

Requirements:

- deterministic for identical canonical inputs;
- culture-invariant at persistence/exchange boundaries;
- units explicit;
- historical rule version preserved;
- local/trade/company profiles supported without forking the engine;
- missing/ambiguous inputs fail visibly rather than silently becoming zero.

## 9. Business-logic gap #2 — Measurement-rule depth

To compete with BLT3D/Cubicost-style QS workflows, the important work is deeper measurement intelligence such as:

- gross vs net;
- opening deductions;
- wall/column/slab/beam intersection treatment;
- finish deductions;
- contact area;
- formwork area;
- construction joint rules;
- host/opening effects;
- rebar lap/splice rules;
- waste factors;
- rounding;
- standard/company-specific rule profiles.

The architecture should separate:

```text
Geometry
   ↓
Semantic Element
   ↓
Measurement Facts
   ↓
Measurement Rule
   ↓
Quantity Breakdown
   ↓
Classification / Work Item
   ↓
BOQ Item
   ↓
Rate / Resource
   ↓
Cost
```

`Measurement Facts` and `Measurement Rule` should be distinct so the same model can be measured under different valid standards without rebuilding geometry logic.

## 10. Business-logic gap #3 — Classification, BOQ mapping, and coverage

Introduce an explicit mapping layer:

```text
Element
 ↓
Classification
 ↓
Measurement Item
 ↓
BOQ Item / Work Item
```

Do not hard-code BOQ mapping inside individual geometry categories.

Recommended project coverage view:

```text
Total model elements:      8,420
Quantity-ready:            7,961
Missing classification:      178
Missing rule:                109
Stale quantity:               67
Ambiguous host:               31
Invalid geometry:             74

BOQ Coverage:               94.5%
```

The exact numbers above are illustrative. The product value is the coverage concept: a QS user must know what is **not yet included or trustworthy**.

## 11. Business-logic gap #4 — Revision → quantity → cost

Stable identity should support a first-class revision ledger.

Target workflow:

1. capture a canonical measurement snapshot;
2. capture/freeze the rate snapshot used for an estimate;
3. regenerate/import the revised state;
4. classify identities as added/removed/unchanged/changed;
5. calculate quantity deltas;
6. explain why the quantity changed;
7. separate rule-version change from geometry/property change;
8. propagate to cost impact;
9. export a reviewable report without independent renderer math.

Target UX question:

> **“Why did concrete increase by 14.2 m³?”**

The user should be able to click the delta and highlight the responsible objects.

Suggested outputs:

- previous/current quantity;
- delta;
- old/new rule version;
- old/new rate version;
- quantity-driven vs rate-driven cost delta;
- source identities/handles;
- unresolved identity warnings.

## 12. Business-logic gap #5 — Cost and estimating domain

QS3D should separate **measurement truth** from **commercial assumptions**.

Candidate concepts:

- `RateBook`
- `RateItem`
- `CostCode`
- `ResourceRate`
- `EstimateLine`
- `EstimateSnapshot`
- `EstimateRevision`

An estimate line should distinguish:

- measured quantity;
- estimating quantity;
- waste/loss factor;
- unit rate;
- currency;
- rate effective date/version;
- direct cost;
- labour/material/equipment/subcontract split where supported;
- overhead/markup/contingency where supported;
- final amount;
- source measurement snapshot/trace.

Target chain:

```text
Quantity
 × Unit Rate
 = Direct Cost
 + Waste / Labour / Equipment / Subcontract
 + Overhead / Markup
```

Do not store tender/rate assumptions in geometric entities simply because a report needs them.

## 13. Business-logic gap #6 — QS rule checker

Build on Semantic Health rather than introducing a parallel validation engine.

A rule should include:

- stable rule ID;
- profile/category;
- severity;
- deterministic condition;
- human explanation;
- affected semantic identities;
- evidence values;
- optional safe autofix only if deterministic and previewable.

Example rules:

```text
QS-WALL-001
Every ArchitecturalWall must have:
- Family
- Floor
- Zone
- Material
- Thickness > 0
- Height > 0

QS-OPENING-002
Every Door must:
- resolve exactly one Host
- be geometrically valid for the host
- have Width > 0
- have Height > 0

QS-QTY-004
Every BQ-included element must:
- have a valid measurement rule
- have classification / mapping
- have no stale calculated quantity
```

Other high-value findings:

- invalid/non-finite inputs;
- malformed semantic/source metadata;
- missing family/level/floor relationships;
- inconsistent units;
- missing rate;
- stale snapshot/report;
- report value without canonical trace;
- revision line that cannot reconcile identity.

IDS/BCF can be integrated later where useful, but should not block the core QS checker.

## 14. Native semantic editing — P0, not a later polish item

The original review explicitly treated native edit behavior as a major gap and high priority.

Target workflows include:

- MOVE;
- ROTATE;
- STRETCH;
- grip edits;
- jigs/direct manipulation;
- batch property edits.

Required invariant:

```text
Native edit
  → semantic state remains valid
  → provenance remains valid
  → dependencies invalidate deterministically
  → generated geometry regenerates
  → quantities refresh
  → reports/revision state detect the change
```

The user should not need a special recovery/sync mental model for ordinary edits.

Batch actions should show:

- selected count;
- applicable count;
- skipped count/reasons;
- warnings;
- previewed geometry/quantity/report effects;
- deterministic apply result.

Avoid silent mutation.

## 15. Large-model performance and native qualification — P0

The session explicitly identified large-model/runtime qualification as a high-priority gap. It must **not** be hidden in P3.

Performance/qualification work should cover representative real-project sizes and the workflows that dominate QS usage, such as:

- project open/load/save/reopen;
- semantic indexing;
- room/topology paths;
- curtain paths;
- BQ/schedule generation;
- rebar/BBS;
- regeneration;
- native editing;
- revision comparison;
- quantity trace generation.

Maintain an explicit host qualification matrix:

- BricsCAD version/build;
- .NET/runtime;
- command/object surface;
- fixture/drawing;
- model size/count;
- expected native effect;
- observed effect;
- transaction/persistence/reopen result;
- timing/memory where relevant;
- evidence artifact/log;
- date;
- pass/fail/blocked;
- known host deviation.

Managed tests remain necessary but are not substitutes for named V25/native evidence.

## 16. 2D + 3D takeoff convergence

The review recommended a future 2D takeoff path because many real QS inputs are PDFs or 2D DWGs rather than clean BIM.

Candidate 2D takeoff primitives:

- count;
- length;
- perimeter;
- area;
- zone/package;
- measurement group;
- custom formula;
- classification/work-item mapping.

Important design idea:

> 2D takeoff does not have to remain a dead-end annotation. Where possible, it should be upgradeable/linkable to a semantic object later.

This lets QS3D support imperfect project inputs without weakening the semantic architecture.

## 17. Rebar roadmap details from the session

QS3D already has enough rebar/BBS foundation to justify deeper specialist logic.

Potential improvements:

- canonical bar shapes;
- shape codes;
- bends/hooks;
- lap/splice rules;
- couplers;
- anchorage/development length;
- bar grouping/marks;
- mesh;
- chairs/spacers where business value is clear;
- waste/cutting optimisation;
- revision delta.

Example future cutting optimisation:

```text
Stock bar: 11.7 m
Required cuts: 4.5 m + 4.5 m + 2.4 m

→ choose cutting pattern
→ calculate off-cut
→ calculate waste %
→ procurement quantity/weight
```

This is a QS/contractor differentiator that does not require QS3D to become a full Tekla replacement.

## 18. IFC / openBIM / BCF direction

The review recognized that semantic interchange work already exists, so the statement is **not** “QS3D has no interoperability.” The gap is production-grade round-trip and ecosystem depth.

Target identity chain:

```text
QS3D Element
↕
IFC GlobalId
↕
IfcClass
↕
Pset
↕
Qto
↕
Classification
↕
Cost Item
```

Critical requirement: round-trip should preserve identity and business meaning wherever technically possible; “can export an IFC file” is not enough.

BCF/issue direction:

```text
Model Health finding
  → Create review issue
  → linked semantic IDs + viewpoint/evidence
  → assigned/status workflow
  → export/import BCF where useful
```

## 19. MEP and civil/earthwork

These remain valid future domains but should not displace P0/P1 quantity trust and edit/runtime work.

### MEP QS candidates

- pipe;
- duct;
- cable tray;
- conduit;
- fittings;
- valves;
- equipment;
- insulation;
- accessories.

### Civil / earthwork candidates

- existing/design surfaces;
- cut/fill;
- excavation zones;
- trench;
- slope;
- disposal;
- backfill;
- swell/shrink;
- haul.

Add these only when they can reuse canonical identity, measurement, trace, edit, reporting, and revision architecture.

## 20. Recommended product architecture

### Layer 1 — Deterministic Core domain

Owns semantic identity, geometry-independent invariants, units, canonical serialization, quantity primitives, and deterministic validation/calculation contracts.

### Layer 2 — Measurement domain

Owns facts, standards, rules, deductions, rounding, traces, and measurement snapshots.

### Layer 3 — Estimate / cost domain

Owns rate books, cost codes, estimate lines/snapshots, commercial adjustments, and revision cost impact.

### Layer 4 — BricsCAD host adapters

Owns entity access, native transactions, geometry extraction/application, document state, selection, custom-object integration, and host-version behavior. It must not become the home of canonical QS policy.

### Layer 5 — Command/UI orchestration

Owns inspect/edit/preview/apply, batch operations, and “why this quantity?” workflows. UI calls domain services; it does not reproduce measurement/cost formulas.

### Layer 6 — Reporting / export projections

BOQ/BQ/BBS/schedules/XLSX/interchange/IFC-facing projections consume canonical quantities/snapshots. Renderers do not create hidden alternate truths.

### Layer 7 — Verification and evidence

Owns deterministic tests, adapter smokes, native qualification, large-model budgets, trace completeness, stale-state detection, and release evidence.

Architecture invariants:

1. One canonical quantity truth.
2. One canonical cost truth for a selected frozen estimate/rate snapshot.
3. Stable identity across model/report/revision/interchange.
4. Reports do not secretly implement measurement math.
5. Rule versions and units are explicit at persistence boundaries.
6. Host dependence is explicit and isolated.
7. Edit/regeneration effects are deterministic and reviewable.
8. Native readiness claims require native evidence.

## 21. Keep, evolve, merge, avoid duplicating

### Keep

- `QuantityRules` as a measurement foundation.
- semantic metadata and health/validation.
- canonical source-handle/provenance identity.
- preview/diff/regeneration.
- BQ/BOQ/BBS/schedule families as projections.
- Core/host separation.

### Evolve

- quantity rules → versioned measurement semantics;
- semantic health → QS checker profiles;
- source provenance → revision/quantity/cost trace;
- preview/diff → default edit/batch-edit workflow;
- reports → pure canonical projections;
- classification → explicit BOQ/work-item mapping;
- interchange → stable round-trip identity/provenance.

### Merge/prevent duplication

- no second quantity engine for reports/exports/categories;
- no report-only deductions/rounding/conversions;
- no second semantic identity scheme;
- no tender/rate assumptions hidden in geometry;
- no BricsCAD adapter policy replacing Core measurement rules;
- consolidate duplicate stale-output, conversion, deduction, rounding, or report calculation helpers when found by targeted audit.

## 22. Product anti-goals

QS3D should **not** optimize for:

- becoming a full Revit clone;
- broad architectural authoring parity only for checklist optics;
- a full MEP routing/fabrication suite before core QS value is mature;
- a cloud CDE/document-management replacement;
- a rendering/general-design platform;
- a full Solibri/Navisworks clash platform before QS-specific checking;
- duplicate measurement engines by category/report;
- categories added only to inflate feature count;
- paper-completed native features without host evidence;
- commercial assumptions embedded into geometry for convenience.

Product position to preserve:

> **BricsCAD handles CAD. QS3D supplies semantic QS intelligence.**

## 23. Prioritized roadmap

### P0-A — Native correctness, editing, and scale

Goal: ordinary CAD editing and real project size must not break semantic/QS truth.

Deliverables:

- native semantic MOVE/ROTATE/STRETCH/grip-safe editing;
- deterministic dependency/regeneration after edits;
- inspector and preview/apply loop;
- large-model performance budgets;
- representative native V25 qualification matrix;
- persistence/reopen evidence for critical workflows;
- stale-output and identity-integrity hardening.

### P0-B — Trust and explain the quantity

Goal: a QS user can inspect and reproduce every important number.

Deliverables:

- canonical `MeasurementTrace`;
- rule identity/versioning;
- gross/deduction/addition/net trace;
- units and rounding trace;
- provenance convergence;
- quantity coverage findings;
- classification/work-item mapping foundation;
- measurement snapshot/delta foundation.

P0 exit criteria:

- high-value reportable quantities have canonical trace;
- same canonical input + rule version gives same result;
- reports do not need hidden recomputation;
- native edit paths keep semantic/quantity state consistent;
- representative large models have named performance/runtime evidence;
- identity ambiguity fails visibly.

### P1 — Revision and estimating

Goal: convert trusted quantity into reproducible commercial impact.

Deliverables:

- quantity revision ledger;
- `RateBook` / `RateItem` / `CostCode`;
- `EstimateLine` / estimate snapshot;
- waste/commercial adjustment separation;
- rate effective date/version/currency;
- revision quantity → cost impact;
- frozen estimate/BQ export.

### P2 — QA, openBIM, and mixed-source takeoff

Goal: make project review and imperfect real-world inputs manageable.

Deliverables:

- declarative QS checker profiles;
- severity/evidence/autofix-preview;
- stale snapshot/report detection;
- 2D PDF/DWG takeoff primitives;
- semantic upgrade/link path where feasible;
- stronger IFC/classification/QTO round-trip;
- issue/provenance loop;
- selective IDS/BCF integration.

### P3 — Specialist expansion

Goal: deepen only workflows supported by real user evidence.

Possible directions:

- company/trade measurement packs;
- richer rebar estimating and cutting optimisation;
- productivity templates/project setup;
- MEP QS;
- civil/earthwork depth;
- broader cost-code/classification mappings;
- collaboration/cloud features where concrete team workflow demands them.

## 24. Recommended epics

| Epic | Priority | Outcome |
|---|---|---|
| Native Semantic Editing | P0 | CAD-native edits preserve semantic/provenance/quantity truth |
| Large-Model Qualification | P0 | Real-project performance and V25 evidence |
| Measurement Rules v2 — Explainable Quantity | P0 | Canonical rule/input/deduction/rounding trace |
| Quantity Coverage & BOQ Mapping | P0–P1 | Know what is measured, unmapped, stale, or missing |
| Quantity Revision Ledger | P0–P1 | Stable snapshot-to-snapshot quantity delta |
| Inspector & Batch Edit | P0 | Fast inspect/edit/preview/apply workflow |
| Cost & Rate Domain | P1 | Reproducible estimating separated from geometry |
| Revision Cost Impact | P1 | Explainable quantity-driven and rate-driven deltas |
| QS Rule Checker | P2 | Declarative quality profiles with evidence |
| 2D/3D Takeoff Convergence | P2 | Support PDF/DWG takeoff without abandoning semantic architecture |
| IFC/BCF Provenance Loop | P2 | Identity-traceable openBIM review/exchange |
| Rebar Optimisation | P3 | Cutting/waste/procurement intelligence on BBS foundation |

## 25. First implementation tickets recommended by the review

These are **proposed tickets**, not reservations and not claims that every capability is absent everywhere.

1. Add canonical `MeasurementTrace` contract.
2. Version measurement rules and preserve rule version in snapshots.
3. Project existing quantity services into trace output without creating a second calculation path.
4. Add “why this quantity?” inspector.
5. Add measurement snapshot + deterministic quantity delta.
6. Add native qualification evidence schema/matrix.
7. Add native edit regression/qualification matrix for MOVE/ROTATE/STRETCH/grips on high-value semantic categories.
8. Add large-project performance fixtures/budgets for regeneration, BQ, rebar, room/topology, revision, and persistence.
9. Add classification/work-item/BOQ mapping contract and coverage report.
10. Add minimal `RateBook` and `RateItem` domain.
11. Add `EstimateLine` from measurement snapshot + rate snapshot.
12. Add revision cost-impact projection separating quantity and rate changes.
13. Add QS Rule Checker v1 on top of Semantic Health.
14. Add stale measurement/estimate/report snapshot detection.
15. Prototype 2D count/length/area takeoff with explicit mapping to canonical work items.
16. Define IFC identity/classification/QTO round-trip acceptance criteria before broader export work.

## 26. Quality gates and product KPIs

These are proposed measures, not current repository metrics.

### Quality gates

- **Trace completeness:** high-value quantities require valid trace before being called explainable.
- **Deterministic rerun:** same canonical input + same rule snapshot → same output.
- **No hidden recomputation:** reports/exports do not independently implement canonical business formulas.
- **Stale-state detection:** measurement/estimate/report snapshots detect outdated source state.
- **Native evidence:** native-dependent release claims require host/version evidence.
- **Identity integrity:** ambiguous/duplicate identity cannot be silently deduplicated in provenance/revision/estimate reconciliation.
- **Unit integrity:** persistence/exchange boundaries use explicit canonical units.
- **Edit integrity:** qualified native edits preserve or deterministically regenerate semantic and quantity truth.
- **Performance budget:** representative project sizes have measurable response-time/memory targets.

### Product KPIs

- % reportable quantity lines with complete trace;
- % semantic elements quantity-ready;
- % elements mapped to classification/work item/BOQ;
- unresolved classification/rule/rate findings per project;
- % target native commands/objects qualified on host matrix;
- revision lines reconciled by stable identity;
- % cost deltas attributable to quantity vs rate change;
- count of known report-specific hidden quantity recomputations — target `0`;
- edit-preview/report consistency failures — target `0` for qualified workflows;
- stale-output defects found after release — target downward trend;
- large-model workflow timings against versioned budgets.

## 27. “Killer workflow” target

The session’s strongest product concept was a traceable loop where every output is reversible back to its cause:

```text
DWG / PDF / IFC
        ↓
Recognize / Capture / Direct Draw
        ↓
Semantic Model
        ↓
Model Health
        ↓
Measurement Rules
        ↓
Explainable Quantities
        ↓
Classification
        ↓
BOQ Mapping
        ↓
Rates / Estimate
        ↓
Revision
        ↓
Quantity Delta + Cost Delta
        ↓
Excel / DWG Tables / IFC / BCF
```

Desired UX invariants:

- every BQ line: **click → model**;
- every object: **click → quantity breakdown**;
- every quantity: **click → formula/rule/deductions**;
- every revision delta: **click → reason/source objects**;
- every estimate line: **click → measurement snapshot + rate snapshot**.

This is the product behavior most likely to differentiate QS3D from a generic BIM authoring clone.

## 28. Decision policy for future features

Before adding a substantial feature/category, answer:

1. Does it materially improve quantity, estimate, rebar, documentation, interchange, or QS-review fidelity?
2. Can the business result be deterministic and explainable?
3. Does it reuse canonical identity/quantity/edit/report infrastructure rather than fork it?
4. Is native qualification budgeted where the host is involved?
5. Does it improve a real specialist QS workflow more than strengthening an existing weak link?

If several answers are “no,” defer the feature even if a competitor has it.

## 29. Recommended delivery sequence

### Wave 1 — Native trust + measurement trust

- native edit invariants;
- large-model performance/qualification;
- `MeasurementTrace`;
- rule versioning;
- identity/provenance convergence;
- trace projection for high-value categories;
- quantity coverage and classification/BOQ mapping foundation.

### Wave 2 — Revision and inspection

- “why this quantity?” inspector;
- batch edit preview/apply;
- measurement snapshot and revision quantity delta;
- stale snapshot/report detection.

### Wave 3 — Estimating

- rate book;
- estimate lines/snapshots;
- waste/commercial adjustments;
- revision cost impact;
- estimate-aware BQ/export.

### Wave 4 — QA and interoperability

- declarative QS checker;
- issue/evidence workflow;
- 2D/3D takeoff convergence;
- IFC provenance/classification/QTO round-trip hardening;
- selective IDS/BCF integration.

### Wave 5 — Specialist expansion

- rebar cutting/waste/procurement optimisation;
- MEP QS when demand is proven;
- civil/earthwork depth;
- selected collaboration/cloud capabilities;
- additional categories only when they reuse the canonical architecture.

## 30. Repository references and source-of-truth policy

Use this note for prioritization, then verify each implementation decision against current repository truth:

- Product boundary: `docs/PRODUCT-BOUNDARY.md`
- Implementation status: `docs/IMPLEMENTATION-STATUS.md`
- Canonical plan: `docs/PLAN.md`
- Multi-agent coordination: `docs/AGENT-WORK-REGISTRATION.md`
- Current claims: `docs/agent-work-claims/`

This note intentionally preserves the review session’s scorecard, examples, benchmark list, and strategic recommendations so they are not lost when the chat is deleted. It must not be used to claim that a recommended feature is implemented, tested, native-qualified, or production-ready.

## 31. Bottom line

The complete review conclusion is:

> **QS3D is already broad enough. The next competitive step is depth of QS truth, not raw feature count.**

The strongest roadmap is:

**native edit/runtime trust → semantic model → explainable measured quantity → classification/BOQ coverage → revision-aware quantity snapshot → rate/estimate snapshot → explainable cost impact → rule-based QS review → IFC/BCF/2D interoperability → specialist rebar/MEP/civil expansion where real demand proves value.**

That direction keeps QS3D focused, deterministic, auditable, and meaningfully different from AutoCAD/BricsCAD as CAD platforms, Revit as a general BIM authoring platform, and BLT3D/Cubicost/CostX as quantity/estimating references.
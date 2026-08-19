# BIM5D quantity -> 4D schedule -> 5D cost model

**Research lane:** #3101  
**Lane-Key:** `issue-3101`  
**Baseline:** `main@a323d25a9f5720eb99b2533d9605298366dd3735`  
**Date:** 2026-08-19  
**Scope:** terminology/domain research only; no feature implementation is authorized by this document.

## 1. Canonical QS3D normalization

For QS3D planning and documentation, use this progression:

```text
3D model / element identity
          |
          v
quantity facts + measurement provenance
          |
          v
4D activity / schedule / sequence linkage
          |
          v
5D cost / rate / budget linkage
          |
          v
progress + change + variance + forecast + reporting
```

Short form:

```text
3D model + quantities -> 4D schedule/time -> 5D cost
```

This is the **canonical QS3D progression**, not a claim that every BIM vendor or every standard requires exactly this implementation order.

buildingSMART Professional Certification describes the common industry progression as model/geometry information, then sequencing and scheduling as 4D, followed by costing, pricing and productivity information as 5D. buildingSMART's IFC documentation separately provides interoperable concepts for element quantities, tasks/work schedules and cost items/cost schedules.

Therefore QS3D should not collapse the three responsibilities into one mutable record:

- model/geometry establishes source identity and measurable facts;
- quantity takeoff establishes auditable measurement facts;
- 4D establishes **when and in what sequence** a modeled/quantified scope is performed;
- 5D establishes **what that scope costs**, with rates, resources, budgets and cost versions;
- progress/change layers compare dated actual or revised states against those baselines.

## 2. Important terminology boundary

The labels `3D`, `4D` and `5D` are widely used BIM workflow terminology, but they are not themselves a substitute for a data contract.

For QS3D:

| Label | Minimum meaning | What is not enough |
|---|---|---|
| **3D / model-based QTO** | stable source element identity plus measurable model information and quantity provenance | geometry with no traceable quantity result |
| **4D-enabled** | explicit activity/task identity, time data and model/quantity-to-activity linkage | a free-text start date attached to a BOQ row |
| **5D-oriented** | explicit cost item/rate/budget data linked back to model/quantity scope, with auditable versions | a single editable `TotalCost` field |
| **Integrated 4D/5D** | model + quantities + schedule + cost can be traversed consistently in both directions and updated without destroying historical baselines | unrelated schedule and estimate files displayed in the same UI |

A quantity-times-rate estimate can be useful without a schedule. In QS3D terminology that should be described precisely as **model-based estimating**, **quantity-cost linkage**, or **cost planning**. Do not claim the full normalized `4D/5D` chain unless the schedule/time linkage also exists.

## 3. Authoritative openBIM / IFC mapping

### 3.1 Quantity layer

IFC 4.3 `IfcElementQuantity` defines derived measures of an element's physical properties. It can carry an optional `MethodOfMeasurement` and a set of physical quantities such as count, weight, length, area, volume and time.

QS3D implication: a quantity fact needs more than a number. It should preserve at least:

```text
QuantityFact
- id
- sourceElementId
- sourceRevisionId
- quantityKind
- value
- unit
- measurementMethod / ruleId
- inputs / deductions / additions
- calculationVersion
- classification / work breakdown references
```

The measurement method is important because two legitimate rules of measurement can produce different reportable quantities from the same geometry.

### 3.2 4D schedule layer

IFC 4.3 provides:

- `IfcTask` for an identifiable unit of work;
- `IfcTaskTime` for scheduled/actual start, finish and duration information;
- `IfcWorkSchedule` for task schedules;
- `IfcRelAssignsToProcess` to assign objects to a process/activity;
- `IfcRelSequence` for predecessor/successor sequencing, including sequence type and optional lag.

QS3D implication: 4D is an explicit relationship, not an attribute smeared into geometry.

A minimum internal schedule contract should resemble:

```text
Activity
- id
- externalScheduleId?          # e.g. external WBS/activity identifier
- name
- plannedStart
- plannedFinish
- actualStart?
- actualFinish?
- status
- scheduleRevisionId

ActivityDependency
- predecessorActivityId
- successorActivityId
- dependencyType
- lag?

QuantityActivityAllocation
- quantityFactId
- activityId
- allocatedQuantity
- allocationUnit
- allocationRule / provenance
```

One quantity scope may be split across multiple activities, zones or dates. The allocation must therefore be explicit and must not rewrite the original measured quantity.

### 3.3 5D cost layer

IFC 4.3 `IfcCostItem` represents cost/financial values in a cost schedule and can combine cost values with quantities. Its documentation explicitly supports quantity-based costing using quantities associated with elements, tasks or resources. `IfcCostSchedule` groups cost items for estimates and other cost presentations.

QS3D implication: cost should reference versioned quantity and schedule scope instead of becoming the storage location for geometry truth.

A minimum internal cost contract should resemble:

```text
CostItem
- id
- costCode / classification
- description
- unit
- costScheduleId

RateSet
- id
- effectiveDate
- currency
- version

Rate
- costItemId
- rateSetId
- unitRate
- resourceBreakdown?

ActivityCostAllocation
- activityId
- costItemId
- quantityFactIds / allocationIds
- pricedQuantity
- rateSetId
- calculatedAmount
- calculationVersion
```

Historical cost snapshots must remain reproducible when rates, model quantities or schedules later change.

## 4. Normalized data flow

### 4.1 Baseline creation

```text
BricsCAD/IFC/external model source
    -> stable source element identity + revision
    -> deterministic quantity rules
    -> QuantityFact snapshot
    -> activity allocation
    -> Activity + dependencies + schedule revision
    -> cost-item mapping + rate-set selection
    -> priced activity / cost snapshot
```

### 4.2 Progress state

```text
Baseline QuantityFact + Activity + Cost snapshot
    -> dated progress measurement
    -> accepted / rejected / pending quantity state
    -> earned / claimed / certified value as separate business states
    -> schedule variance + cost variance
    -> forecast / cash-flow / report views
```

Do not treat `measured`, `installed`, `approved`, `claimed`, `certified` and `paid` as synonyms. They are different business states even when they refer to the same physical scope.

### 4.3 Change propagation

A model change should produce a new revision and an auditable delta:

```text
old element revision -> old quantity snapshot
new element revision -> new quantity snapshot
                     -> quantity delta
                     -> affected activity allocations
                     -> schedule impact candidate
                     -> affected cost allocations
                     -> cost / forecast variance
```

The system should never silently overwrite a previously issued estimate, progress snapshot or certified claim merely because a model element changed.

## 5. IFC facts vs product-specific behavior

Keep these evidence classes separate.

### Open-standard facts

The IFC schema provides neutral concepts for quantities, processes/tasks, scheduling time, process sequencing, controls, cost items, cost schedules and performance history. It also provides relationship objects that can connect products/processes/controls.

This demonstrates that an open data model can represent important parts of 4D/5D information. It does **not** prove that every IFC file, exporter, model view or consuming product carries all of those concepts with complete fidelity.

Several IFC entities used for richer schedule/cost workflows are documented as not belonging to every standardized schema subset/implementation level. Interoperability therefore requires explicit capability detection and fail-closed mapping diagnostics rather than assuming all IFC inputs contain full 4D/5D data.

### Vendor/platform examples

Autodesk Navisworks TimeLiner is a vendor-specific implementation example. Autodesk documents that TimeLiner can link model objects to construction-schedule tasks, compare planned and actual dates, simulate the schedule and attach/import costs to tasks.

That is useful evidence for a mature workflow pattern, but it is **not** an IFC rule and does not make Navisworks data structures the QS3D domain model.

### QS3D product inference

QS3D should adopt the workflow responsibilities, not copy another product's internal model:

- stable model/quantity traceability;
- explicit schedule activity mapping;
- explicit versioned cost mapping;
- reversible navigation from report -> cost -> activity -> quantity -> source element;
- auditable revisions and dated progress/change states.

## 6. Minimum capability gates for truthful QS3D wording

### 6.1 To say `model-based quantity takeoff`

Required minimum:

- stable source/model element identity;
- explicit unit normalization;
- deterministic quantity rule identity;
- source revision/provenance;
- trace from reported quantity back to source element(s);
- reproducible recalculation.

### 6.2 To say `4D-enabled`

All QTO gates above, plus:

- first-class activity/task IDs;
- planned schedule time fields;
- explicit model/quantity-to-activity mapping;
- dependency/sequence representation or external schedule reference;
- schedule revision provenance;
- ability to identify unmapped/orphaned quantity scope.

### 6.3 To say `5D-oriented`

All applicable quantity gates, plus:

- first-class cost items/codes;
- rate/resource data with units and versions;
- quantity-to-cost mapping;
- estimate/budget snapshot identity;
- calculation trace for quantity x rate and rollups;
- currency and effective-date handling;
- revision/delta reporting.

If schedule linkage is absent, prefer the narrower wording `model-based estimating` or `quantity-cost planning` rather than implying the complete normalized 4D/5D chain.

### 6.4 To say `integrated 4D/5D`

All 4D and 5D gates, plus:

- common stable identities across quantity, activity and cost allocations;
- time-phased cost/progress views derived from explicit mappings;
- change propagation that identifies impacted activities and cost items;
- dated baseline/progress/forecast versions;
- orphan/mapping diagnostics;
- navigation and reporting that preserve end-to-end provenance.

## 7. What QS3D-BricsCAD can own independently

Within the locked BricsCAD-plugin product boundary, repository-safe product capabilities can include:

- normalized element/source identity contracts;
- quantity facts, measurement-rule provenance and classifications;
- internal project/WBS/activity records;
- quantity-to-activity allocations;
- schedule revision snapshots and dependency contracts;
- cost codes, rate sets, resources and calculation rules;
- quantity/activity-to-cost mappings;
- estimate/budget/forecast snapshots;
- progress and change-delta records;
- BOQ, variance, progress and traceability reports;
- import/export adapters that fail closed when required fields cannot be mapped.

These domain records should remain host-neutral where practical so they can migrate deliberately toward `QS3D-Platform` without changing this repository into a standalone CAD product.

## 8. What requires host or external integration

### BricsCAD host responsibility

`QS3D-BricsCAD` relies on BricsCAD for native DWG database/geometry, document lifecycle, transactions, selection and viewport behavior. The plugin can derive or attach QS3D semantics, but must not claim to own a standalone DWG engine.

### External schedule systems

An internal schedule model can exist independently, but authoritative enterprise scheduling may live in products such as Microsoft Project, Primavera, SYNCHRO or another planning system. Any connector must preserve external activity IDs, schedule revision/source and mapping diagnostics. Do not imply a live bidirectional integration until such an adapter exists and is verified.

### External cost/ERP/accounting systems

QS3D may own internal rate/budget/estimate snapshots. Contract commitments, procurement, accounting, payment certification or ERP truth may belong to external systems. Those integrations need explicit contracts and cannot be inferred from the presence of a 5D cost model.

## 9. Progress, actuals and change impact

IFC `IfcPerformanceHistory` demonstrates an open-standard mechanism for time-sensitive actual/predicted/simulated performance data, but a construction QS claim/certification workflow is a product/business contract beyond that entity alone.

QS3D should therefore model progress explicitly:

```text
ProgressSnapshot
- id
- asOfDate
- baselineRevisionId
- activityId
- measuredQuantity
- installedQuantity?
- acceptedQuantity?
- claimedQuantity?
- certifiedQuantity?
- notes / evidence references
```

Cost valuation should derive from a named snapshot and rate/cost baseline rather than from a mutable global percentage.

## 10. Non-goals and claim guardrails

This research does **not** authorize:

- implementing schedule/cost features in issue #3101;
- calling every BOQ/cost feature “5D BIM”;
- claiming full IFC schedule/cost interoperability without fixtures and mapping evidence;
- claiming direct integration with Revit, Primavera, Microsoft Project, Navisworks, SYNCHRO, ERP or accounting products without an implemented and verified adapter;
- turning `QS3D-BricsCAD` into a standalone executable;
- copying competitor source/assets or proprietary data formats.

## 11. Canonical implementation order after research

This section corrects the earlier benchmark planning order for future lanes. It is guidance, not pre-authorized implementation scope.

### P0 — quantity truth and provenance

- source identity/revision;
- units and measurement rules;
- deterministic quantity facts;
- explanation and delta model.

### P1 — 4D schedule foundation

- activities/WBS;
- planned/actual time fields;
- dependencies/sequence;
- quantity/model-to-activity allocations;
- schedule revision provenance.

### P2 — 5D cost foundation

- cost codes/items;
- versioned rates/resources/currency;
- activity/quantity-to-cost mappings;
- estimate/budget snapshots;
- time-phased cost and variance calculations.

### P3 — progress, claims and forecasting

- dated progress snapshots;
- measured/installed/accepted/claimed/certified state separation;
- change propagation;
- progress valuation;
- forecast/cash-flow/variance reporting.

### P4 — interoperability and automation

- IFC/openBIM adapters and mapping diagnostics;
- external schedule/cost connectors;
- rule-assisted mapping and anomaly detection only after deterministic contracts exist.

## 12. Source ledger

### Authoritative/open-standard sources

- buildingSMART Professional Certification — *A New Way of Working*: <https://education.buildingsmart.org/a-new-way-of-working/>
- buildingSMART IFC 4.3.2 `IfcElementQuantity`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcElementQuantity.htm>
- buildingSMART IFC 4.3.2 `IfcTask`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcTask.htm>
- buildingSMART IFC 4.3.2 `IfcTaskTime`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcTaskTime.htm>
- buildingSMART IFC 4.3.2 `IfcWorkSchedule`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcWorkSchedule.htm>
- buildingSMART IFC 4.3.2 `IfcRelSequence`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcRelSequence.htm>
- buildingSMART IFC 4 `IfcRelAssignsToProcess` reference: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4/FINAL/HTML/schema/ifckernel/lexical/ifcrelassignstoprocess.htm>
- buildingSMART IFC 4.3.2 `IfcRelAssignsToControl`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcRelAssignsToControl.htm>
- buildingSMART IFC 4.3.2 `IfcCostItem`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcCostItem.htm>
- buildingSMART IFC 4.3.2 `IfcCostSchedule`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcCostSchedule.htm>
- buildingSMART IFC 4.3.2 `IfcPerformanceHistory`: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4_3/HTML/lexical/IfcPerformanceHistory.htm>
- buildingSMART IFC4 change summary — process/4D and cost/5D datasets: <https://standards.buildingsmart.org/IFC/RELEASE/IFC4/FINAL/HTML/annex/annex-f/ifc4/index.htm>

### Established platform documentation — vendor-specific examples

- Autodesk Navisworks TimeLiner overview: <https://help.autodesk.com/cloudhelp/2025/ENU/Navisworks-Timeliner/files/GUID-D0D36E3D-F1D0-43B6-AB4E-2E7799B340A3.htm>
- Autodesk Navisworks TimeLiner workflow: <https://help.autodesk.com/cloudhelp/2026/ENU/Navisworks-Timeliner/files/GUID-96D92B8A-CD9D-4E25-A549-0EB2BF15B5CE.htm>
- Autodesk Navisworks TimeLiner costs: <https://help.autodesk.com/cloudhelp/2022/ENU/Navisworks/files/GUID-0561777D-45B0-40B8-B298-E47BB15D7E9B.htm>

## 13. Relationship to the BLT3D/BIM5D benchmark

`docs/BLT3D-BIM5D-BENCHMARK.md` remains the broader competitive/workflow benchmark. For the specific 3D/4D/5D ordering and domain terminology, **this issue #3101 note is the canonical refinement**:

```text
quantity truth -> 4D schedule/time -> 5D cost
```

Future architecture/gap/UX lanes (#3103, #3104, #3105) should use this normalized ordering rather than interpreting quantity takeoff, schedule and cost as interchangeable dimensions.
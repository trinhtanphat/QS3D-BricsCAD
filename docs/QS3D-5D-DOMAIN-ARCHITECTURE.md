# QS3D quantity → cost → schedule/progress/claim domain architecture

**Issue:** #3104  
**Lane-Key:** `issue-3104`  
**Baseline:** `main@a323d25a9f5720eb99b2533d9605298366dd3735`  
**Status:** architecture/design note only — no implementation code  
**Product boundary:** `QS3D-BricsCAD` remains a BricsCAD V25/V26 Windows x64 hosted plugin.

## 1. Decision summary

QS3D should model 5D-oriented workflows as a chain of **versioned, auditable domain facts and projections** rather than one mutable project object:

```text
host/source model revision
        |
        v
stable source + semantic identity
        |
        v
immutable quantity facts / measurement snapshot
        |
        +------------------------------+
        |                              |
        v                              v
WBS / cost-code mappings         schedule/activity mappings
        |                              |
        v                              v
versioned rates/resources        versioned schedule
        |                              |
        +---------------+--------------+
                        v
                estimate snapshot
                        |
                 +------+------+
                 |             |
                 v             v
          progress snapshot   change / variance
                 |
                 v
              claim
                 |
                 v
        reporting projections
```

The central architectural rule is:

> **Geometry and measured quantity truth are upstream facts. Cost, schedule, progress and claim data reference those facts by stable identity and version; they never rewrite the geometric or measurement truth that produced them.**

This gives QS3D deterministic recalculation, reproducible historical claims, explicit change propagation and a clean adapter boundary for BricsCAD and future external integrations.

## 2. Scope and non-goals

This note defines:

- bounded contexts and dependency direction;
- entities/value objects and stable identifiers;
- provenance requirements;
- quantity/unit, money/currency and time semantics;
- versioning and change propagation;
- schedule/progress/claim semantics;
- external adapter interfaces;
- deterministic/auditable recalculation rules;
- testable invariants;
- compatibility with current `QS3D.Core` quantity/cost primitives.

This note does **not**:

- add implementation code;
- replace BricsCAD's DWG database, editor or viewport;
- turn this repository into a standalone CAD application;
- require proprietary-format reverse engineering;
- define a Revit add-in architecture for this repository;
- make competitor implementation details normative;
- certify accounting, tax, contract or statutory rules for any jurisdiction.

## 3. Product and host boundary

`QS3D-BricsCAD` is a hosted plugin. BricsCAD owns native document/database/editor/viewport lifecycle. QS3D owns domain logic, quantity/cost semantics, project metadata, UI and reporting inside that host boundary.

The architectural split is:

```text
BricsCAD V25/V26 host APIs
        |
        v
BricsCAD host adapter
        |
        v
vendor-neutral QS3D domain contracts
        |
        +--> quantity / cost / schedule / progress / claim
        |
        +--> reporting / persistence abstractions

IFC/openBIM or future external systems
        |
        v
separate anti-corruption adapters
        |
        +--> same vendor-neutral domain contracts
```

Host-native identifiers, proprietary SDK types and transient object handles must stop at the adapter boundary. The domain stores normalized identifiers/provenance, not BricsCAD API objects.

Future migration of host-neutral contracts toward `QS3D-Platform` is compatible with this design, but current `QS3D.Core` remains implementation truth until a separately validated migration slice lands.

## 4. Architectural principles

### 4.1 Facts before projections

Quantities are facts derived from a specific source revision and rule version. Estimates, earned value, claims and reports are downstream projections over those facts.

### 4.2 Immutable history

A published quantity snapshot, estimate snapshot, progress snapshot or certified claim is never edited in place. Corrections create a successor revision with explicit supersession metadata.

### 4.3 Explicit identity

Every cross-context link uses stable domain identifiers. Display names, row positions, BricsCAD `ObjectId`, object handles without source scope, filenames and UI selection indexes are not sufficient domain identities by themselves.

### 4.4 Explicit units/currency/time

No calculation may infer units, currency or timezone from locale/UI defaults. Values carry their semantics explicitly.

### 4.5 Deterministic recalculation

Given the same immutable inputs, rule versions, mappings, rate versions and rounding policies, a recalculation must produce the same canonical result independent of enumeration order or UI state.

### 4.6 Explainability

Every quantity and money result must be traceable back to source identity, source revision, calculation rule, mapping/rate versions and any commercial adjustment.

### 4.7 Sidecar schedule semantics

Schedule/activity data links to quantity scopes. It does not mutate geometry, quantity formulas or model element classification truth.

### 4.8 Claims freeze commercial history

A certified/approved claim references frozen progress and commercial inputs. A later model, rate or schedule change cannot silently change historical certified value.

### 4.9 External systems are adapters

IFC/openBIM, spreadsheets, ERP, scheduling systems or future authoring-system APIs are integration adapters. Their schemas do not become the core domain model.

## 5. Bounded contexts and dependency direction

The proposed contexts are intentionally one-way.

| Context | Owns | May depend on | Must not mutate |
|---|---|---|---|
| Source identity | source models, revisions, element identity/provenance | adapter inputs | quantity/cost/time facts |
| Quantity | measurement traces, quantity facts, measurement snapshots | source identity + rule versions | source geometry |
| Commercial coding | WBS/cost codes, classification mappings | quantity identity | quantity values |
| Rates/resources | rate books, resource compositions, currency/effective dates | cost codes | quantity facts |
| Estimate | estimate lines/snapshots | quantity snapshot + mapping + rates | upstream snapshots |
| Schedule | activities, schedule versions, calendars | external/manual schedule inputs | geometry/quantity |
| Quantity–activity linkage | allocation links/weights | quantity identity + schedule version | both referenced contexts |
| Progress | dated installed/earned quantity snapshots | quantity/activity links + prior progress | historical progress snapshots |
| Claim | claim periods, claim revisions, certification state | frozen progress + commercial snapshot | certified history |
| Change/variance | deltas and causality records | any two versioned snapshots | compared snapshots |
| Reporting | read models/projections | all published snapshots | source domains |

A downstream context can mark itself **stale** when an upstream reference changes. It cannot repair staleness by mutating the upstream object.

## 6. Stable identity model

### 6.1 Identity hierarchy

The minimum identity chain is:

```text
ProjectId
  └─ SourceModelId
       └─ SourceRevisionId
            └─ SourceElementId
                 └─ SemanticElementId
                      └─ QuantityKey
```

Recommended semantics:

- `ProjectId`: QS3D project identity; stable for the project lifetime.
- `SourceModelId`: stable identity for one logical source model/document stream.
- `SourceRevisionId`: immutable revision identity for one acquired model state.
- `SourceElementId`: adapter-normalized identity of a source element within `SourceModelId`.
- `SemanticElementId`: QS3D stable semantic identity used when source objects are replaced/recreated but represent the same business element.
- `QuantityKey`: canonical measurement dimension such as net concrete volume, formwork area or rebar mass.

### 6.2 Identity rules

1. IDs are opaque tokens, not user-editable display labels.
2. IDs are scoped; an element token is never globally interpreted without its project/source scope.
3. A source revision is immutable.
4. A semantic identity may survive source-object replacement only through an explicit reconciliation/mapping decision.
5. A model import that cannot preserve identity records an identity break; it must not silently guess continuity.
6. Deleted and recreated source objects must not inherit history merely because geometry is similar.
7. BricsCAD transient runtime IDs may be adapter evidence but are not the only persisted cross-session identity.

### 6.3 Quantity identity

Current QS3D already uses the tuple:

```text
SemanticIdentity + SourceIdentity + QuantityKey
```

for measurement trace uniqueness. The 5D design should preserve that semantic contract and add explicit project/model/revision scope where persistence or cross-revision comparison needs it.

A conceptual `QuantityFactId` therefore identifies a **measurement meaning**, while `MeasurementSnapshotId` identifies a frozen set of measured results at a specific revision/rule state.

## 7. Provenance envelope

Every published domain snapshot should carry a common provenance envelope.

Minimum fields:

| Field | Meaning |
|---|---|
| `ProjectId` | owning QS3D project |
| `SnapshotId` | immutable snapshot identity |
| `SnapshotKind` | quantity, estimate, schedule, progress, claim, report basis, etc. |
| `Revision` | monotonic domain revision or explicit version token |
| `CreatedAtUtc` | creation instant |
| `CreatedBy` | actor/system identity when available |
| `SourceRevisionIds` | upstream immutable revisions consumed |
| `RuleSetIds/Versions` | calculation/business rules consumed |
| `MappingSetIds/Versions` | WBS/cost/schedule mapping versions consumed |
| `RateBookIds/Versions` | commercial rate inputs consumed |
| `ParentSnapshotIds` | predecessor/superseded snapshots |
| `CanonicalDigest` | deterministic content fingerprint |
| `Warnings/Assumptions` | explicit uncertainty or accepted assumptions |

A digest is supporting evidence, not a substitute for domain IDs. Canonical serialization must be versioned so a future serializer change cannot silently redefine identity.

## 8. Quantity domain

### 8.1 Quantity fact

A quantity fact represents one measured value with enough context to reproduce and explain it.

Conceptual fields:

- semantic/source identity;
- source revision;
- quantity key;
- gross value;
- deductions/additions;
- net value;
- canonical unit;
- input facts/dimensions/properties;
- rule ID + rule version;
- rounding policy;
- warnings/assumptions;
- provenance.

### 8.2 Measurement snapshot

A measurement snapshot is an immutable, canonically ordered set of quantity facts.

It is the primary quantity input to cost, schedule allocation, progress and reporting.

The current `MeasurementSnapshot`/`MeasurementTrace` design already provides important properties that should remain normative:

- immutable snapshot behavior;
- deterministic ordering;
- uniqueness by semantic/source/quantity identity;
- traceable gross/adjustment/net values;
- explicit unit and rounding policy;
- optional rule ID/version;
- canonical serialization.

### 8.3 Quantity truth versus commercial adjustment

A commercial estimating adjustment is not a measurement rewrite.

Example:

```text
measured quantity = 100.00 m3
commercial allowance = +2.00 m3
estimating quantity = 102.00 m3
```

The 2.00 m3 adjustment belongs to estimate/commercial provenance with a reason, not inside the source measurement trace unless it is genuinely a measurement rule adjustment.

## 9. WBS and cost-code mapping

Quantity facts should not embed project-specific cost codes as their intrinsic identity.

Instead introduce a versioned mapping context:

```text
QuantityScope
   -> MappingRule / explicit assignment
      -> WbsNodeId
      -> CostCodeId
      -> optional ResourceClassId
```

A `QuantityScope` can target:

- one quantity fact;
- a semantic element set;
- a classification/property predicate;
- a work package;
- a user-curated selection set with frozen membership.

Mapping precedence must be deterministic. A recommended order is:

1. explicit item assignment;
2. explicit frozen group assignment;
3. rule-based mapping;
4. default/unmapped state.

Ambiguous equal-precedence mappings fail closed and produce diagnostics; they must not depend on rule enumeration order.

## 10. Rates and resources

### 10.1 Rate book

A rate book is versioned business data independent from geometry.

Current `RateBook` semantics are a strong baseline:

- cost code + unit + currency scope;
- non-negative unit rate;
- UTC effective time;
- explicit version token;
- deterministic resolution as-of a requested instant;
- ambiguity rejection for equal effective timestamps in the same scope.

### 10.2 Resource composition

A future resource rate may decompose into labor/material/plant/subcontract/other components, but the estimate line should still resolve to a frozen commercial input.

Conceptual resource item:

- `ResourceId`;
- category;
- unit;
- currency;
- unit rate;
- effective interval;
- supplier/source provenance if applicable;
- version.

A composite rate references resource items and coefficients. Recalculating with a new resource set creates a new estimate snapshot; it does not rewrite the prior estimate.

## 11. Estimate domain

### 11.1 Estimate line

An estimate line is a deterministic projection from:

```text
quantity fact
+ commercial quantity adjustment (optional, reason required)
+ cost-code mapping
+ resolved rate
+ explicit currency/as-of time
+ rounding policy
= line amount
```

Current `EstimateLine` already follows this dependency direction and should be treated as compatibility evidence for the proposed architecture.

### 11.2 Estimate snapshot

An estimate snapshot freezes:

- measurement snapshot identity/digest;
- mapping-set version;
- rate-book version and as-of instant;
- currency policy;
- all line-level commercial adjustments;
- line amounts and totals;
- warnings/unmapped scopes;
- canonical digest.

Changing a rate book, mapping set, measured quantity or commercial adjustment marks the estimate **stale** and produces a successor estimate snapshot when recalculated.

Historical estimate snapshots remain reproducible.

## 12. Currency semantics

### 12.1 Currency identity

Money values use explicit ISO-style three-letter uppercase currency tokens, consistent with current `RateBook` behavior.

No amount exists without a currency.

### 12.2 Decimal arithmetic

Commercial amounts and rates use decimal arithmetic with checked overflow behavior. Binary floating-point money is forbidden.

Current quantity traces use finite `double` values and `EstimateLine` explicitly converts measured quantity to `decimal` with overflow/underflow guards. A future domain extraction must preserve this fail-closed boundary unless an independently validated quantity numeric representation replaces it.

### 12.3 FX policy

If multi-currency estimating is introduced, currency conversion is a separate versioned projection:

- source currency;
- target/reporting currency;
- FX rate;
- effective instant/period;
- FX source/version;
- rounding policy.

An FX revaluation creates a new financial projection; it cannot alter the source-currency estimate snapshot.

## 13. Unit semantics

Each quantity fact carries a canonical unit token.

Rules:

1. Unit conversion is explicit and deterministic.
2. A rate resolves only against a compatible canonical unit.
3. Incompatible dimensions fail closed.
4. UI display units are projections; persisted canonical values are not reinterpreted when display preferences change.
5. Rounding occurs at explicitly named boundaries, not implicitly on UI formatting.
6. Aggregation requires compatible dimensions and declared conversion policy.

The existing lower-case unit token convention in `RateBook` should remain compatible with any future unit registry.

## 14. Time semantics

QS3D needs two distinct time concepts.

### 14.1 Instants

Technical provenance/effective instants use UTC:

- snapshot creation;
- rate effective time;
- import acquisition time;
- approval/certification instant.

### 14.2 Project calendar dates/periods

Construction schedule and claims use project-calendar semantics:

- project timezone ID;
- local work date;
- calendar ID;
- claim period start/end dates;
- data date/status date.

A claim period must not be inferred by converting UTC timestamps using the current workstation timezone.

### 14.3 Schedule duration

Activity duration is interpreted against a named calendar/version. `5 days` without the calendar is not sufficient deterministic schedule data.

## 15. Schedule domain

### 15.1 Activity

Conceptual activity fields:

- `ActivityId` — stable schedule-domain identity;
- external source ID, if imported;
- name/code;
- WBS/activity hierarchy reference;
- planned start/finish;
- calendar ID/version;
- status/data date fields;
- optional actual start/finish;
- schedule provenance.

### 15.2 Schedule version

A schedule is immutable per published version:

- `ScheduleId`;
- `ScheduleVersionId`;
- data date;
- source system/version;
- activities;
- calendars;
- canonical digest.

Imported schedule IDs remain namespaced by source system/project so two systems cannot collide accidentally.

## 16. Quantity–activity linkage

Schedule linkage is a separate versioned association aggregate.

Conceptual `ActivityAllocation`:

- allocation ID;
- schedule version ID;
- activity ID;
- quantity scope/fact IDs;
- allocation basis;
- weight or quantity share;
- effective version;
- mapping provenance.

Allowed allocation bases may include:

- 100% of one quantity fact to one activity;
- quantity split by explicit absolute quantity;
- percentage split whose total equals 100%;
- frozen group membership.

Invariant: allocations can change schedule/progress interpretation but **never change the measurement snapshot**.

## 17. Progress domain

### 17.1 Progress snapshot

A progress snapshot is a dated immutable record of achieved/installed/accepted quantity against eligible quantity scope.

Minimum fields:

- `ProgressSnapshotId`;
- as-of project date/data date;
- schedule version + allocation version;
- referenced measurement snapshot;
- progress lines;
- predecessor snapshot;
- correction/reversal references;
- provenance/digest.

### 17.2 Progress line

A progress line references quantity scope/activity and records:

- eligible quantity;
- prior cumulative achieved quantity;
- period achieved quantity;
- cumulative achieved quantity;
- accepted/certifiable quantity where distinct;
- unit;
- evidence/reference IDs;
- reason for any correction.

### 17.3 Progress rules

1. Normal cumulative achieved quantity is non-decreasing.
2. A decrease requires an explicit correction/reversal event that points to what is being corrected.
3. Cumulative achieved quantity cannot exceed eligible quantity unless the contract/rule explicitly allows overrun and records the reason.
4. Progress cannot be recorded against a quantity identity absent from the referenced quantity snapshot/allocation version.
5. Reclassification of an activity does not rewrite prior progress; it creates a new allocation/progress successor.

## 18. Claim domain

### 18.1 Claim identity and revision

A claim is a commercial/legal snapshot with explicit revision state.

Conceptual identifiers:

- `ClaimId` — stable business identity for the claim cycle/certificate;
- `ClaimRevisionId` — immutable revision;
- `ClaimPeriodId` — period boundaries under project calendar semantics.

### 18.2 Claim states

Recommended domain states:

```text
Draft
  -> Submitted
      -> Assessed
          -> Certified / Rejected
```

Corrections after certification create a successor/adjustment claim; certified snapshots are not edited in place.

### 18.3 Claim line

A claim line references frozen inputs:

- progress snapshot line(s);
- estimate/commercial basis snapshot;
- cost code/WBS;
- prior certified quantity/value;
- current claimed quantity/value;
- assessed/certified quantity/value;
- cumulative certified quantity/value;
- retention/other adjustments where applicable;
- explicit currency;
- provenance and reason codes.

The domain must distinguish:

- **measured quantity**;
- **progress-achieved quantity**;
- **claimed quantity**;
- **assessed quantity**;
- **certified quantity**.

They are not aliases.

## 19. Change and variance domain

Change propagation should be explicit rather than hidden recalculation.

### 19.1 Change event

A change event records:

- changed upstream snapshot/revision;
- old/new identity;
- change kind;
- cause/source;
- impacted downstream snapshot IDs;
- detected time;
- disposition/recalculation status.

Typical change kinds:

- source element added/removed/modified;
- semantic identity reconciliation changed;
- quantity value/rule changed;
- cost mapping changed;
- rate/resource changed;
- schedule/activity changed;
- allocation changed;
- progress correction;
- claim assessment/certification adjustment.

### 19.2 Variance

Variance is a comparison of immutable baselines, for example:

```text
quantity variance = current measurement - baseline measurement
rate variance     = current rate basis - baseline rate basis
cost variance     = current estimate - baseline estimate
progress variance = actual/earned - planned
claim variance    = certified - claimed
```

A variance record must identify both compared snapshot versions; `variance = 5%` without baselines is incomplete.

## 20. Change-propagation matrix

| Upstream change | Directly invalidates/stales | Must remain unchanged |
|---|---|---|
| source revision | quantity snapshot candidate | prior published quantity/estimate/progress/claims |
| quantity rule version | quantity snapshot candidate | source model revision |
| quantity value | estimate, allocation-derived progress basis, reports | source geometry |
| cost-code mapping | estimate/reporting | quantity snapshot |
| rate/resource version | estimate/forecast/reporting | quantity + schedule |
| schedule version | allocations/progress forecast | quantity + estimate quantity truth |
| activity allocation | progress/earned-value projection | measurement snapshot |
| progress snapshot | claim candidate/reporting | estimate/quantity |
| claim assessment | claim revision/reporting | progress snapshot |

Staleness is metadata on downstream projections, not a mutation of upstream facts.

## 21. Deterministic recalculation contract

For each projection define a **calculation input manifest** containing exact immutable input IDs and versions.

Example estimate manifest:

```text
MeasurementSnapshotId
MappingSetVersionId
RateBookId + RateBookVersion/as-of
CurrencyPolicyVersion
CommercialAdjustmentSetId
RoundingPolicyVersion
ProjectionAlgorithmVersion
```

Recalculation rules:

1. Resolve every input by exact version, never by “latest” during calculation.
2. Canonically order input collections before hashing/aggregation.
3. Reject duplicate/ambiguous mappings.
4. Reject unknown/incompatible units.
5. Reject missing required rates rather than silently using zero.
6. Use checked commercial arithmetic.
7. Emit canonical output plus provenance manifest.
8. The same manifest must produce the same canonical digest.
9. A new algorithm/rule version creates a new output revision even if the numeric result happens to match.

## 22. Versioning model

Use append-only versioning for published domain snapshots.

Each aggregate has:

- stable logical ID;
- immutable revision/version ID;
- optional predecessor/supersedes reference;
- lifecycle state;
- canonical content digest.

Recommended behavior:

- **Draft** objects may be mutable inside one editing transaction/workspace.
- **Published/Frozen** snapshots are immutable.
- **Certified** claim revisions have the strongest immutability guarantee.
- “Delete” of published history means tombstone/retention policy, not silent removal from audit history.

## 23. Canonical lifecycle

A normal 5D calculation lifecycle is:

```text
1. acquire source revision
2. normalize identity/provenance
3. calculate measurement traces
4. publish MeasurementSnapshot
5. resolve WBS/cost-code mappings
6. resolve rate/resource version
7. publish EstimateSnapshot
8. import/create ScheduleVersion
9. publish ActivityAllocationSet
10. record ProgressSnapshot(s)
11. prepare ClaimRevision
12. assess/certify ClaimRevision
13. produce reports from frozen references
14. when upstream changes, create successors + variance records
```

There is no operation named “update everything in place”.

## 24. Interfaces / ports

The domain should expose ports by responsibility, not vendor/system name.

### 24.1 Source acquisition ports

- **Model source adapter** — enumerate normalized source elements/properties for one immutable source revision.
- **Source identity resolver** — map host/source identity to persisted source/semantic identity with diagnostics.
- **Revision provenance provider** — provide source document/revision metadata and acquisition evidence.

### 24.2 Quantity ports

- **Measurement rule catalog** — resolve rule ID/version.
- **Measurement snapshot publisher/repository** — persist/retrieve immutable snapshots.
- **Quantity delta service** — compare two measurement snapshots by stable identity.

### 24.3 Commercial ports

- **Cost-code/WBS mapping repository** — versioned mappings.
- **Rate/resource repository** — exact-version/as-of resolution.
- **Estimate projector** — calculate an estimate from an explicit input manifest.

### 24.4 Schedule/progress ports

- **Schedule adapter** — normalize external/manual schedules into `ScheduleVersion`.
- **Activity allocation repository** — versioned quantity/activity links.
- **Progress repository** — append/read immutable dated progress snapshots.

### 24.5 Claim ports

- **Claim repository** — append claim revisions and state transitions.
- **Claim valuation service** — project claim lines from frozen progress/commercial basis.
- **Certification policy** — project-specific rules behind an explicit versioned policy boundary.

### 24.6 Reporting ports

- **Reporting projection service** — generate read models from exact snapshot IDs.
- **Export adapter** — Excel/PDF/CSV/etc. output; export format must not own domain truth.

## 25. Adapter rules

### 25.1 BricsCAD

The BricsCAD adapter may read native database/entity information and resolve host interactions, but it returns normalized domain DTO/value data. Core quantity/cost/schedule logic must not require a live BricsCAD transaction after acquisition.

### 25.2 IFC/openBIM

An IFC adapter should:

- preserve IFC/global/source identity where available;
- normalize units explicitly;
- record schema/version;
- record property/classification loss or unsupported semantics;
- never invent missing property meaning silently.

### 25.3 External schedule/cost systems

Future Primavera/MS Project/ERP/estimating integrations must be anti-corruption adapters:

- preserve external ID + namespace;
- map to QS3D IDs/versioned contracts;
- record import/export version/provenance;
- report lossy mappings;
- avoid embedding vendor SDK types in domain objects.

### 25.4 Proprietary formats

Direct proprietary-format support requires a documented licensed/supported API or lawful interchange path. This architecture does not require reverse engineering a proprietary binary format.

## 26. Persistence and audit model

Persistence should optimize for reproducibility rather than one mutable row per business concept.

Recommended logical stores:

- current editable workspace state;
- immutable published snapshots;
- append-only audit/change events;
- content-addressed/canonical digest metadata;
- adapter import manifests;
- reporting projections/caches that are rebuildable.

A reporting cache is disposable. A certified claim or quantity provenance record is not.

## 27. Current `QS3D.Core` alignment

The current source already contains pieces that fit this architecture.

### 27.1 Measurement

`MeasurementTrace` provides:

- semantic/source/quantity identity;
- input facts;
- gross/net values;
- additions/deductions with reasons and optional rule ID/version;
- explicit unit and rounding policy;
- warnings/assumptions.

`MeasurementSnapshot` provides:

- immutable deterministic ordering;
- duplicate identity rejection;
- canonical serialization.

`MeasurementSnapshotDelta*` establishes an existing direction for explicit revision delta semantics.

### 27.2 Cost

`CostCode`, `RateItem` and `RateBook` already model:

- explicit cost-code identity;
- unit/currency;
- decimal rates;
- UTC effective time;
- version tokens;
- deterministic as-of resolution.

`EstimateLine` already consumes a measurement snapshot/trace and rate book rather than modifying measurement truth. It also separates commercial quantity adjustment from measured quantity and requires a reason for non-zero commercial adjustment.

`EstimateLineFreshness`, `EstimateRevisionCostImpact` and `FrozenEstimateProjection` are compatible signals for the proposed snapshot/freshness/change model.

### 27.3 Architectural implication

The 5D design should **extend the existing one-way quantity → cost semantics**, not replace it with a monolithic mutable BIM5D object.

Schedule, progress and claim should be added as additional versioned downstream contexts that reference the same stable measurement/cost identities.

## 28. Explicitly rejected designs

### 28.1 One mutable `Project5D` object

Rejected because a model/rate/schedule edit could silently rewrite historical cost/progress/claim state and make audit reproduction difficult.

### 28.2 Cost fields embedded directly into model elements

Rejected because rates, currencies, WBS mappings and estimates change independently from geometric truth.

### 28.3 Schedule dates embedded into quantity facts

Rejected because activity sequencing is a sidecar project-management mapping and must not redefine measurement meaning.

### 28.4 Claims calculated from “latest current state”

Rejected because certified history must remain reproducible after later model/rate/schedule changes.

### 28.5 External-system schema as core model

Rejected because it couples QS3D to one vendor and leaks proprietary/runtime concepts across the product boundary.

### 28.6 Silent identity heuristics

Rejected because geometric similarity or display names are insufficient evidence for cross-revision continuity.

## 29. Testable invariants

The implementation lanes that follow this design should encode at least these invariants.

### Identity and provenance

1. Two quantity facts in one measurement snapshot cannot share the same canonical quantity identity.
2. Every published quantity fact identifies its source revision and calculation rule version where a rule is used.
3. Cross-revision semantic identity reconciliation is explicit and auditable.
4. Transient host IDs alone cannot be the persisted global domain identity.

### Quantity

5. Quantity inputs/outputs are finite and carry explicit units.
6. Deterministic measurement inputs produce deterministic canonical output.
7. Measurement-rule adjustments reconcile according to the declared rounding policy.
8. Commercial estimating adjustments do not mutate the measurement snapshot.

### Cost

9. A rate match requires cost code + compatible unit + currency + effective-time semantics.
10. Missing or ambiguous rates fail closed.
11. Money uses checked decimal arithmetic.
12. Repricing creates a new estimate snapshot; an older estimate remains reproducible.
13. A non-zero commercial quantity adjustment requires a recorded reason.

### Schedule

14. An activity allocation references an immutable schedule version and quantity scope.
15. Activity allocations do not mutate measurement facts.
16. Split allocation weights/quantities must reconcile deterministically to the declared basis.

### Progress

17. A progress snapshot references exact quantity/allocation/schedule versions.
18. Normal cumulative progress is non-decreasing; reductions require explicit correction/reversal provenance.
19. Progress cannot silently exceed eligible quantity outside an explicit versioned overrun policy.

### Claim

20. A submitted/certified claim references frozen progress and commercial bases.
21. Certified claim revisions are immutable.
22. Later source/rate/schedule changes cannot alter a certified claim's canonical value.
23. Claimed, assessed and certified quantities/values remain distinct fields.

### Change/reporting

24. Every variance identifies both compared snapshot versions.
25. Downstream staleness is explicit when an upstream referenced version changes.
26. Reports can navigate each material result back to source identity + quantity + commercial/schedule/claim provenance.
27. Rebuilding a report from the same frozen snapshot set yields the same material totals.

### Adapter/product boundary

28. Domain assemblies do not require proprietary host SDK types in their public model contracts.
29. Unsupported/lossy external mappings emit diagnostics rather than invented data.
30. No external adapter path changes the repository's BricsCAD-hosted plugin shipping form.

## 30. Suggested implementation slices after #3104

This is sequencing guidance only; it does not expand this design lane into implementation.

### Slice A — shared identity/provenance contract

- project/source/revision/semantic identifiers;
- common provenance envelope;
- canonical digest versioning;
- compatibility adapter for current measurement identity tuple.

### Slice B — WBS/cost mapping versioning

- mapping-set aggregate;
- deterministic precedence/ambiguity diagnostics;
- estimate input manifest.

### Slice C — estimate snapshot

- freeze `EstimateLine` collections with mapping/rate provenance;
- canonical digest/freshness status;
- baseline/current variance projection.

### Slice D — schedule and activity allocation

- schedule version/calendar contracts;
- quantity-to-activity allocation set;
- no mutation path into measurement.

### Slice E — progress snapshots

- period/data-date semantics;
- cumulative and correction model;
- earned quantity projection.

### Slice F — claim revisions

- claim period/state machine;
- frozen commercial/progress basis;
- assessment/certification successor semantics.

### Slice G — reporting and adapter expansion

- source-to-report trace navigation;
- IFC/openBIM/schedule/ERP adapters behind ports;
- explicit diagnostics for lossy mappings.

Each slice should preserve `netstandard2.0` compatibility for contracts intended to remain consumable by the V25 path until the Platform migration plan explicitly changes that boundary.

## 31. Acceptance mapping for issue #3104

| Issue requirement | Architecture decision |
|---|---|
| stable model element identity | scoped source + semantic identity hierarchy; explicit cross-revision reconciliation |
| quantity facts | immutable trace/snapshot with rule/unit/provenance |
| WBS/cost-code mapping | separate versioned mapping context |
| rates/resources | versioned effective commercial data, independent from geometry |
| schedule/activity links | versioned sidecar allocation context |
| progress/claim snapshots | dated immutable progress; claim revisions over frozen bases |
| variance/change history | explicit change events + baseline-to-version variance |
| reporting | rebuildable projections with traceability to exact snapshots |
| deterministic/auditable recalculation | exact input manifest + canonical ordering/digest + fail-closed ambiguity |
| cost/schedule must not mutate geometric truth | one-way dependency and immutable upstream facts |
| external integrations behind adapters | BricsCAD/openBIM/external-system anti-corruption boundaries |
| no proprietary reverse engineering requirement | lawful API/interchange adapter rule |
| BricsCAD/plugin boundary explicit | hosted plugin remains the shipping/runtime form |

## 32. Final architecture rule

A QS3D result should always be able to answer:

```text
What source element/revision did this come from?
What quantity rule and measured fact produced it?
What mapping and rate version priced it?
What schedule/activity allocation interpreted it over time?
What dated progress snapshot supported the claim?
What claim revision was assessed/certified?
What changed between this result and the chosen baseline?
```

If a design cannot answer those questions without consulting mutable “current state”, it is not sufficiently auditable for the quantity → cost → schedule/progress/claim domain.

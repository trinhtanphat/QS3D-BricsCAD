# QS3D Product Benchmark, Business-Logic Gaps, and Roadmap — 2026-08-12

> **Status:** Advisory / non-canonical product and architecture note.
>
> This document records the current product assessment and recommended direction. It is **not** implementation-completion truth and it does **not** promote managed-code coverage to native BricsCAD production readiness.
>
> Canonical repository truth remains in:
>
> - `docs/PRODUCT-BOUNDARY.md`
> - `docs/IMPLEMENTATION-STATUS.md`
> - `docs/PLAN.md`
> - runtime/native qualification evidence and the current work claims under `docs/agent-work-claims/`
>
> Prepared against the moving `main` branch observed on 2026-08-12. Re-check current source, claims, and native evidence before turning any recommendation below into an implementation claim.

## 1. Executive thesis

QS3D should **not** become “Revit inside BricsCAD.” Its strongest product position is a focused BIM/QS/rebar/BOQ/documentation/interchange layer for quantity surveyors, estimators, structural users, and production teams working in BricsCAD.

The repository already contains substantial geometry, semantic metadata, reporting, rebar, grids, documentation, takeoff, validation, preview/diff, and interchange building blocks. The next product-value step is therefore not simply “add more object categories.” The highest-leverage work is to make quantities **trustworthy, explainable, editable, versioned, cost-aware, revision-aware, and native-runtime qualified**.

The product principle for the next phases should be:

> **One canonical quantity truth, one explainable measurement path, one cost truth, and explicit evidence for every production-readiness claim.**

## 2. Current product baseline

The following are architectural/product assets already visible in the repository and canonical status documentation. Their presence here does **not** imply that every path is fully native-qualified on every supported BricsCAD host/version.

### 2.1 Strong building blocks to preserve

- Parametric/semantic structural modeling and domain objects.
- Stable semantic metadata, source handles/provenance work, and health/validation services.
- Quantity-oriented domain logic and `QuantityRules` concepts.
- Preview/diff/regeneration patterns that can support safe edits.
- BOQ/BQ/BBS/schedule/reporting surfaces.
- Rebar-specific domain logic rather than only generic solids.
- Grid, documentation, annotation, and drawing-production workflows.
- Polygon/mesh/takeoff and measured-solid related quantity paths.
- Interchange/import/export/provenance work.
- A broad managed smoke/regression surface and explicit claim-first coordination.

### 2.2 Important qualification caveat

Managed build/test success and repository implementation depth are not equivalent to native BricsCAD production qualification. Native-dependent commands, adapters, objects, drawing effects, transactions, persistence paths, and host-version behavior need their own evidence.

Product/status reporting should continue to separate at least:

1. implemented in source;
2. covered by deterministic managed tests/smokes;
3. adapter/integration exercised;
4. native-host qualified on a named BricsCAD version/environment;
5. production-ready according to an explicit acceptance gate.

## 3. Competitive capability patterns worth borrowing

This is a capability-pattern benchmark, not an instruction to clone another product.

| Reference | Capability pattern worth learning from | Implication for QS3D | Do not copy blindly |
|---|---|---|---|
| Glodon Cubicost | Quantity takeoff centered on model-derived quantities and calculation/measurement rules | Evolve existing quantity rules into versioned, explainable measurement semantics with local/trade profiles | Do not reproduce every trade-specific UI or build a second quantity engine |
| RIB CostX | Integrated takeoff + estimating + revision-oriented workflow | Add a clean rate/cost domain and connect revision quantity deltas to estimate impact | Do not start by building a large enterprise estimating suite |
| Autodesk Takeoff | Combined 2D/3D quantification, classification, formulas/unit-cost workflow | Strengthen classification, quantity traceability, and convergence of drawing/model takeoff | Keep BricsCAD-native specialist workflow rather than broad platform parity |
| Solibri | Rule-based model checking and open collaboration workflows | Evolve semantic health into declarative QS/model-check profiles; consider IDS/BCF later | Do not prioritize a full clash-detection platform before QS-specific checking is mature |

### External benchmark references

These links are benchmark inputs, not endorsements and not repository completion evidence:

- Glodon Cubicost TAS: <https://www.glodon.com/en/products/cubicost-tas-8>
- RIB CostX: <https://www.rib-software.com/en/rib-costx>
- Autodesk Takeoff: <https://construction.autodesk.com/products/autodesk-takeoff/>
- Solibri: <https://www.solibri.com/>

## 4. Product maturity matrix

| Capability | Current posture observed in this assessment | Desired target | Priority |
|---|---|---|---|
| Semantic identity / source provenance | Strong foundation; still active hardening work | Stable identity across model, report, revision, and interchange | P0 |
| Geometric quantity extraction | Broad managed implementation | Deterministic + host-qualified measurement basis | P0 |
| Measurement semantics | Existing quantity-rule concepts, but explainability/versioning can go further | Versioned measurement standards and rule profiles | P0 |
| Quantity explainability | Information exists across services/outputs, not yet one universal trace contract | Every reportable quantity has a machine-readable and human-readable trace | P0 |
| Edit / inspect / preview UX | Useful primitives exist | Fast inspect → edit → preview diff → apply workflow | P0 |
| Revision quantity delta | Identity/diff foundations exist | First-class added/removed/changed quantity ledger | P0–P1 |
| Rate / cost estimating | No dedicated end-to-end cost domain was identified in this audit pass | Rate book + estimate lines + snapshots + revision impact | P1 |
| Rule-based QS QA | Semantic health/validation exists | Declarative profile-based checker with evidence and safe autofix | P2 |
| Collaboration / issue loop | Interchange/provenance actively improving | Traceable issue/provenance loop, later open-standard exchange | P2 |
| Native production qualification | Must remain separate wherever host evidence is incomplete | Explicit versioned host qualification matrix and artifacts | P0 |

## 5. Keep, evolve, merge, and avoid duplicating

### 5.1 Keep as strategic foundations

- `QuantityRules` as the basis for canonical measurement behavior.
- Semantic metadata and semantic health/validation.
- Source-handle/provenance identity rather than report-local identity.
- Preview/diff/regeneration instead of silent mutation.
- Existing BOQ/BQ/BBS/schedule/report families as projections of domain truth.
- Core/host separation so business rules remain deterministic and testable.

### 5.2 Evolve

- **Quantity rules → measurement semantics:** add standards, versions, deductions, rounding, assumptions, and trace output.
- **Semantic health → QS checker profiles:** make rules declarative, severity-aware, explainable, and reusable.
- **Source provenance → revision/cost trace:** use stable identity to connect object change → quantity change → estimate change.
- **Preview/diff → edit workflow:** make the same safe-diff mechanism the default for batch edits and rule changes.
- **Reports → pure projections:** a report should consume canonical quantities/costs, not independently re-derive hidden business math.

### 5.3 Merge or prevent duplicate systems

The following are architecture rules for future implementation, not assertions that every duplicate currently exists:

- Do not create a second quantity engine for reporting, export, or one object category.
- Do not allow report-specific hidden deductions, rounding, or conversions.
- Do not introduce another semantic/provenance identity scheme beside the canonical one.
- Do not store tender/rate assumptions inside geometric entities simply because a report needs them.
- Do not let BricsCAD adapter details become the source of core measurement rules.
- Consolidate any discovered duplicate rounding, deduction, unit-conversion, stale-output, or report-only quantity helpers into canonical domain services when doing future targeted audits.

## 6. Missing or underdeveloped business logic

### 6.1 Explainable measurement semantics — highest priority

The next generation of quantity logic should make every number answer the question: **“Why is this quantity exactly this value?”**

Candidate domain concepts:

- `MeasurementStandard`
- `MeasurementRuleSet`
- `MeasurementRuleVersion`
- `MeasurementContext`
- `QuantityExpression`
- `DeductionRule`
- `OpeningTreatment`
- `RoundingRule`
- `WasteRule`
- `AggregationRule`
- `MeasurementTrace`
- `MeasurementSnapshot`

A `MeasurementTrace` should be capable of carrying, where applicable:

- source object/semantic identity;
- source geometry/property inputs;
- gross basis;
- each deduction/addition and its reason;
- net measured quantity;
- rule ID and rule version;
- unit and conversion path;
- rounding policy;
- normalized expression/equation;
- warnings, assumptions, fallback values, and unresolved inputs.

Requirements:

- deterministic for identical canonical inputs;
- culture-invariant at persistence/exchange boundaries;
- explicit about units;
- versioned so a later rule change cannot silently rewrite historical measurement meaning;
- able to support local/trade/company rule profiles without forking the quantity engine.

### 6.2 Rate and cost estimation domain

QS3D should separate **measurement truth** from **commercial assumptions**.

Candidate concepts:

- `RateBook`
- `RateItem`
- `CostCode`
- `ResourceRate`
- `EstimateLine`
- `EstimateSnapshot`
- `EstimateRevision`

An estimate line should explicitly distinguish:

- measured quantity;
- estimating quantity;
- waste/loss factor;
- unit rate;
- currency;
- rate effective date/version;
- direct cost;
- markup/overhead/contingency where supported;
- final extended amount;
- source measurement trace/snapshot.

Do not mix rate/tender assumptions into geometry metadata merely to make them easy to display.

### 6.3 Revision → quantity → cost impact

Stable identity becomes much more valuable when it supports a first-class revision ledger.

Target workflow:

1. capture a canonical measurement/estimate snapshot;
2. regenerate or import a revised model/drawing state;
3. classify entities/lines as added, removed, unchanged, or changed;
4. compute quantity deltas by canonical identity and rule version;
5. explain the reason for each delta where possible;
6. propagate the delta into estimate impact using an explicit rate snapshot;
7. export a reviewable revision report without re-computing different business logic in the renderer.

Suggested outputs:

- previous quantity;
- current quantity;
- delta;
- previous/current rule version;
- previous/current rate version where applicable;
- cost delta;
- reason/category of change;
- source handles/provenance;
- unresolved/missing identity warnings.

### 6.4 Rule-based QS/model QA

Build on semantic health instead of starting a parallel checker framework.

A rule should have at least:

- stable rule ID;
- profile/category;
- severity;
- deterministic condition;
- human explanation;
- affected semantic identities;
- evidence values;
- optional safe autofix only when the transformation is deterministic and reversible/previewable.

Good early QS rules:

- missing/ambiguous classification;
- invalid/non-finite/impossible quantity inputs;
- malformed semantic/source metadata;
- missing level/floor/family relationships;
- missing rate for an estimate line;
- stale measurement or estimate snapshot;
- inconsistent unit metadata;
- report values that cannot be traced to canonical quantities;
- revision lines whose identity cannot be reconciled.

Later, open-standard integrations such as IDS/BCF can be considered where they materially help the workflow. They should not block the core QS checker.

### 6.5 Inspect, edit, batch-edit, and “why this quantity?” UX

The product needs a fast interaction loop around the strong domain layer:

- select object(s);
- inspect semantic properties and health;
- inspect the canonical quantity trace;
- see rule/version/unit/rounding/deductions;
- edit one or many allowed properties;
- preview affected geometry/quantities/reports;
- apply only after validation;
- preserve a reviewable change/revision trail.

Avoid silent mutation. Batch actions should surface counts, skipped objects, warnings, and the exact effect before destructive/large updates.

### 6.6 Runtime qualification is part of product quality, not an afterthought

A native-dependent feature is not production-ready merely because its Core logic is correct.

Maintain an explicit host qualification matrix with at least:

- BricsCAD major/minor/build;
- .NET/runtime environment;
- command or object surface tested;
- fixture/drawing used;
- expected and observed native effects;
- transaction/persistence/reopen result where relevant;
- qualification date;
- evidence artifact/log;
- pass/fail/blocked classification;
- known host-specific deviations.

This evidence should remain separate from managed test counts.

## 7. Recommended product architecture

### Layer 1 — Deterministic Core domain

Owns semantic identity, geometry-independent business invariants, units, quantity primitives, validation, canonical serialization, and deterministic calculation contracts.

### Layer 2 — Measurement and Estimate domain

Owns measurement standards/rules/traces/snapshots plus rate books, estimate lines, estimate snapshots, and revision-impact logic.

### Layer 3 — BricsCAD host adapters

Owns entity access, transactions, custom-object integration, geometry extraction/application, document state, selection, and host-version behavior. It must not become the canonical home of QS measurement policy.

### Layer 4 — Command/UI orchestration

Owns inspect/edit/preview/apply flows and batch operations. UI should call domain services rather than reproduce quantity/cost math.

### Layer 5 — Reporting/export projections

BOQ/BQ/BBS/schedules/XLSX/interchange outputs should be projections of canonical domain outputs and snapshots. A renderer/exporter should not invent independent deductions or cost formulas.

### Layer 6 — Verification and evidence

Owns deterministic tests, adapter smokes, native qualification evidence, release gates, trace completeness checks, and stale-snapshot detection.

### Architecture invariants

1. One canonical quantity truth.
2. One canonical cost/estimate truth for a selected estimate snapshot.
3. Revision comparison uses stable identities and explicit snapshots.
4. Reports do not secretly reimplement measurement math.
5. Units and rule versions are explicit at persistence boundaries.
6. Host dependence is explicit and isolated.
7. Every production-readiness claim points to native evidence where native behavior is involved.

## 8. Product anti-goals

For the foreseeable roadmap, QS3D should **not** optimize for:

- becoming a general Revit/family-authoring replacement;
- broad architectural authoring parity for checklist reasons;
- a full MEP routing/fabrication suite;
- a cloud CDE/document-management replacement;
- a rendering/general-design platform;
- a full Solibri-like clash platform before QS-specific checking is mature;
- duplicate measurement engines by category or report;
- object categories added only to increase feature-count optics;
- paper-completed native features without host evidence;
- commercial assumptions embedded into geometry simply for convenience.

## 9. Prioritized roadmap

### P0 — Trust the quantity

Goal: a QS user can trust, inspect, reproduce, and qualify every important quantity.

Deliverables:

- `MeasurementTrace` contract and canonical trace projection.
- Measurement rule identity/versioning.
- Explicit unit/rounding/deduction trace.
- Stable provenance/identity convergence across quantity/report/interchange paths.
- Quantity revision snapshot/delta foundation.
- Inspector path for “why this quantity?”.
- Native qualification matrix and evidence discipline for key workflows.
- Continued removal of stale/duplicate/report-only quantity calculations found by audit.

P0 exit criteria:

- important reportable quantities can be traced to canonical source inputs and rule versions;
- identical canonical input produces deterministic measurement output;
- report output does not require hidden quantity recomputation;
- native-dependent P0 workflows have named host evidence rather than managed-only claims.

### P1 — Quantity to estimate

Goal: turn trusted measured quantities into a small, rigorous estimating workflow.

Deliverables:

- `RateBook` / `RateItem` / `CostCode` contracts.
- `EstimateLine` and estimate snapshot.
- waste factor and explicit commercial adjustments separate from measured quantity;
- currency/rate effective-date/version handling;
- quantity revision → cost impact;
- estimate/BQ export based on frozen snapshots.

P1 exit criteria:

- an estimate can be reproduced from measurement snapshot + rate snapshot;
- changed quantities can produce an explainable cost delta;
- historical estimates do not silently change when rate books or measurement rules are edited.

### P2 — Rules and collaboration

Goal: make QS quality and review scalable across teams.

Deliverables:

- declarative QS checker profiles;
- severity/evidence/autofix-preview model;
- stale snapshot/report detection;
- review issues linked to semantic identities and source provenance;
- consider IDS/BCF integration where it serves concrete workflows;
- stronger interchange issue/provenance round-tripping.

P2 exit criteria:

- a project can run a named QA profile and receive deterministic, reviewable findings;
- issues remain traceable across model/report/interchange revisions;
- safe fixes are previewable and do not bypass domain validation.

### P3 — Scale specialist workflows

Goal: deepen the product where real QS/structural usage proves value.

Possible directions, prioritized by user evidence rather than feature-count parity:

- company/trade measurement-standard packs;
- richer structural/rebar estimating assemblies;
- productivity templates and repeatable project setup;
- larger-project performance and incremental regeneration;
- broader interoperable classification/cost-code mappings;
- selected domain categories only when they reuse canonical measurement/edit/report architecture.

P3 exit criteria should be defined per validated workflow, not by raw number of supported object types.

## 10. Recommended epics

| Epic | Priority | Outcome |
|---|---|---|
| Measurement Rules v2 — Explainable Quantity | P0 | Every important quantity exposes canonical rule/version/input/deduction/rounding trace |
| Quantity Revision Ledger | P0 | Stable snapshot-to-snapshot quantity delta with reasons/provenance |
| Inspector & Batch Edit | P0 | Fast inspect/edit/preview/apply UX over canonical domain services |
| Production Qualification Matrix | P0 | Native readiness backed by host/version-specific evidence |
| Cost & Rate Domain | P1 | Reproducible rate-book and estimate-line model separated from geometry |
| Revision Cost Impact | P1 | Quantity deltas flow into explainable estimate deltas |
| QS Rule Checker | P2 | Declarative quality profiles with evidence and safe fixes |
| Interchange Issue/Provenance Loop | P2 | Review issues and exchanged quantities remain identity-traceable |

## 11. First 10 implementation tickets

These are proposed tickets; they are **not** claims that implementation is currently absent everywhere or that work is already reserved.

### 1. Add a canonical `MeasurementTrace` contract

Acceptance:

- immutable/detached result;
- semantic/source identity included;
- inputs, gross, deductions/additions, net, unit, rounding, rule ID/version represented;
- deterministic equality/serialization decisions documented;
- focused regression coverage for malformed/non-finite inputs.

### 2. Version measurement rules

Acceptance:

- rule identity and version are explicit;
- snapshot stores the version used;
- editing a rule does not silently reinterpret a historical snapshot;
- migration/backward-compat behavior is explicit.

### 3. Project existing quantity services into trace output

Acceptance:

- start with a narrow, high-value category set;
- numeric output remains backward-compatible unless an existing defect is proven;
- trace explains existing result rather than adding a second calculation path;
- report/export consumes the same result.

### 4. Add “why this quantity?” inspector surface

Acceptance:

- selected semantic object can display quantity basis, deductions, unit, rule version, warnings, and provenance;
- unavailable trace fails visibly instead of fabricating an explanation;
- no business math implemented in UI code.

### 5. Add measurement snapshot + quantity delta contract

Acceptance:

- added/removed/changed/unchanged classification is deterministic;
- identity ambiguity fails visibly;
- old/new quantity and delta preserved;
- changed rule version is distinguishable from changed geometry/property input.

### 6. Add native qualification evidence schema/matrix

Acceptance:

- host version/build/environment and test surface recorded;
- managed vs native qualification cannot be confused;
- evidence links/artifacts and date are recorded;
- release/status docs can consume the matrix without manually rewriting facts.

### 7. Add minimal `RateBook` and `RateItem` domain

Acceptance:

- explicit unit, currency, effective date/version;
- immutable snapshot semantics for estimating;
- no geometry dependency;
- invalid/non-finite/ambiguous rates fail closed.

### 8. Add `EstimateLine` from measurement snapshot + rate snapshot

Acceptance:

- measured quantity is preserved separately from waste/estimating quantity;
- rate and commercial adjustments are explicit;
- extended cost is deterministic;
- source measurement trace is linkable;
- report/export does not recompute the estimate independently.

### 9. Add revision cost-impact projection

Acceptance:

- previous/current estimate snapshot comparison;
- quantity delta and rate delta separated;
- cost delta reason visible;
- missing rate/identity produces an actionable finding, not zero-cost silence.

### 10. Add QS Rule Checker v1 on top of semantic health

Acceptance:

- named rules/profile/severity/evidence;
- deterministic evaluation order/output;
- first profile includes classification, invalid quantity input, malformed metadata, stale snapshot, and missing rate checks where applicable;
- autofix, if present, is only for safe deterministic changes and uses preview/apply semantics.

## 12. Proposed quality gates and product KPIs

These are proposed measurements, not current repository metrics.

### Quality gates

- **Trace completeness:** high-value quantities must provide a valid trace before a release can call them explainable.
- **Deterministic rerun:** same canonical input + same rule snapshot produces the same quantity/estimate output.
- **No hidden recomputation:** reports/exports must not implement independent business formulas for canonical quantities/costs.
- **Stale-state detection:** measurement/estimate/report snapshots must detect when their source state is no longer current.
- **Native evidence:** native-dependent release claims require host/version-specific evidence.
- **Identity integrity:** ambiguous/duplicate identity cannot be silently deduplicated in revision, provenance, or estimate reconciliation.
- **Unit integrity:** persistence and external boundaries use explicit, canonical unit semantics.

### Useful product KPIs

- percentage of reportable quantity lines with complete trace;
- percentage of target native commands/objects qualified on the supported host matrix;
- stale snapshot detection rate in regression fixtures;
- unresolved classification/rate findings per project;
- revision quantity lines reconciled by stable identity;
- revision cost deltas that can be attributed to quantity vs rate change;
- count of known report-specific hidden quantity recomputations — target `0`;
- edit-preview/report consistency failures — target `0` for qualified workflows.

## 13. Decision policy for future features

Before adding a substantial feature/category, answer all five questions:

1. Does it materially improve quantity, estimate, rebar, documentation, interchange, or QS-review fidelity?
2. Can its business result be deterministic and explainable?
3. Does it reuse canonical identity/quantity/edit/report infrastructure rather than fork it?
4. Is native qualification work budgeted where the host is involved?
5. Does it serve the specialist QS/structural product boundary better than strengthening an existing workflow?

If the answer to several of these is “no,” defer the feature even if a competitor has it.

## 14. Recommended delivery sequence

### Wave 1 — Measurement trust

- `MeasurementTrace`;
- rule versioning;
- identity/provenance convergence;
- native qualification matrix;
- trace projection for a narrow set of highest-value quantities.

### Wave 2 — Revision and editing

- inspector / “why this quantity?”;
- batch edit with preview/apply;
- measurement snapshot and revision quantity delta;
- stale-snapshot/report detection.

### Wave 3 — Estimating

- rate book;
- estimate lines/snapshots;
- waste/commercial adjustments;
- revision cost impact;
- estimate-aware export/report projection.

### Wave 4 — QS checking and collaboration

- declarative checker profiles;
- issue/evidence workflow;
- interchange provenance loop;
- selective IDS/BCF integration if concrete customer workflows justify it.

## 15. Repository references and source-of-truth policy

Use this note to guide prioritization, then verify each implementation decision against current repository truth:

- Product boundary: `docs/PRODUCT-BOUNDARY.md`
- Implementation status: `docs/IMPLEMENTATION-STATUS.md`
- Canonical plan: `docs/PLAN.md`
- Multi-agent coordination: `docs/AGENT-WORK-REGISTRATION.md`
- Current reservations/completions: `docs/agent-work-claims/`

If this document conflicts with current canonical status, source code, tests, or native qualification evidence, **the current canonical/source/evidence state wins**. Update this roadmap later as a strategy snapshot; do not use it to overwrite factual completion status.

## 16. Bottom line

QS3D is already broad enough that the highest-value next step is not indiscriminate breadth. The stronger product path is:

**semantic model → explainable measured quantity → revision-aware quantity snapshot → rate/estimate snapshot → explainable cost impact → rule-based QS review → qualified BricsCAD production workflow.**

That chain turns existing geometry, metadata, reporting, and interchange depth into a coherent QS product while keeping the codebase focused, deterministic, auditable, and materially different from a generic BIM authoring clone.

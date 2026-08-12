# BLT3D Research → QS3D Agent Workstreams — 2026-08-12

> **Status:** Advisory implementation queue / multi-agent coordination note.  
> **This file does not reserve any implementation scope.** Only current claim files with status `ACTIVE` or `BLOCKED` under `docs/agent-work-claims/` reserve work.
>
> Research source retained separately: `docs/research/BLT3D-GEMINI-RESEARCH-MASTER-2026-08-12.md`.  
> Product/roadmap context: `docs/PRODUCT-BENCHMARK-AND-ROADMAP-2026-08-12.md`.  
> Canonical product boundary remains `docs/PRODUCT-BOUNDARY.md`, current architecture/source/tests/runtime evidence, and current repository status documents.

## 1. Purpose

This document turns the BLT3D research/benchmark material and the QS3D product roadmap into **small, claimable implementation lanes** that multiple agents can work on without duplicating or overwriting each other.

The intent is not to clone BLT3D and not to treat Gemini-generated competitor analysis as verified product fact. The research archive remains a source of ideas and business-workflow questions. Before implementing any idea, an agent must verify that:

1. the requirement fits the current QS3D product boundary;
2. current `main` does not already implement it sufficiently;
3. no current `ACTIVE`/`BLOCKED` claim owns the same capability, invariant, command family, test scenario, or documentation truth;
4. the implementation reuses canonical QS3D identity/measurement/reporting architecture rather than creating a parallel engine;
5. deterministic managed tests can prove the Core behavior, and native BricsCAD claims are made only when real host evidence exists.

## 2. The multi-agent rule — claim first, then implement

Yes: the repository is deliberately coordinated by **publishing ownership to `main` before substantive work**.

Canonical protocol: `docs/AGENT-WORK-REGISTRATION.md`.

### Mandatory sequence for every implementation agent

1. Fetch/refresh current `origin/main` and inspect recent commits.
2. Read `docs/AGENT-WORK-REGISTRATION.md`.
3. Inspect all current claims with status `ACTIVE` or `BLOCKED`.
4. Read the full claim for anything touching the same feature, files, commands, invariants, test scenarios, runtime surface, or canonical documentation.
5. Choose the **smallest non-overlapping sub-lane** from this document or another verified defect/feature.
6. Confirm from current source/tests that the work is actually missing or defective; do not rely only on this roadmap or the BLT3D research archive.
7. Create a unique claim under `docs/agent-work-claims/` with exact scope, exclusions, baseline SHA, expected files/tests, validation plan, and completion condition.
8. Commit and push the **claim alone** to `main`.
9. Refresh `main` again, verify the claim commit is still on current lineage, and recheck newly published claims.
10. Only then modify source/tests/docs in the reserved scope.
11. Add regression tests or deterministic evidence appropriate to the change.
12. Refresh/reconcile with current `main`, commit and push without force-push.
13. Re-fetch the pushed result and verify the intended files/behavior.
14. Update the same claim to `COMPLETED` and record implementation commit(s), validation actually executed, and any remaining native/local gates. If abandoning the lane, mark it `RELEASED` instead.

### Status meaning

- `ACTIVE` — scope is owned; other agents must avoid overlap.
- `BLOCKED` — scope remains owned while blocked; other agents must avoid overlap unless an explicit split/takeover is recorded.
- `COMPLETED` — work is closed; no longer reserves scope.
- `RELEASED` — agent stopped; scope is available again.

### Important coordination rules

- This workstream file is **not** an assignment table and creates no ownership by itself.
- Do not create claims for many future lanes just to “reserve” them. Claim only work the agent is ready to perform.
- Do not claim an entire epic when one invariant or one independently testable behavior is enough.
- File-level non-overlap is not sufficient. Two agents conflict if they independently change the same user-visible capability or business invariant even in different files.
- Parallel work on one epic is allowed only when boundaries are explicit and independently verifiable.
- Never take over an `ACTIVE`/`BLOCKED` lane solely because it looks old.
- Never force-push.
- Do not dispatch GitHub Actions merely because a claim exists; CI/release policy still governs Actions.
- Do not report BricsCAD native PASS unless the exact host/runtime scenario was actually executed.

## 3. Product/repository boundary for this backlog

QS3D remains a **BricsCAD-hosted QS semantic plugin**. The normal in-repo product chain is:

```text
DWG / PDF / IFC input
        ↓
Recognize / Capture / Direct Draw
        ↓
Semantic Project
        ↓
Measurement Facts
        ↓
Measurement Rules
        ↓
Explainable Quantities
        ↓
Classification / Work Item / BOQ
        ↓
Rates / Estimate
        ↓
Revision Quantity + Cost Delta
        ↓
BQ / Schedules / BBS / Excel / DWG Tables / Interchange
```

### Belongs in `QS3D-BricsCAD` when implemented

- deterministic semantic/identity contracts;
- measurement facts/rules/deductions/rounding/traces;
- quantity/regeneration logic;
- classification/work-item/BOQ mapping;
- estimate/rate domain that depends on canonical measurements;
- revision quantity/cost snapshots and deltas;
- QS-specific health/rule checking;
- BricsCAD-native semantic editing and adapters;
- 2D/3D takeoff integration that feeds canonical semantics/measurement;
- IFC/BCF/interchange contracts/adapters related to QS3D identity/provenance;
- rebar/BBS/cutting logic;
- carefully scoped MEP/civil quantity modules after the foundation is mature;
- reporting/export projections consuming canonical truth;
- deterministic tests, smokes, qualification schema and evidence summaries allowed by repository policy.

### Do **not** place directly in this plugin repo as speculative product expansion

These may eventually become separate services/repos/products, but are outside the current plugin implementation queue unless the owner explicitly reopens the product boundary:

- cloud multi-user database/CDE/CRDT/event-sourcing platform;
- account/auth/project-sync SaaS;
- mobile/site-inspection application;
- large AI training/model-serving platform;
- generic ERP/accounting backend;
- central multi-company cost-data SaaS;
- generic carbon/ESG analytics platform;
- DfMA/CNC/G-Code manufacturing platform;
- facilities-management/7D digital-twin platform;
- city-scale analytics, credit scoring, bankruptcy prediction, or national-data infrastructure.

QS3D may later define **contracts/adapters** for external systems, but deterministic Core quantity truth must not require a remote AI/cloud service to function.

## 4. Dependency map

Use this dependency order when choosing work. Later lanes should not invent temporary duplicate infrastructure merely because their prerequisite is unfinished.

```text
                    ┌──────────────────────────────┐
                    │ P0 Native edit / scale      │
                    │ host qualification          │
                    └──────────────┬───────────────┘
                                   │
Semantic identity ──→ Measurement Facts / Rule Versioning
                                   │
                                   ▼
                         MeasurementTrace
                          gross/add/net/unit
                                   │
                    ┌──────────────┼───────────────┐
                    ▼              ▼               ▼
               Why Inspector   BOQ Mapping   Measurement Snapshot
                                   │               │
                                   ▼               ▼
                              Coverage         Quantity Delta
                                   │               │
                                   └──────┬────────┘
                                          ▼
                                  Rate / Estimate
                                          │
                                          ▼
                                  Revision Cost Delta

Semantic Health ─────────────────────────→ QS Rule Checker
Measurement/identity foundation ─────────→ 2D/3D Takeoff
Identity/interchange foundation ─────────→ IFC / BCF round-trip
Rebar/BBS foundation ────────────────────→ Cutting/Waste optimisation
Stable measurement architecture ─────────→ MEP / Civil expansion
```

## 5. Recommended agent waves

Agents do not have to wait for a whole wave to finish. The wave labels describe dependency risk, not ownership.

### Wave 0 — verify and protect the foundation

- inspect current source before assuming roadmap tickets are absent;
- preserve one canonical quantity truth;
- preserve DWG geometry truth + `.qsdb` semantic truth boundary;
- harden identity/stale-state/determinism defects discovered while implementing the later lanes;
- establish small contracts before UI/report projections.

### Wave 1 — P0 quantity trust + native correctness

- measurement trace contract;
- measurement rule identity/versioning;
- project existing quantity calculations into trace output;
- native semantic edit correctness;
- large-model/performance/native evidence schema and budgets;
- measurement snapshot foundation;
- classification/work-item mapping foundation.

### Wave 2 — P0/P1 user value on top of trusted quantity

- “why this quantity?” inspector;
- quantity coverage report/findings;
- deterministic quantity revision delta;
- minimal rate/estimate domain;
- stale measurement/estimate/report detection.

### Wave 3 — P1/P2 interoperability and QS review

- revision cost impact;
- QS Rule Checker on Semantic Health;
- 2D count/length/area takeoff feeding canonical mapping;
- IFC identity/classification/QTO round-trip acceptance and implementation;
- BCF review/provenance loop where useful.

### Wave 4 — specialist expansion

- rebar cutting/waste/procurement optimisation;
- deeper company/trade measurement packs;
- MEP quantity domain;
- civil/earthwork depth;
- external-service contracts only where justified by a real workflow.

## 6. Claimable workstreams

The IDs below are queue identifiers only. They are **not claims**.

### MTR — Measurement Rules v2 / Explainable Quantity

#### MTR-01 — Canonical `MeasurementTrace` contract

**Priority:** P0  
**Dependencies:** current semantic identity + quantity contracts.  
**Goal:** represent the reason behind a quantity without creating a second calculation engine.

Minimum trace information should be evaluated against current source and may include:

- semantic/source identity;
- quantity kind;
- canonical input facts;
- gross basis;
- deduction/addition lines with reasons and source identities;
- net result;
- unit/conversion path;
- rule ID/version;
- rounding policy;
- warnings/assumptions;
- source provenance.

**Claim granularity:** one Core contract + serialization/equality/invariant tests.  
**Do not own in this lane:** report UI, all category migrations, cost, native editing.

**Acceptance:** deterministic representation; finite/canonical units; no hidden recomputation; regression tests demonstrate stable trace identity/content.

#### MTR-02 — Measurement rule identity/versioning

**Priority:** P0  
**Dependencies:** inspect existing `QuantityRules` and persistence first.

**Goal:** make rule identity/version explicit so historical measurement snapshots can explain which rules produced a result.

**Claim granularity:** rule-version contract + persistence/compatibility tests only.  
**Do not own:** re-writing all rule logic or all persistence formats without evidence.

**Acceptance:** same canonical input + same rule version remains deterministic; malformed/unknown version states fail visibly according to current compatibility policy.

#### MTR-03 — Project existing quantity paths into traces

**Priority:** P0  
**Dependencies:** MTR-01; MTR-02 where version is required.

Split by a **small category/family of quantity services**, for example one claim for walls/finishes, another for structural elements, another for openings, only if current architecture supports a clean split.

**Rule:** existing canonical calculation remains authoritative. Trace generation observes/explains that result; it must not independently recalculate the same quantity using separate formulas.

**Acceptance:** output quantity equals canonical existing result; trace gross/deductions/net reconcile exactly; regression proves no duplicate math path.

#### MTR-04 — “Why this quantity?” inspector model

**Priority:** P0/P1  
**Dependencies:** useful MTR-03 coverage.

Separate Core/view-model preparation from host UI if that creates a clean ownership boundary.

**Acceptance:** selecting a quantity can expose source identity, rule/version, formula/breakdown, deductions, warnings and related elements without report-specific math.

#### MTR-05 — Units, rounding and deduction policy audit

**Priority:** P0 continuous hardening.

This is not a license to refactor everything. Agents should claim one proven inconsistency at a time: duplicate rounding, culture-sensitive parse/format, non-finite acceptance, signed-zero canonicality, hidden deduction, or report-specific conversion.

**Acceptance:** one invariant + one focused regression + no parallel business-rule path.

---

### MAP — Classification / Work Item / BOQ Mapping + Coverage

#### MAP-01 — Mapping domain contract

**Priority:** P0/P1  
**Dependencies:** canonical semantic identity; measurement-ready state.

Define or evolve explicit mapping among semantic element/category, classification, measurement item and BOQ/work item.

**Do not:** hard-code BOQ mapping inside geometry/regenerators or individual report renderers.

**Acceptance:** deterministic mapping identity, explicit unmapped state, safe persistence, regression for duplicate/ambiguous mapping.

#### MAP-02 — Quantity coverage evaluator

**Priority:** P0/P1  
**Dependencies:** MAP-01 plus current health/stale semantics.

Coverage should expose counts/reasons such as:

- quantity-ready;
- missing classification;
- missing measurement rule;
- stale quantity;
- ambiguous host/identity;
- invalid geometry/input;
- unmapped BOQ/work item.

**Acceptance:** coverage derives from canonical project/health/quantity state and never silently treats missing data as zero/ready.

#### MAP-03 — Coverage UI/report projection

**Priority:** P1  
**Dependencies:** MAP-02.

**Acceptance:** UI/report is a projection of the evaluator; no independent readiness logic in renderer code.

---

### REV — Measurement Snapshot / Quantity Revision Ledger

#### REV-01 — Canonical measurement snapshot

**Priority:** P0/P1  
**Dependencies:** identity integrity, MTR-01/MTR-02.

Capture enough immutable/canonical information to reproduce/explain a measured state without copying mutable live collections.

**Acceptance:** detached/frozen snapshot, deterministic ordering/fingerprint where used, explicit units/rule version/source identity.

#### REV-02 — Deterministic quantity delta

**Priority:** P0/P1  
**Dependencies:** REV-01.

Classify added/removed/unchanged/changed identities and compute previous/current/delta with explicit unresolved identity findings.

**Acceptance:** duplicate/ambiguous identity fails visibly; quantity delta can be traced back to snapshot lines/source identities.

#### REV-03 — Delta reason classification

**Priority:** P1  
**Dependencies:** REV-02 + measurement trace/version.

Separate geometry/property-driven change, rule-version change, mapping change and unresolved cases where deterministically knowable.

---

### CST — Rate / Estimate / Cost

#### CST-01 — Minimal `RateBook` / `RateItem` / `CostCode`

**Priority:** P1  
**Dependencies:** stable measurement identity/snapshot foundation.

Keep commercial assumptions separate from geometry and semantic measurement facts.

**Acceptance:** explicit unit, currency/effective version/date policy, deterministic lookup/identity, duplicate handling, persistence tests.

#### CST-02 — `EstimateLine` from frozen measurement + rate snapshot

**Priority:** P1  
**Dependencies:** CST-01 + REV-01.

Distinguish measured quantity, estimating quantity, waste/commercial adjustment, unit rate and final amount.

**Acceptance:** line can trace to measurement snapshot + rate snapshot; no rate assumptions stored inside geometry entities.

#### CST-03 — Revision cost impact

**Priority:** P1  
**Dependencies:** REV-02 + CST-02.

Separate quantity-driven delta from rate-driven delta where possible.

**Acceptance:** previous/current quantity, previous/current rate, quantity delta, rate delta and cost delta reconcile deterministically.

#### CST-04 — Frozen estimate/BQ projection

**Priority:** P1  
**Dependencies:** CST-02/03.

Renderer/export must consume canonical estimate state without recreating commercial formulas.

---

### NAT — Native Semantic Editing

Native editing is P0 and should be split carefully because host/UI/Core ownership can overlap easily.

#### NAT-01 — Edit invariant/core planning contract

**Priority:** P0.

Define/verify the shared semantic effect of native edit operations before several agents independently patch category commands.

Target invariant:

```text
Native edit
→ semantic state valid
→ provenance valid
→ dependencies invalidated
→ generated state regenerated
→ quantities refreshed
→ reports/revision observe change
```

**Acceptance:** deterministic Core/adapter contracts and tests where host-independent.

#### NAT-02 — MOVE qualification/fix by explicit semantic category

**Priority:** P0  
**Dependencies:** NAT-01 where applicable.

One agent should claim a **specific category set and scenario**, not “MOVE all QS3D”. Example boundaries may be wall/opening vs structural, but only after checking current source/claims.

**Acceptance:** model effect, semantic/provenance state, regeneration, quantity result, save/reopen expectations; native PASS only with real host evidence.

#### NAT-03 — ROTATE qualification/fix by explicit category

Same ownership/validation rules as NAT-02.

#### NAT-04 — STRETCH / grip edit qualification/fix by explicit category

Same ownership/validation rules as NAT-02. Grip/jig behavior should not be claimed broadly if another agent owns shared host interaction infrastructure.

#### NAT-05 — Batch property edit preview/apply

**Priority:** P0/P1.

Split preview/planning Core from WPF/host apply only when the boundary is explicit.

Expected UX data: selected/applicable/skipped counts, reasons, warnings, previewed semantic/quantity effects, deterministic apply result.

---

### PERF — Large-model performance + native qualification

#### PERF-01 — Qualification evidence schema/matrix

**Priority:** P0.

Define/maintain fields for host version/build, runtime, exact assembly/SHA, workflow, fixture/model size, expected/observed result, persistence/reopen, timing/memory where relevant, artifact/evidence reference and PASS/FAIL/BLOCKED.

**Acceptance:** schema cannot make a managed test look like native proof.

#### PERF-02 — Managed large-model fixtures/budgets

**Priority:** P0.

Split by workflow where independently measurable:

- regeneration;
- BQ/schedule;
- rebar/BBS;
- room/topology;
- revision compare;
- persistence open/save/reopen;
- quantity trace generation.

**Acceptance:** deterministic bounded fixtures, explicit size, measurable budget, no accidental unbounded enumeration/resource use.

#### PERF-03 — V25/V26 native qualification lanes

**Priority:** P0 but local/runtime dependent.

V25 and V26 evidence are separate lanes. Agents without licensed/native environment must not manufacture PASS; they may prepare matrix/fixtures/scripts only if allowed and explicitly label remaining local gates.

---

### QSC — QS Rule Checker on Semantic Health

#### QSC-01 — Declarative QS rule contract/profile

**Priority:** P2 after core quantity trust is stable.

Build on existing Semantic Health rather than a parallel validation engine.

Candidate rule fields: stable ID, profile/category, severity, deterministic condition, human explanation, affected identities, evidence values, optional safe previewable autofix.

#### QSC-02 — High-value rule family

Split claims by coherent rule family, for example:

- wall/family/floor/zone/material/dimension health;
- host/opening health;
- quantity/classification/stale readiness;
- estimate/rate readiness.

**Acceptance:** deterministic evidence and affected identities; malformed/non-finite/ambiguous state remains visible.

#### QSC-03 — Safe autofix preview

Only deterministic fixes with clear preview/rollback semantics belong here. Do not create an “AI autofix” path that silently mutates canonical project truth.

---

### TKO — 2D + 3D Takeoff Convergence

#### TKO-01 — Canonical 2D takeoff primitives

**Priority:** P2.

Candidate primitives: count, length, perimeter, area, zone/package, measurement group and explicit work-item mapping.

**Acceptance:** explicit units/scale/provenance, deterministic identity, no dead report-only numbers.

#### TKO-02 — 2D takeoff → semantic/work-item link

**Priority:** P2  
**Dependencies:** TKO-01 + MAP-01.

Where feasible, a 2D takeoff item should be linkable/upgradable to a semantic object without losing audit/provenance history.

#### TKO-03 — PDF/DWG recognition assistance

Keep recognition assistance separate from canonical measurement truth. AI/heuristic suggestions must be reviewable and must not silently become authoritative quantities.

---

### IFC — IFC / openBIM / BCF

#### IFC-01 — Round-trip acceptance criteria

**Priority:** P2 and can begin as a narrow docs/test-contract lane.

Define expected identity/classification/QTO/provenance behavior before broad implementation.

Target chain:

```text
QS3D Element ↔ IFC GlobalId ↔ IfcClass ↔ Pset/Qto ↔ Classification ↔ Cost Item
```

#### IFC-02 — Identity/classification/QTO round-trip implementation

**Priority:** P2  
**Dependencies:** IFC-01 + stable identity/mapping.

Split import/export only if ownership boundary is explicit and round-trip tests still cover the pair.

#### IFC-03 — BCF review/provenance loop

**Priority:** P2.

Potential path: Health finding → review issue → linked semantic IDs/viewpoint/evidence → status/assignment → BCF exchange.

Do not block core QS checker on BCF availability.

---

### REB — Rebar/BBS specialist depth

#### REB-01 — Canonical stock/cut demand model

**Priority:** P3 or earlier if owner/business evidence raises it.

Represent stock length, required cuts, diameter/grade/group identity, kerf/allowance policy if applicable, off-cut and procurement quantities separately from BBS presentation.

#### REB-02 — Deterministic cutting optimisation

**Dependencies:** REB-01.

Do not assume the algorithm named in BLT3D research is required. Select an algorithm from QS3D's actual constraints and prove deterministic output/tie-breaking/resource bounds.

#### REB-03 — Waste/procurement/report projection

**Dependencies:** REB-02.

Report/project output consumes canonical optimisation result; no independent cutting math in Excel/BBS renderer.

#### REB-04 — Lap/splice/anchorage rule depth

Split by one proven business rule family and standard/profile. Do not hard-code speculative competitor assumptions.

---

### MEP — MEP QS exploration

**Priority:** P3 after stable measurement architecture.  
**Repository rule:** domain/measurement functionality may live in this monorepo; generic routing/fabrication/cloud platform does not automatically belong here.

#### MEP-01 — Domain/measurement spike

Start with one measurable system type and facts, not a full MEP authoring suite.

Potential sequence:

1. pipe/duct/cable-tray/conduit semantic identity;
2. length/size/material facts;
3. fittings/accessory relationships;
4. measurement rule/trace;
5. classification/BOQ projection;
6. revision delta.

Only promote a spike after deterministic requirements and actual user value are demonstrated.

---

### CIV — Civil / earthwork depth

**Priority:** P3.

Potential claimable lanes only after verifying current earthwork capabilities:

- existing/design surface facts;
- cut/fill quantity rules;
- excavation/trench zones;
- backfill/swell/shrink profiles;
- disposal/haul measurement;
- trace/revision/report projection.

Use existing identity/measurement/report architecture rather than building an independent civil quantity engine.

---

### EXT — Future external products/services — not current plugin implementation lanes

The following identifiers exist to prevent accidental scope creep, not to invite immediate work:

- `EXT-CLOUD` — collaboration/project-sync service;
- `EXT-FIELD` — mobile/site application;
- `EXT-AI` — model training/serving platform;
- `EXT-ERP` — enterprise integration backend;
- `EXT-ESG` — carbon/ESG analytics service;
- `EXT-DFMA` — fabrication/CNC platform;
- `EXT-FM` — facilities management / 7D digital twin;
- `EXT-CITY` — city/national-scale analytics.

**Default decision:** do not implement these in `QS3D-BricsCAD`. If the owner explicitly opens one, first write/approve the product boundary and API/contract boundary; then decide whether it belongs in a separate repo/service.

## 7. Suggested parallel agent allocation

This is an example of safe-ish parallelism after each agent independently confirms current claims/source. It is not a reservation.

### Four-agent start

- Agent A → MTR-01 canonical trace contract.
- Agent B → PERF-01 qualification evidence schema/matrix or one PERF-02 managed fixture lane.
- Agent C → IFC-01 round-trip acceptance criteria only.
- Agent D → audit/claim one NAT category/scenario only after shared edit ownership is clear.

These can be relatively independent if current claims permit them.

### After MTR-01 lands

- Agent A → MTR-02 rule versioning or hand off.
- Agent E → MTR-03 one specific quantity-service family.
- Agent F → REV-01 measurement snapshot.
- Agent G → MAP-01 mapping contract.

### After snapshot/mapping foundations land

- Agent H → REV-02 quantity delta.
- Agent I → MAP-02 coverage evaluator.
- Agent J → CST-01 rate domain.
- Agent K → MTR-04 why-inspector model/UI boundary.

Avoid starting several agents simultaneously on a shared foundational contract unless the split is explicit in all affected claims.

## 8. Claim naming examples

Use the repository's actual timestamp/agent naming convention. Examples only:

```text
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-measurement-trace-contract.md
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-wall-quantity-trace.md
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-measurement-snapshot.md
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-boq-mapping-contract.md
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-native-wall-move-integrity.md
docs/agent-work-claims/YYYY-MM-DD-HHMM-<agent>-rebar-cutting-optimizer.md
```

A good claim title describes one independently testable outcome. Bad examples are `work on roadmap`, `finish BLT3D features`, `improve QS3D`, or `native editing` with no category/scenario boundary.

## 9. Definition of Done for an implementation lane

A lane is not complete merely because code was written.

For a normal Core/product lane, completion should include as applicable:

- current-main ownership was reserved before substantive write;
- implementation is within the claimed scope;
- canonical source-of-truth architecture is preserved;
- no duplicate business-calculation engine was introduced;
- deterministic/culture/unit/finite/identity invariants are handled according to current contracts;
- regression tests cover the defect or new invariant;
- stale-state behavior is explicit where derived data is involved;
- reporting/UI is a projection of canonical domain results;
- compatibility/migration behavior is tested when persistence changes;
- source/test diff is reread after integration with latest `main`;
- pushed commit is re-fetched/verified;
- claim is marked `COMPLETED` with actual validation listed.

For native BricsCAD-dependent lanes, additionally:

- managed tests/smokes are not reported as native runtime proof;
- V25/V26 qualification is host-major specific;
- exact candidate SHA/assembly/build and fixture are recorded for native evidence;
- save/reopen and multi-document lifecycle are included where the feature requires them;
- unexecuted local/native gates remain explicitly `BLOCKED`/not proven rather than being guessed.

## 10. Research-to-implementation rule

The BLT3D master archive intentionally remains available for further research:

`docs/research/BLT3D-GEMINI-RESEARCH-MASTER-2026-08-12.md`

Agents may mine it for:

- user pain points;
- workflow ideas;
- measurement concepts;
- UX patterns;
- edge cases to investigate;
- competitor questions to verify independently.

Agents must **not** treat unverified statements in that archive as implementation facts. In particular, named algorithms, market claims, internal architecture, AI methods, cloud/event-sourcing design, CNC/7D/future features and similar content require independent evidence or an owner-approved QS3D requirement before they become product commitments.

Preferred translation:

```text
Research observation
        ↓
Verify against QS3D source + real business requirement
        ↓
Define QS3D invariant / acceptance criteria
        ↓
Publish narrow claim
        ↓
Implement + regression
        ↓
Push + verify + close claim
```

Not:

```text
Gemini says competitor does X
        ↓
copy X into QS3D
```

## 11. Priority summary

### P0 — do first / protect product truth

- MTR-01/02/03 — explainable measurement foundation;
- NAT — native semantic edit integrity;
- PERF — representative scale/native qualification;
- REV-01/02 — measurement snapshot and quantity delta foundation;
- MAP-01/02 — mapping and quantity coverage foundation;
- continuing identity/stale/determinism hardening where a concrete defect is proven.

### P1 — trusted quantity → commercial value

- CST-01/02/03/04 — rate/estimate/revision cost;
- MTR-04 — why-inspector UX;
- MAP-03 — coverage projection;
- REV-03 — delta reason classification.

### P2 — QA + mixed-source + openBIM

- QSC — QS checker;
- TKO — 2D/3D takeoff convergence;
- IFC — IFC/BCF round-trip/provenance.

### P3 — specialist expansion

- REB cutting/waste depth;
- MEP QS;
- CIV/earthwork depth;
- other company/trade measurement packs justified by user evidence.

### Outside current plugin scope by default

- EXT-CLOUD / FIELD / AI / ERP / ESG / DFMA / FM / CITY.

## 12. Final coordination principle

The purpose of parallel agents is **not** to maximize the number of simultaneous edits. It is to maximize independently verifiable progress without creating divergent truths.

The desired repository behavior is:

```text
Agent chooses one small lane
        ↓
refreshes main + reads ACTIVE/BLOCKED claims
        ↓
publishes claim to main
        ↓
other agents see and avoid that lane
        ↓
agent implements + tests
        ↓
pushes coherent change
        ↓
closes claim
        ↓
next agent can safely build on the new main
```

When in doubt, prefer a smaller claim, stronger regression, and explicit handoff over a broad “continue all” claim that overlaps several agents.
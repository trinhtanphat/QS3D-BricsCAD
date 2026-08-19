# BIM3D-QS customer-first MVP plan

**Parent program:** #3142  
**Planning lane:** #3151  
**Baseline:** `main@4674ff6f2f604c77e0e2bbfcbeb653a7857457c8`  
**Date:** 2026-08-19  
**Product boundary:** `QS3D-BricsCAD` remains a BricsCAD-hosted BIM/QS plugin. This plan does not create a standalone CAD engine and does not authorize copying competitor code/assets.

---

## 1. Why this milestone comes first

The current customer signal is narrow and useful:

> BIM 3D is enough when the QS engineer can **model the building and export quantities**.

For the next customer milestone, QS3D should therefore optimize the shortest path from **3D authoring to auditable quantity output** before expanding the product story into 4D schedule, 5D cost, claims, ERP connectors or AI-agent automation.

This is not a retreat from the broader roadmap. It is a sequencing decision:

```text
FIRST: BIM 3D + quantity truth + customer workflow
THEN: 4D / 5D / integrations / AI automation
```

A QS engineer should not need schedule or cost-management infrastructure just to model a wall, beam, slab or foundation and obtain trustworthy quantities.

---

## 2. Customer promise

For this milestone, the product promise is:

> **QS3D lets a QS engineer build or capture a practical 3D quantity model inside BricsCAD, inspect the resulting quantities, locate the source/model element, and export a traceable quantity workbook.**

The promise is intentionally limited to behavior we can test and qualify.

The MVP does **not** promise:

- full 4D construction sequencing;
- full 5D estimate/cost control;
- claim certification;
- Primavera/MS Project/ERP synchronization;
- complete native Revit authoring parity;
- universal BIM authoring;
- fully automated AI takeoff without user review.

---

## 3. Current-main capability we must reuse

Current `main` already contains substantial source-side implementation. The MVP should harden and connect these pieces rather than replace them.

### 3.1 Project / semantic model

Already represented in the repository:

- Project state;
- Zone;
- Floor / Level;
- Family / Type;
- semantic Element identity;
- drawing-bound source/generated ownership;
- project persistence and regeneration state;
- model health / release readiness.

### 3.2 3D authoring and generation

Current source exposes canonical authoring/capture paths including:

- `QS3DDRAWWALL`;
- `QS3DDRAWBEAM`;
- `QS3DDRAWCOLUMN`;
- `QS3DDRAWSLAB`;
- `QS3DDRAWSTRUCTWALL`;
- `QS3DDRAWFOUNDATION`;
- `QS3DDRAWDOOR`;
- `QS3DDRAWOPENING`;
- legacy capture commands for existing CAD;
- `QS3DBUILD3D` as the guarded rebuild path.

The source architecture already converges Direct Draw and legacy capture on the same semantic/native model, which is the correct boundary to preserve.

### 3.3 Quantity / review / export

Current source includes:

- `QS3DTAKEOFF`;
- `QS3DBQ`;
- wall quantity review;
- Quantity Insight / explanation paths;
- Locate/highlight/focus/isolate;
- `QS3DED2`;
- XLSX / CSV reporting;
- source/semantic/drawing provenance in supported exports;
- revision baseline/delta services.

### 3.4 Important qualification boundary

Source implementation is not equivalent to customer readiness.

The milestone must explicitly close the gap between:

```text
SOURCE EXISTS
    and
CUSTOMER CAN RELIABLY MODEL + REVIEW + EXPORT IN BRICSCAD
```

Licensed exact-SHA BricsCAD runtime proof remains a separate evidence class and should be coordinated through the existing local qualification boundary (#72).

---

## 4. P0 golden path

The customer-ready P0 workflow is:

```text
Open drawing
  -> Open/create QS3D project
  -> Resolve drawing/project units
  -> Select/create Floor
  -> Select/create Family / Type
  -> Tạo mới or Capture element
  -> Enter/inherit required dimensions
  -> Create semantic element
  -> Build/refresh owned native 3D
  -> Inspect model / health
  -> Calculate quantity
  -> Review quantity rows/detail
  -> Locate / explain quantity
  -> Export selected/Floor/Zone/All scope
  -> Save
  -> Close/reopen
  -> Recalculate and obtain intended same result
```

A normal user should not need to understand internal command naming, generated-owner metadata, regeneration graphs or `.qsdb` internals to complete this flow.

---

## 5. P0 model category envelope

The first customer-ready slice should be deliberately bounded.

### 5.1 ArchitecturalWall

Required:

- direct draw from two-point or supported multi-segment plan path;
- thickness;
- height;
- bottom offset / level relation;
- Family / material association;
- owned native 3D;
- length / gross area / gross volume where applicable;
- opening/door deductions when physically/semantically supported;
- quantity explanation / Locate;
- export traceability.

### 5.2 Beam

Required:

- direct draw from supported line/path geometry;
- width / height;
- bottom elevation relation;
- Family / material;
- owned native 3D;
- length;
- cross-section-derived area/volume where canonical calculation supports it;
- Floor/Zone grouping;
- quantity/export traceability.

### 5.3 Column

Required:

- center placement;
- width / depth;
- height;
- bottom elevation relation;
- Family / material;
- owned native 3D;
- count;
- height/volume/applicable area;
- quantity/export traceability.

### 5.4 Slab

Required:

- closed supported plan boundary;
- thickness;
- bottom elevation relation;
- Family / material;
- owned native 3D;
- plan area;
- volume;
- quantity/export traceability.

### 5.5 StructuralWall

Required:

- canonical capture/direct-draw path;
- supported plan path;
- thickness / height / elevation semantics;
- Family / material;
- owned native 3D;
- applicable wall quantities;
- quantity/export traceability.

### 5.6 Foundation

Required:

- canonical supported footprint/path;
- thickness / elevation semantics;
- Family / material;
- owned native 3D;
- plan area / volume / count where applicable;
- quantity/export traceability.

### 5.7 Door / WallOpening

Required for the P0 wall quantity story:

- hosted semantic relationship to a valid wall;
- width / height / sill/bottom offset where applicable;
- stable source identity;
- clear distinction between semantic opening and physical boolean result;
- deduction evidence where supported;
- Locate/explanation;
- no orphan opening accepted as a successful customer operation.

---

## 6. P1 categories explicitly after the P0 path

These are valuable, but they must not delay the first customer milestone unless a customer specifically requires them:

- Room;
- Floor Finish / Wall Finish / Ceiling Finish;
- Waterproofing / Skirting;
- Curtain Wall;
- Stair;
- Railing;
- Earthwork;
- Rebar/BBS;
- MEP takeoff;
- recognition/AI-assisted workflows;
- IFC/OpenBIM deep interoperability.

A P1 capability already present on `main` may remain available. The sequencing rule only means P0 completion is judged independently from those broader features.

---

## 7. Quantity truth contract

The product should expose **applicable quantities**, not a fixed set of numeric cells that implies every quantity makes sense for every object.

### 7.1 Common identity fields

Every reportable P0 element should preserve, where the canonical project schema supports it:

- semantic Element ID;
- category;
- element display/name;
- Family / Type;
- Floor / Level;
- Zone;
- effective material;
- source CAD Handle;
- generated/owned native reference where appropriate;
- active drawing fingerprint/project provenance.

### 7.2 Common engineering quantity types

Supported outputs may include:

- count;
- length;
- plan area;
- side/gross/net area;
- volume;
- opening/deduction amount;
- mass only when an explicit valid density/source contract exists.

### 7.3 Applicability rule

If a quantity is not meaningful or not supported for a category:

```text
NOT APPLICABLE / NOT AVAILABLE
```

is preferable to:

```text
0.000
```

when zero would falsely imply a measured result.

### 7.4 Numerical integrity

All published quantity results must:

- be finite;
- obey non-negative constraints where required;
- reject impossible/unsupported geometry rather than guessing;
- preserve drawing-to-engineering unit conversion;
- preserve deterministic ordering/grouping;
- detect stale source/generated references before publication where the current workflow requires freshness.

### 7.5 Quantity provenance

The user should be able to answer:

> “Con số này đến từ đối tượng nào?”

For the P0 customer path, quantity output should retain enough provenance to resolve the quantity back to the intended semantic/source/model object under the existing guarded Locate contract.

---

## 8. Gross, net and deductions

Wall/Opening behavior is the most important early deduction case.

The product should not hide whether a number is:

- gross geometry;
- net geometry;
- semantic opening deduction;
- exact native/BREP evidence;
- estimated/fallback evidence;
- unavailable because a host/boolean/source is stale.

When exact evidence is not available, the result must not be labeled as exact.

The P0 experience should prioritize **explainability over apparent completeness**.

---

## 9. Model ownership / regeneration contract

A customer model must not accumulate duplicate quantity truth after editing or rebuilding.

Required invariants:

1. Each semantic element has a stable canonical identity.
2. Source CAD provenance remains resolvable or fails closed.
3. Generated native geometry is owner-scoped.
4. Rebuild replaces/invalidates old owned output according to canonical ownership rules.
5. Generated output must never be independently counted as another semantic source object.
6. Failed authoring/build operations clean up only operation-owned geometry.
7. Regeneration failures do not silently publish fresh quantities for stale geometry.
8. Save/reopen restores the intended semantic/source/generated relation.

These invariants are more important to a QS product than visual 3D parity alone because duplicated/stale geometry can directly corrupt quantity totals.

---

## 10. User experience target

The user should perceive one simple sequence:

```text
MÔ HÌNH
  Tạo mới / Capture
      -> Tham số
      -> 3D

KHỐI LƯỢNG
  Tính / Làm mới
      -> Chi tiết
      -> Diễn giải
      -> Định vị
      -> Xuất Excel
```

### 10.1 Primary action principle

At each stage there should be one obvious primary action.

Examples:

- no project -> `Tạo/Mở dự án`;
- no Floor -> `Chọn/Tạo tầng`;
- valid Family + model context -> `+ Tạo mới`;
- selected semantic object -> `Khối lượng`;
- quantity row -> `Định vị` / `Diễn giải`;
- valid quantity scope -> `Xuất Excel`.

### 10.2 Progressive disclosure

Advanced controls should not block the normal path.

Keep secondary operations contextual:

- Regenerate;
- Health;
- Material;
- isolate/section;
- advanced capture;
- revision compare;
- custom schedules.

### 10.3 Error state principle

A failed precondition should explain the repair:

- unresolved units -> ask user to resolve units;
- no compatible Family -> offer/select Family;
- missing source -> explain stale source;
- unsupported geometry -> state the unsupported shape/type;
- stale generated 3D -> offer regenerate;
- wrong drawing/project -> fail closed and explain context.

Silent no-op is not acceptable in the customer golden path.

---

## 11. Export contract

The first customer deliverable should be a readable quantity workbook/report rather than a raw internal dump.

### 11.1 Scope choices

Existing scope behavior should remain explicit:

- Selection;
- active Floor;
- active Zone;
- All.

### 11.2 Detail sheet

The detail sheet should preserve one semantic element per row where the existing ED2/BQ design does so.

Expected customer-review fields:

- item/element name;
- category;
- Family/Type;
- Floor;
- Zone;
- material;
- applicable quantity values;
- units;
- Element ID;
- source Handle/provenance fields;
- notes/status.

### 11.3 Summary sheet

The summary should group only by explicit deterministic dimensions, for example:

- category;
- Family;
- material;
- Floor;
- Zone;

without losing the ability to drill back to detail rows.

### 11.4 Spreadsheet safety

Exports must retain current repository expectations for:

- deterministic output;
- bounded inputs;
- formula/text safety;
- no silent density/mass invention;
- explicit provenance.

---

## 12. Customer acceptance scenario A — simple structural bay

A minimal synthetic acceptance model should include:

- 4 Columns;
- 4 Beams;
- 1 Slab;
- 1 Foundation or foundation group;
- one Floor/Level;
- explicit dimensions/materials.

Expected checks:

1. model objects can be created/captured;
2. native 3D generation succeeds for supported source geometry;
3. quantities match independently calculated expected values;
4. BQ contains the intended rows;
5. each row can locate the intended object;
6. XLSX export contains the same intended quantities;
7. save/reopen/recalculate remains stable.

---

## 13. Customer acceptance scenario B — wall with opening

Synthetic model:

- one ArchitecturalWall;
- one hosted Door or WallOpening;
- explicit wall thickness/height/length;
- explicit opening width/height/sill;
- valid physical/semantic host relation.

Expected checks:

1. Wall + opening identities remain distinct;
2. opening is not accepted without a valid host;
3. gross wall quantity is explainable;
4. net/deduction quantity is explainable when supported;
5. result Locate targets the intended wall/opening;
6. rebuild does not duplicate or double-deduct;
7. exported detail preserves provenance.

---

## 14. Customer acceptance scenario C — drawing unit parity

Use mathematically equivalent simple geometry in at least:

- meter drawing units;
- millimeter drawing units.

Expected engineering quantities must agree within the repository-approved deterministic tolerance after unit conversion.

Unresolved or unsupported units must fail closed or require explicit user confirmation; no hidden millimeter assumption is allowed.

---

## 15. Customer acceptance scenario D — invalid/stale state

The product must prove at least representative fail-closed behavior for:

- missing source Handle;
- stale generated 3D;
- unsupported source geometry;
- wrong active drawing/project;
- orphan Door/Opening;
- invalid/non-finite dimensions;
- stale export provenance.

Customer trust depends as much on refusing bad data as on returning good data.

---

## 16. Persistence / reopen contract

The customer milestone is incomplete if quantities work only in the creation session.

Required behavior:

```text
create model
-> save project/drawing
-> close/reopen
-> bind same intended project
-> resolve source semantics
-> resolve or rebuild generated state
-> recalculate
-> obtain intended quantity/report result
```

The P0 Definition of Done must record which parts are deterministic Core/source evidence and which require licensed BricsCAD runtime qualification.

---

## 17. Runtime evidence matrix

Evidence classes remain separate.

### 17.1 Source/static

Can prove:

- command/route wiring;
- ownership guards;
- source-shape expectations;
- rollback paths;
- no duplicate command registration;
- source-level orchestration.

### 17.2 Core deterministic smoke

Can prove:

- quantity formulas/contracts;
- unit conversion;
- semantic identity/provenance;
- grouping/reporting;
- persistence structures;
- deterministic recalculation;
- bounded/fail-closed inputs.

### 17.3 Licensed BricsCAD exact-SHA

Must prove:

- actual `NETLOAD`/DemandLoad;
- interactive Direct Draw point acquisition;
- native `Solid3d` generation;
- native booleans/opening behavior;
- Undo/Cancel;
- modeless WPF lifecycle;
- selection/zoom/Locate;
- save/reopen against the host;
- multi-DWG isolation;
- Unicode/HiDPI;
- representative customer/private-DWG behavior when authorized.

Use the existing #72 local qualification boundary rather than creating a competing local lane.

---

## 18. Workstream map

### WS-A — Modeling completeness

**Issue:** #3149

Purpose:

- audit the P0 authoring chain on current `main`;
- close only proven source-side gaps;
- preserve Direct Draw/capture/builder architecture;
- separate local-runtime-only rows from source defects.

### WS-B — Quantity completeness

**Issue:** #3144

Purpose:

- create a per-category quantity applicability matrix;
- close proven quantity calculation/projection gaps;
- pin corrected behavior with deterministic tests;
- prevent stale/generated double counting.

### WS-C — Export acceptance

**Issue:** #3145

Purpose:

- make current BQ/ED2/XLSX output coherent for the P0 categories;
- preserve identity, units and provenance;
- verify selection/Floor/Zone/All scopes;
- prevent silent category omission.

### WS-D — Synthetic E2E golden project

**Issue:** #3146

Purpose:

- create repository-owned deterministic fixtures;
- prove model-domain -> quantity -> report -> persistence behavior;
- pin independently computable expected quantities.

### WS-E — Onboarding / starter project

**Issue:** #3147

Purpose:

- minimize time-to-first-valid quantity;
- reuse project/unit/Floor/Family services;
- avoid overwriting existing catalog/project state.

### WS-F — Customer golden-path UX

**Issue:** #3148

Purpose:

- connect existing model/quantity/export capabilities into one obvious user path;
- reuse generic Workspace infrastructure;
- preserve selection/context from model to quantity review.

---

## 19. Existing dependency map — do not duplicate

### #72 — exact-SHA licensed V25 qualification

Role in this program:

- final native/customer runtime evidence.

Do not create another local qualification owner for the same exact runtime surface.

### #74 — Direct Draw preview / repeated authoring

Role:

- productivity enhancement after/beside basic P0 authoring stability.

The first customer milestone can be functionally valid without advanced transient preview if direct authoring is reliable, but repeated authoring is a high-value follow-up.

### #79 — Grid / Level constraints

Role:

- richer positioning/reference model.

The P0 MVP should use the current Floor/Level model. Rich Grid/reference constraints should not block basic model-to-quantity acceptance unless a specific category cannot be positioned correctly without them.

### #80 — native modify/edit semantics

Role:

- richer edit workflow.

P0 must at minimum regenerate safely from supported authoritative source changes. Full grip/jig semantic editing can remain a subsequent product enhancement if the canonical source-sync path is safe.

### #3113 — generic Workspace architecture

Role:

- reusable feature registry/action bar/inspector/floating-tool infrastructure.

#3148 should consume these primitives and must not implement a competing generic UI architecture.

### #3103 — QS3D/BLT3D/BIM5D gap research

Role:

- broader product gap research.

This P0 plan is customer-priority execution and should not take over the active research carrier.

---

## 20. Critical path

Recommended order:

```text
1. #3149 Modeling completeness audit/fixes
2. #3144 Quantity completeness
3. #3145 Export acceptance
4. #3146 Synthetic E2E golden project
5. #3147 Onboarding
6. #3148 Golden-path UX
7. #72 exact-SHA licensed customer-style qualification
```

Parallelism is allowed where file ownership does not collide.

Practical parallel grouping:

```text
Wave A
  #3149 Modeling
  #3144 Quantity
  #3147 Onboarding

Wave B
  #3145 Export
  #3146 E2E fixture

Wave C
  #3148 UX integration

Gate
  #72 licensed runtime acceptance
```

Do not artificially wait for a whole wave if one child is ready and non-overlapping.

---

## 21. Merge/integration strategy

Each child task has its own:

- Issue;
- Lane-Key;
- owner/session;
- canonical branch;
- PR;
- validation evidence.

Agents must self-claim before mutation.

When a child finds that its intended source file/symbol is already reserved by another ACTIVE lane:

```text
DUPLICATE/COLLISION
-> do not mutate that file
-> record dependency
-> split remaining non-overlapping gap if useful
```

The final BIM3D-QS integration should include only child carriers that are:

- terminal/ready;
- fresh with current main;
- required CI green;
- review/collision clean;
- semantically compatible with each other.

Do not integrate 4D/5D changes merely because they happen to be available; they are not required for this customer milestone.

---

## 22. Definition of Done — Modeling

A P0 category is **Modeling DONE** only when:

- user/capture route exists;
- required parameters are validated;
- semantic identity is created/updated correctly;
- supported native 3D is produced or the unsupported case fails closed;
- ownership is recorded;
- rebuild does not leave duplicate live owned output;
- quantity freshness is updated correctly;
- source/static tests exist for corrected defects;
- runtime-only behavior is not falsely marked PASS.

---

## 23. Definition of Done — Quantity

A P0 category is **Quantity DONE** only when:

- applicable quantity types are explicitly known;
- deterministic calculation exists;
- units are correct;
- finite/non-negative rules are enforced;
- stale/unsupported input fails closed;
- gross/net/deduction semantics are explicit where relevant;
- element/source provenance is retained;
- report grouping does not double count generated output;
- regression covers corrected gaps.

---

## 24. Definition of Done — Export

The customer export is **DONE** only when:

- P0 valid elements appear in the intended export scope;
- applicable quantities/units are present;
- inapplicable values are not fabricated;
- Floor/Zone/Family/category/material context is retained where supported;
- provenance fields can support guarded Locate;
- output is deterministic and spreadsheet-safe;
- save/reopen/re-export produces intended equivalent data.

---

## 25. Definition of Done — Customer workflow

The customer milestone is **DONE** only when a QS engineer can complete:

```text
Project
-> Floor
-> Family
-> Model
-> 3D
-> Quantity
-> Explain/Locate
-> Export
-> Save/Reopen
```

without requiring:

- 4D scheduling;
- 5D rates/cost;
- an external database;
- AI automation;
- a second standalone QS3D CAD application.

---

## 26. Release-readiness checklist

Before customer preview/release, record:

### Project/bootstrap

- [ ] Units explicit/resolved.
- [ ] Project bound to active DWG.
- [ ] Floor/Level available.
- [ ] compatible Family/Type available.

### Modeling

- [ ] Wall.
- [ ] Beam.
- [ ] Column.
- [ ] Slab.
- [ ] StructuralWall.
- [ ] Foundation.
- [ ] Door/Opening host path.

### Quantity

- [ ] count applicability.
- [ ] length applicability.
- [ ] area applicability.
- [ ] volume applicability.
- [ ] gross/net/deduction evidence.
- [ ] units.
- [ ] provenance.

### Review

- [ ] BQ rows.
- [ ] quantity detail/explanation.
- [ ] Locate/reveal.
- [ ] stale/error state.

### Export

- [ ] Selection.
- [ ] Floor.
- [ ] Zone.
- [ ] All.
- [ ] detail rows.
- [ ] summary.
- [ ] workbook safety.

### Lifecycle

- [ ] regenerate.
- [ ] save.
- [ ] close/reopen.
- [ ] recalculate.
- [ ] revision/stale detection.
- [ ] no duplicate generated ownership.

### Evidence

- [ ] source/static guards.
- [ ] Core deterministic smoke.
- [ ] protected PR CI.
- [ ] exact-SHA licensed V25 customer-style acceptance.

---

## 27. Product decisions intentionally deferred

Do not block P0 on these decisions:

- detailed WBS/cost-code model;
- schedule activity links;
- progress claim snapshots;
- resource/rate database;
- AI autonomous tool planning;
- cloud collaboration;
- full RVT interoperability;
- universal IFC round-trip;
- fabrication-grade rebar rules;
- MEP trade-specific estimating.

These can build on top of trustworthy 3D quantity facts later.

---

## 28. Success metrics

The milestone should be judged with product-level outcomes rather than number of commands implemented.

Suggested acceptance metrics:

1. **Time to first quantity** — a fresh user can reach a valid modeled quantity with a short, understandable setup path.
2. **Quantity traceability** — every accepted P0 quantity can be tied back to the intended semantic/source model object.
3. **Recalculation stability** — repeated regenerate/recalculate does not change unchanged intended quantities.
4. **Lifecycle stability** — save/reopen retains intended quantity identity/state.
5. **Error honesty** — unsupported/stale cases fail visibly instead of silently fabricating values.
6. **Export usability** — the workbook can be reviewed without reading internal QS3D storage.
7. **Runtime proof** — the final exact SHA has licensed BricsCAD acceptance evidence for the actual customer flow.

---

## 29. Agent task board

| Issue | Workstream | State at creation | Can run in parallel? | Main dependency |
|---|---|---|---|---|
| #3149 | Modeling completeness | Unclaimed | Yes | current main; avoid #74/#79/#80 collisions |
| #3144 | Quantity completeness | Unclaimed | Yes | current quantity engines |
| #3145 | Export acceptance | Unclaimed | Yes after/with quantity audit | existing BQ/ED2/export |
| #3146 | Synthetic E2E golden project | Unclaimed | Yes | stable quantity/report contracts |
| #3147 | Onboarding | Unclaimed | Yes | project/Floor/Family/unit services |
| #3148 | Golden-path UX | Unclaimed | Later/partial | generic Workspace primitives #3113 |
| #72 | Licensed V25 qualification | Existing owned lane | No takeover | final exact candidate |

Agents choose an unclaimed child, register the lane, and implement only that scope.

---

## 30. Final program close criteria

#3142 may be closed only when:

1. #3149 has a terminal P0 modeling assessment and all required proven source gaps are fixed or explicitly local-blocked.
2. #3144 confirms the P0 quantity contract and fixes required deterministic gaps.
3. #3145 proves the intended customer export contains the P0 quantity truth with provenance.
4. #3146 pins the synthetic golden-project expectations.
5. #3147 provides a bounded first-run path.
6. #3148 presents the coherent customer flow without duplicating generic UI infrastructure.
7. final protected candidate checks are green.
8. exact-SHA licensed BricsCAD customer-style acceptance is recorded through the existing local boundary.
9. remaining P1/4D/5D items are listed as deferred rather than hidden.

At that point the product can truthfully say the customer-requested milestone is complete:

> **BIM 3D cho QS: dựng hình, kiểm tra và xuất khối lượng có truy vết.**

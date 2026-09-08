# QS3D QS Workstation 10/10 — Program Design

Issue: #6127
Baseline main: `e85cf398016165dd81bcfab9d68b64b5d87751df`
Date: 2026-09-08

## 1. Purpose

Turn QS3D-BricsCAD from a strong CAD-native quantity/commercial foundation into an end-to-end Quantity Surveying workstation that a QS can use across pre-contract and post-contract work without relying on UI-side duplicate arithmetic.

“10/10” is an evidence threshold, not a promise based on source volume. A capability is complete only when the relevant domain authority, product surface, deterministic verification, current exact-head CI, and—when BricsCAD-host behavior is involved—licensed V25/V26 runtime evidence are all present.

No paid service or paid cloud dependency is required for these workstation capabilities.

## 2. Current baseline and existing authorities

The program builds on existing authorities rather than replacing them:

- quantity/measurement/evidence/revision infrastructure in `QS3D.Core`;
- cost, rate-book and estimating authorities in `QS3D.Core.Cost`;
- `TenderEvaluationService` for bid arithmetic and ranking;
- `CommercialQsSettlement` for Variation, IPC and Final Account arithmetic;
- exact-decimal and revision/freshness guards in `QS3D.Core.Commercial`;
- existing BricsCAD V25 commercial workspace as the initial product adapter;
- existing deterministic smoke/preflight conventions;
- Reservation-v2 ownership and fail-closed lane collision enforcement.

The active #6124 / PR #6126 lane owns the current Tender/Procurement Core lifecycle and CVR/Forecast Core slice. This program must consume that work after it lands and must not create competing arithmetic or overlapping reservations.

## 3. Approaches considered

### A. One large “10/10” PR

This minimizes visible PR count but creates unacceptable collision risk, review surface, CI diagnosis cost and native qualification ambiguity. It also conflicts with Reservation-v2’s narrow ownership model.

**Rejected.**

### B. Continue adding Core services first, UI later

This is safe for arithmetic but worsens the current imbalance: backend integrity is already stronger than day-to-day QS usability. More isolated services would not by themselves make QS3D a workstation.

**Rejected as the primary strategy.**

### C. Evidence-gated vertical slices — recommended

Deliver independent vertical slices from Core authority through product surface, export and verification. Each lane owns narrow paths, reuses existing arithmetic, and can merge independently when current and green.

**Selected.**

## 4. Target product architecture

```text
BricsCAD V25 / V26 Workspaces
        |
        | thin view-model / adapter layer
        v
QS Workstation Application Services
        |
        +----------------+----------------+----------------+
        |                |                |                |
Measurement/BOQ     Estimating/Rate   Commercial       Reporting
        |                |                |                |
        +----------------+----------------+----------------+
                         |
                    QS3D.Core
                         |
      existing quantity/cost/commercial authorities
                         |
                   CAD evidence/revision
```

### Authority rule

UI, view-model and export code may shape, filter and present results, but may not own monetary or quantity business arithmetic already defined in Core. A source guard must fail if a product adapter begins duplicating protected calculations.

### Revision rule

Commercial and measurement mutations use fresh revision identities and canonical reasons where the existing domain requires them. Frozen/approved/certified records do not silently mutate; changes produce a new revision or explicit lifecycle transition.

### Evidence rule

Every value presented as CAD-derived must support drill-down to evidence/provenance sufficient to explain its source revision and constituent objects. Manual adjustments must remain distinguishable from extracted values.

## 5. Program decomposition

### Lane 1 — Commercial Control Core stabilization

**Owner:** existing #6124 / PR #6126.

**Scope:** Tender/Procurement package/compliance/recommendation/award; CVR/Forecast period arithmetic and lifecycle.

**Program dependency:** later UI lanes consume these Core types after merge. No duplicate implementation is allowed.

**Exit:** exact-head CI green/current/mergeable and merged to `main`; hosted CI remains explicitly distinct from licensed-native evidence.

### Lane 2 — Measurement + BOQ Professional Workspace

**User outcome:** a QS can review, filter, approve and reconcile measurement at scale rather than reading raw generated output.

**Capabilities:**

- virtualized/scalable measurement and BOQ grids;
- stable work-item grouping and hierarchical breakdown;
- review states: unreviewed, accepted, adjusted, rejected;
- manual adjustment records separated from extracted quantity;
- adjustment reason, actor/audit text and revision identity;
- CAD → BOQ/evidence selection and BOQ/evidence → CAD highlight/navigation;
- previous/current revision comparison with added/removed/changed classification;
- safe batch accept/reject/assign operations;
- deterministic export of the reviewed measurement state.

**Non-goal:** reimplement extraction mathematics in the grid.

### Lane 3 — Estimating + Rate Build-up Workspace

**User outcome:** a QS/estimator can build and audit rates without leaving QS3D for basic rate analysis.

**Capabilities:**

- rate code, description, unit, currency and effective-date/revision metadata;
- labour, material, plant and subcontract components;
- quantity/productivity factors and wastage where applicable;
- OH&P / markup components through Core-owned arithmetic;
- supplier/subcontract quotation provenance references;
- rate status/revision history;
- compare tendered, current and revised rates;
- bulk assignment preview with unmatched/conflict handling;
- rate build-up export/report.

**Authority:** existing Cost/Rate services remain canonical; any new calculations live in Core and are called by UI.

### Lane 4 — Structured Post-contract Commercial Registers

**User outcome:** Variation, IPC and Final Account become daily-use registers rather than delimiter/textbox engineering forms.

**Variation register:**

- variation/change identifier, description, instruction/source, dates and status;
- BOQ/work-item linkage;
- added/omitted quantity and rate basis;
- quoted, assessed and approved values obtained from Core authority;
- attachments/evidence references and revision/audit history;
- deterministic impact on contract sum.

**IPC register:**

- application/certificate sequence and valuation period;
- claimed vs assessed/certified values;
- approved variations, materials on/off site, retention, deductions, advance recovery and previous-certified continuity;
- current and cumulative views;
- certificate continuity and fail-closed stale revision behavior;
- payment/certificate export.

**Final Account register:**

- original contract sum and all approved adjustment classes supported by Core;
- reconciliation to prior certificates/approved variations;
- settlement status and audit trail;
- deterministic final-account statement export.

**Authority:** `CommercialQsSettlement` and related Core commercial contracts own arithmetic. The UI never computes settlement totals independently.

### Lane 5 — Commercial Control Product Surface

**User outcome:** Tender/Procurement and CVR/Forecast Core become usable BricsCAD workflows.

**Tender/Procurement:**

- package dashboard, tender requirements and compliance matrix;
- bid import/edit surface;
- deterministic comparison and recommended bid display;
- explicit award transition with provenance;
- blocked award visualization for incomplete/non-compliant/stale bids.

**CVR/Forecast:**

- period list and lifecycle state;
- revised budget, cost-to-date, commitments, accruals, earned value, ETC, EAC, variance and margin;
- freeze/reopen/revise workflow with canonical audit reason;
- period-to-period trend without recomputing Core metrics in UI.

### Lane 6 — Reporting and Export Convergence

**User outcome:** every major QS register has professional deterministic output with identical totals/provenance to its on-screen Core result.

**Outputs:**

- measurement/BOQ review register;
- rate build-up / cost analysis;
- tender comparison and award decision;
- variation register;
- IPC application/certificate;
- CVR/forecast report;
- final account statement.

**Rules:**

- exports consume result DTOs/snapshots from Core/application services;
- no spreadsheet formula is allowed to become an alternative source of truth;
- project/revision identity, generation timestamp and provenance metadata are included where applicable;
- generation is deterministic for the same input snapshot except explicitly non-deterministic metadata such as a documented generation timestamp.

### Lane 7 — Licensed V25/V26 Qualification

**User outcome:** “production-ready” claims are backed by real host evidence.

**Qualification matrix:**

1. load the exact built plugin in licensed BricsCAD;
2. open representative real DWG;
3. run quantity/measurement workflow;
4. review/adjust/trace evidence;
5. run estimating/rate workflow;
6. run Variation/IPC/Final Account;
7. run Tender/CVR surfaces where host-exposed;
8. export reports;
9. save DWG/project state;
10. close/reopen;
11. re-run and verify stable results/revision behavior;
12. exercise expected stale/mutation/error fences.

V25 and V26 are qualified separately. Hosted CI, compile success or mocked adapters never substitute for this matrix.

## 6. Shared UX requirements

All workstation grids and registers follow these rules:

- no delimiter-driven primary data entry for normal daily workflows;
- keyboard-friendly data entry, multi-select and batch actions;
- sorting/filtering/search without mutating domain order or totals;
- validation errors attached to the affected row/field;
- read-only state is visually distinct from editable state;
- stale revision or invalid transition is displayed as an actionable blocked state, not silently retried;
- long-running CAD read/highlight operations do not freeze the main UI;
- large datasets use virtualization/paging or equivalent bounded rendering;
- user-facing errors are sanitized while diagnostics retain safe technical context.

## 7. Data-flow rules

### Measurement adjustment

```text
CAD revision
  -> extraction/evidence authority
  -> measurement snapshot
  -> review state
  -> explicit manual adjustment record
  -> revised reviewed snapshot
  -> BOQ/estimate/export
```

The original extracted amount remains recoverable. A manual adjustment never overwrites provenance as though it came from CAD.

### Commercial workflow

```text
revisioned input/register row
  -> Core service
  -> immutable result/snapshot
  -> UI projection
  -> deterministic export
```

The UI projection may format currency/percent/date values but cannot alter the commercial result.

## 8. Error handling and fail-closed behavior

- invalid money/quantity input fails before mutation;
- overflow/unsupported precision fails rather than rounds silently;
- stale revisions fail before mutation;
- invalid lifecycle transition fails before mutation;
- frozen/approved/certified states reject mutation except through explicit allowed transitions;
- broken CAD evidence links remain visible as integrity errors rather than disappearing;
- export failure does not mutate the underlying register/state;
- native host exceptions shown to users are sanitized; diagnostic logs must not expose secrets or private file content unnecessarily.

## 9. Testing strategy

Each implementation lane must use test-first changes and carry the smallest deterministic verification set that proves its authority boundary.

### Core tests

- exact arithmetic and boundary values;
- lifecycle transitions;
- stale revision rejection;
- provenance preservation;
- deterministic ordering/ranking;
- large-count stability where collection behavior matters.

### Product adapter tests/source guards

- UI delegates to Core rather than duplicating arithmetic;
- view-model transformations preserve IDs/revisions/totals;
- invalid/stale states are blocked;
- export consumes the same result snapshot as the UI;
- major grids have deterministic column/field contracts.

### CI

- fresh exact-head run on the final head SHA;
- required jobs terminal green;
- branch current enough to merge under repository policy;
- no reservation collision or out-of-scope changed path.

### Native

Native tests record exact build/head identity and host version. A result is PASS only for the host/version actually exercised.

## 10. Delivery mechanics

- One Reservation-v2 issue and canonical branch per vertical slice.
- Expected paths remain narrow and are expanded only by updating the owning issue before mutation.
- Existing active owners have priority; do not rename ownership keys to evade collisions.
- Every PR references its task issue, lists authority reused, verification boundary and native-evidence status.
- Merge only when the exact head is current, mergeable and required CI is terminal green.
- After merge, close the reservation issue and release the lane per repository policy.

## 11. Evidence-based 10/10 scorecard

A domain reaches 10/10 only when all applicable columns are PASS:

| Domain | Core authority | Product surface | Audit/revision | Deterministic tests | Exact-head CI | Licensed V25 | Licensed V26 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Measurement/BOQ | required | required | required | required | required | required | required |
| Estimating/Rate | required | required | required | required | required | required | required |
| Tender/Procurement | required | required | required | required | required | required if host-exposed | required if host-exposed |
| Variation | required | required | required | required | required | required | required |
| IPC | required | required | required | required | required | required | required |
| CVR/Forecast | required | required | required | required | required | required if host-exposed | required if host-exposed |
| Final Account | required | required | required | required | required | required | required |
| Reporting/Export | consumes authorities | required | required | required | required | required | required |

No missing column is converted to a passing score by averaging. Until all applicable evidence exists, the scorecard shows the missing gate explicitly.

## 12. Sequence

1. Repair and merge #6126 without duplicating its reserved implementation.
2. Measurement + BOQ professional workspace.
3. Estimating + rate build-up workspace.
4. Structured Variation/IPC/Final Account registers.
5. Tender/Procurement + CVR/Forecast BricsCAD surfaces consuming the merged Core authority.
6. Reporting/export convergence.
7. Licensed V25 qualification.
8. Licensed V26 qualification.
9. Final evidence scorecard and only then a 10/10 claim.

This order front-loads the largest daily-use UX gaps while respecting the currently active commercial-control lane and preserving one arithmetic source of truth.
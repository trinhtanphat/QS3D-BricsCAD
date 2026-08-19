# QS3D Estimating, Segmentation, Progress Claim & Reporting UX Specification

Status: implementation-ready UX specification  
Carrier: Issue #3105  
Parent/reference: #3098  
Product boundary: BricsCAD-hosted QS3D plugin; no standalone QS3D shell  
Reference synthesis: `docs/BLT3D-BIM5D-BENCHMARK.md`

## 1. Purpose

This specification defines the source-safe QS3D user journeys that connect model/object selection to quantity review, estimating, segmentation, schedule/activity linkage, progress measurement, progress claims, change/variance review, and reporting.

The goal is not to reproduce any competitor interface. The goal is to give QS3D a testable interaction contract for 5D work while preserving model provenance, edit history, and issued-claim traceability.

This document is intentionally implementation-neutral. Exact WPF controls, palette placement, command names, persistence technology, and service boundaries belong to focused implementation issues.

## 2. Product and source-safety constraints

1. Every workflow remains hosted by BricsCAD/QS3D. No standalone application shell is introduced.
2. BLT3D or other 5D BIM products may inform workflow concepts only. Their UI layouts, icons, text, proprietary schemas, binaries, and private implementation details are not copied.
3. A user must always be able to identify the model/version, quantity source, rate source, schedule source, claim period, and report scope behind a commercial result.
4. An edit that changes cost, allocation, measured progress, or a claimable amount must be traceable.
5. An issued claim or frozen commercial snapshot is immutable. Corrections are represented by a superseding revision or adjustment rather than silent mutation.
6. Refreshing a model, rate card, or schedule must never silently overwrite user-entered commercial decisions.
7. Missing or stale source data is surfaced as an explicit state, never hidden by a plausible-looking zero.

## 3. Users and jobs to be done

### 3.1 Estimator / quantity surveyor

- Review extracted quantities and their source objects.
- Group and classify quantities.
- Assign cost codes and rates in bulk or individually.
- Detect incomplete pricing, mixed-unit mistakes, and stale quantity snapshots.
- Produce a priced estimate with reproducible provenance.

### 3.2 Planner / project controls user

- Link estimate/quantity scope to schedule activities.
- Segment scope by level, zone, WBS, phase, package, or another project-defined dimension.
- Review unallocated and multiply allocated scope.
- Measure progress by quantity and/or approved percentage method.

### 3.3 Commercial / claims user

- Prepare a claim period from approved measured progress.
- Compare baseline, approved changes, prior certified amount, current measured amount, current claim, cumulative claimed amount, and remaining amount.
- Freeze, review, issue, and supersede claims without losing history.
- Export reports that carry enough provenance to be independently reconciled.

## 4. Canonical UX concepts

The names below are interaction concepts, not mandatory class names.

### 4.1 Model source and model version

A `ModelSource` identifies the BricsCAD drawing/model context. A `ModelVersion` identifies the concrete revision used for a quantity or report snapshot.

At minimum, provenance should be able to show:

- source/model identifier or path-safe display name;
- revision/version identifier when available;
- stable object references used by the quantity snapshot;
- snapshot timestamp;
- quantity/extraction engine version when available.

### 4.2 Selection set

A `SelectionSet` is the user-visible scope chosen from the active BricsCAD model or recovered from a saved QS3D scope. It may contain one or many model objects.

Selection is live until the user creates/recomputes a quantity snapshot. Commercial downstream states refer to the snapshot rather than assuming the model is unchanged.

### 4.3 Quantity line

A `QuantityLine` represents a measurable commercial line with:

- source object references;
- quantity and unit;
- measurement basis/rule identifier where available;
- grouping/classification keys;
- source model version;
- extraction timestamp;
- warning state when source objects are missing, changed, unsupported, or mixed-unit.

A displayed zero must distinguish `measured zero` from `not measured`, `unsupported`, and `failed`.

### 4.4 Cost code and rate assignment

A priced quantity line may reference:

- cost code;
- rate source/card;
- rate revision/effective version;
- base rate;
- manual override, if any;
- override reason;
- derived amount.

Bulk assignment must preview the affected count, mixed-unit scope, conflicts, and resulting total before commit.

### 4.5 Activity link

An `ActivityLink` connects quantity/estimate scope to an external or QS3D schedule activity. The UI must preserve the schedule source and schedule revision used to create the link.

The workflow may support one-to-many allocation only when each allocation is explicit and measurable. A line must never appear fully linked merely because one of several required allocations exists.

### 4.6 Segment

A `Segment` is a project-defined partition such as level, zone, WBS, phase, package, workfront, or custom grouping.

Each segment allocation has:

- dimension/type;
- segment value/id;
- allocation rule or explicit allocation;
- allocated quantity and/or percentage;
- source/reason for manual override;
- validation state.

For claimable scope, allocations across the same segmentation dimension must be deterministic and reconcilable. Over-allocation and ambiguous overlap are blocking validation errors unless the workflow explicitly models a non-exclusive dimension.

### 4.7 Progress measurement

A `ProgressMeasurement` records progress for a period and a scoped quantity/activity/segment. Supported conceptual methods are:

- installed/measured quantity;
- percentage complete backed by an approved basis;
- milestone or weighted rule only when a project-specific implementation defines it.

The record includes period, method, measured value, author, timestamp, evidence/reference if supported, and review state.

### 4.8 Claim and claim line

A `Claim` is a period-bound commercial snapshot. Each line can reconcile:

- original/baseline quantity and amount;
- approved change quantity and amount;
- revised entitlement;
- previously certified/claimed amount;
- current measured progress;
- current proposed claim;
- current certified amount when certification is in scope;
- cumulative amount;
- remaining amount.

The claim stores the exact quantity, rate, segment, activity, and source revisions used at freeze/issue time.

### 4.9 Change / variance item

A `VarianceItem` explains a difference caused by one or more of:

- model revision;
- quantity recomputation;
- classification change;
- rate/rate-card change;
- schedule link or schedule revision change;
- segmentation/allocation change;
- progress correction;
- approved commercial change.

A variance must not be reduced to a single unexplained delta amount.

### 4.10 Report snapshot

A `ReportSnapshot` captures the report definition and provenance at generation time. It should include filters, grouping, units/currency context, source revisions, claim period where relevant, generated timestamp, and generator/user identity when available.

## 5. Overall workflow

The canonical happy path is:

`Model/Object Selection -> Quantity Review -> Classification/Rate Assignment -> Activity Link -> Segmentation -> Progress Measurement -> Progress Review/Approval -> Claim Draft -> Claim Freeze/Review -> Claim Issue -> Reporting/Export`

Change/variance review is reachable whenever an upstream source changes after a downstream commercial decision exists.

A user may stop at estimating or reporting without using schedule, segmentation, progress, or claims. Downstream modules therefore show `not configured` rather than treating optional upstream stages as errors.

## 6. Workflow 1: model/object selection and quantity review

### 6.1 Entry

The user enters from the active BricsCAD/QS3D context and chooses current selection, model scope, or a saved QS3D scope.

### 6.2 Review surface

The quantity review shows:

- total source object count;
- measurable vs unsupported/unresolved object count;
- grouped quantity lines;
- quantity and unit;
- source/model version status;
- warnings and blockers;
- drill-through from a quantity line to its source references.

The exact visual control is not prescribed. It must fit an in-host QS3D palette/panel/dialog pattern.

### 6.3 Commit/recompute behavior

Recompute creates a new quantity snapshot or explicit revision. It does not silently alter an estimate/claim that is already based on an earlier snapshot.

When the active model differs from the quantity snapshot, the UI displays a `stale source` warning and offers an explicit compare/recompute path.

### 6.4 Empty/loading/error states

- **No selection:** explain that model objects must be selected or a saved scope chosen; no fake zero totals.
- **Selection has no measurable objects:** show unsupported/unresolved count and reasons where available.
- **Loading/extracting:** show progress/working state and preserve the last valid snapshot until the new result succeeds.
- **Extraction failure:** retain prior valid data, show failure, and make retry explicit.
- **Mixed units:** group safely or block aggregation where summing would be invalid.

## 7. Workflow 2: estimating, classification, and rate assignment

### 7.1 Pricing readiness

Each line has one of the following interaction states:

- `Unclassified`
- `Classified / Unpriced`
- `Priced`
- `Priced with override`
- `Blocked`
- `Stale`

The aggregate estimate displays counts and value for each state so a user cannot mistake a partial estimate for a complete estimate.

### 7.2 Bulk cost-code/rate assignment

Before applying a bulk operation the UI previews:

- selected line count;
- quantity/unit distribution;
- existing assignments that will change;
- target cost code;
- rate source/revision;
- rows with no matching rate;
- rows that require conversion or are invalid;
- value delta before/after.

The commit is atomic from the user's perspective: failed validation does not leave an unknown subset silently changed.

### 7.3 Manual rate override

A manual override requires an explicit reason and records the previous rate and source. Removing the override restores the applicable referenced rate rather than inventing a new value.

### 7.4 Reversibility

Before an estimate is frozen/issued, user edits may support undo/revert. After a frozen commercial snapshot exists, a commercial change creates a new revision and variance record.

## 8. Workflow 3: schedule/activity linkage

### 8.1 Link creation

The user selects estimate/quantity scope and one or more schedule activities from a known schedule source/revision.

The preview shows:

- selected commercial scope;
- target activity/activity set;
- allocation rule or percentage when multiple links are used;
- currently linked scope that will be replaced or supplemented;
- resulting unlinked/over-allocated scope.

### 8.2 Schedule refresh

When a newer schedule revision is loaded:

- unchanged activity identifiers remain linked but show the new revision only after explicit refresh/acceptance;
- missing/renamed activities are unresolved, not auto-mapped by label alone;
- affected progress/claim scope is flagged if a downstream snapshot already exists.

## 9. Workflow 4: segmentation

### 9.1 Supported segmentation intent

QS3D must support the concept of segmenting commercial scope by one or more dimensions such as level, zone, WBS, phase, package, or workfront. Implementations may add project-specific dimensions without changing the validation contract below.

### 9.2 Allocation modes

Conceptual modes:

- derived from a model/property rule;
- inherited from classification or activity mapping;
- manually assigned;
- manually split by quantity or percentage.

Every manual split records who/when and an optional/required reason according to project policy.

### 9.3 Validation

For an exclusive claim dimension:

- total allocation must equal 100% (or the full measured quantity) before the line is claim-ready;
- allocation above 100% is blocking;
- zero/unallocated remainder is visible;
- conflicting rules are surfaced with the competing sources;
- rounding residuals use a deterministic project rule and remain reconcilable to the source line.

### 9.4 Segment states

- `Unallocated`
- `Partially allocated`
- `Valid`
- `Conflicted`
- `Stale`
- `Frozen in downstream snapshot`

## 10. Workflow 5: progress measurement

### 10.1 Period and scope

The user chooses a progress period/cut-off and filters by activity, segment, cost code, model scope, or other supported grouping.

The UI shows for each line:

- revised entitled quantity/amount;
- previously approved/certified progress;
- current measured quantity/percentage;
- cumulative measured quantity/percentage;
- remaining measurable quantity;
- validation status.

### 10.2 Measurement rules

1. Cumulative measured quantity must not exceed revised entitled quantity unless an approved change/overrun workflow explicitly permits it.
2. A percentage method must identify its basis; it cannot silently replace quantity measurement where quantity is the governing method.
3. A negative correction is not a destructive overwrite of prior approved history. It is represented as a current-period correction or a superseding record.
4. Editing one segment must preserve reconciliation to its parent quantity line.
5. Progress based on stale model/rate/schedule/segmentation inputs is visibly stale before approval.

### 10.3 Progress states

- `Draft`
- `Measured`
- `Needs review`
- `Approved`
- `Rejected`
- `Superseded`
- `Stale`

Only approved/eligible measurements flow automatically into claim preparation.

## 11. Workflow 6: progress claim preparation and issue

### 11.1 Create claim draft

The user selects a claim period/cut-off and eligible progress scope. The draft records the upstream revisions used.

A claim summary presents at minimum:

- baseline/original amount;
- approved changes;
- revised amount;
- previously claimed/certified amount;
- current measured amount;
- current proposed claim;
- cumulative proposed/certified amount;
- remaining amount;
- blocked/stale line counts.

### 11.2 Claim-line validation

A claim line is blocked from issue when any required condition is unresolved, including:

- quantity source failed or missing;
- price/rate required but missing;
- invalid mixed-unit aggregation;
- required segment allocation incomplete or over-allocated;
- required activity link unresolved;
- progress not approved/eligible;
- source became stale and project policy requires refresh/acknowledgement;
- cumulative commercial amount would exceed entitlement without an approved change.

### 11.3 Freeze

`Freeze` captures the exact commercial snapshot for review. After freeze:

- ordinary editing of captured fields is disabled;
- new upstream changes do not mutate the frozen claim;
- the UI can show differences between the frozen claim and current project state.

### 11.4 Issue

An issued claim is immutable. If a correction is needed, the user creates a new revision/superseding claim or an explicit adjustment according to the implementation's commercial policy.

### 11.5 Claim states

`Draft -> Frozen / In review -> Approved for issue -> Issued`

Alternative transitions:

- `In review -> Draft` with reason;
- `In review -> Rejected` with reason;
- `Issued -> Superseded` only through a new revision/adjustment relationship;
- `Draft/Frozen -> Cancelled` with reason if policy permits.

No transition from `Issued` back to an editable draft is allowed.

## 12. Workflow 7: change and variance review

Whenever the current project state differs from a commercial snapshot, the user can open a variance view that separates causality rather than only showing a net delta.

The minimum categories are:

- quantity/model variance;
- classification variance;
- rate variance;
- schedule-link variance;
- segmentation/allocation variance;
- progress correction;
- approved commercial change;
- unresolved/other with mandatory explanation.

For each variance the UI shows before value, after value, delta, source revision(s), affected scope, downstream impact, and resolution status.

Resolution never deletes the historical difference. It records acceptance, rejection, superseding action, or explanatory classification.

## 13. Workflow 8: reporting and export

### 13.1 Core report families

The UX contract supports at least these report intents:

1. Quantity review / takeoff summary.
2. Priced estimate by cost code/classification.
3. Estimate by segment/activity.
4. Unpriced/unallocated/unlinked exception report.
5. Progress status by period, activity, and segment.
6. Claim summary and claim detail.
7. Change/variance reconciliation.
8. Provenance/audit report for a selected commercial snapshot.

Exact export file formats are implementation decisions; tabular exports must preserve identifiers and provenance rather than flattening the result into unexplained display text.

### 13.2 Report provenance block

Every generated commercial report/export must be able to expose:

- project/report identifier;
- report type/version;
- generated timestamp and timezone;
- generated-by identity when available;
- model source/version/snapshot;
- quantity snapshot/extraction revision;
- rate source/revision;
- schedule source/revision when used;
- segmentation definition/revision when used;
- progress cut-off/period when used;
- claim id/revision/status when used;
- filters/scope/grouping;
- unit and currency context;
- known stale/unresolved warnings at generation time.

### 13.3 Empty/loading/error behavior

- A report with zero matching rows says `No data for the selected scope` and keeps filters visible.
- Report generation failure does not replace the last successfully generated report with an empty page.
- Export failure leaves the report snapshot intact and allows retry.
- A report generated from stale inputs is clearly marked; an issued/frozen historical report continues to show the provenance it originally used.

## 14. Shared interaction requirements

### 14.1 Selection and bulk operations

- Bulk edits always expose the selected row/object count.
- Mixed-state values use an explicit mixed state rather than showing one arbitrary value.
- Destructive/replacing changes preview their impact.
- A validation failure identifies affected rows and the corrective path.

### 14.2 Filters and grouping

Filters and grouping may change the view but not underlying commercial values. Report/filter state must never be confused with saved segment/allocation state.

### 14.3 Stale-data banner

A stale-data indicator includes:

- what source changed;
- the snapshot/revision currently in use;
- the newer detected revision when known;
- which downstream stages may be affected;
- explicit actions such as compare, refresh/recompute, acknowledge where policy permits, or keep historical snapshot.

### 14.4 Save and failure semantics

A user-visible committed operation is either fully recorded or reported as failed/partial with exact affected scope. Silent partial writes are not acceptable for cost, allocation, progress, or claims.

## 15. Audit, provenance, and reversibility contract

Commercially meaningful actions should produce an audit event containing as much of the following as the implementation can support:

- event id;
- entity type/id;
- action;
- actor;
- timestamp/timezone;
- before/after revision or value summary;
- source revision(s);
- reason when required;
- correlation/batch id for bulk operations.

Required traceability cases:

- cost code/rate assignment changed;
- manual rate override created/removed;
- segment allocation created/changed;
- schedule/activity link changed;
- progress measurement approved/rejected/corrected;
- claim frozen/unfrozen before issue if allowed;
- claim issued/cancelled/superseded;
- variance classified/resolved.

Audit history is append-oriented. UI undo may create an inverse event but must not erase the original event after a commercial snapshot depends on it.

## 16. State and error matrix

| Area | Empty/not configured | Working/loading | Recoverable warning | Blocking error |
|---|---|---|---|---|
| Model scope | No selection/saved scope | Resolving objects | Some unsupported objects | Source unavailable |
| Quantity | No measurable lines | Extracting/recomputing | Stale snapshot, mixed groups | Extraction failed / invalid aggregate |
| Estimating | No classification/rates | Applying/validating | Partial pricing | Required rate/unit invalid |
| Activity link | Schedule not configured | Loading/linking | Some unlinked scope | Required activity missing/conflicting |
| Segmentation | No segments configured | Allocating/validating | Partial allocation | Over-allocation/conflict |
| Progress | No period/eligible scope | Calculating/validating | Stale basis | Exceeds entitlement / invalid method |
| Claim | No eligible progress | Building/freezing | Warning requiring acknowledgement | Blocking unresolved commercial state |
| Reporting | No rows for filters | Generating/exporting | Generated from stale basis | Generation/export failure |

Warnings do not automatically become blockers. The implementation must define policy for acknowledgement, but issued claims can never hide unresolved blocking conditions.

## 17. Acceptance scenarios

### A1. Empty model selection

**Given** no active selection and no saved QS3D scope  
**When** the user opens quantity review  
**Then** the UI shows an actionable empty state and does not display a misleading zero-value estimate.

### A2. Quantity source becomes stale

**Given** an estimate is based on quantity snapshot Q1  
**And** the active model changes to a newer detectable revision  
**When** the user returns to the estimate  
**Then** Q1 remains intact, the estimate is marked stale, and refresh/recompute requires an explicit action.

### A3. Mixed-unit aggregation

**Given** selected lines contain incompatible units  
**When** the user attempts an aggregate or bulk rate operation that requires compatible units  
**Then** QS3D groups safely or blocks the invalid aggregation and identifies the affected lines.

### A4. Bulk cost assignment preview

**Given** multiple quantity lines have mixed existing cost codes and rates  
**When** the user bulk assigns a new code/rate  
**Then** a preview shows affected count, replacements, unmatched rows, and total value delta before commit.

### A5. Manual rate override traceability

**Given** a line is priced from rate-card revision R5  
**When** the user overrides the rate  
**Then** a reason is required, the R5 value remains traceable, and removing the override restores the applicable referenced rate.

### A6. Segmentation overlap conflict

**Given** an exclusive claim dimension has allocations totaling more than 100%  
**When** validation runs  
**Then** the line is `Conflicted`, the competing allocations are shown, and it cannot become claim-ready.

### A7. Segmentation incomplete allocation

**Given** an exclusive claim dimension totals 75%  
**When** the user prepares progress/claim scope  
**Then** the remaining 25% is visibly unallocated and the line is blocked if that dimension is required for claiming.

### A8. Schedule revision breaks an activity link

**Given** a quantity line links to activity A-120 in schedule S3  
**And** S4 removes or changes that activity identity  
**When** the user refreshes schedule data  
**Then** the link becomes unresolved rather than silently remapping by display label.

### A9. Progress cannot silently exceed entitlement

**Given** revised entitled quantity is 100 and approved cumulative progress is 90  
**When** the user enters 20 current-period units without an approved change  
**Then** validation blocks or explicitly routes the 10-unit overrun to the defined change workflow.

### A10. Progress correction preserves history

**Given** prior approved progress exists  
**When** a later measurement corrects it downward  
**Then** the prior approved record remains in history and the correction is represented explicitly.

### A11. Frozen claim is stable

**Given** claim C7 is frozen from quantity Q4, rates R5, schedule S3, and segmentation G2  
**When** Q5 or R6 later becomes current  
**Then** C7 values do not mutate and the UI can show the variance between C7 and current project state.

### A12. Issued claim cannot be edited in place

**Given** claim C7 is issued  
**When** a correction is required  
**Then** the user is directed to a superseding revision/adjustment path and C7 remains immutable.

### A13. Claim reconciliation is complete

**Given** a claim contains prior certified value and approved changes  
**When** the user reviews the claim summary  
**Then** baseline, approved changes, revised entitlement, prior amount, current amount, cumulative amount, and remaining amount reconcile arithmetically.

### A14. Variance explains causality

**Given** current estimate differs from a frozen estimate  
**When** the user opens variance review  
**Then** QS3D distinguishes quantity/model, rate, classification, allocation, schedule, progress, and approved-change causes instead of showing only a net delta.

### A15. Commercial report carries provenance

**Given** a claim detail report is generated  
**When** the report/export is inspected  
**Then** it identifies the claim revision/period plus the model, quantity, rate, schedule, and segmentation revisions that materially contributed to it.

### A16. Report generation failure is non-destructive

**Given** a user has a previously generated valid report  
**When** regeneration/export fails  
**Then** the valid report snapshot remains available and the failure/retry path is explicit.

### A17. Bulk operation fails atomically from the user perspective

**Given** a bulk assignment includes invalid rows  
**When** commit validation fails  
**Then** the UI does not imply that all rows succeeded; it either commits the validated transaction or reports the exact partial outcome if the implementation cannot guarantee transactionality.

### A18. Historical report remains historical

**Given** an issued claim report was generated from model M8 and rates R5  
**When** the project advances to M9 and R6  
**Then** reopening the historical report still identifies M8/R5 and does not silently recompute itself from current sources.

## 18. Implementation slicing guidance

This specification should be implemented through focused carriers rather than one monolithic change. A safe slice sequence is:

1. Quantity review + estimating readiness/rate-assignment UX.
2. Activity linkage + segmentation/allocation UX and validation.
3. Progress measurement/review UX and period model.
4. Claim draft/freeze/issue/supersede UX.
5. Change/variance reconciliation UX.
6. Report/export provenance and audit surfaces.

Each implementation slice must preserve the contracts in Sections 2, 15, and 17 even if later slices are not yet present.

## 19. Explicit non-goals for Issue #3105

- Production code implementation.
- A replacement for the BricsCAD host shell.
- Copying BLT3D visual design or proprietary behavior.
- Defining accounting, tax, payment-certificate, or contract-law rules not already established by QS3D/project policy.
- Selecting a database/schema technology.
- Hard-coding a single schedule or rate-card vendor format.
- Treating BIM model revisions as a source of truth for issued historical claims.

## 20. Definition-of-done mapping for #3105

- **User journeys explicit enough to test:** Sections 5-13 and acceptance scenarios A1-A18.
- **Quantity, cost, progress edits traceable/reversible:** Sections 7, 10, 11, and 15.
- **Reports preserve source/model/version provenance:** Sections 4.10, 13, and A15/A18.
- **No competitor UI/proprietary assets copied:** Section 2 and explicit non-goals.
- **Source-safe QS3D/BricsCAD boundary maintained:** Sections 1-2 and 6.2.

The implementation team may refine labels and control composition, but any change that weakens traceability, immutability of issued claims, stale-source visibility, allocation reconciliation, or report provenance requires an explicit product decision rather than an incidental UI shortcut.

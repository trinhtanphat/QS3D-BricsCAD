# QS3D source/product execution plan — 2026-08-10

Status: living plan for the current `main`. This document separates **source implementation**, **read-only preview/diagnostic capability**, **semantic mutation**, **native BricsCAD mutation**, and **runtime/engineering/release qualification** so progress cannot be overstated.

## 1. Product logic

QS3D is a BricsCAD V25 plugin, not a separate CAD engine. BricsCAD owns the DWG database, viewport and native entities. `QS3D.Core` owns deterministic semantic intent and quantity logic. The V25 adapter translates between native CAD entities and that semantic model.

The intended product flow is:

```text
BricsCAD source geometry / user input
        ↓ guarded capture / Direct Draw
QS3D semantic ProjectState (.qsdb source of truth)
        ↓ dirty flags + dependency graph
read-only Rule Preview / Regeneration Preview
        ↓ explicit mutation decision
Core regeneration / rules / quantities
        ↓ Model Health regression gate
native builder / replacement transaction
        ↓ canonical Generated*Handle(s) ownership
health + locate + cleanup + schedules + BQ/BBS
        ↓
interchange / diagnostics / release readiness
```

No layer may silently repair malformed identity/ownership data merely to keep moving. If an ID, generated owner, timestamp, source reference or import policy is ambiguous, the correct behavior is to stop before destructive mutation and surface a diagnostic.

## 2. Architectural invariants

### Semantic state is authoritative

`.qsdb` stores project identity, catalogs, semantic elements, dependencies, properties, quantities, rules, audit state and generated-output metadata. Native CAD entities are materialized output/provenance, not a replacement semantic database.

### Preview before high-impact mutation

Operations that can affect many quantities/elements should have a detached dry-run path. A preview is read-only against the live `ProjectState`, is deterministic for unchanged state, and becomes stale when the recomputed outcome differs.

Current source support now includes:

- `QuantityRulePreviewService.PreviewElement(...)`;
- `QuantityRulePreviewService.PreviewProject(...)`;
- `RegenerationPreviewService.Preview(...)`;
- `QS3DRULEPREVIEW` adapter command;
- `QS3DREGENPREVIEW` adapter command.

### Mutation is staged, guarded and recoverable

`QuantityRuleEngine` evaluates/stages managed outputs before stale cleanup. `RegenerationEngine` uses `ProjectStateSnapshot` transactional rollback. New guarded APIs add stale-preview rejection and Model Health regression checks before a batch is accepted.

Current source support now includes:

- `QuantityRulePreviewService.ApplyElement(...)`;
- `QuantityRulePreviewService.ApplyProject(...)`;
- `QuantityRulePreviewService.ApplyProjectWithHealthGuard(...)`;
- `RegenerationPreviewService.Apply(...)`.

These Core APIs are implemented source-side. A production adapter button/command that mutates from a preview still requires explicit confirmation/Undo/session behavior to be qualified in real V25 before it is presented as production-ready UX.

### Native generated output has one-owner semantics

Generated native objects must resolve through `GeneratedHandleOwnershipPolicy`. Never make two semantic elements own one destructive-union result. Foreign/ambiguous generated ownership must fail closed. Replacement must preserve the last valid output if the new build fails.

### Health is a before/after contract, not only a final report

`ModelHealthBaselineService` can capture deterministic health state and compare it as New / Resolved / Persistent issues. This allows an operation to prove that it did not introduce new errors instead of only reporting an opaque final error count.

### Support diagnostics must not require customer DWG disclosure

`ProjectDiagnosticSummaryExporter` produces an aggregate diagnostic JSON containing schema/count/category/health-code counts only. It deliberately omits project/drawing identity, paths/fingerprints, source/generated CAD handles, semantic IDs/names, properties, quantities and health messages. `QS3DDIAGSUMMARY` exposes the source-side export flow.

### Interchange is explicit-policy and ownership-safe

Portable interchange may transfer semantic data only through validated/canonical identity contracts. Source drawing handles remain drawing-local provenance and must not become target ownership. KeepTarget/UseSource/append-only behavior stays explicit; generated/native ownership is never implicitly reconstructed from JSON.

## 3. Newly implemented product features in this source wave

### A. Quantity Rule Dry-Run

Goal: let the user see quantity/provenance changes before applying project rules.

Implemented source behavior:

- per-element and whole-project previews;
- Added / Changed / Removed output classification;
- before/after numeric value and rule provenance;
- stale managed output cleanup represented in the preview;
- provenance-only stale output is represented as Removed;
- exact project-owned element identity guard;
- stale-preview rejection before Apply;
- project-wide rollback on Apply failure;
- optional Model Health guard that rolls the project back if a new Health Error appears.

### B. Semantic Regeneration Dry-Run

Goal: preview what `RegenerateDirty` would change before touching live semantic/native state.

Implemented source behavior:

- detached project regeneration using the real default `RegenerationEngine`;
- reuse of `RevisionService` for element/property/quantity deltas instead of a second comparator;
- before/after Model Health diff;
- regenerated-element, changed-element and changed-field counts;
- stale-preview rejection;
- pre-apply blocking when the preview itself introduces new Health Errors;
- live rollback if post-apply health still introduces a new Error.

### C. Health Baseline / Regression Diff

Goal: make operation quality measurable.

Implemented source behavior:

- deterministic read-only health baseline;
- New / Resolved / Persistent issue sets;
- new/resolved Error and Warning counts;
- duplicate issue collapse;
- cross-project compare rejection.

### D. Privacy-safe Diagnostic Summary

Goal: allow debugging/support without sending a customer DWG or semantic payload.

Implemented source behavior:

- deterministic `QS3D.DiagnosticSummary` v1 JSON;
- project schema and aggregate collection counts;
- dirty/null entry counts;
- element-category counts;
- health severity and code counts;
- no project ID/name, DWG path/fingerprint, CAD handles, semantic IDs/names, properties, quantities or health messages;
- atomic file publication through `AtomicFileCommit`;
- `QS3DDIAGSUMMARY` source-side adapter command.

### E. Interchange source-reference fail-closed export

Goal: prevent a malformed live project from being made to look valid by export normalization.

Implemented source behavior:

- blank source handles/dependencies are rejected;
- padded values are rejected instead of trimmed;
- case-insensitive duplicates are rejected instead of silently deduplicated;
- canonical values are only sorted for deterministic output.

## 4. Product roadmap by workstream

### Workstream A — Modeling productivity and safe review

Source-complete in this wave:

- Rule Preview;
- Regeneration Preview;
- deterministic semantic project-tree planning from concurrent source work;
- Health baseline/diff;
- privacy-safe diagnostic export.

Next source-safe candidates:

- persistable named preview snapshots for team review without CAD handles;
- UI pane for large preview filtering/grouping by Floor/Zone/Category;
- operation summary integration into Audit Trail after a confirmed apply;
- dependency-impact visualization using the existing `DependencyGraph` rather than a new graph engine.

Runtime-gated:

- confirmation/Undo workflow for `ApplyProjectWithHealthGuard` and `RegenerationPreviewService.Apply` in V25;
- modeless preview lifetime across active-document switches;
- focus/highlight/locate from preview rows;
- keyboard/DPI/accessibility behavior.

### Workstream B — Wall / Curtain / Room geometry

Source already has broad semantic and guarded native planning. Remaining completion is dominated by native ownership/geometry proof:

- physical L/T/X wall-junction reconciliation without destructive shared ownership;
- Curtain panel-by-panel native glass with explicit panel ownership;
- curved/open-POLYLINE Curtain panel/frame runtime proof;
- richer freeform WallPier specialization where current generic fallback is insufficient;
- complex corner-spanning curved opening booleans.

Do not solve these by destructive Boolean union of existing owner solids or by creating a second competing topology planner.

### Workstream C — Structure / reinforcement

Source already includes longitudinal rebar, shapes, stirrups, ties, Slab/Wall/Foundation meshes, polygon/hole-aware Core mesh planning and standards-neutral qualification metadata.

Remaining work:

- native ownership/association for generalized polygon/hole source loops;
- disconnected multi-region/island semantic ownership decision;
- engineering-standard-specific bend/hook/lap/anchorage/cover/spacing rules;
- engineer-approved BBS/detailing provenance for the chosen standard/revision;
- exact V25 geometry proof.

### Workstream D — BQ / schedules / commercial reporting

Current source already covers BQ, Room Finish, Material, Curtain, Door/Opening, BBS CSV/XLSX/native table paths and finite/overflow guards. It also provides a CAD-independent quantity-report revision review that snapshots the authoritative `ProjectQuantityReportBuilder.Detail` projection, reuses `RevisionService`, and classifies stable Element-keyed Added/Removed/Changed report rows without live mutation.

Next source-safe candidates:

- shared export provenance block/version manifest where file-format compatibility permits;
- saved BQ filter/view definitions in semantic project metadata;
- report-level health/readiness banner driven by release/qualification state.

Do not add hidden auto-rounding or unit coercion that changes source quantities.

### Workstream E — Interchange / collaboration

Current source has validated semantic JSON, canonical typed reader, preview/diff, append-only, KeepTarget and explicit generic policy routing plus source-replacement work from concurrent agents.

Next work must preserve:

- explicit collision policy;
- drawing-local source handle semantics;
- no generated/native ownership import;
- name/ID/category collision checks;
- preview/apply consistency;
- rollback across semantic replacement.

Future IFC/Revit/BCF/cloud work remains a separate mapping/adapter problem and must not bypass this semantic boundary.

### Workstream F — Release / support / diagnostics

Source now has release health, ownership gates, transactional installer/update hardening, preview/regression diagnostics and a privacy-safe support summary.

Remaining production gates:

- exact-head Windows x64 build;
- BricsCAD V25 adapter compile;
- NETLOAD/DemandLoad;
- save/reopen and multi-DWG lifecycle;
- representative private-DWG regression;
- production Authenticode certificate/timestamp;
- clean install/upgrade/uninstall;
- performance and DPI qualification.

## 5. Execution order from here

P0 — preserve safety contracts while concurrent agents modify `main`: re-fetch every target blob before update, never force-push, and keep Actions manual-only.

P1 — complete source-review UX around previews: large-result filtering, optional audit summary after confirmed apply, and deterministic report/revision comparison where no equivalent implementation exists.

P1 — complete remaining Core topology/data contracts that are independent of BricsCAD APIs. Do not classify native geometry as complete merely because a Core planner exists.

P2 — local V25 implementation/qualification for Curtain panels and physical wall junction output under one-owner/atomic replacement semantics.

P2 — engineering qualification of fabrication rebar against an explicitly selected standard/revision.

P3 — exact-SHA release qualification, signed package, installer/update rollback, private-DWG regression, performance and visual QA.

## 6. Definition of source-complete vs product-complete

`SOURCE_IMPLEMENTED` means the deterministic Core/adapter source contract exists and is covered by source smoke/preflight code. It does **not** mean that those tests were executed in this session.

`LOCAL_ONLY` means completion depends on a licensed BricsCAD V25 runtime, Windows/native CAD behavior, private representative drawings, certificate/private key material, or engineer-approved design-code inputs.

`PRODUCT_COMPLETE` requires the exact same SHA to pass source/static checks, Core build/smokes, V25 adapter build, NETLOAD/DemandLoad, representative interactive scenarios, save/reopen/multi-DWG, production installer/signing gates and the relevant engineering qualification.

## 7. Non-goals

- no standalone QS3D CAD engine;
- no fake BLT source/code copying;
- no hidden fallback that silently changes malformed identity/ownership data;
- no automatic GitHub Actions dispatch from ordinary source work;
- no claim that standards-neutral rebar metadata is equivalent to engineering-code compliance;
- no production PASS based only on repository inspection.

For runtime-only work, continue with `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md` and `docs/LOCAL-V25-QUALIFICATION.md`.

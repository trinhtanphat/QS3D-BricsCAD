# QS3D BricsCAD V25 — full repository review and parallel development workstream plan

Updated: 2026-08-10 (UTC+7)

Repository: `trinhtanphat/QS3D-BricsCAD`

Review baseline observed immediately before this plan: `14d0f9cf5f9fbe8a7fbe0a4412b9cacc7d582616`

> `main` is actively modified by multiple agents. This SHA is a review snapshot, not a branch lock. Every agent must fetch the newest `main` before editing and again before integration. Current source wins over this document if implementation moves ahead.

## 1. Product boundary

QS3D remains a **BricsCAD V25 x64 hosted .NET plugin**, not a standalone CAD application.

- BricsCAD owns the DWG database, editor, viewport, native transactions and document lifecycle.
- `QS3D.Core` remains CAD-independent for deterministic domain/geometry/persistence/reporting/test logic.
- `QS3D.BricsCAD.V25` remains the native adapter/UI/plugin layer.
- BLT/BLT3D is a clean-room workflow/UX reference only.
- No roadmap item below authorizes a separate `QS3D.exe`, a QS3D-owned DWG engine, proprietary BLT assets/source or committed BricsCAD proprietary DLLs.

The highest product goal is not command-count parity. The goal is a coherent semantic CAD/QS workflow where authoring, generated geometry, ownership, quantity, schedules, documentation, health, save/reopen and release behavior all agree.

## 2. Executive assessment

### What is already strong

The repository is substantially beyond a prototype. Current source already contains:

- Project / Zone / Floor(Level) / Family(Type) / Element semantic model;
- `.qsdb` persistence, migration, locking, snapshot rollback, audit and revision paths;
- deterministic dependency/regeneration and Health/Release Readiness;
- shared generated-handle ownership and stale/invalidation concepts;
- BLT-style Workspace, Family/Instance property scopes and semantic selection synchronization;
- Direct Draw for Wall, Beam, Column, Slab, GlassWall, WallPier, StructuralWall, Foundation, Door and Opening;
- Room/HT_PHONG, wall/opening/curtain/structure/rebar workflows;
- BQ, ED2, XLSX/CSV, schedules and reverse Locate flows;
- native semantic tags and project-owned native documentation-table source paths;
- semantic JSON interchange export/validation/append-only and narrower collision-policy source slices;
- substantial static preflight coverage and Core smoke/performance harnesses;
- manual-only CI/release policy and a local exact-SHA BricsCAD V25 qualification runbook.

### Main maturity gap

The primary gap is no longer “missing CRUD/features everywhere”. It is the transition from **broad source implementation** to a **coherent, runtime-qualified, commercially shippable product**.

The hard areas are now cross-cutting:

1. DWG + `.qsdb` lifecycle and drawing-unit truth;
2. native transaction/rollback across multi-stage generated geometry;
3. Level/elevation consistency across hosts, openings, curtain and rebar;
4. ownership-safe advanced geometry rather than destructive guessing;
5. real V25 UI/runtime qualification;
6. generic interchange mutation semantics;
7. engineering-standard provenance for fabrication-grade output;
8. commercial licensing/signing/install trust;
9. documentation/status drift while `main` moves quickly.

## 3. Immediate integration blockers before broad feature expansion

### P0-A — integrate/resolve draft PR #173: DWG lifecycle -> `.qsdb` persistence

Current draft PR #173 (`fix(project): persist QSDB with DWG lifecycle`) addresses a product-level durability gap:

- persist pending semantic state after native DWG Save/SaveAs;
- Save / Discard / Cancel on close with pending semantic state;
- veto close on canonical sidecar failure;
- detached recovery copy;
- monotonic project-change tracking.

This should be reviewed/integrated before large feature work assumes manual `QS3DSAVE` is the only persistence boundary.

Suggested branch/integration lane: `integration/p0-project-save-lifecycle`

### P0-B — integrate/resolve draft PR #165: drawing units + Proxy/B4D safety

Current draft PR #165 (`fix(b4d): bind drawing units and gate proxy capture`) addresses another foundational assumption:

- fail closed on undefined/unsupported `INSUNITS` unless an explicit QS3D unit override exists;
- bind captured quantities to the resolved drawing unit;
- keep metricless ProxyEntity/BRC candidates review-only;
- stop Family defaults from manufacturing quantities for unsupported proxy capture;
- expose/configure unit state in Project Tools;
- bind B4D/BQ/ED2/source reconcile to the same unit truth.

Suggested branch/integration lane: `integration/p0-drawing-units-proxy-safety`

### P0-C — reconcile both foundation PRs before downstream merges

PR #173 and #165 both touch persistence/status/source contracts and some shared files. Integrate them deliberately, not by independent blind merges. After integration, rebase every new feature branch onto the resulting main baseline.

## 4. Parallel workstream model

Use one coherent branch per workstream. Suggested branch names below are planning names; agents may use equivalent naming if no collision exists.

Concurrency classes:

- **GREEN** — mostly isolated Core/docs/tests; suitable for parallel remote agents.
- **YELLOW** — touches shared domain/ownership/adapters; coordinate before editing.
- **RED / LOCAL_ONLY** — requires installed/licensed BricsCAD V25, native UI/runtime behavior, approved engineering policy or production signing material.

## 5. Workstreams

### WS-01 — Project lifecycle, persistence, recovery

Priority: **P0**  
Concurrency: **YELLOW**  
Suggested branch: `feat/project-lifecycle-persistence`

Scope:

- integrate the DWG Save/SaveAs/Close semantic persistence lifecycle;
- monotonic project-change versioning;
- canonical save stamp and sidecar freshness;
- recovery-copy lifecycle and explicit recovery UX;
- Save As drawing fingerprint transition;
- multi-DWG cache invalidation/reload behavior;
- no accidental stale modeless editor write after project replacement;
- atomic `.qsdb` temp/replace/backup behavior;
- shutdown/close failure semantics.

Acceptance:

- no silent loss of semantic state after normal DWG save/close;
- no write to a replaced/stale `ProjectState` object;
- failed `.qsdb` save cannot silently destroy the last recoverable state;
- local V25 Save/SaveAs/Close matrix passes on exact SHA.

Depends on: P0-A integration.

### WS-02 — Drawing units and numeric provenance

Priority: **P0**  
Concurrency: **YELLOW**  
Suggested branch: `feat/drawing-unit-contract`

Scope:

- one canonical drawing-unit resolution policy;
- explicit project/DWG override behavior;
- provenance recording for captured measurements;
- fail-closed behavior for undefined/unsupported units;
- consistent use across Direct Draw, capture, B4D, source reconcile, BQ, ED2 and geometry builders;
- unit-change detection after semantic capture;
- health/release blocker when semantic quantities are no longer trustworthy under the active unit contract.

Acceptance:

- mm and m drawings produce identical SI semantics for equivalent geometry;
- unsupported/unknown units do not silently produce plausible but wrong quantities;
- unit state is visible to the user.

Depends on: P0-B integration.

### WS-03 — Project transaction/snapshot/journal platform

Priority: **P0**  
Concurrency: **YELLOW**  
Suggested branch: `feat/core-operation-journal`

Scope:

- continue centralizing semantic operation boundaries;
- standard result type for operation success / rollback failure / partial native commit;
- reusable project-state snapshot helpers;
- durable recovery marker only where multi-stage native operations genuinely need it;
- audit entries that distinguish planned mutation, commit and recovery;
- avoid per-feature ad-hoc rollback conventions.

Primary users:

- Curtain orchestration;
- advanced wall junction output;
- future interchange replace;
- source reconcile + rebuild orchestration;
- documentation/native artifact replacement.

### WS-04 — Generated ownership registry and health evolution

Priority: **P0**  
Concurrency: **YELLOW**  
Suggested branch: `feat/generated-ownership-v2`

Scope:

- keep `GeneratedHandleOwnershipPolicy` canonical;
- owner-slot schema/versioning for new artifact families;
- safe multi-handle ownership and duplicate detection;
- selection/Locate/B4D exclusion for every new generated family;
- common live-XData ownership validation;
- common stale/fingerprint lifecycle;
- no hard-coded generated-family lists in scanners/health unless a destructive subset intentionally requires one.

Required before adding:

- Curtain panel solids;
- junction-owned output;
- richer grid/documentation artifacts;
- future sheet/view ownership.

### WS-05 — Dependency/regeneration engine and incremental invalidation

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/incremental-regeneration`

Scope:

- explicit dependency reasons/types rather than opaque ID-only edges where useful;
- faster dirty propagation for large models;
- deterministic topological regeneration diagnostics;
- cycle explanation path;
- generated-family invalidation map;
- partial recompute metrics;
- profiling hooks for large projects.

Do not weaken the current cycle/release blocker semantics.

### WS-06 — Direct Draw common authoring engine

Priority: **P1**  
Concurrency: **YELLOW**  
Suggested branch: `refactor/direct-draw-authoring-core`

Scope:

- consolidate shared prompt/family/default/validation logic across P0/P1/openings;
- common point acquisition plan models;
- shared operation-owned source tracking;
- consistent cancel/error messages;
- consistent active-document affinity;
- last-used safe parameter model;
- no second Family/property system.

Goal: reduce duplicated wrappers before adding more Direct Draw categories.

### WS-07 — Direct Draw transient preview + repeated authoring

Priority: **P1**  
Concurrency: **RED / LOCAL_ONLY**  
Suggested branch: `feat/v25-direct-draw-preview-repeat`

Scope:

- real V25 DrawJig/transient preview;
- wall thickness/profile preview;
- column/slab footprint preview;
- Door/Opening host/width cue;
- OSNAP/ORTHO/dynamic input interaction;
- repeated creation mode using active Family/previous safe values;
- ESC/UNDO/document-switch cleanup.

Rule: preview creates no persistent DWG/semantic ownership before acceptance.

### WS-08 — Source Reconcile / Modify workflow

Priority: **P1**  
Concurrency: **YELLOW**  
Suggested branch: `feat/source-reconcile-modify`

Scope:

- strengthen `QS3DSYNCSOURCE` as the Modify path for source-authoritative elements;
- explicit change preview before semantic overwrite where useful;
- source-metric delta display;
- deterministic invalidation of host/rebar/curtain/documentation dependents;
- batch reconcile;
- conflict handling when source is missing/replaced;
- future “Sync + Build” only after a real shared transaction/recovery contract exists.

### WS-09 — Architectural wall geometry and physical L/T/X/Multi junctions

Priority: **P0/P1**  
Concurrency: **RED/YELLOW**  
Suggested branch: `feat/wall-junction-owned-geometry`

Scope:

- preserve existing semantic wall host ownership;
- design dedicated junction-owned infill/composite output or explicit junction semantic identity;
- dependency list to participating walls;
- invalidate/rebuild on source/profile/elevation changes;
- keep Door/Opening host relation on original wall;
- support differing thicknesses/elevations only with explicit deterministic rules;
- avoid boolean-union that consumes one wall owner.

Acceptance must include L/T/X, 2/3/4 owners, rebuild/removal, openings and real V25 failure injection.

### WS-10 — WallPier advanced path/profile authoring

Priority: **P1**  
Concurrency: **YELLOW/LOCAL**  
Suggested branch: `feat/wallpier-path-profile`

Scope:

- multi-segment Direct Draw WallPier;
- deterministic profile-around-corner contract;
- chamfer/profile continuity;
- bulged path behavior only when proven;
- quantity/source semantics aligned with legacy captured WallPier.

Do not silently route unsupported multi-segment Direct Draw into generic wall-prism behavior.

### WS-11 — Room boundary / HT_PHONG advanced modeling

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/room-finish-v2`

Scope:

- robust room topology diagnostics;
- user-reviewable boundary provenance;
- opening/shaft/void semantics where explicitly modeled;
- finish-layer composition and per-surface override;
- room naming/numbering tools;
- room/finish bulk edit;
- better stale/reuse visualization;
- performance for large mixed LINE/POLYLINE/ARC/SPLINE networks.

### WS-12 — Door/Opening host and boolean lifecycle

Priority: **P1**  
Concurrency: **YELLOW/LOCAL**  
Suggested branch: `feat/opening-host-v2`

Scope:

- improve host candidate review UX;
- explicit re-host workflow;
- opening orientation/handing parameters for Door semantics;
- safe incremental cut journal or deterministic rebuild-first policy;
- more complex curved/corner host cases only with explicit geometry contract;
- no implicit global cut from Direct Draw.

### WS-13 — Curtain whole-command atomicity/recovery

Priority: **P0**  
Concurrency: **RED / LOCAL_ONLY for native proof**  
Suggested branch: `feat/curtain-orchestration-atomicity`

Source status: **implemented; LOCAL_ONLY failure injection pending**.

The selected architecture is shared native transaction orchestration: one command-level outer transaction encloses the canonical nested host + LINE/path frame replacement transactions, with a command-level semantic snapshot restored on abort. `PARTIAL COMMIT` is no longer the source contract.

Remaining acceptance is exact-SHA V25 phase-failure injection, health inspection and save/reopen proof. This runtime proof remains a blocker for panel-by-panel native glass qualification.

### WS-14 — Curtain panel-by-panel native glass

Priority: **P1 after WS-13**  
Concurrency: **RED/YELLOW**  
Suggested branch: `feat/curtain-native-panels`

Scope:

- canonical `GeneratedCurtainPanelHandles`-style ownership;
- stale/fingerprint/health/release integration;
- bounded object count;
- LINE + guarded path mapping;
- Door/Opening interruption/clipping;
- select/Locate/cleanup integration;
- host + frame + panel recoverable replacement.

Depends on: WS-04 and WS-13.

### WS-15 — Grid/reference system

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/grid-system-workflow`

Existing Core/native source already covers Grid capture/naming/intersections/system planning and native annotation slices. Remaining scope:

- native creation/materialization of rectangular/radial Grid systems;
- reviewed spatial ordering UI;
- intersection markers and constraint references;
- structure-to-grid hosting/snapping;
- paper-space annotation lifecycle;
- Direct Draw/repeat Grid authoring;
- stable semantic Grid IDs through edit/rebuild.

### WS-16 — Floor/Level vertical-placement chain

Priority: **P0**  
Concurrency: **RED/YELLOW**  
Suggested branch: `feat/level-native-placement`

Scope:

Use `ElementVerticalPlacementService` as one semantic source of truth across:

- Wall/GlassWall/WallPier/StructuralWall;
- Beam/Column/Slab/Foundation/Stair/Railing where applicable;
- Door/WallOpening host matching/cutters;
- Curtain host/frame/panel;
- rebar/tie/stirrup/mesh Z placement;
- Direct Draw initial values;
- quantity/fingerprint/stale invalidation.

Compatibility contract:

- no Level refs = existing legacy source-relative geometry exactly;
- Bottom only = Level elevation + offset + legacy height;
- Bottom + Top = effective absolute height;
- Top without Bottom / missing Level / top <= bottom = fail closed.

Do not expose broad Level assignment UI until dependent native families consume the same contract.

### WS-17 — Structural host geometry v2

Priority: **P1**  
Concurrency: **YELLOW**  
Suggested branch: `feat/structural-geometry-v2`

Scope:

- richer Beam/Column/Slab/Foundation/Stair/Railing/Earthwork shape support;
- profile catalogs only with deterministic geometry contracts;
- opening/void semantics where applicable;
- Level integration;
- regeneration and generated ownership parity;
- no arbitrary profile support merely for feature-count parity.

### WS-18 — Slab/Foundation polygon mesh and holes

Priority: **P1**  
Concurrency: **YELLOW/LOCAL**  
Suggested branch: `feat/polygon-mesh-native-holes`

Core already has substantial polygon/hole planning. Remaining native/product work:

- outer-loop + hole-loop source identity/association;
- straight/bulged loop extraction;
- loop ownership/reconcile/stale/health;
- save/reopen/Undo/multi-DWG behavior;
- multiple disconnected outer regions/islands only after explicit topology ownership design;
- real V25 geometry proof.

Do not confuse scanline clipping with engineering code compliance.

### WS-19 — Rebar generated-geometry architecture

Priority: **P1**  
Concurrency: **YELLOW**  
Suggested branch: `refactor/rebar-generation-platform`

Scope:

- common generated-rebar replacement/result contracts;
- common count/spacing/diameter mode validation;
- reusable cover/face/layer semantics;
- common ownership/health/live-fingerprint services;
- batch caps and performance;
- consistent host Level integration;
- reduce one-off implementation differences among Column/Beam/Tie/Stirrup/Slab/Wall/Foundation families.

### WS-20 — Fabrication-grade rebar standards

Priority: **P0 product-policy gate / P1 implementation**  
Concurrency: **RED: engineering decision required**  
Suggested branch: `feat/rebar-standard-profile-<standard>`

Owner/engineer must choose the exact standard and revision first. Then implement versioned rule profiles for approved items only:

- bend diameter/radius;
- hook type/angle/tail;
- lap splice;
- anchorage/development;
- cover/clear spacing constraints;
- shape-code/BBS conventions;
- tolerances/rounding.

Every output/export must carry standard + revision provenance. `Approved`/qualification metadata is evidence plumbing, not automatic structural-engineering approval.

### WS-21 — Quantity/BQ/ED2/reporting platform

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/reporting-platform-v2`

Scope:

- report definition abstraction without duplicating quantity calculations;
- reusable scope/filter/group/sort configuration;
- deterministic column schema/version;
- customer/company report templates;
- formula provenance and unit display;
- round-trip Locate traceability;
- large-table performance;
- comparison/delta reports between revision baselines.

### WS-22 — Native semantic documentation tags/tables

Priority: **P1**  
Concurrency: **YELLOW/LOCAL**  
Suggested branch: `feat/native-documentation-v2`

Existing source includes semantic MText tags and multiple native Table artifacts. Remaining scope:

- MLeader/leader geometry;
- associative/batch tag placement;
- richer TableStyle/column/format standards;
- persisted user-defined semantic schedule definitions when product requirements justify them;
- robust Paper Space behavior;
- runtime qualification for save/reopen/Undo/Unicode/HiDPI/multi-DWG.

### WS-23 — Layout / Sheet / Viewport / title block system

Priority: **P2**  
Concurrency: **RED/YELLOW**  
Suggested branch: `feat/sheets-views-layouts`

Scope:

- stable QS3D sheet/view IDs separate from titles;
- generated Layout/Viewport ownership;
- user-layout protection;
- paper size/scale/lock rules;
- title-block mapping;
- create/update/rename/delete lifecycle;
- schedule/table placement;
- save/reopen/multi-DWG runtime proof.

Depends on: WS-22 native documentation maturity.

### WS-24 — Semantic interchange generic import

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/interchange-import-v1`

Existing source has export, validation, immutable read, diff/preview, append-only and KeepTarget-oriented slices. Remaining scope:

- executable `UseSourceSemanticData`;
- dependency-ordered replacement;
- explicit generated/native ownership clearing;
- controlled rebuild requirement;
- rename/remap policy;
- property/quantity/catalog precedence;
- source-handle provenance-only option;
- generic reviewed `QS3DINTERCHANGEIMPORT` UX;
- Undo/session/save-reopen/multi-DWG qualification.

No importer may deserialize portable JSON directly into native CAD ownership.

### WS-25 — IFC / BCF / Revit / external interoperability

Priority: **P2 after WS-24**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/interoperability-formats`

Scope candidates:

- IFC semantic export first;
- BCF issue/reference exchange;
- mapping profiles for external estimators;
- Revit exchange only after explicit schema/mapping requirements;
- no cloud/team service assumption until privacy/auth/product policy exists.

### WS-26 — Recognition / B4D / assisted capture

Priority: **P1**  
Concurrency: **GREEN/YELLOW**  
Suggested branch: `feat/recognition-b4d-v2`

Scope:

- rule/profile management UI;
- explainable confidence/rejection reasons;
- preview before batch apply;
- improved proxy/custom entity adapters only when authoritative metrics are available;
- source-unit contract integration;
- generated-output exclusion via canonical ownership;
- bounded scanning/performance;
- company layer/profile templates.

### WS-27 — Revision / audit / change review

Priority: **P1**  
Concurrency: **GREEN**  
Suggested branch: `feat/revision-change-review`

Scope:

- richer semantic revision diff;
- geometry/source-handle change classification;
- quantity delta by category/floor/zone;
- generated stale impact summary;
- audit filtering/export;
- change approval markers if needed;
- compare two `.qsdb`/semantic snapshots without requiring native CAD mutation.

### WS-28 — Workspace / Ribbon / Hub UX consolidation

Priority: **P1**  
Concurrency: **YELLOW**  
Suggested branch: `feat/ui-workspace-v2`

Scope:

- one discoverability map for authoring/capture/modify/review/schedule/documentation;
- compact BLT-familiar navigation without copying proprietary assets;
- reduce duplicate Hub entry points;
- document-bound vs active-document-dynamic window behavior clearly;
- stateful splitter/panel sizing;
- context menus;
- keyboard navigation/accessibility;
- consistent warning/error/result vocabulary;
- Family/Instance/Level/Zone interaction polish.

Avoid broad UI refactor while persistence/project-lifetime behavior is still moving.

### WS-29 — Unicode / HiDPI / theme runtime polish

Priority: **P1**  
Concurrency: **RED / LOCAL_ONLY**  
Suggested branch: `fix/v25-ui-dpi-theme`

Scope:

- 100/125/150/200% DPI;
- Vietnamese Unicode paths/text;
- dark/light BricsCAD host theme;
- narrow/normal/wide palettes;
- focus/selection behavior;
- modal/modeless Z-order;
- screenshot-driven layout fixes from real V25.

### WS-30 — Performance and large-model scalability

Priority: **P1 after correctness baseline**  
Concurrency: **GREEN + LOCAL measurement**  
Suggested branch: `perf/large-model`

Measure before optimizing:

- Project/Family/Element counts;
- regeneration/dependency graph;
- room graph;
- wall junction planning;
- Auto Host matching;
- Curtain grids/frames/panels;
- BQ/schedules/BBS/Interchange;
- ownership/Health/Release Check;
- generated rebar/mesh batches;
- multi-DWG window lifecycle;
- memory/object growth after rebuild/close/reopen.

Never remove safety/health checks merely to improve benchmark numbers.

### WS-31 — Commercial license policy and adapter enforcement

Priority: **P0 before paid distribution**  
Concurrency: **RED: owner policy required**  
Suggested branch: `feat/commercial-license-gating`

Owner must explicitly decide:

- SKU/product IDs;
- trial/subscription/perpetual model;
- seat/machine/user/org binding;
- expiry/grace/clock policy;
- offline vs activation service;
- public verification key rotation;
- replacement/deactivation/support process;
- license file scope/location;
- unlicensed command whitelist.

Then implement centralized startup/command gating. Do not copy license checks into every command.

### WS-32 — Packaging, Authenticode, installer/updater trust

Priority: **P0 before production release**  
Concurrency: **RED / release engineering**  
Suggested branch: `release/windows-signing-install`

Scope:

- real Authenticode certificate outside Git;
- trusted timestamp;
- package/manifest/tag version binding;
- sign -> verify -> finalize hashes/ZIP;
- clean install/upgrade/rollback/uninstall;
- DemandLoad without weakening `SECURELOAD`;
- updater substitution/replay rejection;
- customer-like Windows profile qualification.

### WS-33 — Exact-SHA BricsCAD V25 runtime qualification

Priority: **P0 release gate**  
Concurrency: **RED / LOCAL_ONLY**  
Suggested branch: no feature branch; execute against exact candidate SHA.

Use `docs/LOCAL-V25-QUALIFICATION.md` and `scripts/run-local-v25-qualification.ps1`.

Required families include:

- build against installed V25 assemblies;
- NETLOAD/DemandLoad;
- Ribbon/Workspace/hubs;
- Direct Draw/cancel/UCS;
- capture/edit/Build3D;
- Door/Opening;
- Room/HT_PHONG;
- Curtain;
- Structure/Rebar;
- save/reopen/SaveAs/multi-DWG;
- BQ/BBS/Excel/native tables/tags;
- Health/Release Check;
- Unicode/HiDPI;
- private-DWG regression;
- clean install lifecycle.

Only exact-SHA local evidence may be called `LOCAL_PASS`.

### WS-34 — Documentation/status reconciliation

Priority: **P0 ongoing hygiene**  
Concurrency: **GREEN**  
Suggested branch: `docs/status-reconciliation`

Current docs have begun to drift because implementation is moving faster than older architecture/handoff text. Examples include older statements about polyline/curtain/documentation gaps that newer source/status docs have already advanced beyond.

Scope:

- maintain one canonical current-state summary;
- mark historical handoffs as history rather than current truth;
- periodically reconcile `README.md`, `IMPLEMENTATION-STATUS.md`, `PLAN.md`, `ARCHITECTURE.md`, feature docs and open issue progress;
- separate `SOURCE_IMPLEMENTED`, `REMOTE_DONE`, `LOCAL_ONLY`, `LOCAL_PASS`, `NOT QUALIFIED`;
- avoid listing a feature as both missing and implemented in different current docs;
- update command inventory from source rather than handwritten drift.

### WS-35 — Test architecture / source-contract quality

Priority: **P1**  
Concurrency: **GREEN**  
Suggested branch: `test/contract-harness-v2`

Scope:

- keep deterministic Core tests distinct from static token/preflight tests;
- convert brittle implementation-token checks into behavior/architecture tests when possible;
- test catalog for ownership, rollback, persistence, units, interchange and generated families;
- mutation/fault-injection harness abstractions;
- performance baseline storage without customer data;
- test fixture provenance.

### WS-36 — Future hosted CAD adapters

Priority: **P3**  
Concurrency: **GREEN after Core boundaries mature**  
Suggested branch: `feat/autocad-adapter-spike`

Possible future work:

- AutoCAD hosted adapter reusing `QS3D.Core`;
- adapter-neutral CAD capability interfaces only where proven useful;
- never weaken BricsCAD V25 behavior merely to reach premature cross-CAD abstraction.

This is not a standalone CAD application plan.

## 6. Recommended merge/dependency order

### Phase 0 — stabilize foundations

Merge/resolve in this order where conflicts require serialization:

1. P0-A project Save/SaveAs/Close lifecycle;
2. P0-B drawing-unit/proxy safety;
3. WS-03 transaction/journal platform as needed;
4. WS-04 generated ownership evolution;
5. WS-34 documentation reconciliation after source baseline settles.

### Phase 1 — reliability and Modify workflow

Parallel lanes:

- WS-05 regeneration/invalidation;
- WS-08 Source Reconcile;
- WS-26 Recognition/B4D;
- WS-27 Revision/Audit;
- WS-21 Reporting.

### Phase 2 — BLT-familiar modeling depth

Parallel with coordination:

- WS-06 Direct Draw common engine;
- WS-09 Wall junction architecture;
- WS-10 WallPier advanced paths;
- WS-11 Room/Finish v2;
- WS-12 Opening/host v2;
- WS-15 Grid;
- WS-16 Level native placement;
- WS-17 Structure v2.

Local lane:

- WS-07 Direct Draw DrawJig/repeat.

### Phase 3 — generated-detail depth

Serialize key dependencies:

1. WS-13 Curtain orchestration atomicity;
2. WS-14 Curtain panels;
3. WS-18 native polygon/hole mesh;
4. WS-19 Rebar generation platform;
5. WS-20 standards-specific fabrication only after engineering policy exists.

### Phase 4 — documentation/interchange

Parallel:

- WS-22 native documentation v2;
- WS-24 generic semantic import;
- WS-23 sheets/views after documentation ownership is stable;
- WS-25 external formats after interchange semantics are stable.

### Phase 5 — commercial/product polish

- WS-28 Workspace/Ribbon consolidation;
- WS-29 real V25 DPI/theme polish;
- WS-30 large-model performance;
- WS-31 commercial license enforcement after owner policy;
- WS-32 signing/install trust.

### Phase 6 — release candidate qualification

- freeze exact SHA;
- run aggregate source/Core gates;
- run WS-33 exact-SHA V25 qualification;
- fix failures and repeat on the new exact SHA;
- only publish a production claim/release when the required exact-SHA gates are genuinely green.

## 7. Suggested multi-agent allocation

A practical parallel allocation after foundation integration:

- Agent A: persistence/project lifecycle;
- Agent B: units/B4D/source metrics;
- Agent C: generated ownership + health;
- Agent D: Direct Draw common engine;
- Agent E: wall/junction/WallPier Core planning;
- Agent F: room/finish;
- Agent G: opening/host lifecycle;
- Agent H: Curtain orchestration;
- Agent I: Grid + Level Core contracts;
- Agent J: polygon/rebar Core planning;
- Agent K: reporting/documentation models;
- Agent L: interchange import semantics;
- Agent M: revision/audit/tests;
- Agent N: docs/status reconciliation;
- Local V25 Agent: native runtime/UI/DrawJig/failure injection/qualification only.

Avoid assigning two agents simultaneously to the same large shared adapter files such as `Commands.cs`, core ownership policy, `ProjectElement`, central project context/lifecycle, Ribbon wiring or a single native builder family without an explicit integration plan.

## 8. Branch/merge discipline

For every workstream:

1. branch from the newest `main`;
2. keep one coherent owner-request/workstream purpose per branch;
3. inspect open PRs and current source before implementing functionality that may already exist;
4. avoid force-push/reset of shared `main`;
5. before merge, fetch/rebase/reapply onto current `main`;
6. preserve newer concurrent behavior rather than overwriting stale blobs;
7. add deterministic tests/preflights appropriate to the new contract;
8. update only the documentation whose status materially changed;
9. do not dispatch GitHub Actions merely because code/docs were merged;
10. when runtime proof is required, leave an exact local scenario instead of manufacturing `LOCAL_PASS` remotely.

## 9. Definition of product milestones

### Milestone A — Reliable Preview/Beta

Required:

- project save lifecycle settled;
- drawing units settled;
- no known ownership split-brain in supported families;
- Core/static validation green on candidate;
- exact V25 build/NETLOAD/primary workflows green;
- save/reopen/multi-DWG green;
- installer path usable on a clean test profile.

### Milestone B — Strong commercial QS/Modeling Beta

Add:

- Direct Draw repeat/preview polish;
- Level placement coherent;
- wall/opening/curtain major workflows runtime-qualified;
- polygon mesh native subset qualified;
- stronger documentation tables/tags;
- reporting templates and performance baseline.

### Milestone C — Production 1.0

Required in addition:

- exact-SHA runtime qualification complete;
- Curtain whole-command recovery solved for claimed supported output;
- every advertised generated family has ownership/stale/live-health coverage;
- commercial license policy implemented if product is sold with enforcement;
- production signing/timestamp/install/update trust proven;
- legal/source distribution model intentionally chosen;
- customer-facing docs contain no source-vs-runtime overclaim;
- known unsupported advanced geometry/engineering-standard behavior is explicitly bounded.

### Milestone D — Advanced/Enterprise

Candidates:

- physical junction output;
- panel-level Curtain modeling;
- fabrication-standard profiles;
- sheets/views/title blocks;
- generic interchange replace/merge;
- IFC/BCF/external integrations;
- company standards/templates;
- future hosted CAD adapter.

## 10. Overall review conclusion

QS3D currently has a broad and increasingly coherent source foundation. The best next step is **not** to keep adding unrelated commands to `main`. Development should now be split into explicit workstreams with a small number of shared foundation contracts:

- Project lifecycle/persistence;
- Units/provenance;
- Operation rollback/journaling;
- Generated ownership/stale/health;
- Vertical placement;
- Native runtime qualification.

Once these are stable, modeling/detail/reporting/interchange agents can work much more independently without repeatedly creating cross-layer regressions.

The repository should continue to describe source implementation and BricsCAD V25 runtime qualification separately. That distinction is essential until the exact candidate release SHA has passed the licensed local V25 matrix.

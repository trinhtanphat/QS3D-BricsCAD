# QS3D-BricsCAD full BLT3D parity design

**Issue / Lane-Key:** #6315 / `issue-6315`  
**Design date:** 2026-09-10  
**Approved interactively by owner:** architecture, parity matrix, data/transaction/error model, testing and rollout sequence  
**Repository boundary:** `QS3D-BricsCAD` remains a BricsCAD V25/V26 Windows x64 hosted plugin.

## 1. Purpose

The owner wants QS3D-BricsCAD to reach full user-visible functional and workflow parity with the recovered BLT3D reference, including both the BIM/QS product surface and the surrounding launcher, activation/license, updater, settings and installation experience.

This is not a request for a pixel-only clone or a second standalone CAD application. The implementation must preserve the existing QS3D/BricsCAD host boundary, reuse authoritative QS3D domain and persistence contracts, and add missing capabilities through explicit adapters or new QS3D-owned modules.

The target is therefore:

> BLT3D-familiar product experience and equivalent user-facing capability, implemented as QS3D-owned BricsCAD plugin workflows with auditable V25/V26 behavior.

## 2. Reference and legal/technical boundary

BLT3D is a reference product/specification source for observed workflow, UI organization and behavior. QS3D must not depend on BLT3D at runtime.

The parity effort must not copy or embed vendor secrets, private signing material, activation keys, license-server credentials, hidden licensing algorithms or other security-sensitive internals. License, activation, updater, installer and launcher functionality must be QS3D-owned equivalents.

The repository remains governed by `docs/PRODUCT-BOUNDARY.md`:

- BricsCAD owns the live DWG database, document/editor lifecycle, viewport, selection and native transactions;
- QS3D contributes Ribbon, palettes, modeless WPF UI, semantic BIM/QS state, recognition, quantities, reports and guarded generated geometry;
- V25 is `net48`, V26 is `net8.0-windows`;
- no standalone `QS3D.exe` CAD engine is introduced by this effort.

A bootstrapper/launcher may detect, install, repair, update and launch the correct BricsCAD host. It must not replace BricsCAD's CAD engine.

## 3. Definition of 100% parity

Parity is measured by observable behavior and domain outcomes, not by copied implementation details or matching source code.

A feature does not count as complete merely because a button, menu or screenshot exists. Every parity item progresses through the following evidence states:

```text
REFERENCE_CAPTURED
→ UI_PRESENT
→ COMMAND_WIRED
→ SEMANTIC_BEHAVIOR_PASS
→ SAVE_REOPEN_PASS
→ V25_V26_PARITY_PASS
```

The final product may only claim `100% BLT3D parity` when the parity manifest has no item in `PARTIAL`, `STUB`, `UI_ONLY`, `NOT_WIRED`, `UNVERIFIED` or any other non-pass state.

For capabilities where BLT3D uses proprietary/native internals that are not appropriate to reproduce, `100%` means equivalent supported user behavior under QS3D's architecture, with documented differences where the BricsCAD host requires them.

## 4. High-level architecture

The selected approach is a compatibility/parity layer rather than a big-bang port.

```text
BricsCAD V25 / V26
        │
        ▼
┌───────────────────────────────────────────────┐
│           QS3D BLT3D-Parity Shell            │
│ Ribbon • Workspace • Right Panel • dialogs   │
└────────────────────┬──────────────────────────┘
                     │
           Command / Workflow Registry
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
 existing QS3D   adapted QS3D    new parity
   services        services       services
       │             │             │
       └─────────────┴─────────────┘
                     │
              QS3D.Core / Platform
```

The parity shell presents BLT3D-familiar navigation and workflows. The command/workflow registry routes actions into authoritative QS3D operations. Existing services remain preferred when their semantics are correct. Missing behavior is implemented as a bounded new QS3D module rather than as duplicated parallel state.

## 5. UI and navigation target

The target workspace uses BricsCAD's real viewport and arranges QS3D around it:

```text
BricsCAD Ribbon with BLT3D-familiar domain tabs
┌─────────────────────────────────────────────────────────────────┐
│ left palette        │ native BricsCAD viewport │ right palette │
│ Project/Zone/Floor  │                           │ Properties    │
│ BIM model tree      │                           │ Drawing Mgr   │
│ contextual actions  │                           │ contextual UI│
└─────────────────────────────────────────────────────────────────┘
status/footer: active floor/elevation/workflow state
```

The owner-approved target includes the BLT3D-style product organization represented by these major Ribbon areas: `KHỞI ĐẦU`, `THIẾT LẬP DỰ ÁN`, `MÔ HÌNH BIM`, `NHẬN DẠNG`, `VẼ`, `TOOL`, `MODELING`, `CỐT THÉP`, `XEM`, `ĐỊNH LƯỢNG`, and `BẢN SỬA ĐỔI`.

Exact host placement may differ where BricsCAD Ribbon/palette APIs require it, but the workflow grouping, discoverability, labels, interaction state and command availability must remain equivalent.

## 6. Capability parity matrix

### 6.1 Shell, project and model organization

QS3D must provide BLT3D-familiar shell behavior, dark/light theme support, contextual controls, icons, footer/status context and keyboard shortcuts.

Project lifecycle covers New/Open/Save/Save As behavior, project metadata, Zone, Floor/Level, active context, recovery and schema migration. Existing QS3D drawing-bound project state remains authoritative.

Family/Type capability covers category trees, create/edit/delete/duplicate flows, parameters/property schemas, custom user/system types and model-linked selection.

### 6.2 BIM authoring

Required authoring workflows include point placement, line drawing, CAD-line following, arc, rectangle, circle and arbitrary closed-profile workflows.

Domain coverage includes at minimum the BLT3D-referenced structural/architectural families such as beams, slabs, columns, walls, openings/doors, stairs, foundations/piles/pile caps, finishes and relevant special categories.

Where QS3D already has a correct semantic command, the parity UI routes to it. Missing workflows become explicit new semantic operations and must not be implemented as untracked raw CAD entities.

### 6.3 Editing and modeling tools

Required editing includes move, copy, rotate, mirror, align, array, split, join, L/T connections, measurement, grips and contextual edit tools.

Required slab/profile tools include slope creation, slab cutting, boundary editing, openings and arbitrary profile editing.

Required modeling capability includes suitable primitives, extrusion, sweep, loft, union, subtract, intersect and working-plane workflows where supported by the product contract.

Native BricsCAD operations may be reused only when QS3D can keep semantic state, dependency/freshness state and undo/persistence coherent.

### 6.4 Recognition and CAD-to-BIM

Recognition parity covers selection/review, boundaries, labels/text, marks, grids, beams, columns and other supported recognition categories, plus preview, restore/review behavior and settings.

Recognition quality is not considered complete solely by detecting geometry. A successful flow must map accepted recognition results into correct Project/Zone/Floor/Family/Type/Element semantics and retain provenance to source CAD handles where applicable.

### 6.5 Rebar and structural specialist workflows

Parity includes rebar families/shapes, spacing/distribution, hooks, exclusions and applicable beam/column/slab/foundation reinforcement workflows represented by the reference product.

Existing QS3D structural/rebar services are reused first; missing workflows are added with dedicated tests and no hidden host-specific types leaking into host-neutral contracts.

### 6.6 Finishes, rooms, earthwork and special categories

Parity includes room/finish workflows, floor/wall/ceiling finish generation where supported, waterproofing/skirting-type outputs, earth mass/excavation/taluy operations, linking/unlinking, intersection visualization and materialization behavior where represented by the reference.

These operations must remain reversible and must distinguish authoritative source objects from generated/derived geometry.

### 6.7 View, selection and Drawing Manager

View/selection parity includes isolate/hide/show, clipping, direction/view helpers, fit/zoom, highlight and model-linked selection as appropriate to BricsCAD.

Drawing Manager parity includes add/load/reference, move/transform where supported, delete, rename, clip and management of linked/background drawing references with clear ownership and persistence.

### 6.8 IFC and interoperability

Parity includes the supported user workflows for IFC import, lightweight/reference import where technically appropriate, clearing references, export and storey/category mapping.

Existing QS3D interoperability contracts remain authoritative. No unsupported direct-RVT or other proprietary file-parser claim is implied by BLT3D parity.

### 6.9 Quantity, cost, reports and revisions

Quantity/BQ parity includes calculate/recalculate, settings/rules, breakdowns, summaries, per-floor/grouped detail, materials/rates and model-linked result review.

QS3D's existing measurement provenance, rate book, estimating, commercial adjustment, progress claim, audit and revision impact foundations remain authoritative even when the UI is reorganized to a BLT3D-familiar workflow.

Report parity targets Excel, PDF and Word outputs for supported summary/detail/floor views. Every report path must preserve traceability to the underlying QS3D model/quantity identity where the format permits it.

Revision/change workflows reuse QS3D revision and cost-impact infrastructure and expose a coherent BLT3D-familiar user flow rather than creating a separate revision database.

### 6.10 LUNA/AI and MCP

The parity scope includes an AI/chat surface, context display, CAD actions, status/error presentation and MCP integration.

The preferred architecture remains direct `ChatGPT → MCP → CAD` where the current QS3D MCP contract supports it. BLT3D is not inserted into that path and is not a runtime dependency.

MCP mutations must use the same semantic/transaction services as local UI commands so AI cannot bypass project invariants, audit, save/reopen or emergency-stop behavior.

### 6.11 Settings, shortcuts and localization

Settings parity includes theme, shortcut/key binding, project/general options, material/user-type management and other stable product settings represented by the reference.

At least Vietnamese and English strings must be normalized rather than scattered hard-coded text. Additional localization may reuse the repository's broader language strategy without blocking the parity core.

### 6.12 Launcher, activation/license, updater and installer

The owner explicitly approved including both application capability and surrounding product infrastructure.

The QS3D-owned launcher/bootstrapper must detect installed BricsCAD V25/V26 hosts, plugin registration state and version compatibility, and offer appropriate launch, install, repair and update entry points.

The QS3D-owned license system must support user-visible states equivalent to:

```text
Unlicensed → Trial → Activated → Expired/Revoked → Reactivate
```

It must provide status, activation/deactivation where applicable, offline/error handling and diagnostics. Expired or unavailable licensing must never corrupt a DWG or project. Necessary recovery/read access and uninstall/repair behavior must remain safe according to product policy.

Updater flow remains fail-closed:

```text
check manifest
→ validate product/version/host-major
→ download
→ hash/signature verification
→ staging
→ atomic install
→ verification
→ rollback on failure
```

V25 and V26 identities remain separate. A V25 binary/package must never be relabeled as V26 or vice versa.

Installer flows must preserve registration ownership, checksum/signature/version checks, transactional rollback and safe upgrade/repair/uninstall behavior.

## 7. Source of truth and data flow

The parity implementation must not introduce a BLT3D-shaped database beside QS3D's own model.

Authoritative semantic state remains in QS3D Project/Zone/Floor/Family/Type/Element and related existing contracts. CAD handles, dependency/freshness, quantity provenance and persistence remain connected to that same model.

Every modifying workflow follows this conceptual flow:

```text
BLT3D-style UI / command
    ↓
workflow registry
    ↓
validation of active document/project/zone/floor/family/type
    ↓
QS3D semantic operation
    ↓
BricsCAD transaction
    ↓
CAD geometry + semantic state
    ↓
atomic commit
    ↓
dirty/dependency/regeneration/audit
    ↓
save/report/BQ/MCP consumers
```

It is an invariant that a successful user operation cannot leave CAD geometry created without its required semantic state, or semantic state committed without its required CAD mutation. Failure before commit must roll back the whole operation.

## 8. Transaction and undo/redo semantics

Undo/redo boundaries follow user-visible business operations rather than raw entity counts.

Examples:

- one stair command that creates many CAD entities is one logical undo step;
- one array operation creating many elements is one logical operation;
- one accepted recognition batch has a documented atomic boundary;
- linked/generated child geometry is updated or rolled back consistently with its source operation.

Long-running operations may use staged internal work, but the final user-visible mutation must either reach a consistent committed state or fail without leaving a partial project.

## 9. Error handling

Errors are classified into three layers.

### User-validation errors

Examples: no active Family, invalid category, open profile, incompatible selection. These must produce no mutation and give actionable feedback.

### Domain/geometry errors

Examples: unsupported join, invalid intersection, geometry generation failure. These must roll back the current logical operation and preserve prior valid state.

### Runtime/host/infrastructure errors

Examples: BricsCAD transaction failure, file I/O failure, IFC failure, activation service error or updater failure. Mutation paths fail closed, preserve recovery evidence and expose diagnostics.

Presentation-only helpers may fail softly if they cannot corrupt semantic state. Mutation, persistence, license enforcement, quantity, reporting and update flows must not silently swallow errors and claim success.

Project/schema migration must never overwrite a source project irreversibly without the configured backup/version/recovery contract.

## 10. Parity manifest

A machine-readable or deterministically parseable parity manifest will be introduced as an implementation foundation. Each item needs a stable feature ID, reference description, owning domain and evidence status.

Conceptual record:

```text
Feature: BIM.Draw.Rectangle
Reference: BLT3D / MÔ HÌNH BIM / Chữ nhật
UI: PASS
Command: PASS
Geometry: PASS
SemanticData: PASS
Undo: PASS
SaveReopen: PASS
V25: PASS
V26: PASS
Parity: PASS
```

The manifest must distinguish `NOT_APPLICABLE_BY_HOST_BOUNDARY` from incomplete work. Such a status requires an explicit product decision and must not be used to hide a missing capability that can reasonably be implemented inside BricsCAD.

The closure gate reports counts by domain and fails any attempted `100%` claim while non-pass items remain.

## 11. Test and qualification strategy

Each implemented capability receives the narrowest deterministic test set that proves its contract, plus host runtime evidence where host behavior is essential.

The complete evidence ladder is:

```text
Core/unit tests
+ source/contract guards
+ V25/V26 compile
+ UI/command smoke
+ semantic mutation tests
+ undo/redo tests
+ save → close → reopen tests
+ quantity/report consistency tests where applicable
+ licensed BricsCAD runtime qualification where required
```

Static/host-neutral evidence must never be labeled as licensed BricsCAD runtime PASS.

CI uses the repository's protected branch/PR lifecycle. New code is merged only through canonical Reservation-v2 carriers and protected PR checks. Guards may be strengthened for correctness; they must not be weakened merely to make parity work green.

## 12. Rollout and decomposition

The total program is too large for a single implementation carrier. This document is the master architecture and acceptance contract; implementation must be decomposed into independently reviewable subprojects, each with its own Reservation-v2 ownership and, where architectural enough, its own focused spec/plan.

Recommended sequence:

```text
P0  migrate/verify QS3D workspaces from C: to D: without data loss
P1  parity manifest + workflow/command registry foundation
P2  shell/workspace/Ribbon + Project/Zone/Floor/Family navigation
P3  BIM authoring + editing/modeling tools
P4  recognition/CAD-to-BIM
P5  rebar + advanced structural workflows
P6  finishes/rooms/earthwork/special categories
P7  quantity + costing + reports + revision UX
P8  Drawing Manager + IFC/interoperability
P9  LUNA/AI + direct MCP-to-CAD integration
P10 settings + shortcuts + themes + localization normalization
P11 launcher + QS3D license/activation + updater + installer UX
P12 V25/V26 full qualification and save/reopen campaign
P13 parity closure gate reaches 100%
```

The sequence may be refined when current-main evidence shows that a later domain already has stronger implementation than expected, but dependency order must be respected. In particular, visible UI must not outrun the semantic command it claims to expose.

## 13. Repository migration safety

The owner's disk-management requirement is part of program setup: QS3D-BricsCAD workspaces and worktrees currently living on C: should be migrated to D: because C: is nearly full.

Migration must be copy-first and verification-first, not destructive move-first. Required safety steps are:

1. inventory all registered worktrees and dirty/untracked state;
2. copy to D: without following junctions/reparse loops;
3. repair Git worktree links after path changes;
4. verify branch, HEAD and working-tree status for each migrated worktree;
5. preserve dirty/untracked files and any evidence artifacts;
6. run Git integrity checks appropriate to the repository;
7. keep a refs/bundle or equivalent recovery point before deleting source copies;
8. only reclaim C: storage after verification proves the D: counterpart is usable.

Processes already using C: paths must not be killed or invalidated merely to complete the migration. Compatibility junctions may be used temporarily if required to avoid breaking active agents/tools, but D: becomes the canonical workspace target for this parity program.

## 14. Non-goals

This program does not:

- create a standalone CAD engine in `QS3D-BricsCAD`;
- duplicate BricsCAD's DWG database or viewport;
- create a second semantic project database solely to resemble BLT3D internals;
- copy BLT3D vendor secrets, keys, signing materials or private license-server implementation;
- treat a screenshot or UI-only button as functional parity;
- bypass protected-main checks, Reservation-v2 ownership or V25/V26 identity rules;
- claim direct proprietary RVT support solely because BLT3D has an import/integration workflow.

## 15. Completion criteria

The overall program is complete only when all of the following are true:

1. the parity manifest contains every agreed BLT3D-reference capability and has no unresolved non-pass item;
2. shell/UI, commands and semantic behavior agree rather than presenting unavailable functionality;
3. mutating workflows preserve atomic transaction and undo/redo semantics;
4. representative project data survives save/close/reopen without semantic or CAD divergence;
5. quantity/report outputs remain traceable and consistent after relevant editing workflows;
6. launcher/license/updater/installer are QS3D-owned and fail safely;
7. V25 and V26 have separate correct binaries and pass the required qualification evidence;
8. direct MCP-to-CAD actions use the same guarded semantic mutation layer as local commands;
9. every implementation carrier has passed protected CI and landed through the repository's normal PR-only main path;
10. final closure evidence supports the literal `100%` parity claim rather than an estimated percentage.

## 16. First implementation boundary after this design

After this master spec is reviewed and approved, implementation planning should begin with the smallest foundational subproject rather than a mega-plan for all domains.

The first implementation plan should cover P0/P1 prerequisites only: finish the safe D: workspace migration, establish the parity manifest format/closure semantics, and introduce the minimal workflow/command registry seam needed by later UI and domain carriers.

Subsequent domain work should be planned and merged incrementally against fresh `main`, using separate carriers whenever ownership, risk or revert boundaries differ.
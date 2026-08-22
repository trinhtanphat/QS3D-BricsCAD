# QS3D documentation layer

The documentation layer must remain connected to QS3D semantic identity. A DWG text/table object that cannot be traced back to semantic source data is not a completed QS3D documentation workflow.

## Source-implemented Core foundation

### Semantic tag rendering

`QS3D.Core.Documentation.SemanticTagRenderer` renders bounded deterministic labels from a real `ProjectElement` that belongs to the supplied `ProjectState`.

Supported tokens:

```text
{Id}
{Category}
{Family}
{Floor}
{Zone}
{P:<semantic-property-name>}
{Q:<quantity-name>}
```

Examples:

```text
{Category} • {Family}
{P:Mark} • V={Q:VolumeM3}
{Floor}/{Zone} • {Id}
```

Unknown tokens fail closed. Missing referenced Family/Floor/Zone fails closed. Missing optional `P:`/`Q:` values render empty so one template can be reused across compatible element variants.

Generated/native runtime ownership is not documentable through `P:`. The renderer rejects canonical generated owner slots plus `Generated*`, `QS3D.Generated*` and `PhysicalOpeningCut*` properties. Native object handles are not semantic annotation values.

### Semantic documentation table model

`SemanticDocumentationTableBuilder` adds a CAD-independent table model that deliberately reuses `SemanticTagRenderer` for every cell instead of creating another property/quantity interpretation engine.

Current contract:

- caller supplies an explicit ordered semantic element-ID list; Core preserves that order and does not infer CAD/table sort order;
- caller supplies bounded column definitions as `Header + SemanticTagRenderer template`;
- title, row count, column count, headers and element IDs are bounded;
- duplicate element IDs and duplicate headers are rejected case-insensitively;
- every element ID must resolve uniquely in the supplied project before rows are rendered;
- generated/native ownership properties remain blocked because cell rendering passes through `SemanticTagRenderer`;
- output rows/cells/headers are **defensively copied** into read-only collections; mutating a caller-owned source list after construction cannot rewrite a previously built documentation snapshot, and casting the exposed `IReadOnlyList` back to `IList` does not make it writable;
- the builder is read-only: it returns `SemanticDocumentationTable` / row/cell data and never creates CAD entities or changes semantic state.

This is a reusable input to the source-implemented native Semantic Element Table adapter and to other documentation exporters. It is **not** a second BQ/BBS/schedule calculation engine and must not be used to bypass the existing schedule models where a specialized schedule already exists.

### Semantic View / Sheet planning and persistence

The CAD-independent View/Sheet planning layer is already source-implemented and must remain the authority for future native Layout/PaperSpace materialization:

- `SemanticViewPlanner` validates deterministic semantic view definitions, stable IDs/names and optional Floor/Zone/category filters;
- `SemanticSheetPlanner` validates stable sheet IDs/numbers, paper bounds, optional title-block name and non-overlapping view placements;
- `SemanticSheetAutoLayoutPlanner` performs deterministic multi-sheet packing with bounded margins/gaps and reserved bottom/title-block space instead of making CAD runtime code invent another packing algorithm;
- `SemanticSchedulePlacementPlanner` places explicit schedule rectangles on an existing validated sheet using persisted `SemanticScheduleDefinition.Id` as the stable identity, paper-millimetre coordinates, deterministic bounded packing, existing semantic view placements as occupied regions and optional reserved bottom/title-block space;
- `SemanticSheetIndexBuilder` derives a bounded, handle-free Sheet Index from validated semantic sheet plans, preserves stable `SheetId` separately from display number/name, sorts deterministically by number then ID, rejects duplicate IDs/numbers case-insensitively, and returns a defensive read-only snapshot;
- `SemanticTitleBlockParameterMapBuilder` maps bounded opaque destination parameter tags to explicit semantic Sheet fields (`SheetId`, number, name, optional title-block name and placed-view count), renders values deterministically/invariantly and does not encode BricsCAD attribute syntax or native IDs in Core;
- `SemanticDocumentationCatalogStore` persists the documentation catalog in project metadata with bounded XML parsing/serialization;
- `SemanticDocumentationCatalogEditor` performs referentially safe View/Sheet replacement/removal so a view cannot silently disappear while sheet placements still reference it.

Schedule placement is deliberately separate from `SemanticSheetPlacementDefinition.ViewId`: a schedule never masquerades as a view. Schedule margins constrain **new schedule candidates**, not pre-existing validated view placements; an existing view may legally sit closer to the paper edge than the configured schedule margin as long as it remains inside the paper bounds. Missing/duplicate schedule IDs, non-finite or non-positive geometry, unusable paper regions and items that cannot be placed without overlap fail closed.

These classes are planning/persistence/documentation infrastructure. They do **not** by themselves prove native BricsCAD Layout, PaperSpace Viewport, title-block insertion/attribute discovery, viewport scale/lock, native schedule/Table placement, native Sheet Index Table materialization or save/reopen behavior.

Source checks:

```text
python scripts/preflight-semantic-tags.py
python scripts/preflight-semantic-documentation-table.py
python scripts/preflight-semantic-sheet-index.py
python scripts/preflight-semantic-title-block-map.py
python scripts/preflight-semantic-schedule-placement.py
```

The Core smoke suite includes `SemanticTagRendererSmoke`, `SemanticDocumentationTableSmoke`, `SemanticViewSheetPlannerSmoke` and `SemanticSchedulePlacementSmoke`. Schedule-placement coverage locks stable schedule identity, deterministic collision avoidance, reserved title-block space, valid paper-edge views outside schedule margins and fail-closed missing/duplicate IDs, oversized items and invalid geometry. Documentation-table, Sheet Index and title-block-map coverage verifies that returned snapshots are not externally mutable; the Sheet documentation smoke also locks deterministic ordering, bounds, duplicate identity/tag rejection and unknown mapping-field failure.

## Native V25 status

Issue #77 remains open because native documentation support is intentionally incremental. Distinguish source-implemented native slices from host/runtime work that still requires exact BricsCAD V25 qualification.

### Semantic tags — source-implemented MText slice

The current V25 source implements an ownership-aware MText semantic-tag lifecycle:

- `QS3DTAG` resolves one authoritative semantic CAD source, renders through the central semantic tag renderer and creates/updates generated MText;
- `QS3DTAGREFRESH` rebuilds the generated MText at its stored world position/rotation;
- `QS3DTAGREMOVE` removes only ownership-resolved generated semantic tag content and fails closed on foreign generated objects;
- `QS3DTAGHEALTH` is read-only and reports persisted/native ownership/content drift without bootstrapping project state.

The current P0 placement slice is deliberately narrower than full annotation parity: it requires a single authoritative source handle and a supported UCS plane. It must not be documented as MLeader/leader support.

Still open for semantic annotations:

- native MLeader/leader geometry and style behavior;
- associative/batch tag placement and richer placement policies;
- explicit Model Space/Paper Space annotation workflows beyond the currently qualified slice;
- exact-SHA V25 compile/runtime, Unicode/HiDPI and save/reopen qualification.

Do not guess MLeader or other V25 API signatures. Compile against the exact installed V25 managed assemblies before adding host-specific calls.

### DWG tables — source-implemented native Table slice

Native Table creation is also present in source. `QS3DELEMENTTABLE`, `QS3DELEMENTTABLEREFRESH`, `QS3DELEMENTTABLEREMOVE` and `QS3DELEMENTTABLEHEALTH` provide a bounded generic semantic-element Table lifecycle backed by the central documentation table model and generated ownership.

The current Semantic Element Table P0 explicitly requires ModelSpace; PaperSpace/Layout behavior belongs to the sheet lifecycle rather than being silently inferred. Specialized QS3D schedules remain authoritative for their calculated rows/units and must not be replaced by a second quantity engine.

Still open for native table qualification/expansion:

- richer V25 `TableStyle`, column/format and specialized schedule presentation behavior;
- Unicode Vietnamese, row/column bounds, long values and units on the real host;
- PaperSpace/layout/page behavior where a workflow actually requires it;
- deterministic refresh/replacement and save/reopen qualification on exact V25 builds.

### Layout / Sheet / View — Core planned, native materialization still open

Core planning/persistence, deterministic schedule placement, the handle-free Sheet Index model and title-block parameter mapping contract are implemented, but native BricsCAD Layout/PaperSpace/Viewport/title-block/Schedule-Table/Sheet-Index-Table materialization is still open. Native code should consume the existing `SemanticViewPlanner`, `SemanticSheetPlanner`, `SemanticSheetAutoLayoutPlanner`, `SemanticSchedulePlacementPlanner`, `SemanticSheetIndexBuilder`, `SemanticTitleBlockParameterMapBuilder` and documentation catalog rather than rebuilding their identity/layout/index/mapping rules.

Before calling this native workflow complete, establish and qualify:

- stable QS3D sheet/view/schedule identity separate from display title;
- ownership of generated layouts/viewports/title blocks/schedule tables without deleting user-created content;
- mapping of semantic sheet paper bounds, view placements and schedule placement plans into Layout/PaperSpace coordinates;
- title-block selection/insertion and destination-attribute discovery rules without assuming a customer-private block definition exists;
- view target/direction, viewport scale and viewport lock rules;
- native schedule/Table placement/update ownership rules driven from the Core schedule placement plan;
- native Sheet Index Table placement/update ownership rules driven from the Core index rather than rescanning layouts as a second source of truth;
- update/recreate/rename/delete behavior for both semantic catalog and native objects;
- Model/Paper Space context switching safety;
- save/reopen and multi-DWG behavior;
- exact V25 API compile/runtime proof.

Do not mark native Sheet/View complete from model-space view commands alone; general model-space focus/zoom/orbit commands are not a PaperSpace viewport lifecycle.

## Local close-out

For a documentation feature, append a sanitized result to the local qualification handoff:

```text
Exact SHA: <40-char SHA>
BricsCAD V25 edition/build: <value>
Feature: <Tag | DWG Table | Sheet/View>
Core semantic renderer/schedule: PASS/FAIL
Native ownership/replacement: PASS/FAIL
Unicode/HiDPI: PASS/FAIL
Model/Paper Space behavior: PASS/FAIL
Save/reopen: PASS/FAIL
Multi-DWG: PASS/FAIL
Known blockers: <sanitized list>
```

No private DWG, proprietary BricsCAD DLL or customer data should be committed as evidence.

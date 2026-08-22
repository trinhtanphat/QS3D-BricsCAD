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
- the builder is read-only: it returns `SemanticDocumentationTable` / row/cell data and never creates CAD entities or changes semantic state.

This is intended as a reusable input to a future native BricsCAD Table adapter or to other documentation exporters. It is **not** a second BQ/BBS/schedule calculation engine and must not be used to bypass the existing schedule models where a specialized schedule already exists.

Source checks:

```text
python scripts/preflight-semantic-tags.py
python scripts/preflight-semantic-documentation-table.py
```

The Core smoke suite includes `SemanticTagRendererSmoke` and `SemanticDocumentationTableSmoke`.

## Native V25 work that remains

Do not mark #77 complete from the Core renderers/models alone. A local agent with the exact BricsCAD V25 assemblies/runtime must design and qualify native annotation/document behavior.

### Semantic tag placement

Required contract:

- select/resolve a semantic owner through canonical source/generated ownership;
- render text only through `SemanticTagRenderer` or a compatible centrally tested renderer;
- store a stable semantic owner ID and tag-template identity on the generated annotation;
- give generated tag entities their own canonical generated ownership slot; do not overload `GeneratedSolidHandle`;
- replacement/update must be ownership-safe and transactional;
- source/property/quantity changes must make affected tags stale or update them deterministically;
- deleting/untracking an owner must not leave a tag pretending to be valid;
- foreign/ambiguous annotations must fail closed rather than being erased;
- Paper Space vs Model Space behavior must be explicit; do not silently move annotations between spaces.

Use native MText/MLeader/Table APIs only after compiling against the installed V25 SDK/managed assemblies. Do not guess API signatures.

### DWG tables

A first native table slice should reuse an existing QS3D schedule model (for example BQ, Door/Opening, Room Finish, Material or BBS) or the bounded `SemanticDocumentationTable` model where a generic semantic table is explicitly desired. Do not create a second quantity calculation engine.

The native table should carry schedule/table kind, schema/version, project identity and generated ownership, with deterministic refresh/replacement. If a specialized QS3D schedule already exists, that schedule remains authoritative for its calculated rows/units; `SemanticDocumentationTableBuilder` is not a substitute for BQ/BBS logic.

Local acceptance must cover table styles, Unicode Vietnamese, row/column bounds, long values, units, page/layout behavior and update after semantic changes.

### Layout / Sheet / View

Treat BricsCAD Layout/Viewport lifecycle as runtime-gated. Before adding automatic sheet generation, establish:

- stable QS3D sheet/view identity separate from display title;
- ownership of generated layouts/viewports without deleting user-created ones;
- scale, paper size and viewport lock rules;
- update/recreate/rename/delete behavior;
- model/paper-space context switching safety;
- save/reopen and multi-DWG behavior;
- exact V25 API/runtime proof.

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

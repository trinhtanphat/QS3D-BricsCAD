# QS3D documentation layer

The documentation layer must remain connected to QS3D semantic identity. A DWG text object that cannot be traced back to a semantic element is not a completed QS3D tag workflow.

## Source-implemented Core foundation

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

Source checks:

```text
python scripts/preflight-semantic-tags.py
```

The Core smoke suite includes `SemanticTagRendererSmoke`.

## Native V25 work that remains

Do not mark #77 complete from the renderer alone. A local agent with the exact BricsCAD V25 assemblies/runtime must design and qualify native annotation/document behavior.

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

A first native table slice should reuse an existing QS3D schedule model (for example BQ, Door/Opening, Room Finish, Material or BBS) rather than create a second calculation engine. The table should carry schedule kind/version/project ID and generated ownership, with deterministic refresh/replacement.

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

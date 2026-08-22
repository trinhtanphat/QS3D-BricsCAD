# BLT legacy entity adapter — Q&A, requirements and implementation record

Date: 2026-08-21 (UTC+7)  
Repository: `trinhtanphat/QS3D-BricsCAD`  
Issue / Lane-Key: #3352 / `issue-3352`  
Canonical owner/session: `interactive-20260821-0917-g56s-bltlegacy`  
Canonical branch: `agent/interactive-20260821-0917-g56s-bltlegacy/issue-3352-blt-legacy-adapter`  
Baseline `main`: `5446d92dfb1216e4c7f064d7803a15b7dfe30dde`

This is the single lane-specific Markdown requested by the owner to consolidate the questions, requirements, answers, implementation decisions and acceptance boundary for the BLT3D legacy-object compatibility work. The broader historical session review remains in `docs/SESSION-REVIEW-BLT3D-QS3D-2026-08-21.md`; this file is the focused engineering record for Issue #3352 and does not replace repository policy.

## 1. Owner goal

The business goal is:

> **Open an old DWG authored with BLT3D → do not redraw → QS3D recognizes Column/Beam/Slab/Foundation/Structural Wall → calculate concrete/formwork from authoritative evidence → export Excel → trace the workbook row back to the original legacy CAD object.**

The required compatibility path is intentionally clean-room and non-destructive:

```text
Legacy BLT3D DWG
      ↓
public BricsCAD/ODA entity evidence
      ↓
BltLegacyEntityAdapter
      ↓
QS3D semantic ProjectElement
      ↓
existing Quantity Engine 2
      ↓
existing customer Excel exporter
      ↓
existing Excel → CAD Handle trace
```

QS3D must not require a redraw, destructive explode, conversion of the source object, copied BLT implementation, or a proprietary BLT runtime dependency.

## 2. Q&A — can the supplied BLT1 archive be inspected?

### Owner question

Can the supplied `BLT1.rar` be read/analyzed?

### Answer / evidence

The archive directory could be enumerated. It contains a BLT3D installation/build payload including notable managed binaries and debug symbols such as:

- `BLT3D V25.6.exe`;
- `blt3D.dll` / `blt3D.pdb`;
- `BltColumnWrapper.dll` / `.pdb`;
- `blt_library.dll` / `.pdb`;
- `BLT_QS_V2021.dll`, `BLT_QS_V2023.dll`;
- `model_from_cad.dll` / `.pdb`;
- `BLT-BIM.dll` and related BricsCAD-side modules;
- `blt_qs.lsp`;
- Excel templates/resources, DWGs, icons and rebar-shape assets.

The current remote environment did not have a RAR extraction backend during that inspection, so archive payload enumeration was confirmed but a complete extracted static analysis was not used as the implementation authority for this lane.

More importantly, repository clean-room policy means the new compatibility layer must be implemented from observable/public data contracts rather than copying or depending on proprietary BLT implementation details.

## 3. Q&A — can current QS3D directly export quantities from original BLT3D objects?

### Owner question

For objects drawn by BLT3D, can current QS3D export their quantities to Excel?

### Answer

Before Issue #3352, the correct answer was:

- **QS3D semantic/native objects:** yes;
- **native Solid3d captured/recognized into QS3D semantic data:** yes;
- **objects already mapped to QS3D semantic elements:** yes;
- **original BLT3D custom/proxy objects retaining only legacy metadata/runtime class:** not proven as a direct input path;
- **legacy ProxyEntity with no supported measurement adapter:** not guaranteed.

The output side already existed. `QS3DEXCEL` and the quantity/report pipeline can export semantic elements with source CAD Handle provenance, and `QS3DEXCELTRACE` can locate those handles back in the drawing. The missing bridge was the input side:

```text
BLT legacy object
    ↓  missing before #3352
QS3D semantic element
    ↓  already implemented
Quantity / Excel / reverse trace
```

## 4. Q&A — requested plan for `BltLegacyEntityAdapter`

### Owner requirement

Create a plan whose target is:

> `mở file cũ vẽ bằng BLT3D → không cần redraw → QS3D nhận Cột/Dầm/Sàn/Móng/Vách… → bóc BT/VK → xuất Excel`

### Answered architecture

The proposed implementation was split conceptually into six phases:

1. **PR-1 / probe:** inspect the real public storage/runtime shape of BLT legacy entities;
2. **candidate + evidence model:** normalize legacy signals without mutating project state;
3. **category/family/floor semantic mapping:** convert only proven evidence to QS3D semantics;
4. **concrete integration:** reuse authoritative geometry/legacy quantity evidence;
5. **formwork integration:** require explicit/per-face/qualified evidence rather than default-family fabrication;
6. **scan/import/audit + Excel round trip:** make the end-user workflow usable and testable.

For repository collision safety, Issue #3352 carries these phases as one canonical vertical slice instead of creating several competing branches that edit the same semantic/quantity surfaces.

## 5. Q&A — why was PR-1 proposed?

### Owner question

What is the purpose of PR-1?

### Answer

PR-1 was proposed to answer the questions that cannot be safely guessed from a screenshot or category list:

- Is a legacy BLT object a custom entity or `ProxyEntity` in the target host state?
- What runtime/original class name is publicly exposed?
- Does it retain XData?
- Does it retain an Extension Dictionary / Xrecords?
- Where are category, floor, family, element name, material and dimensions stored?
- Is concrete/formwork already stored as a unit-labelled value?
- Does a proxy expose exact public geometry directly or through transient `Explode()` output?
- What remains readable with and without the historical BLT runtime installed?

Without this probe, an implementation might incorrectly infer category from Layer or use a bounding box as volume/formwork. That can silently produce wrong results for round columns, irregular/sloped beams, slabs with openings, arbitrary foundations or walls with openings.

Therefore PR-1 is an **engineering diagnostic**, not a customer quantity feature by itself.

## 6. Q&A — how should PR-1 be run?

### Owner question

How do I run PR-1?

### Answer before implementation

At the time of the question, `QS3DBLTPROBE` did not yet exist on `main`. The repository only had `QS3DBRCPROBE`, an automation/public-metric probe deliberately designed to omit CAD handles, layers, property values and BLT-specific evidence, so it could not replace the required legacy schema probe.

The intended workflow was:

1. build/load the matching QS3D BricsCAD plugin;
2. open a **copy** of an old BLT3D-authored DWG;
3. `NETLOAD` the QS3D DLL;
4. run `QS3DBLTPROBE`;
5. select representative legacy objects — ideally 2–3 each for Column, Beam, Slab, Foundation and Structural Wall;
6. inspect the JSON report;
7. where possible, repeat with the historical BLT runtime loaded and unloaded to compare the public entity surface.

## 7. Owner integration request

The owner then explicitly requested the full repository lifecycle:

- create one Markdown consolidating all questions/requirements/answers;
- implement the complete BLT legacy adapter direction in `QS3D-BricsCAD`;
- read repository rules first;
- commit/push through the repository lifecycle;
- fix CI failures;
- process the canonical issue/branch/PR;
- merge the authorized task to `main` when protected checks are green.

Repository rules prohibit direct task writes to `main`. The owner instruction explicitly authorizes integration/merge for this task, but implementation still follows:

```text
leaf Issue → one canonical agent branch → branch CI → one canonical PR
→ protected preflight/core → merge → refresh main
```

Issue #3352 is the leaf reservation for this scope.

## 8. Implemented design in Issue #3352

### 8.1 Shared Core adapter

`src/QS3D.Core/Legacy/BltLegacyEntityAdapter.cs` introduces:

- `BltLegacyEvidenceMode`;
- `BltLegacyMetadataKeys`;
- `BltLegacyElementCandidate`;
- `BltLegacyEntityAdapter`.

Evidence modes are intentionally explicit:

- `Insufficient`;
- `MetadataOnly`;
- `SemanticReconstructed`;
- `ExactGeometry`;
- `ExactLegacyQuantity`.

The adapter is fail-closed. It refuses to claim a generic third-party proxy as BLT and refuses to import an ambiguous category.

### 8.2 Supported structural category recognition

The first compatibility target is:

- `Column` / Cột;
- `Beam` / Dầm;
- `Slab` / Sàn;
- `Foundation` / Móng, đài, móng băng/bè;
- `StructuralWall` / Vách BTCT.

Text/runtime/XData/dictionary evidence may identify these categories when explicit. Integer codes are **not guessed**. Only integer aliases independently established by existing repository quantity-rule code are accepted initially:

- `601` → `Column`;
- `701` → `StructuralWall`.

Other BLT integer codes remain diagnostic evidence until a real legacy drawing establishes their meaning.

### 8.3 Public host inspector

`src/QS3D.BricsCAD.V25/BltLegacyCommands.cs` is linked into V26 by the existing V26 project structure and provides a bounded, read-only host inspector that records:

- source CAD Handle;
- runtime entity type;
- Layer;
- proxy state;
- public proxy original-class/DXF/application description when exposed;
- geometric extents for diagnostics only;
- XData typed values;
- Extension Dictionary keys and Xrecord typed values;
- direct Curve/Area/Solid3d metrics;
- transient proxy `Explode()` result count;
- an exact proxy-solid metric only when **all top-level transient explode parts are Solid3d**.

Calling `Entity.Explode()` in this inspector does not erase or replace the source object. Returned DBObjects are transient and disposed after inspection.

### 8.4 Commands

#### `QS3DBLTPROBE`

- uses PICKFIRST or prompts for selection;
- inspects selected entities read-only;
- writes `QS3D_BLT_LEGACY_PROBE_V1` JSON under the user's temp directory in `QS3D-BLT-Probe`;
- records candidate category, evidence mode, import readiness, metrics, reason and bounded metadata.

#### `QS3DBLTSCAN`

- scans Current Space with a 250,000-entity guard;
- reports how many explicit BLT candidates were found;
- reports ready/blocked/unknown-category counts and category totals.

#### `QS3DBLTAUDIT`

- performs the scan;
- prints the first blocked handles and the exact fail-closed reason;
- guides the user to probe representative objects when schema evidence is missing.

#### `QS3DBLTIMPORT`

- resolves drawing units first;
- scans explicit BLT candidates;
- imports only candidates whose category and quantity/geometry evidence meet the current capture eligibility policy;
- calls the existing `SemanticCaptureService` rather than creating another semantic/quantity engine;
- preserves original source Handles;
- upserts through the existing semantic Handle ownership behavior;
- applies exact unit-labelled legacy concrete/formwork values when such evidence exists;
- maps an exact Floor/Family hint only to an already-existing matching QS3D item;
- keeps unresolved hints as traceable `CAD.BLT.*` properties;
- removes default-generated `FormworkM2` when no exact formwork evidence exists, so a legacy import does not silently export default-family VK as if it were measured;
- leaves blocked/unknown objects unchanged.

After import, the existing user path remains:

```text
QS3DQUANTITYENGINE2
QS3DEXCEL
QS3DEXCELTRACE
```

Because semantic capture preserves the original CAD source Handle, the existing workbook provenance can trace back to the legacy object rather than to a redrawn replacement.

## 9. Concrete vs formwork evidence policy

### Concrete (BT)

Preferred authority order:

1. exact public Solid3d/proxy-solid volume in drawing units, converted through the existing QS3D unit policy;
2. explicit unit-labelled legacy quantity such as `ConcreteM3` when present in public metadata;
3. future schema-qualified semantic reconstruction from proven dimensions;
4. otherwise blank/blocked — never bounding-box volume.

### Formwork (VK)

Formwork is more sensitive than volume because total surface area is not automatically the same as formwork area. The lane therefore follows:

1. exact unit-labelled legacy `FormworkM2` if publicly stored and unambiguous;
2. future qualified per-face/BRep calculation with category/intersection rules;
3. otherwise no authoritative `FormworkM2` is exported for the legacy import.

A bounding box or default QS3D family size is not accepted as evidence for an arbitrary legacy body.

## 10. Idempotence and source safety

Required invariants:

- source BLT/Proxy entity is opened `ForRead`;
- source entity is not erased, replaced, exploded into the DWG, converted or redrawn;
- source Handle remains the semantic provenance authority;
- repeated import of the same source Handle must resolve/update the existing semantic owner rather than intentionally create a second legacy semantic instance;
- generic non-BLT proxies remain outside this adapter;
- ambiguous category evidence remains blocked;
- unsupported data remains visible through audit/probe rather than silently guessed.

## 11. Tests added

`BltLegacyAdapterSmoke` covers the shared deterministic adapter contract:

- a BLT Column Proxy with exact geometry becomes import-ready;
- embedded explicit `ConcreteM3` / `FormworkM2` / Floor / Family hints are parsed and preserved;
- ambiguous `BLT_COLUMN_BEAM` evidence fails closed;
- a generic third-party Proxy is not claimed as BLT.

The smoke is wired into the repository smoke-test entry point.

## 12. How to run the implemented probe/import workflow

Once the build containing Issue #3352 is loaded in BricsCAD:

```text
NETLOAD
<choose QS3D.BricsCAD.V25.dll or QS3D.BricsCAD.V26.dll>
```

Then on a **copy** of a legacy BLT3D DWG:

```text
QS3DBLTSCAN
QS3DBLTAUDIT
QS3DBLTPROBE
```

For `QS3DBLTPROBE`, select representative objects. The command prints the generated JSON path.

When `SCAN/AUDIT` reports `ready > 0`:

```text
QS3DBLTIMPORT
QS3DQUANTITYENGINE2
QS3DEXCEL
```

To verify provenance, use:

```text
QS3DEXCELTRACE
```

and select a workbook row. The expected result is selection/zoom back to the original live source Handle.

## 13. Recommended legacy test matrix

At minimum, qualify:

- rectangular Column;
- circular Column if present;
- ordinary Beam;
- intersecting Beam;
- plain Slab;
- Slab with opening;
- isolated/strip/raft Foundation variants where available;
- Structural Wall;
- Structural Wall with opening.

For each sample record:

- legacy object count;
- runtime/proxy class;
- category evidence;
- Floor/Family evidence;
- concrete value/reference;
- formwork value/reference;
- source Handle;
- imported semantic ID;
- exported Excel row;
- Excel trace result.

Where possible repeat the same DWG with historical BLT runtime **loaded** and **not loaded**.

## 14. Acceptance boundary

### Source-safe acceptance

The repository lane can prove through source/tests/CI that:

- commands and adapter are wired;
- detection is bounded/fail-closed;
- deterministic category/evidence logic passes tests;
- no proprietary BLT dependency is introduced;
- original source Handle is preserved into the existing semantic/export path.

### Runtime acceptance still requires a real old DWG

A remote CI run cannot prove what a particular historical BLT custom entity exposes in a licensed BricsCAD runtime. Therefore these remain `PENDING_LOCAL` until probe evidence is produced from a real legacy file:

- the complete BLT category-code table for Beam/Slab/Foundation and other types;
- exact field names/storage locations for Floor/Family/material/dimensions;
- whether unloaded BLT entities expose exact top-level Solid3d geometry after transient explode;
- exact legacy formwork field/face semantics where not explicitly unit-labelled;
- parity of BT/VK against representative historical BLT output.

The correct behavior for an unqualified object is **blocked + diagnostic**, not a guessed quantity.

## 15. Definition of done for the product objective

The broader objective is complete when a representative legacy drawing can demonstrate this sequence without source redraw:

```text
open old BLT3D DWG
→ QS3DBLTSCAN sees legacy objects
→ QS3DBLTIMPORT maps supported objects
→ Column/Beam/Slab/Foundation/StructuralWall semantics are correct
→ BT is authoritative
→ VK is authoritative where evidence/rules qualify it
→ QS3DEXCEL exports expected rows
→ QS3DEXCELTRACE selects the original BLT source object
→ save/reopen does not duplicate semantic ownership
```

This lane intentionally treats real-DWG probe output as data needed to extend exact legacy mappings, not as permission to fabricate mappings in advance.

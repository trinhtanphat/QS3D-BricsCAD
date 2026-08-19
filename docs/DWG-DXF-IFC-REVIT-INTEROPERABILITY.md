# DWG / DXF / IFC / Revit interoperability architecture

**Issue:** #3102  
**Parent research:** #3098  
**Baseline:** `main@a323d25a9f5720eb99b2533d9605298366dd3735`  
**Date:** 2026-08-19  
**Scope:** architecture and clean-room interoperability research. This document does not itself claim that every mapped path is already a shipping QS3D feature.

## 1. Decision summary

QS3D should treat DWG, DXF, IFC and Revit as **different integration modes**, not as four equivalent file extensions behind one generic importer.

The recommended boundary is:

```text
                         source systems / files

 BricsCAD active DWG      external DWG/DXF       IFC/openBIM       Revit
         |                      |                    |                |
         | native host API      | host file I/O     | exchange       | IFC export
         |                      |                    |                | or external API bridge
         v                      v                    v                v
 +--------------------------------------------------------------------------+
 |                    source-specific adapter boundary                       |
 |                                                                          |
 | BricsCADNativeAdapter | DwgDxfExchangeAdapter | IfcExchangeAdapter       |
 |                                            | RevitExchangeBridge         |
 +-------------------------------------+------------------------------------+
                                       |
                                       v
                         normalized interoperability facts
          identity + provenance + units + geometry + properties +
               classification + quantities + loss diagnostics
                                       |
                                       v
                      existing QS3D semantic / quantity core
                                       |
                     +-----------------+------------------+
                     |                                    |
                     v                                    v
             measurement / reports                  4D / 5D links
```

The architectural rules are:

1. **DWG is native host state in this repository.** BricsCAD owns the native database, document lifecycle, transactions, handles, geometry and viewport. QS3D reads or writes that state through the V25/V26 host adapter.
2. **DXF is an exchange path, not a second semantic model.** When QS3D uses BricsCAD/Teigha DXF I/O, host-specific types remain outside `QS3D.Core`.
3. **IFC is the preferred open semantic interchange path.** Preserve IFC identity, schema/entity information, spatial context, properties, classifications, units and quantity evidence before mapping them into QS3D semantics.
4. **“Revit integration” must be qualified.** Safe supported architecture means either Revit -> IFC -> QS3D, or a separately built Revit API bridge/add-in that emits IFC or a QS3D-neutral interchange payload. It does **not** mean that this repository parses proprietary RVT files directly.
5. **Never conflate source identity with target ownership.** A DWG handle from another drawing, an IFC `GlobalId`, a Revit element identifier and a QS3D semantic ID are different identifiers with different scopes.
6. **Unknown units or lossy semantic mappings are first-class diagnostics.** Quantity calculation must not silently proceed from an unresolved unit basis or invented classification/property data.
7. **Declared source quantities and QS3D-derived quantities remain distinguishable.** A value imported from an IFC quantity set is evidence from the source model; a value recalculated by QS3D is a separate result with its own rule and provenance.

## 2. Product boundary and non-goals

This design preserves `docs/PRODUCT-BOUNDARY.md`:

- `QS3D-BricsCAD` remains a BricsCAD V25/V26 hosted plugin;
- BricsCAD remains the owner of the live/native DWG database and CAD viewport;
- `QS3D.Core` remains host-neutral;
- vendor-neutral contracts may migrate deliberately toward `QS3D-Platform`;
- a future standalone native CAD engine belongs to `QS3D-CAD`, not this repository.

This issue does **not** authorize or promise:

- a standalone DWG engine inside `QS3D.Core`;
- a home-grown DXF/DWG parser when the BricsCAD host already owns that responsibility;
- direct proprietary RVT parsing;
- copying Autodesk, Bricsys, BLT, Glodon or other proprietary implementation code/assets;
- semantic fidelity merely because a file can be opened or viewed;
- treating a BricsCAD RVT viewing export as a semantic Revit round-trip;
- claiming current product support for a future adapter that is only specified here.

## 3. Three integration modes

### 3.1 Native-host integration

Native-host integration means QS3D executes inside BricsCAD and uses the host-managed `Database`, `Document`, transactions, object IDs/handles, editor selection and geometry APIs.

This is the authoritative mode for the **active DWG**. A QS3D semantic element may refer back to native objects, but the native object remains owned by the active drawing and BricsCAD lifecycle.

For V25, the official BricsCAD .NET API exposes the managed host through `BrxMgd.dll` and `TD_Mgd.dll`. The `Teigha.DatabaseServices.Database` represents drawing state and provides DWG/DXF I/O methods.

Architecture consequence:

```text
BricsCAD / Teigha type
        |
        | V25/V26 adapter only
        v
host-neutral capture / semantic facts
        |
        v
QS3D.Core
```

No `Teigha.*`, `Bricscad.*`, Autodesk Revit API or other proprietary host type should leak into a vendor-neutral Core contract.

### 3.2 Exchange-file integration

Exchange-file integration starts from a persisted artifact such as `.dxf` or `.ifc` rather than the active drawing database.

The importer must treat the file as an **external source revision**. It should produce a preview and diagnostics before committing semantic state. Imported source IDs are provenance unless and until a deliberate mapping creates QS3D semantic elements.

Exchange-file import must capture at least:

- source path/URI or opaque source label;
- content fingerprint when available;
- format and schema/version;
- import batch/revision identity;
- coordinate/unit basis;
- original element identifiers;
- properties/classification/quantity evidence;
- warnings, unsupported constructs and lossy mappings.

### 3.3 External-authoring API integration

External-authoring integration means code runs in, or talks explicitly to, another authoring platform such as Revit using that platform's supported API.

For Revit, a future bridge can use the Revit API to:

- inspect source model objects/parameters;
- export IFC with controlled mappings; or
- emit a vendor-neutral QS3D interchange snapshot.

That bridge is a **separate runtime boundary**. It is not loaded into BricsCAD and must not cause Revit API assemblies/types to enter `QS3D.Core`.

The preferred bridge output is:

```text
Revit model
   |
   +--> IFC ----------------------------------+
   |                                          |
   +--> neutral QS3D interchange snapshot ----+--> QS3D import/normalization
```

Direct RVT file parsing is outside the support claim of this architecture unless a separately licensed, implemented and qualified adapter is approved later.

## 4. Interoperability matrix

| Source/path | Integration mode | Geometry authority | Identity to preserve | Semantic/property posture | Unit posture | Recommended QS3D boundary | Support wording |
|---|---|---|---|---|---|---|---|
| Active BricsCAD DWG | Native host | BricsCAD database | drawing fingerprint + native handle/object identity + QS3D semantic ID | Native properties plus QS3D semantic state | resolve native drawing unit through current unit policy | V25/V26 host adapter -> Core | **Native QS3D host path** |
| External DWG opened/read by BricsCAD | Host file I/O | BricsCAD/Teigha database reader | source drawing fingerprint + source-local handles | preserve source state as provenance; do not claim target ownership | resolve/validate source unit before measurement | host adapter -> normalized snapshot | **Host capability / future explicit QS3D workflow unless separately exposed** |
| DXF through BricsCAD `Database.DxfIn` | Exchange via host | BricsCAD/Teigha database reader | source file fingerprint + source-local entity identity where available | DXF is principally CAD exchange; semantic richness may be lower than IFC | unresolved source unit is blocking for quantity import | host adapter -> normalized snapshot | **Architecture-supported exchange path; shipping feature must be verified separately** |
| IFC | OpenBIM exchange | IFC representation + importer interpretation | `GlobalId` where present plus source file/schema/revision | preserve entity/predefined type, psets, classification, spatial context and quantity sets; emit loss diagnostics | honor project and property/quantity units explicitly | `IfcExchangeAdapter` -> normalized facts | **Preferred semantic exchange architecture** |
| Revit -> IFC -> QS3D | OpenBIM exchange | Revit export + IFC representation | IFC identity plus source/revision provenance | preserve export mapping context where available | IFC unit rules apply after export | Revit export configuration + IFC adapter | **Valid meaning of “Revit interoperability”** |
| Revit API bridge -> neutral snapshot | External-authoring API | Revit API extraction | Revit document/revision key + source element identity, then mapped QS3D ID | explicit parameter/category/classification mapping and diagnostics | bridge emits explicit units for every measured value | external bridge -> `ProjectInterchange*` style payload | **Valid future integration; separate runtime/tooling required** |
| Direct `.rvt` parsing inside this repo | Proprietary file parsing | undefined without approved technology | undefined | undefined | undefined | none | **Not promised / unsupported by this research** |
| BricsCAD export to RVT “for viewing only” | View/export artifact | BricsCAD export | not a QS3D semantic interchange contract | viewing fidelity does not establish semantic round-trip fidelity | not a QS3D quantity source contract | none for semantic interchange | **Do not market as Revit data integration** |

The matrix intentionally separates **host capability** from **current QS3D product capability**. Issue #3103 owns the current-feature/gap audit; this document defines the architecture and safe wording.

## 5. Normalized interoperability fact model

A future implementation should normalize source data into explicit facts before quantity/cost logic consumes it. The names below are architectural roles, not a requirement to add these exact CLR types.

### 5.1 Source provenance

Minimum fields:

```text
SourceProvenance
- SourceSystem              // BricsCAD, IFC, RevitBridge, etc.
- Transport                 // NativeHost, DWG, DXF, IFC, NeutralSnapshot
- SourceDocumentId          // stable source label where available
- SourceFingerprint         // file/model revision fingerprint where available
- SourceSchemaVersion       // IFC4, IFC4x3, DXF/DWG version, bridge schema, etc.
- ImportBatchId
- CapturedUtc
```

The source document/fingerprint scopes every source-local identity. A drawing handle without a drawing fingerprint is not a globally usable identity.

### 5.2 Element identity

Keep identifiers in separate slots:

```text
ElementIdentity
- Qs3dElementId
- SourceSystem
- SourceDocumentId / fingerprint
- SourceElementId
- DwgHandle?                // only meaningful with source drawing scope
- IfcGlobalId?              // IFC identity when present
- ExternalAuthoringId?      // e.g. Revit bridge identity
```

Rules:

- never copy an imported source DWG handle into target-native ownership;
- never use an IFC `GlobalId` as a replacement for QS3D semantic identity without an explicit mapping;
- never assume an external authoring element ID has the same stability or scope as IFC `GlobalId`;
- keep source identity stable across a single import/revision comparison even when the target semantic ID differs.

### 5.3 Geometry facts

A geometry fact should record:

- source representation kind;
- source coordinate system and transform to project coordinates;
- source length unit;
- geometry type/topology sufficient for the downstream rule;
- tolerance/precision assumptions;
- whether geometry is native, imported, tessellated, proxy or degraded;
- any unsupported representation warning.

Quantity code must be able to distinguish an exact/native solid from an approximated/tessellated representation when that affects measurement trust.

### 5.4 Property facts

Do not flatten every property into an anonymous string dictionary at the adapter boundary. Preserve context:

```text
PropertyFact
- Namespace / source schema
- SetName
- Name
- PrimitiveType
- Value
- Unit?                     // when property is measured
- SourceElementId
```

For IFC, the property-set name and property name are both meaningful. Custom property sets must remain distinguishable from standardized property sets.

### 5.5 Classification references

Preserve classification as references rather than inventing a single universal code:

```text
ClassificationRef
- System / source
- Edition / version? 
- Code
- Name?
- SourceRelationship / provenance
```

A missing classification is **missing**, not permission to guess one from geometry. Recognition or suggested mapping can be a later, separately audited layer.

### 5.6 Quantity facts

Keep source-provided and derived quantities distinct:

```text
QuantityFact
- Name / measure kind
- Value
- Unit
- Origin                    // DeclaredSource | DerivedQS3D
- SourceSet / source path
- MethodOfMeasurement?
- CalculationRuleId?        // QS3D-derived values
- SourceElementId / Qs3dElementId mapping
```

For IFC quantity sets, preserve `IfcElementQuantity` context and `MethodOfMeasurement` when present. For a QS3D-derived quantity, preserve the QS3D rule/trace instead. If the two disagree, report a delta rather than silently choosing one.

### 5.7 Loss diagnostics

Every adapter should produce structured diagnostics such as:

```text
LossDiagnostic
- Code
- Severity                  // Info | Warning | Blocking
- SourceElementId?
- SourcePath / field?
- Message
- FallbackApplied?          // explicit, never hidden
```

Blocking examples:

- unresolved length unit for geometry that will be measured;
- duplicate/conflicting source identity inside one import scope;
- invalid/non-finite numeric quantity;
- transform/coordinate basis unavailable when required for spatial measurement;
- quantity supplied without a resolvable unit;
- malformed source payload that prevents trustworthy provenance.

Warning examples:

- unsupported IFC entity mapped to a generic proxy;
- property set omitted or type degraded;
- classification unavailable;
- geometry converted to tessellation;
- source-declared quantity retained but not independently reproducible by QS3D.

## 6. DWG architecture

### 6.1 Active drawing

The active BricsCAD drawing is the native authority. QS3D host code should read/write through ordinary BricsCAD transactions and current source-handle/semantic ownership rules.

Do not serialize a host `ObjectId` as a portable cross-drawing identity. A native handle is drawing-local and must remain scoped by drawing identity/fingerprint.

### 6.2 External DWG

The BricsCAD V25 managed API exposes `Database.ReadDwgFile(...)` for reading an external drawing into a `Database`. Official API documentation warns that this method should be used with a newly created database whose `buildDefaultDrawing` constructor argument is `false`; inappropriate use can cause memory leaks/fatal errors.

Therefore a future external-DWG inspection adapter should:

1. live in the BricsCAD host project, not Core;
2. use a disposable, newly-created database with the documented host contract;
3. record file fingerprint/version/codepage diagnostics;
4. extract a host-neutral snapshot;
5. dispose/close the external database cleanly;
6. never promote source handles into target-native ownership.

Codepage conversion is also an interoperability concern. If the host reports or requires conversion, record it because text/layer/property fidelity can be affected.

## 7. DXF architecture

The BricsCAD V25 managed API exposes `Database.DxfIn(fileName, logFilename)` to read a DXF file into a database. Its documentation carries the same newly-created-database constraint.

Recommended DXF flow:

```text
DXF file
  -> BricsCAD host DxfIn into isolated Database
  -> inspect unit/header/entity state
  -> normalize geometry + source identity + text/layer metadata
  -> diagnostics/preview
  -> semantic mapping only after unit and identity policy passes
```

DXF should not be treated as equivalent to IFC. It is excellent for drawing geometry exchange, but BIM semantics, standardized property sets, classifications and quantity-set evidence may be absent or encoded in application-specific ways.

### DXF quantity gate

If source drawing units cannot be resolved safely:

- preview geometry may still be allowed where useful;
- **measurement/quantity import must remain blocked** until an explicit project/source-unit mapping is supplied;
- the chosen override and resulting quantity binding must be persisted/audited.

This matches the existing `DrawingUnitResolutionPolicy`, which resolves from native units or an explicit project override and rejects changes that conflict with quantities already bound to another unit.

## 8. IFC/openBIM architecture

IFC is the strongest format in this group for open semantic exchange and should be treated as a first-class **evidence-preserving adapter**, not only a geometry import.

### 8.1 Identity

IFC objects derived from `IfcRoot` can carry `GlobalId`, defined by the IFC specification as a globally unique identifier. QS3D should preserve that value exactly when present.

Preservation does not imply ownership equivalence:

```text
IFC GlobalId != QS3D semantic ID != DWG handle
```

The mapping between them is explicit and revision-scoped.

### 8.2 Properties

Preserve:

- IFC entity type and predefined type where available;
- property-set name;
- property name/type/value/unit;
- custom property sets;
- material/association references required by quantity rules;
- spatial containment and relevant relationships;
- source schema/version.

Do not collapse IFC2x3/IFC4 distinctions or importer mapping decisions into an unqualified “IFC property” string if those distinctions matter for traceability.

### 8.3 Quantities

buildingSMART IFC quantity sets use `IfcElementQuantity`; quantity occurrences cover count, length, area, volume, weight, time and related combinations. `MethodOfMeasurement` records how the values were calculated, and predefined quantity templates use the specified base-quantity convention.

QS3D should preserve source quantity evidence separately from QS3D measurement traces:

```text
IFC quantity evidence
       |                         QS3D geometry/rule
       v                               |
source quantity snapshot               v
       |                         derived quantity
       +---------------+---------------+
                       v
                 comparison/delta
```

A source quantity can be accepted as imported evidence without falsely claiming QS3D independently reproduced it.

### 8.4 BricsCAD IFC host capability

Official BricsCAD V25 BIM documentation states support for IFC2x3 and IFC4 import/export, with import coverage also described for IFC4x1 and IFC4x3 and custom property sets made available after import. IFC4x3 behavior is marked experimental in the documented V25 path.

This is **host capability evidence**. It does not automatically prove that every QS3D workflow currently captures every imported IFC semantic field. A QS3D IFC adapter still needs explicit extraction/mapping tests.

### 8.5 IFC adapter placement

Long-term preferred split:

```text
IFC parser / openBIM mapping
          |
          v
vendor-neutral IFC snapshot contract
          |
          v
QS3D-Platform / Core semantic normalization
          |
          v
QS3D-BricsCAD host presentation / linking
```

Until Platform migration reaches the relevant slice, `QS3D.Core` can remain the current contract home, but host-specific BricsCAD IFC objects must stay behind the V25/V26 adapter.

## 9. Revit interoperability architecture

### 9.1 Safe support meanings

The phrase **Revit integration** is acceptable only when the transport is named. Recommended support vocabulary:

- **“Revit via IFC exchange”** — Revit exports IFC; QS3D consumes IFC.
- **“Revit API bridge”** — a separately shipped/qualified Revit-side component uses Autodesk's supported API and emits IFC or a QS3D-neutral exchange snapshot.
- **“Direct RVT import”** — do not use this wording unless a distinct approved implementation really parses/reads RVT with licensed/supported technology and has qualification evidence.

### 9.2 Revit -> IFC

Autodesk's current Revit documentation provides IFC export and export mapping configuration. The Revit API exposes `Document.Export(..., IFCExportOptions)` and supports custom IFC exporter behavior/property mapping.

This makes IFC the cleanest current cross-product bridge:

```text
Revit
 -> configured IFC export
 -> IFC file + source/revision metadata
 -> QS3D IFC adapter
 -> normalized semantic/quantity evidence
```

The export configuration is part of provenance because category/property mapping choices can change the resulting IFC semantics.

### 9.3 Revit API bridge

Autodesk also exposes IFC-related API extension points, including custom export and an external IFC importer server interface. A future QS3D Revit bridge may use supported Revit API access to create a neutral payload.

That bridge should include:

- source Revit document identity/revision fingerprint;
- source element identity;
- category/type/family where relevant;
- parameter name/type/value/unit;
- level/spatial references;
- geometry evidence only to the precision required by the downstream contract;
- explicit mapping diagnostics;
- bridge schema version.

The bridge output must remain vendor-neutral at the QS3D boundary. Revit API objects must not cross into `QS3D.Core` or the BricsCAD plugin runtime.

### 9.4 RVT wording guard

BricsCAD V25 documentation mentions export to RVT **for viewing only**. That statement must never be repurposed into claims such as:

- QS3D semantically round-trips RVT;
- QS3D directly imports native Revit project data;
- RVT viewing export preserves all Revit parameters/identity/relationships/quantities.

Those claims require independent implementation and evidence.

## 10. Current QS3D seams to reuse

Current `main` already contains useful contracts that future adapters should extend rather than duplicate.

### `IfcRoundTripProjection`

`src/QS3D.Core/Export/IfcRoundTripProjection.cs` already models:

- `Qs3dElementId`;
- `IfcGlobalId`;
- semantic classification;
- numeric dimensions with explicit units;
- primary quantity and unit;
- provenance tokens;
- quantity evidence;
- uniqueness checks for IFC and QS3D identities.

This is strong evidence that IFC identity + quantity + provenance are already treated as separate, auditable concepts.

### `ProjectInterchangeSourceHandleProvenance`

`src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs` explicitly stores imported drawing-local handles as **provenance only** and keeps them outside target `ProjectElement.SourceHandles` / generated ownership slots. It also requires a source drawing fingerprint when drawing-local handles are present.

That policy should remain the canonical rule for external DWG/DXF handle provenance.

### `DrawingUnitResolutionPolicy`

`src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`:

- prefers native drawing units when available;
- supports explicit project override;
- binds quantities to an effective unit;
- rejects changing to a unit inconsistent with already-bound quantities;
- fails when legacy quantities have no trustworthy unit binding.

External adapter work should feed this policy, not bypass it.

### Existing interchange infrastructure

Current Core also contains `ProjectInterchange*` validation/import/merge/provenance components, snapshot diffing, semantic-reference validation, measurement traces, revision services and reporting provenance. A future DWG/DXF/IFC/Revit adapter should normalize into these existing concepts where they fit instead of creating an unrelated import subsystem.

## 11. Recommended adapter decomposition

Names below are conceptual; implementation lanes may choose final names after checking existing conventions.

### V25/V26 host layer

```text
BricsCadNativeSourceAdapter
- capture active drawing objects through host transaction/API
- preserve drawing fingerprint + local handles
- resolve drawing units
- emit host-neutral snapshots

BricsCadDwgDxfExchangeAdapter
- isolated external Database lifecycle
- ReadDwgFile / DxfIn through supported host API
- extract host-neutral source snapshot
- preserve codepage/unit/import diagnostics
```

These adapters must be implemented separately for V25/V26 where managed-host API differences require it, while sharing a host-neutral output contract.

### Vendor-neutral layer

```text
InteroperabilitySnapshot
InteroperabilityElement
InteroperabilityProperty
InteroperabilityQuantityEvidence
InteroperabilityClassification
InteroperabilityDiagnostic
InteroperabilityRevisionIdentity
```

Prefer extending/reusing existing `ProjectInterchange*`, IFC round-trip, measurement and provenance contracts instead of introducing these exact parallel names if existing types already cover the requirement.

### IFC layer

```text
IfcExchangeAdapter
- schema/version detection
- identity/property/quantity/classification extraction
- unit conversion with original-unit preservation
- unsupported-entity diagnostics
- source-vs-derived quantity comparison
```

The parser implementation should remain replaceable. Semantic/quantity code should depend on the normalized contract, not a specific IFC library's object graph.

### Revit-side boundary

```text
RevitExchangeBridge   // separate Revit runtime/tool, not QS3D-BricsCAD Core
- reads via supported Revit API
- exports configured IFC and/or neutral snapshot
- emits explicit provenance + mapping diagnostics
```

## 12. Revision and update model

Interoperability is not complete with “import once.” The revision model should compare source snapshots before mutating target semantic state.

Recommended flow:

```text
source revision N
      |
      v
normalized snapshot N
      |
      +------ compare ------ normalized snapshot N+1
                              |
                              v
                     added / removed / changed
                              |
                              v
                   preview + mapping conflicts
                              |
                              v
                     deliberate apply/update
```

Rules:

- compare using source-scoped identity first;
- never infer “same element” from geometry proximity alone without an explicit reconciliation policy;
- preserve deleted/changed source evidence long enough to explain quantity deltas;
- recompute QS3D-derived quantities after semantic/geometry changes;
- retain source-declared quantity changes as separate revision evidence;
- historical reports/claims should continue to point at the source and calculation revisions used to create them.

## 13. Unit policy

### 13.1 General rule

Normalize units at a clearly defined boundary, but preserve the original value/unit for audit.

```text
source value + source unit
          |
          v
validated conversion
          |
          +--> original evidence retained
          |
          v
QS3D project/canonical value
```

### 13.2 DWG/DXF

Use native drawing unit metadata when trustworthy. If unavailable, require an explicit project/source override before quantities are measured or imported as geometry-derived results.

Do not treat “looks like millimetres” or typical project scale as evidence.

### 13.3 IFC

Do not assume one global length unit is sufficient for every property/quantity. Preserve units attached to IFC measured values and the project unit context used by the source model.

### 13.4 Revit bridge

The bridge must emit explicit units after using the Revit API's supported unit metadata/conversion facilities. A naked numeric parameter without type/unit information should not be treated as a measured quantity.

## 14. Identity and ownership policy

| Identifier | Scope | Can become QS3D semantic ID automatically? | Can claim target DWG native ownership? |
|---|---|---:|---:|
| QS3D semantic ID | QS3D project | already authoritative | no, ownership mapping is separate |
| Active target DWG handle | one drawing/database | no | only through normal native ownership/link policy |
| Imported DWG/DXF handle | source drawing | no | **never directly**; provenance only |
| IFC `GlobalId` | IFC/openBIM object identity | explicit mapping required | no |
| Revit bridge element ID | source Revit document/session semantics | explicit mapping required | no |

This table is the central defense against destructive cross-file linking.

## 15. Property/classification loss policy

An adapter must classify every important mapped field as one of:

- **Preserved** — source meaning and unit/type retained;
- **Normalized** — converted into an equivalent QS3D concept with source provenance retained;
- **Degraded** — useful value retained but some source semantics lost;
- **Unsupported** — not imported; diagnostic emitted;
- **Unknown** — source meaning cannot be established safely.

Do not silently convert **Unsupported** or **Unknown** into a guessed QS3D classification.

For quantity-critical fields, degraded/unsupported status should be visible in model health/review surfaces before the result is eligible for trusted reporting.

## 16. Security and robustness boundary

All external files and interchange payloads are untrusted input.

Adapters should apply:

- bounded collection sizes and recursion/depth limits;
- finite-number validation;
- canonical string/identifier validation;
- duplicate identity detection;
- path/URI safety;
- no execution of embedded macros/scripts/content;
- isolated temporary/external database lifecycle;
- explicit parser warnings/errors;
- rollback/transaction boundaries for apply operations;
- preview before destructive remap/merge;
- deterministic canonical ordering where practical for reproducible diffs.

Current IFC round-trip and ProjectInterchange contracts already demonstrate several of these fail-closed patterns and should remain the model.

## 17. Architecture acceptance tests for a future implementation

A future implementation lane should add fixtures/tests for at least:

### Identity

- two source drawings with identical local handles do not collide;
- duplicate IFC `GlobalId` inside one normalized scope is rejected or diagnosed;
- target native ownership is never created from imported source handles;
- source-to-QS3D mapping remains reproducible across a source revision.

### Units

- millimetre DWG/DXF -> project metre conversion is explicit and reproducible;
- unresolved source unit blocks quantity measurement;
- unit override cannot mutate already-bound historical quantities silently;
- IFC quantities retain their declared units and method evidence.

### Properties/classification

- IFC custom property sets survive normalization with set/name/value/type/unit context;
- unsupported properties produce diagnostics, not silent drops;
- missing classifications stay missing unless an explicit mapping is selected.

### Revision

- add/remove/change source elements produce deterministic deltas;
- source-declared and QS3D-derived quantities can be compared independently;
- a source revision does not rewrite historical report provenance.

### Host boundary

- Core tests contain no BricsCAD/Revit proprietary runtime types;
- external DWG/DXF host database lifecycle follows documented isolated-database rules;
- V25/V26 adapter differences do not fork the vendor-neutral semantics.

## 18. Safe product wording

Use wording like:

- “QS3D runs natively inside BricsCAD and works with the active DWG through the BricsCAD API.”
- “IFC/openBIM interoperability is designed around explicit identity, properties, quantities, units and provenance.”
- “Revit interoperability can be provided through IFC exchange or a separately implemented Revit API bridge.”
- “Imported source identifiers remain provenance until explicitly mapped.”

Avoid wording like:

- “QS3D natively parses every DWG/DXF/IFC/RVT file.”
- “Revit import” without naming IFC or a specific implemented API bridge.
- “RVT round-trip” based solely on BricsCAD's viewing export.
- “IFC quantity = QS3D verified quantity” when the value was only imported as source evidence.
- “same object” merely because IDs or geometry appear similar across unrelated source documents.

## 19. Recommended implementation sequence

This research issue does not pre-authorize these source changes. Suggested follow-up order:

### P0 — normalized provenance / loss diagnostics

- formalize source document + source element identity mapping;
- reuse ProjectInterchange provenance rules;
- expose mapping/loss diagnostics in preview/health surfaces.

### P1 — IFC evidence adapter

- parse/capture IFC identities, psets, classifications, units and quantity sets;
- project into existing IFC round-trip / interchange evidence;
- compare imported quantity evidence with QS3D-derived quantities.

### P2 — host DWG/DXF exchange adapter

- isolated external database lifecycle;
- host-neutral snapshot extraction;
- unit/codepage/source-fingerprint diagnostics.

### P3 — Revit exchange bridge

- separate Revit API runtime/tool;
- configured IFC export and/or neutral snapshot;
- explicit bridge schema/version and provenance;
- no Revit API dependency in QS3D Core.

### P4 — revision/reconciliation UX

- source revision diff;
- mapping conflict preview;
- explicit remap/keep/append policies;
- quantity/cost impact review.

## 20. Primary references

### Repository truth

- `docs/PRODUCT-BOUNDARY.md`
- `docs/BLT3D-BIM5D-BENCHMARK.md`
- `src/QS3D.Core/Export/IfcRoundTripProjection.cs`
- `src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs`
- `src/QS3D.Core/Export/ProjectInterchangeSourceHandleProvenance.cs`
- `src/QS3D.Core/Units/DrawingUnitResolutionPolicy.cs`
- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- `src/QS3D.Core/Measurement/MeasurementTrace.cs`

### Bricsys / BricsCAD primary documentation

- BricsCAD V25 .NET API: <https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/dotNETAPI.htm>
- `Database` methods (`ReadDwgFile`, `DxfIn`, etc.): <https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/html/e81e44c5-14be-00a9-bfd5-57cdb6d4db4b.htm>
- `Database.ReadDwgFile`: <https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/html/4b6f4396-2669-c3bc-f809-3cfa5f52b9fc.htm>
- `Database.DxfIn`: <https://developer.bricsys.com/bricscad/help/en_US/V25/DevRef/source/html/06480e65-cd56-f616-a2ba-a6e99a3e7286.htm>
- IFC import/export in BricsCAD V25: <https://help.bricsys.com/en-us/document/bricscad-bim/project-collaboration/ifc-import-and-export-in-bricscad?version=V25>
- `IFCEXPORT` command V25: <https://help.bricsys.com/en-us/document/command-reference/i/ifcexport-command?version=V25>

### buildingSMART primary documentation

- IFC 4.3.2 quantity sets: <https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/concepts/Object_Definition/Quantity_Sets/content.html>
- IFC 4.3.2 `IfcPropertySet`: <https://ifc43-docs.standards.buildingsmart.org/IFC/RELEASE/IFC4x3/HTML/lexical/IfcPropertySet.htm>
- buildingSMART technical standards: <https://technical.buildingsmart.org/>

### Autodesk / Revit primary documentation

- Revit 2026 IFC export developer guide: <https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API/files/Revit_API_Developers_Guide/Advanced_Topics/Export/Revit_API_Revit_API_Developers_Guide_Advanced_Topics_Export_IFC_Export_html.html>
- `Document.Export(..., IFCExportOptions)`: <https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/7efa4eb3-8d94-b8e7-f608-3dbae751331d.htm>
- `IIFCImporterServer`: <https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/9fff7078-273b-363b-04f5-b4adcd4a5590.htm>
- Revit IFC documentation: <https://help.autodesk.com/cloudhelp/2026/ENU/Revit-DocumentPresent/files/GUID-6EB68CEC-6C17-4B16-A509-30537F666C1F.htm>

## 21. #3102 completion criteria mapping

| Issue criterion | Evidence in this document |
|---|---|
| IFC claims grounded in buildingSMART documentation | Sections 8, 20 |
| Native-host vs exchange-file vs external-API clearly separated | Sections 3, 4 |
| Unit risks documented | Sections 5, 7, 13, 17 |
| Identity risks documented | Sections 5, 14, 17 |
| Property/classification loss documented | Sections 5, 8, 15 |
| Quantity provenance/method requirements documented | Sections 5, 8, 17 |
| No unsupported direct proprietary-format promise | Sections 2, 4, 9, 18 |
| Recommended QS3D adapter boundaries | Sections 1, 10, 11, 19 |

Research/architecture scope is complete when this exact documentation candidate passes repository branch and protected PR validation and lands through the authorized PR path.
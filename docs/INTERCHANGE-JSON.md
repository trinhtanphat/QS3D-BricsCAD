# QS3D Semantic Snapshot JSON

`QS3DINTERCHANGEJSON` exports a **read-only semantic interchange snapshot** from the active QS3D project.

`QS3DINTERCHANGEVALIDATE` opens an existing snapshot and performs **read-only structural/semantic validation**. A validation PASS means the file is structurally consistent with the supported v1 snapshot contract for review; it does **not** import, merge, restore or mutate the active QS3D project/DWG.

This is intentionally not a replacement for `.qsdb`, not a DWG backup and not a two-way import contract. The first interoperability goal is to let reporting, estimating, QA and integration systems consume stable QS3D semantic data without depending on BricsCAD-native object handles.

## Format contract

- format: `QS3D.SemanticSnapshot`
- format version: `1`
- deterministic collection ordering by stable IDs/keys
- UTF-8 without BOM
- numeric values use invariant culture
- length: metres (`m`)
- area: square metres (`m2`)
- volume: cubic metres (`m3`)
- mass: kilograms (`kg`)

The snapshot includes:

- project stable ID, name, schema version, drawing fingerprint and project update timestamp;
- Zones and Floors/Levels with stable IDs;
- Families/Types with stable IDs, categories and semantic properties;
- semantic elements with stable IDs, category, Family/Floor/Zone references;
- semantic dependencies;
- source provenance through drawing fingerprint and source DWG handles;
- semantic properties and calculated quantities.

`sourceHandles` are explicitly marked with `sourceRefScope = drawing-local`. Consumers must not treat a DWG handle as a cross-document stable element ID. The QS3D semantic element `id` is the primary interchange identity.

## Ownership and safety boundary

Generated native CAD output is implementation/runtime state, not interchange identity. Export therefore excludes:

- canonical generated owner slots such as `GeneratedSolidHandle` / generated rebar/mesh/frame handle collections;
- `PhysicalOpeningCut*` generated ownership state;
- `Generated*` and `QS3D.Generated*` runtime/stale/fingerprint properties.

The validator also rejects these generated/native ownership fields if an externally modified snapshot tries to put them back into Family/element semantic properties. A semantic snapshot is never allowed to become native CAD ownership authority merely because it passes JSON parsing.

This prevents a downstream system from persisting a native BricsCAD handle and later presenting it as portable QS3D ownership.

The exporter never mutates the project. The BricsCAD export command preserves that read-only guarantee end-to-end:

1. the Save dialog is shown before the active project is obtained, so Cancel performs no semantic work;
2. the live `ProjectState` is deep-copied with `ProjectStateSnapshot.CreateDetachedCopy`;
3. dirty semantic quantities are regenerated only on that detached copy;
4. JSON is built from the detached copy and committed through the existing temporary-file/replace boundary;
5. the live project is never restored or replaced, so modeless UI references remain attached to the original live state.

A failed or cancelled export therefore does not clear dirty flags, change quantities/properties, advance semantic timestamps, or replace live project object references.

## Commands

```text
QS3DINTERCHANGEJSON
QS3DINTERCHANGEVALIDATE
```

Default export extension:

```text
*.qs3d.json
```

### Read-only validator contract

`ProjectInterchangeJsonValidator` parses the supported v1 semantic snapshot without constructing or replacing a live `ProjectState`. The adapter command does not call `ProjectContextCoordinator.GetOrCreate` and does not touch DWG entities.

The validator currently checks, fail-closed where applicable:

- exact `QS3D.SemanticSnapshot` format name and supported `formatVersion = 1`;
- strict UTF-8 decoding for file input; invalid byte sequences produce `JSON_UTF8` instead of replacement-character decoding;
- exact SI unit declarations `m`, `m2`, `m3`, `kg`;
- guarded file/object-graph/collection limits;
- required top-level `zones`, `floors`, `families` and `elements` collections, even when legitimately empty;
- required project/catalog names and stable IDs;
- required Family `properties` and element `sourceHandles` / `dependencies` / `properties` / `quantities` containers, even when empty;
- project identity/schema/timestamps;
- case-insensitive uniqueness of Zone/Floor/Family/element IDs;
- valid `ElementCategory` names;
- Family references and Family/category consistency;
- Floor/Zone references;
- `sourceRefScope = drawing-local` and duplicate/oversized source-handle entries;
- dependency existence, duplicate/self dependencies and dependency cycles using an iterative graph check rather than recursive traversal;
- bounded semantic property/quantity keys and values;
- finite numeric quantities/elevations;
- rejection of generated/native ownership runtime fields such as `Generated*`, `QS3D.Generated*` and `PhysicalOpeningCut*`.

Warnings such as missing/non-UTC provenance timestamps can be reported without turning a structurally usable review snapshot into an import contract. The command prints only a bounded number of issues to the BricsCAD editor and always labels the result `READ-ONLY / NOT IMPORTED`.

## Validation

Source/static contracts:

```text
python scripts/preflight-interchange-json.py
python scripts/preflight-interchange-validation.py
```

The export preflight explicitly rejects regressions where the command calls `RegenerateDirty(project)` or exports the live `project`, and it requires the Save dialog to occur before `ProjectContextCoordinator.GetOrCreate(document)`. Core smoke coverage also proves the detached copy does not share mutable project/element instances with the live project.

The validation preflight guards that `QS3DINTERCHANGEVALIDATE` remains read-only, that the validator stays bound to the exporter format/version and SI/provenance/ownership rules, that strict UTF-8 and required v1 structure remain fail-closed, and that validator smoke coverage remains registered.

Before commercial release, also run the normal Core build/preflight and exact-SHA V25 qualification. Export/validation logic is CAD-kernel-independent after file/project acquisition, but command registration, dialog behavior, Unicode paths and real customer snapshots still need the local V25 matrix.

## Intentionally not claimed yet

Version 1 still does **not** claim:

- JSON re-import/round-trip;
- snapshot merge into the current project;
- ID collision resolution;
- current-DWG source-handle rebinding;
- ownership reconstruction;
- schema/version migration beyond the exact supported v1 validation boundary;
- IFC import/export;
- Revit exchange;
- BCF;
- vendor-specific APIs;
- cloud/team synchronization.

Any future importer must define collision handling, unit validation, project/drawing identity, ownership reconstruction, provenance, schema/version migration and rollback before it can mutate a project. Do not deserialize a semantic snapshot directly into live generated CAD ownership, and do not treat `QS3DINTERCHANGEVALIDATE PASS` as permission to import it.

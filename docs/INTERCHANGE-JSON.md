# QS3D Semantic Snapshot JSON

`QS3DINTERCHANGEJSON` exports a **read-only semantic interchange snapshot** from the active QS3D project.

`QS3DINTERCHANGEVALIDATE` opens an existing snapshot and performs **read-only structural/semantic validation**. A validation PASS means the file is structurally consistent with the supported v1 snapshot contract for review; it does **not** by itself import, merge, restore or mutate the active QS3D project/DWG.

A deliberately narrow mutating path now exists through `QS3DINTERCHANGEAPPEND`. It accepts only the guarded append-all-new contract documented in [`INTERCHANGE-APPEND-ONLY-IMPORT.md`](INTERCHANGE-APPEND-ONLY-IMPORT.md); validation PASS alone is never permission to mutate a project.

This is intentionally not a replacement for `.qsdb`, not a DWG backup and not a general two-way round-trip contract. The interoperability goal is to let reporting, estimating, QA and integration systems consume stable QS3D semantic data without depending on BricsCAD-native object handles, while keeping any supported mutation path behind a separate fail-closed policy boundary.

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

The append-only importer has a separate mutation boundary. It validates and plans without mutation, requires all incoming IDs/names to be new under its documented collision rules, repeats preflight immediately before apply, discards source CAD ownership/handles, marks imported elements dirty, and restores a `ProjectStateSnapshot` if apply/final validation throws. It does not create, erase or replace native BricsCAD entities.

## Commands

```text
QS3DINTERCHANGEJSON
QS3DINTERCHANGEVALIDATE
QS3DINTERCHANGEAPPEND
```

Default export extension:

```text
*.qs3d.json
```

### Read-only validator contract

`ProjectInterchangeJsonValidator` parses the supported v1 semantic snapshot without constructing or replacing a live `ProjectState`. The validator command does not call `ProjectContextCoordinator.GetOrCreate` and does not touch DWG entities.

The validator currently checks, fail-closed where applicable:

- exact `QS3D.SemanticSnapshot` format name and supported `formatVersion = 1`;
- strict UTF-8 decoding for file input; invalid byte sequences produce `JSON_UTF8` instead of replacement-character decoding;
- exact SI unit declarations `m`, `m2`, `m3`, `kg`;
- guarded file/object-graph/collection limits;
- required top-level `zones`, `floors`, `families` and `elements` collections, even when legitimately empty;
- required canonical project/catalog names with no leading/trailing whitespace, plus stable IDs;
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

Warnings such as missing/non-UTC provenance timestamps can be reported without turning a structurally usable review snapshot into mutation authority. The validation command prints only a bounded number of issues to the BricsCAD editor and always labels its own result `READ-ONLY / NOT IMPORTED`.

### Guarded append-only mutation contract

`QS3DINTERCHANGEAPPEND` is intentionally narrower than a generic importer. It uses the canonical validated snapshot reader plus the append-only planner/importer contract. Before mutation it rejects ID/name collisions according to the target-authority rules, presents a read-only plan, and repeats preflight after confirmation so stale intent cannot be applied if the target changed while the dialog was open.

Source drawing handles are provenance from another DWG, not target ownership. They are discarded rather than rebound. Existing target project identity/context remains authoritative, imported elements are marked dirty, and no native CAD entities are reconstructed by the append operation.

For the full supported mutation boundary, atomicity rules and remaining limitations, read [`INTERCHANGE-APPEND-ONLY-IMPORT.md`](INTERCHANGE-APPEND-ONLY-IMPORT.md).

## Validation

Source/static contracts:

```text
python scripts/preflight-interchange-json.py
python scripts/preflight-interchange-validation.py
python scripts/preflight-interchange-append-only-import.py
```

The export preflight explicitly rejects regressions where the command calls `RegenerateDirty(project)` or exports the live `project`, and it requires the Save dialog to occur before `ProjectContextCoordinator.GetOrCreate(document)`. Core smoke coverage also proves the detached copy does not share mutable project/element instances with the live project.

The validation preflight guards that `QS3DINTERCHANGEVALIDATE` remains read-only, that the validator stays bound to the exporter format/version and SI/provenance/ownership rules, that strict UTF-8 and required v1 structure remain fail-closed, and that validator smoke coverage remains registered.

The append-only preflight/smoke contract guards the separate Plan/import boundary, all-new collision rules, source-handle/ownership discard, repeated preflight and rollback behavior. Do not infer those guards passed for a commit unless they were actually executed on that exact source SHA.

Before commercial release, also run the normal Core build/preflight and exact-SHA V25 qualification. Export/validation/import planning logic is largely CAD-kernel-independent after file/project acquisition, but command registration, dialogs, Unicode paths, save/reopen behavior, multi-DWG behavior and real customer snapshots still need the local V25 matrix.

## Intentionally not claimed yet

Version 1 still does **not** claim a generic JSON round-trip importer. The only mutating snapshot path currently documented here is the conservative `QS3DINTERCHANGEAPPEND` all-new append contract.

Still not claimed:

- merge/replace of existing snapshot identities in the current project;
- automatic skip/rename/remap collision execution;
- current-DWG source-handle rebinding;
- generated/native ownership reconstruction;
- native CAD geometry creation/replacement from imported semantic objects;
- schema/version migration beyond the exact supported v1 validation boundary;
- generic import undo/session/save-reopen qualification;
- IFC import/export;
- Revit exchange;
- BCF;
- vendor-specific APIs;
- cloud/team synchronization.

Any broader importer or merge/replace path must define collision execution, unit validation, project/drawing identity, ownership clearing/reconstruction, provenance, schema/version migration and rollback before it can mutate existing project state. Do not deserialize a semantic snapshot directly into live generated CAD ownership, and do not treat `QS3DINTERCHANGEVALIDATE PASS` as permission to import it.
# QS3D Semantic Snapshot JSON

`QS3DINTERCHANGEJSON` exports a **read-only semantic interchange snapshot** from the active QS3D project.

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

This prevents a downstream system from persisting a native BricsCAD handle and later presenting it as portable QS3D ownership.

The exporter never mutates the project. The BricsCAD command now preserves that read-only guarantee end-to-end:

1. the Save dialog is shown before the active project is obtained, so Cancel performs no semantic work;
2. the live `ProjectState` is deep-copied with `ProjectStateSnapshot.CreateDetachedCopy`;
3. dirty semantic quantities are regenerated only on that detached copy;
4. JSON is built from the detached copy and committed through the existing temporary-file/replace boundary;
5. the live project is never restored or replaced, so modeless UI references remain attached to the original live state.

A failed or cancelled export therefore does not clear dirty flags, change quantities/properties, advance semantic timestamps, or replace live project object references.

## Command

```text
QS3DINTERCHANGEJSON
```

Default extension:

```text
*.qs3d.json
```

## Validation

Source/static contract:

```text
python scripts/preflight-interchange-json.py
```

The preflight explicitly rejects regressions where the command calls `RegenerateDirty(project)` or exports the live `project`, and it requires the Save dialog to occur before `ProjectContextCoordinator.GetOrCreate(document)`. Core smoke coverage also proves the detached copy does not share mutable project/element instances with the live project.

Before commercial release, also run the normal Core build/preflight and exact-SHA V25 qualification. Export itself is CAD-kernel-independent after the active project is obtained, but command registration, dialog behavior, Unicode paths and real customer project contents still need the local V25 matrix.

## Intentionally not claimed yet

Version 1 does **not** claim:

- JSON re-import/round-trip;
- IFC import/export;
- Revit exchange;
- BCF;
- vendor-specific APIs;
- cloud/team synchronization.

Any future importer must define collision handling, unit validation, project/drawing identity, ownership reconstruction, provenance, schema/version migration and rollback before it can mutate a project. Do not deserialize a semantic snapshot directly into live generated CAD ownership.

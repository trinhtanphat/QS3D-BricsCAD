# Interchange Append-Only Import

Status: **source-implemented; BricsCAD V25 runtime qualification still required**.

This is the first deliberately narrow mutating path for `QS3D.SemanticSnapshot` v1. It does not turn the broader import-resolution planner into a generic merge engine.

## Command

`QS3DINTERCHANGEAPPEND`

The adapter validates the file with the guarded interchange validator, builds a read-only collision preview, requires explicit Yes/No confirmation, and then calls `ProjectInterchangeAppendOnlyImporter`.

The command does **not** auto-save `.qsdb` and does not claim that imported semantic objects already have native geometry in the current DWG.

## Mutation contract

Append-only means every incoming semantic identity must be new in the target project.

The importer rejects before mutation when any incoming:

- Zone ID or Zone name collides;
- Floor ID or Floor name collides;
- Family ID collides, or the same category already uses that Family name;
- element ID collides.

There is no implicit rename, merge, replace or skip behavior. Those operations remain behind the explicit `ProjectInterchangeImportResolutionPlanner` policy boundary and require a separate reviewed mutation contract.

## Target authority

The target project remains authoritative for:

- `ProjectId`;
- project name;
- drawing path and drawing fingerprint;
- existing Zone/Floor/Family/element objects;
- existing active Zone/Floor/Family context.

If a target catalog was empty, its normal first-item active default may be established by the imported definitions. Existing active context is never silently replaced by source context.

## Portable semantic state

For new identities, the importer copies:

- Zone ID/name;
- Floor ID/name/elevation;
- Family ID/name/category/properties;
- element ID/category/Family/Floor/Zone references;
- element dependencies;
- portable properties;
- portable quantities.

Imported elements are marked `ElementDirtyFlags.All` so they must pass the normal review/regeneration/build lifecycle before downstream output is trusted.

## CAD provenance and ownership boundary

Source CAD identity is not portable ownership.

The importer deliberately:

- does **not** copy `sourceGeometry.drawingHandles` into target `SourceHandles`;
- clears the imported element drawing fingerprint rather than asserting source drawing identity in the target;
- does not reconstruct generated/native ownership properties or CAD handles;
- does not create/erase/replace native BricsCAD entities.

The result reports how many source handles were discarded. Last-import provenance is recorded in project metadata and in the audit trail, including the source project ID, source schema version, source drawing fingerprint, source timestamp, import timestamp and discarded-handle count.

This provenance is informational. It is not permission to bind source handles to the target DWG.

## Atomicity

Before semantic mutation, the importer captures `ProjectStateSnapshot`. Any exception during the apply/final-validation phase restores the project snapshot and rethrows.

Validation and identity/name collision checks run before the first mutation. The adapter treats palette refresh as non-authoritative UI work after a successful import; a refresh failure does not roll back a completed semantic import.

## Source guards

Smoke coverage is registered through `ProjectInterchangeAppendOnlyImporterSmoke` and covers:

- successful append while preserving target identity/context;
- dependency/property/quantity transfer;
- source-handle and source-drawing ownership discard;
- provenance/audit output;
- collision rejection before mutation;
- invalid snapshot rejection before mutation.

Static contract guard:

```text
python scripts/preflight-interchange-append-only-import.py
```

Do not claim those tests or the static guard passed unless they were actually executed on the exact source SHA.

## Still open for issue #84

Append-only is intentionally not a full round-trip importer. Still open:

- collision execution for `KeepTarget` / `UseSourceSemanticData`;
- rename/remap semantics;
- generated/native ownership clearing and controlled rebuild when existing elements are replaced;
- source-handle provenance-only storage versus discard policy execution;
- project/drawing identity policy execution beyond this conservative target-authority mode;
- first-class review/undo/session persistence UX;
- save/reopen and multi-DWG behavior;
- exact-SHA licensed BricsCAD V25 qualification;
- IFC/Revit/BCF/cloud interoperability.

Do not rename this command to a generic `QS3DINTERCHANGEIMPORT` until those broader mutation semantics are explicitly designed and qualified.

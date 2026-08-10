# Interchange Keep-Target Import

Status: **Core/source implementation only; no licensed BricsCAD V25 runtime qualification is claimed**.

This slice advances the structured `QS3D.SemanticSnapshot` v1 import path beyond append-only by executing one deliberately narrow collision policy: **KeepTarget**. Existing target semantic identities remain authoritative; source identities that do not collide may be appended after the existing validation and resolution-planning gates pass.

There is intentionally no generic `QS3DINTERCHANGEIMPORT` adapter command in this slice. The public Core API is `ProjectInterchangeKeepTargetImporter.Plan(...)` / `Import(...)`. Adapter UX, file selection, confirmation and save/session behavior remain separate reviewed work.

## Executable policy

The importer fixes every existing-identity collision policy to `KeepTarget`:

- Zone collision: KeepTarget
- Floor collision: KeepTarget
- Family collision: KeepTarget
- Element collision: KeepTarget
- Project ID: AllowDifferent
- Drawing fingerprint: AllowDifferentOrUnknown
- Source CAD handles: Discard

`ProjectInterchangeImportResolutionPlanner` is authoritative for deciding whether each source identity is `AddSourceSemanticData`, `KeepTarget`, blocked or unresolved. Mutation is refused unless every source identity resolves to exactly Add or Keep.

This means an existing target ID is never renamed, overwritten or partially merged by this importer. In particular, a source Zone/Floor name, Floor elevation, Family properties, element references/properties/quantities and source CAD provenance are ignored for a colliding ID. The current target object remains unchanged.

## New identities

A source identity with a new semantic ID may be added only when the existing resolution planner accepts its naming/category constraints. New Zone/Floor names therefore cannot steal a name owned by another target ID, and a new Family cannot collide with an existing same-category Family name.

New elements copy only portable semantic state:

- category and semantic Family/Floor/Zone references;
- semantic dependencies;
- portable properties;
- quantities from the validated snapshot.

New elements are marked dirty for review/regeneration. Their source drawing fingerprint is cleared and source CAD handles are not imported. No generated/native CAD ownership is reconstructed.

A new element may reference a kept target identity by the same semantic ID. This is deliberate KeepTarget behavior: the target identity remains authoritative for that ID while the new element can bind to it semantically.

## Read-only planning and rollback

`Plan(...)` validates the existing target, reads the already strict validated snapshot and executes the resolution planner without mutating target timestamps, metadata, audit history or semantic collections. It returns exact Add/Keep counts plus the count of source CAD handles that will be discarded.

`Import(...)` repeats the same preparation immediately before mutation. The target is captured with `ProjectStateSnapshot` and restored if an apply/validation step fails. Existing active Zone/Floor/Family context is preserved when the target already had that catalog.

Successful import records source project/schema/fingerprint/timestamp provenance, import time, discarded-handle count, Add/Keep counts and `ImportInterchangeKeepTarget` audit history. The target Project ID and target drawing identity remain authoritative.

## What this does not implement

This is **not** a merge/replace engine. It does not execute `UseSourceSemanticData`, rename/remap identities, merge property/quantity dictionaries, preserve source handles as portable ownership, clear live generated/native CAD ownership, rebuild native geometry, or define precedence when target/source semantic data disagree.

Those operations have materially different ownership and rollback consequences. In particular, replacing an existing element requires an explicit generated/native ownership clearing and controlled rebuild contract before `UseSourceSemanticData` can become an executable mutation path.

The generic adapter UX, Undo/session/save-reopen/multi-DWG behavior and additional IFC/Revit/BCF/cloud formats remain separate issue #84 work.

## Static source guard

```text
python scripts/preflight-interchange-keep-target-import.py
```

The guard checks that KeepTarget mutation remains planner-driven, rollback-protected, source-handle-discarding and unable to execute UseSource replacement semantics. The smoke source also covers read-only planning, Add-versus-Keep behavior, target-authoritative collisions, new portable state and fail-before-mutation conflict handling.

Static/source evidence is not BricsCAD V25 runtime evidence.

## Local qualification boundary

Before exposing this as a production V25 command, a local exact-SHA qualification should exercise confirmation/cancel behavior, Undo/session semantics, save/close/reopen, two-DWG switching, source/target sidecars, Unicode content and target-owned generated/native objects while proving that KeepTarget never erases or adopts CAD entities from the source snapshot.

Do not commit private/customer DWGs, licensed BricsCAD assemblies or raw sensitive runtime evidence.

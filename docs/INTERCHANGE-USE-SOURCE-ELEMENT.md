# Interchange UseSource Element — guarded replacement boundary

`QS3DINTERCHANGEUSESOURCE` is the first executable `UseSourceSemanticData` slice for `QS3D.SemanticSnapshot` v1.

It is intentionally narrower than a generic merge/import engine.

## Collision policy

- Zone ID collision: **KeepTarget**.
- Floor ID collision: **KeepTarget**.
- Family ID collision: **KeepTarget**.
- Element ID collision: **UseSourceSemanticData**, only when the planner confirms the same semantic category.
- New non-colliding Zone/Floor/Family/Element identities may be appended.
- Name collision on a different identity remains fail-closed because rename/remap policy is not defined yet.
- Incoming source CAD handles are **Discard**.
- Replaced generated output policy is **ClearOwnershipAndRequireRebuild**.

## Target DWG ownership boundary

Replacing portable semantic data does **not** authorize the snapshot to own CAD objects in the active DWG.

For an existing target element:

- its stable semantic `Id` and `Category` stay target-owned;
- existing target `SourceHandles` remain unchanged;
- existing target `DrawingFingerprint` remains unchanged;
- incoming `sourceHandles` and source drawing fingerprint are never adopted as target DWG ownership;
- Family/Floor/Zone references, dependencies, portable properties and quantities are replaced from the validated snapshot.

This means a later explicit `QS3DSYNCSOURCE` may refresh source-derived measurements/properties from the authoritative target CAD source again. That is expected: the target drawing remains authoritative for its own source-object ownership.

## Cross-layer atomicity

Before semantic replacement, the BricsCAD adapter computes an affected closure containing:

1. each replaced target element;
2. existing semantic dependents transitively;
3. the existing host of a Door/WallOpening when linked;
4. an existing incoming host referenced by an accepted source Door/WallOpening.

Inside one rollback-capable BricsCAD transaction it then:

1. captures a `ProjectStateSnapshot`;
2. calls `GeneratedDependentGeometryInvalidator.Prepare(...)` while old target ownership metadata is still available;
3. appends planner-approved new catalog identities and elements;
4. applies source portable semantic state to planner-approved element collisions;
5. marks the affected closure dirty;
6. clears invalidated generated ownership metadata through `CommitMetadata()`;
7. re-validates the combined semantic project and dependency ordering;
8. records provenance/audit metadata and touches the project;
9. commits the CAD transaction.

If any operation fails before the CAD commit, the native transaction aborts and the semantic snapshot is restored. The command does not report success for a pre-commit failure.

## Generated output behavior

The import can erase target-owned generated outputs for the affected closure, including supported generated solid/rebar/curtain/grid outputs, through the existing ownership guards. It does not adopt handles from the source file.

Rebuild is deliberately explicit after import. The command does **not** call:

- `QS3DBUILD3D`;
- opening boolean cut commands;
- rebar generation commands;
- curtain frame generation commands;
- Grid annotation generation commands.

Recommended review sequence after a successful replacement is `QS3DHEALTHALL`, inspect the affected semantic elements, then run only the required explicit rebuild/cut commands.

## Still not claimed

This slice does not complete issue #84. Still open include:

- generic `QS3DINTERCHANGEIMPORT` policy-selection UX;
- UseSource replacement for colliding Zone/Floor/Family definitions;
- rename/remap and merge precedence;
- provenance-only storage of incoming source handles;
- automatic source-handle rebinding to the current DWG;
- automatic physical rebuild/cut;
- undo/session/save-reopen/multi-DWG qualification;
- exact-SHA BricsCAD V25 runtime qualification;
- IFC/Revit/BCF/vendor/cloud interchange.

Source/static implementation is not evidence of licensed BricsCAD V25 runtime qualification.

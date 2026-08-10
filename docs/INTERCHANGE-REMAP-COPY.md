# Interchange Remapped Copy / Federated Copy

## Status

Source implementation only. This document does **not** claim licensed BricsCAD V25 NETLOAD/runtime qualification, save/reopen qualification, private/customer DWG qualification, or release readiness.

## Why this mode exists

The existing `QS3DINTERCHANGEIMPORT` command is a collision-policy import: append when safe, KeepTarget, Replace Element semantic, or Replace Catalog semantic. Those policies intentionally operate on the target identity space.

`QS3DINTERCHANGEREMAP` solves a different problem: bring another QS3D semantic project into the current project as an **isolated copied namespace** without replacing the target identities even when the source reuses the same Zone/Floor/Family/Element IDs.

This is a copied/federated snapshot. It is **not** a live external link, Revit-style linked model, automatic merge, or CAD-handle ownership transfer.

## Command

`QS3DINTERCHANGEREMAP`

Flow:

1. select a validated `.qs3d.json` semantic snapshot;
2. enter an import namespace;
3. Core builds a read-only deterministic mapping plan;
4. the command shows counts, remapped-reference counts, discarded-handle counts and sample Element ID mappings;
5. explicit confirmation is required;
6. Core applies the copy atomically;
7. imported elements remain dirty and require explicit regeneration/build;
8. the command does not automatically save `.qsdb`.

A different namespace represents a different copied instance. Reusing the same namespace for the same source deterministically produces the same target IDs and therefore fails closed if those IDs already exist.

## Deterministic identity contract

Each source identity maps from `(namespace, identity kind, source id)` through SHA-256 to a bounded target-safe ID:

- Zone -> `RZ-...`
- Floor/Level -> `RL-...`
- Family -> `RF-...`
- Element -> `RE-...`

The mapping does not depend on enumeration order, process-global state, clock time or random GUIDs. Any mapped-ID collision with the current target blocks the plan. There is no silent numeric suffixing.

Zone/Floor/Family display names are namespaced as well. If the normal namespaced form collides, a deterministic short source-ID hash is added. Names remain inside the current domain-service limits.

## Reference closure

Canonical references are remapped before mutation:

- `ProjectElement.FamilyId`
- `ProjectElement.FloorId`
- `ProjectElement.ZoneId`
- every `ProjectElement.DependsOn` entry

Portable semantic references stored inside property dictionaries are **not** rewritten generically. The finite registry in `ProjectInterchangeSemanticReferencePolicy` currently declares:

- `BottomLevelId` -> Floor
- `TopLevelId` -> Floor

This matters because arbitrary string properties can legitimately contain text equal to another semantic ID. Generic search/replace would corrupt unrelated data.

If an unregistered property key looks reference-like and its value resolves exactly to a source Zone/Floor/Family/Element ID, remapped-copy planning fails closed and asks for an explicit policy to be added first. This is deliberate forward-compatibility behavior.

Opening host relationships do not need a special property rule: current opening logic treats `DependsOn` as the authoritative host relation, and `DependsOn` is remapped as part of the canonical closure.

## CAD ownership boundary

The remapped copy never imports source native ownership into the target DWG:

- `ProjectElement.SourceHandles` from the source are counted and discarded;
- imported `DrawingFingerprint` values are cleared;
- generated/native ownership-like properties (`*Handle*`, `QS3D.Generated*`) are discarded defensively;
- no source handle is rebound to a target BricsCAD entity;
- imported Elements are `ElementDirtyFlags.All` and need explicit rebuild/regeneration.

The existing target semantic elements, source handles, drawing fingerprint and generated outputs are not replaced by this mode.

## Mutation and rollback

Planning completes validation, mapping, reference closure and collision checks before any target mutation. Apply uses `ProjectStateSnapshot`; any exception restores the target project state.

Mutation order is deterministic and dependency-safe for catalog references:

1. Zones
2. Floors
3. Families
4. Elements with already-remapped canonical/property references

Existing active Zone/Floor context is preserved when the target already had those catalogs. If the target catalog was empty, the normal domain service may establish the first active context.

Audit operation: `ImportInterchangeRemapCopy`.

Project metadata records the last source project/schema/fingerprint/timestamp, namespace, handle-discard count, property-reference remap count and generated-ownership-property discard count.

## What this feature deliberately does not do

- no Replace/UseSource behavior;
- no same-ID semantic merge;
- no live synchronization back to the source project;
- no external-file watcher;
- no source CAD handle ownership import;
- no automatic native 3D generation;
- no IFC/Revit/BCF import claim;
- no automatic `.qsdb` save;
- no claim that source and target engineering standards are compatible.

## Source verification

`tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapCopySmoke.cs` guards deterministic mapping, preservation of colliding target identities, canonical dependency remap, Bottom/Top Level property remap, handle discard, dirty imported Elements and fail-closed unknown reference-like properties.

`scripts/preflight-interchange-remap-copy.py` is a static source guard and is automatically discovered by `scripts/preflight-all.py` under the repository's existing convention.

## LOCAL_ONLY V25 qualification

A local owner with licensed BricsCAD V25 x64 must still qualify the exact commit SHA:

- build the V25 adapter against the approved installed BricsCAD V25 SDK/runtime;
- NETLOAD and execute `QS3DINTERCHANGEREMAP` in a disposable test DWG;
- import a sanitized snapshot with intentional ID/name collisions;
- verify the preview and explicit confirmation path;
- inspect the remapped Project Tools/Workspace state;
- rebuild generated geometry and verify no source handles became target ownership;
- verify Undo/close/reopen/save behavior according to the project persistence workflow;
- verify multi-DWG context isolation;
- capture only sanitized evidence allowed by repository policy.

Until that exact-SHA evidence exists, this feature is **source-implemented, runtime-unqualified**.

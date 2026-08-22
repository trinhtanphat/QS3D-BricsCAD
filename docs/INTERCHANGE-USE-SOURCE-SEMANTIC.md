# Interchange — UseSourceSemanticData execution boundary

Status: `SOURCE_IMPLEMENTED` for the Core semantic phase only. BricsCAD V25 native cleanup/orchestration remains `LOCAL_ONLY`.

`ProjectInterchangeUseSourceSemanticImporter` executes the previously planned `UseSourceSemanticData` collision decision without treating portable JSON as authority over target-DWG objects.

This path is intentionally stricter than append-only, KeepTarget and Import As New because replacing an existing semantic identity can invalidate native generated output that already belongs to the target drawing.

## What the Core path does

The importer re-reads and strictly validates the snapshot, re-plans against the current target, and applies one deterministic policy:

- all new Zone/Floor/Family/Element identities use source semantic data as additions;
- same-ID compatible Zone/Floor/Family/Element identities use source semantic data as replacement;
- ProjectId and target drawing identity stay owned by the target project;
- incoming source handles are discarded;
- incoming drawing fingerprint ownership is discarded;
- replaced elements receive portable semantic properties/quantities/dependencies only;
- replaced/new elements are dirty and require regeneration;
- replaced Floor/Family identities conservatively invalidate target elements that consume them;
- semantic dependents and `HostWallId` dependents of replaced elements are included in the affected target set;
- generated/native ownership metadata is cleared only after explicit native cleanup authorization when live target generated handles are involved;
- `ProjectStateSnapshot` restores the target semantic state if the Core mutation throws.

The importer mutates matching Zone/Floor/Family/Element objects in place where possible so a same-ID semantic replacement does not deliberately create a second semantic identity or unnecessarily detach existing model references.

## Native cleanup authorization

Core can identify target elements that currently claim generated/native owner handles through `GeneratedHandleOwnershipPolicy`, but Core cannot erase BricsCAD entities.

`ProjectInterchangeUseSourceSemanticPlan.TargetElementIdsRequiringNativeCleanup` is therefore an explicit handoff boundary. If that list is non-empty, `Import(...)` refuses to mutate until the caller supplies `ProjectInterchangeNativeCleanupAuthorization` covering every required target element.

The authorization means the native adapter has already completed or transactionally staged cleanup for those exact semantic owners. It is not a command-line bypass and it is not evidence that the CAD deletion actually happened.

A target element can be affected even when it is absent from the source snapshot. Examples include a target-only instance of a replaced Family, an element on a replaced Floor, or a dependent of a replaced semantic element. Those elements keep their target source handles/fingerprint, but their generated ownership metadata is cleared and they are marked dirty after authorized native cleanup.

## What it deliberately does not do

The Core importer does not:

- erase, modify or rebuild BricsCAD entities;
- adopt the source snapshot's CAD handles;
- copy the source drawing fingerprint onto target elements;
- automatically run `QS3DBUILD3D`, opening cuts, rebar, Curtain, Grid or documentation generation;
- provide field-level merge precedence;
- remap conflicting IDs/names;
- make native cleanup + semantic mutation one BricsCAD transaction;
- provide Undo/save-reopen/multi-DWG runtime certification.

For this reason, a generic `QS3DINTERCHANGEIMPORT` command must not expose UseSource replacement until the BricsCAD adapter has a proven whole-operation transaction or durable compensation/recovery workflow. That adapter/runtime work is `LOCAL_ONLY`.

## Why target-only dependents are invalidated

Replacing only the colliding element is not enough.

A replaced Family can change inherited dimensions/material behavior for target-only instances. A replaced Floor can change vertical placement. A replaced host element can invalidate dependent generated output. The plan therefore computes a conservative affected set before mutation and expands reverse dependencies, including the registered `HostWallId` relation.

Any affected element with current generated owner handles appears in the native-cleanup requirement. After authorization, generated ownership metadata is removed and the element becomes dirty so stale geometry cannot remain trusted.

## Source ownership policy

Portable semantic snapshots are not target-DWG ownership documents.

For every source Element added or replaced:

- `sourceHandles` are counted for provenance reporting but discarded;
- `DrawingFingerprint` is cleared;
- `Generated*` metadata is not imported;
- `PhysicalOpeningCut*` metadata is not imported;
- handle-shaped drawing-local properties are not imported.

Target-only affected elements are different: their source handles and drawing fingerprint remain intact because their authoritative target CAD source was not replaced. Only generated/native output metadata is cleared.

## Failure and rollback

Planning is non-mutating.

Execution:

1. validates current target state;
2. validates and reads the source snapshot;
3. re-plans the current collisions;
4. computes affected targets and required native cleanup;
5. rejects missing cleanup authorization before semantic mutation;
6. captures `ProjectStateSnapshot`;
7. applies catalog and element semantic replacement/addition;
8. clears generated ownership for affected target-only elements;
9. marks affected elements dirty;
10. preserves target project/drawing/active-context identity;
11. records import metadata and audit;
12. validates the resulting target references.

Any Core exception after snapshot capture restores the previous semantic project state. This rollback does not roll back BricsCAD native cleanup; that is exactly why a future adapter command requires native transaction/recovery orchestration rather than calling this Core API directly from an unguarded command.

## Qualification boundary

The source contract and smoke/preflight coverage can be completed remotely. The following remain `LOCAL_ONLY`:

- native generated-object deletion under real BricsCAD transactions;
- failure injection between native cleanup and semantic apply;
- Undo;
- Save/SaveAs/reopen;
- multi-DWG/document switching;
- actual regeneration of affected families;
- exact-SHA licensed BricsCAD V25 qualification.

Do not describe this Core implementation as full generic round-trip import or as V25 runtime-qualified interoperability.

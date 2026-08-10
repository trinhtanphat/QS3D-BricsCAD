# Interchange Import As New — guarded execution

`QS3DINTERCHANGEREMAPAPPEND` executes the deterministic remap designed by `ProjectInterchangeRemapPlanner` as a **semantic-only append**.

It exists for snapshots whose incoming identities/names collide with the target but should be imported as a separate semantic namespace instead of replacing target data.

## Preconditions

The command first runs the same strict bounded UTF-8 snapshot validation and remap plan used by `QS3DINTERCHANGEREMAPPLAN`.

Execution is refused when:

- the remap plan contains unresolved property-carried references;
- the planner cannot allocate collision-free candidate IDs/names;
- a typed Family/Floor/Zone/dependency/HostWall reference cannot resolve through the source snapshot;
- an ID/ref-like property has no explicit rewrite policy;
- the snapshot does not actually need an ID/name remap, in which case the canonical `QS3DINTERCHANGEAPPEND` path should be used instead.

The importer **re-plans against the current target immediately before mutation**. A previously displayed dry-run plan is informational and is not stale authorization.

## Import semantics

The complete incoming semantic set is appended under the current deterministic plan:

- Zone IDs/names are mapped to planned targets;
- Floor IDs/names are mapped while preserving source `ElevationM`;
- Family IDs/names are mapped while preserving source category/properties;
- Element IDs are mapped;
- `FamilyId`, `FloorId`, `ZoneId` and `DependsOn` are rewritten to the mapped identities;
- `HostWallId` is rewritten only through the explicit source-Element map.

Existing target Zone/Floor/Family/Element objects are not renamed, replaced or deleted by this policy.

## Native ownership stripping

Import As New deliberately has no authoritative CAD source in the active target drawing.

For every new Element:

- incoming `sourceHandles` are discarded;
- target `DrawingFingerprint` is left empty rather than copying the source drawing fingerprint;
- property keys beginning `Generated` are discarded;
- property keys beginning `PhysicalOpeningCut` are discarded;
- any property key containing `Handle` is discarded as drawing-local/native ownership metadata.

Descriptive non-handle CAD properties can remain semantic metadata, but they do not create target ownership because no source handle/fingerprint is assigned.

Generated output is therefore stale/absent by design. Every new Element is marked dirty and must be explicitly built/cut/rebar/curtain/grid-generated later if required.

## Property-reference boundary

`HostWallId` is the only currently registered property-carried Element relation for remap execution.

Other non-empty property keys shaped like `*Id`, `*Ids`, `*Ref`, `*Refs`, `*RefId` or `*RefIds` are treated as unresolved semantic/reference candidates and block execution until an explicit rewrite policy exists.

This is intentionally conservative. Keeping an unknown embedded source ID unchanged could silently link imported semantic data to the wrong object in the target project.

## Atomicity

No native target DWG object is modified, so this path does not open a BricsCAD entity transaction or invalidate existing generated ownership.

Semantic apply is guarded by `ProjectStateSnapshot`:

1. strict-read and validate source;
2. re-plan against current target;
3. reject unresolved reference policy;
4. capture project state;
5. append remapped Zone/Floor/Family/Element data;
6. rewrite registered references;
7. validate the combined target references, Family category compatibility and dependency graph;
8. record import audit/metadata and touch the project;
9. return success.

Any exception after snapshot capture restores the previous project state.

## Post-import actions

The command does not automatically:

- create native geometry;
- run `QS3DBUILD3D`;
- perform opening booleans;
- generate rebar/curtain/grid output;
- assign source CAD ownership;
- save `.qsdb`.

Review semantic health first, then rebuild/save explicitly.

## Still separate

Import As New does not replace KeepTarget, UseSource Element, UseSource Catalog or UseSource ALL. It is a distinct collision policy for retaining both target and source semantic identities.

Field-level merge precedence, target-DWG source-handle rebinding, external IFC/Revit/BCF/vendor formats, provenance+semantic combined authorization and exact BricsCAD V25 save/reopen/runtime qualification remain separate work.

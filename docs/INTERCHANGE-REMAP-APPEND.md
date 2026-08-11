# Interchange Import As New — guarded execution

`QS3DINTERCHANGEREMAPAPPEND` executes the deterministic remap designed by `ProjectInterchangeRemapPlanner` as a **semantic-only append**.

It exists for snapshots whose incoming identities/names collide with the target but should be imported as a separate semantic namespace instead of replacing target data.

## Preconditions

The command first runs the same strict bounded UTF-8 snapshot validation and remap plan used by `QS3DINTERCHANGEREMAPPLAN`.

Execution is refused when:

- the remap plan contains unresolved property-carried references;
- the planner cannot allocate collision-free candidate IDs/names;
- a typed Family/Floor/Zone/dependency or registered property reference cannot resolve through the source snapshot;
- an ID/ref-like property has no explicit rewrite policy;
- the snapshot does not actually need an ID/name remap, in which case the canonical `QS3DINTERCHANGEAPPEND` path should be used instead.

The importer **re-plans against the current target immediately before mutation**. A previously displayed dry-run plan is informational and is not stale authorization.

## Import semantics

The complete incoming semantic set is appended under the current deterministic plan:

- Zone IDs/names are mapped to planned targets;
- Floor IDs/names are mapped while preserving source `ElevationM`;
- Family IDs/names are mapped while preserving source category/properties; Family display-name collision scope remains category-aware;
- Element IDs are mapped;
- `FamilyId`, `FloorId`, `ZoneId` and `DependsOn` are rewritten to the mapped identities;
- registered property-carried semantic references are rewritten through their declared identity kind;
- every registered source reference must resolve **inside the source snapshot** before it may map into the imported namespace.

Existing target Zone/Floor/Family/Element objects are not renamed, replaced or deleted by this policy.

## Typed property-reference policy

`ProjectInterchangeSemanticReferencePolicy` is the single registry used by both dry-run planning and mutation. It currently defines three portable `ProjectElement.Properties` references:

| Property | Reference kind | Import As New behavior |
| --- | --- | --- |
| `HostWallId` | Element | remap to the imported host Element ID |
| `BottomLevelId` | Floor/Level | remap to the imported bottom Floor/Level ID |
| `TopLevelId` | Floor/Level | remap to the imported top Floor/Level ID |

`BottomLevelId` and `TopLevelId` are the same semantic vertical-placement keys used by `ProjectFloorService` / `ElementVerticalPlacementService`; Import As New therefore preserves level-relative intent instead of keeping a stale source ID or binding by coincidence to an existing target Floor.

The registry does **not** authorize arbitrary similarly named properties. Other non-empty keys shaped like `*Id`, `*Ids`, `*Ref`, `*Refs`, `*RefId` or `*RefIds` remain unresolved candidates and block execution until an explicit typed policy is added. Family properties remain fail-closed under the same conservative suffix screen; the current registry applies only to `ProjectElement.Properties`.

A registered reference that points outside the source snapshot also blocks the plan. Import As New never guesses that an external/source-local ID should bind to a target identity with the same text.

After apply, the combined target is checked again for every registered property reference. `TopLevelId` without `BottomLevelId` is rejected and the semantic snapshot rollback restores the target.

## Native ownership stripping

Import As New deliberately has no authoritative CAD source in the active target drawing.

For every new Element:

- incoming `sourceHandles` are discarded;
- target `DrawingFingerprint` is left empty rather than copying the source drawing fingerprint;
- generated/native owner slots recognized by `GeneratedHandleOwnershipPolicy` are discarded;
- property keys beginning `Generated` are discarded;
- property keys beginning `PhysicalOpeningCut` are discarded;
- any property key containing `Handle` is discarded as drawing-local/native ownership metadata.

Descriptive non-handle CAD properties can remain semantic metadata, but they do not create target ownership because no source handle/fingerprint is assigned.

Generated output is therefore stale/absent by design. Every new Element is marked dirty and must be explicitly built/cut/rebar/curtain/grid-generated later if required.

## Atomicity

No native target DWG object is modified, so this path does not open a BricsCAD entity transaction or invalidate existing generated ownership.

Semantic apply is guarded by `ProjectStateSnapshot`:

1. strict-read and validate source;
2. re-plan against current target;
3. reject unresolved reference policy;
4. capture project state;
5. append remapped Zone/Floor/Family/Element data;
6. rewrite first-class and registered property-carried references;
7. verify planned native-ownership discard counts did not change;
8. validate combined target references, registered property references, Family category compatibility and dependency graph;
9. record import audit/metadata and touch the project;
10. return success.

Any exception after snapshot capture restores the previous project state.

## Static regression coverage

`ProjectInterchangeRemapPlannerSmoke` covers category-scoped Family names, opaque reference blocking, typed Bottom/Top Level planning and missing-source reference blocking.

`ProjectInterchangeRemapLevelReferenceSmoke` covers apply-time Bottom/Top Level remapping, preservation of offsets, source handle/fingerprint stripping, preservation of existing target identities and rollback for invalid `TopLevelId`-without-`BottomLevelId` state.

`scripts/preflight-interchange-remap-append.py` locks planner/executor parity around the shared property-reference registry, while retaining the existing atomicity, ownership, command-registration and deterministic-remap source guards.

These are **source/static contracts**. They do not replace exact-SHA licensed BricsCAD V25 build/NETLOAD/save-reopen/multi-DWG/runtime qualification.

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

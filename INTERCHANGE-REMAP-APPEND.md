# Interchange Import As New — guarded execution

`QS3DINTERCHANGEREMAPAPPEND` executes the deterministic remap designed by `ProjectInterchangeRemapPlanner` as a **semantic-only append**.

It exists for snapshots whose incoming identities/names collide with the target but should be imported as a separate semantic namespace instead of replacing target data.

## Preconditions

The command first runs the same strict bounded UTF-8 snapshot validation and remap plan used by `QS3DINTERCHANGEREMAPPLAN`.

Execution is refused when:

- the remap plan contains unresolved property-carried references;
- the planner cannot allocate collision-free candidate IDs/names within target runtime limits;
- target Zone/Floor/Family capacity would be exceeded;
- a portable Family property exceeds the target runtime key/value limit and would require truncation;
- a typed Family/Floor/Zone/dependency or registered property reference cannot resolve through the source snapshot;
- an Element property shaped like `*Id`, `*Ids`, `*Ref`, `*Refs`, `*RefId` or `*RefIds` has no explicit rewrite policy;
- a Family property has one of those ID/ref-like shapes and no explicit Family-property rewrite policy;
- the snapshot does not actually need an ID/name remap, in which case the canonical `QS3DINTERCHANGEAPPEND` path should be used instead.

A blocked plan remains inspectable in the dry-run/command UX. `Import` validates execution safety before taking the mutation snapshot. The command also binds confirmation to the reviewed project instance and `ChangeVersion`, re-resolves the current project before apply, and refuses a stale confirmation if the project changed. The importer itself still **re-plans against the current target immediately before mutation**.

## Runtime-bounded remap identities

The planner does not reuse the broader interchange JSON envelope as the target catalog limit. It allocates candidates within the current runtime domain-service limits:

- Zone: ID <= 64, name <= 120;
- Floor: ID <= 64, name <= 120;
- Family: ID <= 80, name <= 160;
- Element: ID <= 128.

An incoming Zone/Floor/Family identity that is valid in the interchange envelope but longer than the target runtime limit is deterministically remapped before apply. Incoming duplicate names are also resolved deterministically within the same import batch, and Family display-name collisions remain scoped by Family category.

The compatibility plan additionally checks combined target + source capacities: Zone <= 2000, Floor <= 2000 and Family <= 10000. Portable Family property keys/values must fit the target runtime limits of 120/1000 characters; Import As New does not silently truncate semantic data.

## Import semantics

The complete incoming semantic set is appended under the current deterministic plan:

- Zone IDs/names are mapped to planned targets;
- Floor IDs/names are mapped while preserving source `ElevationM`;
- Family IDs/names are mapped while preserving source category and safe portable properties;
- Element IDs are mapped;
- `FamilyId`, `FloorId`, `ZoneId` and `DependsOn` are rewritten to mapped identities;
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

`BottomLevelId` and `TopLevelId` are the same vertical-placement keys used by `ProjectFloorService` and `ElementVerticalPlacementService`. Import As New therefore preserves level-relative intent instead of retaining a stale source ID or accidentally binding to an existing target Floor with the same text.

The registry does **not** authorize arbitrary similarly named properties. Other non-empty Element keys shaped like `*Id`, `*Ids`, `*Ref`, `*Refs`, `*RefId` or `*RefIds` remain unresolved candidates and block execution until an explicit typed policy is added. Family properties use the same fail-closed ID/ref-shape screen but currently have no registered remap relation.

A registered reference that points outside the source snapshot also blocks the plan. Import As New never guesses that an external/source-local ID should bind to a target identity with the same text.

After apply, newly imported Elements are checked again for every registered property reference. Level references additionally enforce the semantic relation rules: `TopLevelId` requires `BottomLevelId`; offsets require their corresponding level reference; offsets must be finite invariant numbers; and when both levels are present, top elevation plus offset must remain above bottom elevation plus offset. Failure rolls the semantic import back.

## Native ownership stripping

Import As New deliberately has no authoritative CAD source in the active target drawing.

For incoming **Family and Element properties**:

- keys recognized by `GeneratedHandleOwnershipPolicy` are discarded;
- keys beginning `Generated` are discarded;
- keys beginning `PhysicalOpeningCut` are discarded;
- any property key containing `Handle` is discarded as drawing-local/native ownership metadata.

For every new Element:

- incoming `sourceHandles` are discarded;
- `DrawingFingerprint` is left empty rather than copying the source drawing fingerprint.

The plan counts all Family + Element ownership properties that must be discarded. Apply recounts the actual discarded properties; if the number differs from the plan, import is rejected and semantic state is rolled back instead of recording stale authorization/audit metadata.

Descriptive non-handle CAD properties can remain semantic metadata, but they do not create target ownership because no source handle/fingerprint is assigned. Generated output is stale/absent by design; every new Element is marked dirty and must be explicitly rebuilt later if required.

## Atomicity

No native target DWG object is modified, so this path does not open a BricsCAD entity transaction or invalidate existing generated ownership.

Semantic apply is guarded by `ProjectStateSnapshot`:

1. strict-read and validate source;
2. re-plan against current target;
3. reject unresolved reference policy and runtime compatibility blockers before mutation snapshot;
4. count source handles and Family/Element ownership metadata that must be discarded;
5. capture project state;
6. append remapped Zone/Floor/Family/Element data;
7. rewrite first-class and registered property-carried references and discard drawing-local/native ownership metadata;
8. verify the applied ownership-discard count still matches the plan;
9. validate combined first-class references, imported registered property references, level relation consistency, Family category compatibility and dependency graph;
10. record import audit/metadata and touch the project;
11. return success.

Any operation exception after snapshot capture restores the previous project state. If restoration itself fails, the importer preserves both the operation and rollback errors in an aggregate failure instead of hiding rollback loss.

## Project Tools and review freshness

Project Tools exposes two separate entries:

- `QS3DINTERCHANGEREMAPPLAN` — dry-run only, including unresolved-reference and runtime-compatibility blockers;
- `QS3DINTERCHANGEREMAPAPPEND` — explicit confirmed semantic mutation using a fresh re-plan.

The apply command records the project `ChangeVersion` shown at confirmation and re-resolves the project before mutation. A document/project switch or semantic version change invalidates the reviewed confirmation. This keeps **reviewed state** separate from later mutated state.

The dedicated Import As New path remains distinct from replacement policies so users can clearly distinguish **retain both namespaces** from **replace target semantic data**.

## Static regression coverage

`ProjectInterchangeRemapPlannerSmoke` covers category-scoped Family names, inspectable blocked plans, runtime-bounded catalog identities, duplicate incoming names, opaque-reference blocking, typed Bottom/Top Level planning and missing-source registered references.

`ProjectInterchangeRemapLevelReferenceSmoke` covers apply-time Bottom/Top Level remapping, level-offset preservation, source handle/fingerprint stripping, preservation of existing target identities and rollback for invalid `TopLevelId`-without-`BottomLevelId` state.

`scripts/preflight-interchange-remap-append.py` locks planner/executor parity around the shared property-reference registry while retaining inspectable-plan, runtime compatibility, confirmation freshness, atomicity, rollback, ownership and command-registration source guards.

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

Import As New does not replace KeepTarget, UseSource Element, UseSource Catalog, UseSource ALL or the newer UseSource Semantic policy. It remains a distinct collision policy for retaining both target and source semantic identities.

Field-level merge precedence beyond defined policies, target-DWG source-handle rebinding, external IFC/Revit/BCF/vendor formats, provenance+semantic combined authorization and exact BricsCAD V25 save/reopen/runtime qualification remain separate work.

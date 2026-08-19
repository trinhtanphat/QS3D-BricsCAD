# QS3D Feature Menu Architecture + Agent Backlog

**Issue / Lane-Key:** #3113 / `issue-3113`  
**Baseline:** `main@f35ad8110eb3fae6207e27a6cf2bdc688e689f64`  
**Status:** architecture decision + migration backlog  
**Applies to:** `QS3D-BricsCAD` Workspace / Mô hình navigation and the business flows opened from that navigation

## 1. Why this note exists

QS3D is accumulating many left-navigation entries such as grid, room, room finishes, beam, slab, column, wall, opening, door, stair, foundation, earthwork and custom quantities. The difficult part is not rendering a `TreeView`; each leaf can have a different business workflow:

- some create an object immediately;
- some need one or more creation modes;
- some are generated from a host object such as a room or slab;
- some have 3D geometry and some are parameter/data only;
- some calculate area, some length, some volume, some multiple quantities;
- some depend on another feature and must become dirty when the host changes;
- some need material, quantity, recognition, regenerate, visibility or special inspection actions.

If every leaf owns bespoke Workspace UI and event wiring, the number of partial classes, special cases and interaction branches grows roughly with the number of features. That makes the menu difficult to maintain and also makes the product harder to use because the same-looking action can behave differently in unrelated places.

The architecture decision in this note is:

> **The left menu is navigation, not the source of business logic. A menu leaf resolves to a feature descriptor in a central registry. The descriptor declares capabilities and delegates behavior to a small set of reusable engines/recipes.**

This is intentionally a clean-room QS3D design. BLT3D screenshots/public workflow research are usability/reference evidence only; they are not a source-code or private-schema specification. See `docs/BLT3D-BIM5D-BENCHMARK.md` and `docs/BLT3D-FEATURE-COMMAND-INVENTORY.md`.

## 2. Current QS3D seams that motivate the change

Current `main` already has the beginning of a semantic key system, but responsibility is spread across UI code:

- `ReferenceWorkspaceTreeAugmenter` hard-codes the visible tree hierarchy and puts strings such as `Room`, `FloorFinish`, `Waterproofing`, `Skirting`, `Beam`, `Slab`, etc. in `TreeViewItem.Tag`.
- `WorkspacePanel.OnModelTreeSelectedItemChanged` parses the tag into `ElementCategory` and applies the family filter.
- `WorkspacePanel.OnAddClick` then decides a category from the current family/filter and performs a generic create/duplicate path.
- multiple `WorkspacePanel.*` partials repair or specialize layout/actions around that generic surface.

That works as an incremental compatibility layer, but it should not become the long-term definition of each business feature.

Two active lanes already prove that different leaves need different behavior:

- **#3095** owns the current Room-specific direct-Add/right-pane runtime behavior. Do not duplicate or take over that lane.
- **#3107** owns missing category-specific quick property schemas for many family categories. Do not duplicate or take over that lane.

The migration described below must consume those results from `main` after they land rather than racing them.

## 3. Product mental model

The product should expose four layers with one-way responsibility:

```text
Navigation tree / search / favorites
            |
            v
      Feature Registry
            |
            +--> Feature Descriptor
                    |-- create recipes
                    |-- capabilities
                    |-- property schema
                    |-- quantity rule
                    |-- dependency / dirty policy
                    |-- semantic mapping
            |
            v
      Reusable domain engines
            |
            v
      BricsCAD host adapters
```

### 3.1 Navigation layer

Owns only discoverability and selection:

- group/order;
- display label and localized aliases;
- icon key;
- search keywords;
- favorite/recent state;
- visibility based on product edition/context.

It does **not** know how to create geometry or calculate quantities.

### 3.2 Feature registry

The registry is the source of truth for what a QS3D feature can do. UI asks the registry what to show; commands ask the registry how to dispatch; tests can enumerate the registry and prove completeness.

### 3.3 Domain engines / recipes

A small number of engines implement recurring patterns. Individual features specialize them with configuration/recipes rather than copy-pasting a new engine.

### 3.4 Host adapters

All direct BricsCAD document/entity/selection/transaction behavior remains behind host-specific adapters. Core feature definitions must not depend on WPF controls or on searching UI controls by label.

## 4. Target feature contract

Names below are architectural names, not a mandate to put every type in one file or namespace. Implementation agents should fit the existing project layering.

```csharp
public sealed record FeatureDescriptor(
    FeatureId Id,
    FeatureGroupId GroupId,
    string DisplayNameKey,
    FeatureEngineKind Engine,
    IReadOnlyList<CreateRecipeId> CreateRecipes,
    CreateRecipeId PrimaryCreateRecipe,
    FeatureCapabilities Capabilities,
    PropertySchemaId PropertySchema,
    QuantityRuleId? QuantityRule,
    DependencyRuleId? DependencyRule,
    DirtyPolicy DirtyPolicy,
    SemanticMapping? SemanticMapping);
```

Recommended supporting contracts:

```text
FeatureId
FeatureGroupId
FeatureDescriptor
FeatureCapabilities (flags/value object)
FeatureEngineKind
CreateRecipeId / IFeatureCreateRecipe
PropertySchemaId / PropertySchemaDefinition
QuantityRuleId / IQuantityRule
DependencyRuleId / IDependencyRule
DirtyPolicy
SemanticMapping
IFeatureRegistry
```

The descriptor is intentionally data-oriented. Do not put giant feature-specific event handlers into it.

## 5. Capabilities drive the action surface

The generic Workspace should render actions from capabilities rather than from category-specific `if/else` blocks.

Example capability set:

```text
Create
Duplicate
Delete
Parameters
Geometry3D
Material
Quantity
CaptureSelection
AutoCreate
Regenerate
Visibility
Inspect
```

Examples:

| Feature | Parameters | 3D | Material | Quantity | Auto | Regenerate |
|---|---:|---:|---:|---:|---:|---:|
| Room | yes | yes | optional | yes | yes | yes |
| Floor Finish | yes | yes | yes | yes | yes | yes |
| Waterproofing | yes | yes | yes | yes | yes | yes |
| Skirting | yes | yes | yes | yes | yes | yes |
| pure metadata helper | yes | no | no | no | no | no |

**Rule:** if a capability is irrelevant, do not display a dead button merely for visual uniformity.

## 6. Creation recipes replace the overloaded `+ Add`

`+ Add` should mean **perform the safest/default creation recipe for the selected feature**.

If the feature has alternative creation modes, use a split/dropdown adjacent to the same primary action. Do not force a mode chooser on every feature.

```text
+ Add                 -> primary recipe
  v
  From Room
  Pick Face
  Draw Boundary
  Capture Existing CAD
```

This resolves the Room problem conceptually: Room can declare a direct primary recipe, while another feature can declare `ParameterFamily` / `Solid3D` or other relevant alternatives without making that chooser global.

### Modal rule

Use a modal/popup only when the user must make a decision that cannot be inferred safely. Prefer inline/contextual defaults for common flows.

## 7. Reusable engine families

QS3D should aim for a small engine set rather than one engine per menu item.

### 7.1 Spatial engine

Typical features:

- Room;
- future Zone/Space-like business objects.

Typical behavior:

- closed boundary / spatial containment;
- name/number sequencing;
- floor/zone context;
- derived area/perimeter/volume;
- host for finish features.

### 7.2 Surface engine

Typical features:

- Floor Finish;
- Waterproofing;
- Wall Finish;
- Ceiling Finish;
- plaster/covering-like layers.

Typical behavior:

- derive from room/host faces or pick faces manually;
- thickness/material parameters;
- area and optionally volume;
- regenerate when host geometry changes.

### 7.3 Linear / path engine

Typical features:

- Skirting;
- Railing;
- edge/path driven quantity objects.

Typical behavior:

- derive from perimeter/edges/path;
- cross-section/profile parameters;
- length and optionally volume/weight;
- exclusions at openings or joins.

### 7.4 Hosted / structural engine

Typical features:

- Wall;
- Slab;
- Beam;
- Column;
- Foundation;
- Opening/Door where host relations are required.

Typical behavior:

- explicit host/level relation;
- family/type + instance data;
- native geometry builder;
- structural quantities.

### 7.5 Derived / recognition engine

Typical features:

- create from selected existing CAD/BIM geometry;
- recognition/classification workflows;
- custom quantities derived from existing objects.

Typical behavior:

- capture provenance/source handles;
- validation/confidence or deterministic matching;
- update/rebuild from source geometry.

## 8. Concrete pilot mappings

These are the first vertical slices because together they exercise spatial, surface and linear behavior.

### 8.1 Room

```text
Id                  = ROOM
Group               = ROOM_FINISH
Engine              = Spatial
PrimaryCreateRecipe = CreateRoomDirect
AlternateRecipes    = DetectBoundary, PickBoundary, DrawBoundary (when/if implemented)
NamingRule          = "Phòng-{n}"
Capabilities        = Parameters | Geometry3D | Quantity | AutoCreate | Regenerate
PropertySchema      = RoomSchema
QuantityRule        = RoomSpatialQuantity
DependencyRule      = RoomDependents
DirtyPolicy         = BoundaryDriven
Semantic            = IfcSpace-like mapping metadata
```

UX:

1. select `HT_Phòng > Phòng`;
2. press `+ Add`;
3. next room is created, selected and shown in the detail/property surface;
4. advanced creation modes are optional, not a mandatory popup.

Current implementation lane #3095 owns the immediate runtime fix. The registry migration must wait for and preserve its user-visible behavior.

### 8.2 Floor Finish

```text
Id                  = FLOOR_FINISH
Group               = ROOM_FINISH
Engine              = Surface
Host                = Room
PrimaryCreateRecipe = FromSelectedRoom
AlternateRecipes    = PickFace
Capabilities        = Parameters | Geometry3D | Material | Quantity | Regenerate
PropertySchema      = FloorFinishSchema
QuantityRule        = Area + VolumeWhenThicknessKnown
DependencyRule      = HostRoomBoundary
DirtyPolicy         = HostGeometryDriven
Semantic            = IfcCovering / FLOORING-like metadata
```

### 8.3 Waterproofing

```text
Id                  = WATERPROOFING
Group               = ROOM_FINISH
Engine              = Surface
Host                = Room or selected face set
PrimaryCreateRecipe = FromSelectedRoom
AlternateRecipes    = PickFaces
Capabilities        = Parameters | Geometry3D | Material | Quantity | Regenerate
PropertySchema      = WaterproofingSchema
QuantityRule        = Area (+ optional upturn length/area rules)
DependencyRule      = HostRoomOrFace
DirtyPolicy         = HostGeometryDriven
Semantic            = IfcCovering / MEMBRANE-like metadata
```

### 8.4 Skirting

```text
Id                  = SKIRTING
Group               = ROOM_FINISH
Engine              = LinearPath
Host                = Room
PrimaryCreateRecipe = FromRoomPerimeter
AlternateRecipes    = PickEdges
Capabilities        = Parameters | Geometry3D | Material | Quantity | Regenerate
PropertySchema      = SkirtingSchema
QuantityRule        = NetLength (+ optional volume)
DependencyRule      = RoomPerimeterAndOpenings
DirtyPolicy         = HostGeometryDriven
Semantic            = IfcCovering / SKIRTINGBOARD-like metadata
```

## 9. Dependency, dirty and regenerate contract

A dependency-aware model is required to avoid the failure mode where the UI says nothing is dirty even though a host changed.

Recommended instance state:

```text
Clean
Dirty
Invalid
MissingHost
Regenerating
```

Example:

```text
Room boundary edited
   -> Room revision changes
   -> dependent Floor Finish / Waterproofing / Skirting compare host revision
   -> affected dependents become Dirty
   -> UI shows a compact Dirty badge + Regenerate action
   -> user can regenerate one feature or a safe batch
```

Rules:

1. Never silently claim `Clean` merely because no current in-memory flag was set.
2. Store enough dependency/revision/provenance information to recompute dirty state deterministically.
3. Expensive geometry should not auto-regenerate invisibly on every small edit unless the recipe is explicitly designed and tested for that behavior.
4. Regenerate should preserve stable semantic identity where possible; rebuilding geometry must not unnecessarily create a new business object identity.
5. Missing/invalid hosts must be surfaced clearly instead of producing blank property/detail panes.

## 10. UX: easy operation despite many business features

The user should not have to understand the architecture. Complexity belongs behind the registry.

### 10.1 Stable screen grammar

Use consistent zones:

```text
LEFT     = find/select feature
CENTER   = instances + primary/contextual actions
RIGHT    = properties/detail of current selection
```

Do not move the meaning of these zones per category unless a special workflow truly requires it.

### 10.2 One primary action

For each leaf, expose one visually primary action. In most creation-oriented features that is `+ Add`.

Alternate modes go behind the adjacent dropdown. Contextual secondary actions are capability-driven.

### 10.3 1–2 click core loop

Target the existing QS3D/BLT3D benchmark principle: the common action for a known feature should normally complete or start within one or two clicks after the feature is selected.

Examples:

```text
Phòng -> + Add
Sàn Hoàn Thiện -> + Add (from selected room)
Chân Tường -> + Add (from selected room perimeter)
```

Do not count unavoidable CAD picking in the drawing as extra UI navigation complexity.

### 10.4 Progressive disclosure

Default surface shows only the actions relevant to the current feature/selection. Advanced settings, alternate recipes and uncommon repair tools stay in dropdowns/expanders/context menus.

### 10.5 Discoverability for a long menu

As the taxonomy grows, add these as navigation projections over the same registry:

- search by display name/alias/keyword;
- Favorites;
- Recent features;
- optionally `Used in current project`;
- keyboard focus/search shortcut;
- preserve expanded groups and last selection.

Do **not** create a second independent feature list for search/favorites; all projections resolve the same `FeatureId`.

### 10.6 Empty/error states

Every generic pane needs an actionable empty state:

```text
No Room yet       -> + Add Room
No host selected  -> Select a Room, then Add Floor Finish
Dirty             -> Regenerate
Missing host      -> Rebind host / inspect source
Unsupported       -> explain capability boundary; do not show a blank pane
```

### 10.7 Naming and language

Use stable internal IDs and localization keys. Do not use visible Vietnamese strings as the semantic dispatch key. Visible labels can change/localize without breaking behavior.

## 11. What must NOT be done

1. Do not add a giant `switch (category)` to every Workspace action.
2. Do not make every menu leaf a new WPF partial class.
3. Do not use button/header text lookup as the long-term business dispatch mechanism.
4. Do not let `TreeViewItem.Tag` strings become the only domain registry.
5. Do not clone the property renderer per feature when a schema can drive it.
6. Do not show `Tham số / 3D` to every feature just because some features need both modes.
7. Do not migrate all categories in one PR.
8. Do not change active lane #3095 or #3107 from a new migration task.

## 12. Migration strategy

Use strangler-style migration: keep the current Workspace working while registry-backed behavior is introduced behind stable adapters.

### Phase A — foundation

- add feature identity/descriptor/registry contracts;
- register existing categories without changing user-visible behavior;
- add completeness tests proving stable unique IDs and valid descriptors.

### Phase B — navigation projection

- build the current left tree from registry navigation metadata;
- preserve existing labels/order and `ElementCategory` behavior during transition;
- remove hard-coded duplicate taxonomy only after parity tests pass.

### Phase C — generic action surface

- capability-driven action visibility;
- primary/alternate create recipe dispatch;
- generic empty/error states.

### Phase D — pilot vertical slices

Migrate in this order after active lanes land:

1. Room (Spatial);
2. Floor Finish + Waterproofing (Surface);
3. Skirting (Linear/Path).

Do not continue to all categories until these pilots prove that the abstractions reduce special cases rather than merely moving them.

### Phase E — scale-out

Migrate remaining categories engine-by-engine and add search/favorites/recent projections.

## 13. Definition of Done for a migrated feature

A feature is `MIGRATED` only when all applicable items are true:

- stable `FeatureId` is registered exactly once;
- navigation is generated/resolved from registry metadata;
- primary and alternate creation recipes are explicit;
- capabilities determine actions without category-specific UI branching;
- non-empty property schema exists when properties apply;
- quantity rule is explicit when quantity applies;
- host/dependency behavior is explicit when the feature is hosted/derived;
- dirty/regeneration behavior is testable;
- empty/error states are non-blank and actionable;
- localization does not change semantic dispatch;
- current project persistence/compatibility is preserved or migrated intentionally;
- deterministic unit/smoke/source guards cover the feature contract;
- protected CI is green on the exact merge candidate.

## 14. Agent self-selection protocol

This table is a **planning backlog, not a reservation**. A `READY` row has no owner until an agent claims it under repository policy.

Before working a row, an AI agent must:

1. read `AGENTS.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, `docs/AGENT-WORK-REGISTRATION.md`, `docs/AGENT-LANE-LOCK.md`, and this note;
2. refresh exact current `main`;
3. perform the minimum collision search for the Task-Key, files/symbols and equivalent active Issue/carrier;
4. if an equivalent active carrier exists, stop and choose another `READY` row;
5. create one dedicated GitHub Issue for the chosen Task-Key (unless an owner-created issue already uniquely owns it), then use `issue-<number>` as the repository Lane-Key;
6. set the Issue to `Status: ACTIVE`, record owner/session, exact baseline, scope/exclusions, expected files/tests and the one canonical branch;
7. create one `agent/**` branch and follow the branch-CI-before-PR rule where applicable;
8. never claim two dependent rows in one oversized carrier merely to skip the dependency graph;
9. do not edit a sibling agent's branch/PR; consume only work that has landed on current `main` unless the owner explicitly authorizes coordination;
10. stop before merge unless merge authority is explicit for that lane.

### Avoiding central-backlog edit races

Agents **do not need to edit this Markdown merely to claim a task**. The GitHub Issue is the live reservation. Include `Task-Key: FM-xxx` in the Issue title/body so another agent can discover the claim with a minimal Issue search.

A periodic coordinator/docs lane may update the status table after work lands. This keeps multiple agents from creating unnecessary merge conflicts in this file.

## 15. Agent-ready task backlog

Status meanings:

- `READY` — safe to evaluate/claim after collision check;
- `BLOCKED` — dependency must first land on `main`;
- `ACTIVE-EXTERNAL` — an already-owned Issue is working that prerequisite; do not take it over;
- `DONE` — landed on `main` and acceptance criteria met.

| Task-Key | Status | Priority | Scope | Depends on | Primary conflict surface | Acceptance summary |
|---|---|---:|---|---|---|---|
| FM-001 | READY | P0 | Feature identity + descriptor + registry foundation | none | new Core/Application registry files; minimal category bridge | unique stable IDs; descriptor validation; registry enumeration/completeness tests; no user-visible behavior change |
| FM-002 | BLOCKED | P0 | Registry-backed left navigation projection | FM-001 | `ReferenceWorkspaceTreeAugmenter`, tree binding/selection adapter | current taxonomy/order preserved; visible text no longer semantic source of truth; all leaves resolve stable FeatureId |
| FM-003 | BLOCKED | P0 | Capability-driven generic Workspace action surface | FM-001 | Workspace action/presentation layer | actions derive from capabilities; one primary action; irrelevant actions hidden; no giant category switch |
| FM-004 | BLOCKED | P0 | Create recipe dispatch + primary/alternate Add UX | FM-001, FM-003 | `OnAddClick` bridge / creation service | primary recipe runs directly; alternate modes are contextual; transactional/error behavior preserved |
| FM-005 | BLOCKED | P0 | Dependency + revision + dirty/regeneration domain contract | FM-001 | Core domain/persistence; regeneration service | deterministic Clean/Dirty/Invalid/MissingHost state; persisted/recomputable provenance/revision strategy; unit tests |
| FM-006 | BLOCKED | P0 | Room Spatial vertical-slice migration | FM-001..FM-005 + #3095 landed | Room-specific Workspace/runtime files | preserve #3095 direct Add/pane UX; Room uses registry/recipe/capabilities; dependent feature hooks exposed; no regression |
| FM-007 | BLOCKED | P1 | Floor Finish + Waterproofing Surface vertical slices | FM-001..FM-005 + #3107 landed | finish creation/geometry/quantity; property schema bridge | room-hosted primary creation; material/properties; area/volume rules explicit; dirty/regenerate from host |
| FM-008 | BLOCKED | P1 | Skirting Linear/Path vertical slice | FM-001..FM-005 + #3107 landed | path/perimeter builder + quantity | room-perimeter recipe; opening/exclusion policy explicit; length quantity; dirty/regenerate from host |
| FM-009 | BLOCKED | P1 | Search + Favorites + Recent feature navigation | FM-002, FM-003 | navigation UI/state persistence | all projections resolve same FeatureId; keyboard-search path; no duplicate taxonomy source |
| FM-010 | BLOCKED | P1 | Registry/migration completeness and contract guards | FM-006..FM-008 | tests/scripts/CI guard only where justified | pilots cannot silently fall back to legacy empty action/schema paths; descriptor and navigation completeness proven |
| FM-011 | BLOCKED | P2 | Semantic mapping metadata catalog (IFC-aligned where useful) | FM-001 | metadata/domain mapping; no importer/exporter promise | Room/Grid/Covering/Railing/etc. mappings are metadata with explicit confidence/scope; no unsupported IFC interoperability claim |
| FM-012 | BLOCKED | P2 | Migrate remaining menu categories engine-by-engine | FM-006..FM-010 | category-specific builders/services | batches are small/non-overlapping; each migrated leaf meets Section 13; no one-PR whole-menu rewrite |

### Existing prerequisite lanes — do not self-claim

| Existing Issue | State at architecture baseline | Why it matters |
|---|---|---|
| #3095 | ACTIVE-EXTERNAL | owns Room direct Add and Room detail-pane runtime behavior that FM-006 must preserve after it lands |
| #3107 | ACTIVE-EXTERNAL | owns concrete non-empty quick/property schemas for many categories that FM-007/FM-008 must consume after it lands |

## 16. Suggested parallelization

The dependency graph deliberately prevents ten agents from all editing `WorkspacePanel.xaml.cs` simultaneously.

```text
FM-001 registry foundation
   |-----------> FM-002 navigation
   |-----------> FM-003 generic actions -> FM-004 create recipes
   |-----------> FM-005 dirty/dependency

#3095 ---------------------------------------> FM-006 Room pilot
#3107 --------------------------+------------> FM-007 Surface pilot
                                 +------------> FM-008 Linear pilot

FM-002 + FM-003 ----------------------------> FM-009 discoverability
FM-006 + FM-007 + FM-008 -------------------> FM-010 completeness
FM-001 --------------------------------------> FM-011 semantic metadata
pilots + guards -----------------------------> FM-012 scale-out
```

When FM-001 lands, FM-002, FM-003, FM-005 and FM-011 can often be claimed by separate agents if their concrete file scopes remain non-overlapping. FM-004 should avoid landing on a stale generic-action contract and therefore follows FM-003.

## 17. Testing strategy

Prefer tests at the lowest stable layer:

### Registry tests

- IDs unique;
- group/order deterministic;
- primary recipe is included in recipe set;
- required property/quantity/dependency IDs resolve;
- capability combinations validated;
- localization labels do not affect IDs.

### Recipe tests

- explicit preconditions;
- safe default path;
- deterministic error result;
- transaction boundary and no partial mutation on failure;
- created object is selected/active when UX contract requires it.

### Dirty/dependency tests

- host revision change marks only actual dependents dirty;
- unchanged host stays clean;
- missing host is not reported clean;
- regeneration returns to clean only after successful rebuild.

### Workspace smoke tests

- every visible leaf resolves a FeatureId;
- capabilities produce expected action set;
- unsupported/missing state is actionable rather than blank;
- core 1–2 click paths remain reachable.

Source guards may supplement tests for fragile WPF integration contracts, but they should not replace domain tests when behavior can be tested normally.

## 18. Semantic/IFC alignment is metadata, not UI coupling

Useful conceptual mappings discovered during public standards research include:

| QS3D concept | Useful IFC semantic analogue |
|---|---|
| Grid | `IfcGrid` / `IfcGridAxis` |
| Room | `IfcSpace` |
| Floor Finish | `IfcCovering` / `FLOORING` |
| Waterproofing | `IfcCovering` / `MEMBRANE` |
| Skirting | `IfcCovering` / `SKIRTINGBOARD` |
| Wall Finish | `IfcCovering` / `CLADDING` |
| Ceiling Finish | `IfcCovering` / `CEILING` |
| Railing | `IfcRailing` |

These are useful for stable vocabulary and future interoperability design. They do **not** mean that declaring a mapping automatically implements IFC import/export or proves complete schema compatibility.

Primary public standards references used for this architectural vocabulary:

- buildingSMART IFC 4.3 documentation: <https://ifc43-docs.standards.buildingsmart.org/>
- buildingSMART bSDD data structure: <https://technical.buildingsmart.org/services/bsdd/data-structure/>
- buildingSMART IDS: <https://www.buildingsmart.org/standards/bsi-standards/information-delivery-specification-ids/>
- ISO 12006-2 classification framework overview: <https://www.iso.org/standard/61753.html>

## 19. Architecture decision summary

For QS3D, the scalable answer to a large left menu is **not fewer business features** and not one hard-coded Workspace implementation per feature. The scalable answer is:

```text
one stable FeatureId per business leaf
+ one central registry
+ a small set of reusable engines
+ explicit recipes/schemas/rules
+ capability-driven UI
+ dependency/dirty semantics
+ multiple navigation projections over the same registry
```

That lets QS3D add many specialized quantity/BIM workflows while keeping the user interaction consistent and keeping implementation work divisible into collision-resistant agent lanes.
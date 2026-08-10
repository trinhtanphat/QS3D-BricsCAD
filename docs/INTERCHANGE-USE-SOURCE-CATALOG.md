# Interchange UseSource Catalog — guarded replacement boundary

`QS3DINTERCHANGEUSESOURCECATALOG` applies source semantic definitions for colliding Zone, Floor and Family IDs while keeping existing Element collisions target-authoritative.

This is deliberately separate from `QS3DINTERCHANGEUSESOURCE`, which replaces same-category Element portable semantic state while keeping catalog collisions on the target.

## Policy

- Zone collision: **UseSourceSemanticData**.
- Floor collision: **UseSourceSemanticData**.
- Family collision: **UseSourceSemanticData**, only when source and target Family categories match.
- Element collision: **KeepTarget**.
- New non-colliding Zone/Floor/Family/Element identities may be appended.
- Incoming source CAD handles: **Discard** as target ownership.
- Generated outputs affected by catalog replacement: clear ownership safely and require explicit rebuild.

Name collisions owned by a different semantic ID and category-incompatible Family/Element collisions remain fail-closed because rename/remap policy is not defined.

## What catalog replacement changes

For a planner-approved colliding identity:

- Zone: source name replaces target name.
- Floor: source name and `ElevationM` replace target values.
- Family: source name and portable Family properties replace target values; category remains the already-validated same category.

Family properties are not replaced with a raw `Properties.Clear()`. The adapter routes replacement through `ProjectFamilyService` semantics:

- a Family property removed by the source is removed from member Elements only when their instance value still equals the previous inherited Family value;
- a changed/new Family property propagates to member Elements that were inheriting the old/default value;
- true Element-level overrides remain untouched;
- removed inherited values are processed before new values are propagated, while the previous Family values are still available for inheritance detection.

All existing members of a replaced Family are already in the generated-output invalidation closure before those semantic updates occur.

Existing Element collisions are not edited by this policy. Their source handles, drawing fingerprint and portable semantic state remain target-owned, except for inheritance-aware Family defaults that they had not overridden.

## Affected generated-output closure

Before changing catalog definitions, the adapter finds existing target elements that reference any replaced:

- `ZoneId`;
- `FloorId`;
- `FamilyId`.

The closure then expands through existing semantic dependents and linked Door/WallOpening hosts. A newly appended Door/WallOpening also invalidates an existing referenced host when applicable.

This is necessary even for data that can look like metadata: a changed Floor elevation or Family parameter can invalidate generated 3D/rebar/curtain/opening results.

## Cross-layer transaction

The guarded apply path:

1. strict-validates and policy-plans the snapshot;
2. computes the affected closure from the pre-import target state;
3. captures `ProjectStateSnapshot`;
4. opens the BricsCAD CAD transaction;
5. prepares `GeneratedDependentGeometryInvalidator` while old target ownership metadata is still available;
6. applies catalog add/replacement with inheritance-aware Family propagation and appends planner-approved new Elements;
7. marks the affected existing closure dirty;
8. clears invalidated generated owner metadata;
9. re-validates the combined semantic project/dependency graph;
10. records audit/provenance summary and touches the project;
11. commits the CAD transaction.

A pre-commit failure aborts the native transaction and restores the semantic snapshot, including Family definitions and propagated member values. Post-commit UI refresh is best-effort only.

## Rebuild remains explicit

Catalog replacement does not automatically call `QS3DBUILD3D`, opening boolean cuts, rebar generators, curtain generators, Grid generators or project save.

Run `QS3DHEALTHALL`, inspect affected elements, then execute only the required rebuild/cut workflow.

## Still open

This slice does not implement rename/remap, merge precedence between individual catalog properties, automatic target source-handle rebinding, automatic physical rebuild, or V25 runtime qualification. It also does not combine imported source-handle provenance with semantic mutation; provenance retention remains the separate `QS3DINTERCHANGEPROVENANCE` authorization path.

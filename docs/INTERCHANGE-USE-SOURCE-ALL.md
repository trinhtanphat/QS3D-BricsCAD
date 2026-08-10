# Interchange UseSource ALL — one-transaction replacement boundary

`QS3DINTERCHANGEUSESOURCEALL` is the combined executable `UseSourceSemanticData` path for compatible Zone, Floor, Family and Element collisions.

It is **not** implemented by calling `QS3DINTERCHANGEUSESOURCECATALOG` and `QS3DINTERCHANGEUSESOURCE` one after the other. Those specialist paths each own a transaction; sequencing them would create a partial-commit window if the second mutation failed.

## Policy

- Zone collision: **UseSourceSemanticData**.
- Floor collision: **UseSourceSemanticData**.
- Family collision: **UseSourceSemanticData**, only for planner-compatible same-category Family identities.
- Element collision: **UseSourceSemanticData**, only for planner-compatible same-category Element identities.
- New non-colliding semantic identities may be appended.
- Incoming source CAD handles: **Discard** as target ownership.
- Generated outputs: **ClearOwnershipAndRequireRebuild** for the affected target closure.

Name conflicts belonging to different semantic identities and category-incompatible collisions remain fail-closed. ALL is not an override for unresolved rename/remap policy.

## Union invalidation closure

The closure is computed from the pre-import target state and includes:

1. every target Element whose semantic state is being replaced;
2. every existing target Element referencing a replaced Zone, Floor or Family;
3. transitive semantic dependents of those Elements;
4. old linked hosts of affected Door/WallOpening Elements;
5. existing target hosts referenced by accepted incoming Door/WallOpening semantic state.

That union is passed once to `GeneratedDependentGeometryInvalidator.Prepare(...)` before catalog or element mutation begins.

## Family inheritance during ALL replacement

Family definitions are applied through `ProjectFamilyService` instead of raw dictionary replacement.

For an existing replaced Family:

- source-removed properties are removed first while the old Family value is still available;
- a member Element loses that property only if its instance value still equals the previous inherited Family value;
- changed/new source Family properties propagate only to Elements that still inherit the Family value/default;
- true Element-level overrides remain preserved at this stage.

After catalog propagation, an Element that is itself selected for `UseSourceSemanticData` receives its complete source portable Element properties in the same transaction. Target-only Elements therefore retain their genuine overrides, while replaced Elements intentionally receive the source Element state.

Every existing member of a replaced Family is included in the union invalidation closure before this propagation occurs.

## One transaction

The apply order is intentionally strict:

1. strict snapshot validation and all-scope collision planning;
2. union affected-closure calculation from the old target semantic graph;
3. one `ProjectStateSnapshot.Capture(project)`;
4. one BricsCAD document lock and one native CAD transaction;
5. one generated-output invalidation preparation using old target ownership metadata;
6. apply all planner-approved Zone/Floor/Family source definitions using inheritance-aware Family services;
7. apply all planner-approved Element portable semantic state;
8. preserve existing target Element `SourceHandles` and drawing fingerprint for replaced Elements;
9. mark the affected existing closure dirty;
10. clear invalidated generated ownership metadata while native rollback is still possible;
11. revalidate combined semantic identity/reference/dependency state;
12. record import metadata/audit and touch the project;
13. commit the single native transaction.

If anything fails before native commit, the CAD transaction aborts and the captured semantic project state is restored, including any Family propagation already performed inside the semantic phase.

## Source CAD ownership remains target-local

Even in ALL mode, incoming `sourceHandles` and source drawing fingerprints do not become authority in the active target DWG.

For an existing replaced Element:

- stable target ID remains the same;
- category must already be compatible;
- target `SourceHandles` stay unchanged;
- target element `DrawingFingerprint` stays unchanged;
- portable Family/Floor/Zone references, dependencies, properties and quantities come from the source snapshot.

For a newly appended Element, incoming source handles are discarded and no target drawing fingerprint is invented.

## Explicit rebuild boundary

ALL can remove invalidated generated outputs, but it does not automatically regenerate them. It does not call `QS3DBUILD3D`, opening boolean cuts, rebar generators, curtain generators, Grid generators or project save.

After a successful import, run `QS3DHEALTHALL`, inspect semantic/source ownership, then explicitly rebuild only the outputs that should exist.

## Relationship to partial policies

The partial commands remain valid and useful:

- `QS3DINTERCHANGEUSESOURCE` changes Element collisions while keeping catalog collisions target-authoritative.
- `QS3DINTERCHANGEUSESOURCECATALOG` changes catalog collisions while keeping Element collisions target-authoritative.
- `QS3DINTERCHANGEUSESOURCEALL` changes both scopes atomically when the all-scope planner accepts the snapshot.

`QS3DINTERCHANGEIMPORT` can route to these policies explicitly; it never simulates ALL by sequentially running the two partial paths.

## Still not claimed

This source implementation does not close the interoperability epic. Rename/remap, field-level merge precedence, source-handle rebinding, provenance+semantic combined authorization, automatic physical rebuild/cut, IFC/Revit/BCF/vendor/cloud formats, save/reopen/undo qualification and exact-SHA licensed BricsCAD V25 runtime qualification remain separate work.

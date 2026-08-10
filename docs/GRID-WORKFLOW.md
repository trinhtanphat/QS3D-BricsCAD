# QS3D Grid / Trục reference workflow

Updated: 2026-08-10 (UTC+7)

## Current commands

`QS3DGRID`

Captures selected BricsCAD `LINE` or `ARC` geometry as `ElementCategory.Grid` semantic references.

`QS3DGRIDNUMBER`

Assigns deterministic semantic Grid labels to already tracked Grid sources using the user's **explicit click order**. The command does not infer spatial order from CAD geometry.

`QS3DGRIDANNOTATE`

Replaces owned native endpoint extension/bubble/text annotation for selected already-captured Grid sources.

`QS3DGRIDANNOTATEALL`

Replaces owned native annotation for every semantic Grid that already has a non-empty `GridLabel`.

The Project Ribbon exposes the full guarded workflow: **Grid / Trục → Đánh số Grid → Gắn nhãn Grid / Gắn nhãn tất cả Grid**.

## Source contract

- `QS3DGRID` selection must contain only `LINE` / `ARC` sources;
- every source must expose a finite positive curve length;
- malformed/unsupported selection fails before semantic mutation;
- capture reuses `SemanticCaptureService`, including generated-output rejection, collision checks and project-state rollback;
- Grid uses the existing `GenericTakeoffRegenerator` and therefore carries semantic `LengthM` and `Count` quantities;
- the original DWG entity remains the source of truth and keeps its stable drawing-local CAD Handle provenance;
- Grid does not create or claim a generated 3D structural host solid.

## Semantic Grid label sequencing

`GridNamingService` provides the CAD-independent, fail-closed naming contract over the **existing** `ElementCategory.Grid` elements. It does not create another Grid catalog.

Current semantics:

- caller supplies an explicit ordered list of Grid element IDs; Core does not pretend to infer spatial Grid order from CAD geometry;
- numeric sequences support prefix/suffix, start index and bounded zero-padding;
- alphabetic sequences use deterministic `A..Z, AA..AZ, BA...` numbering;
- labels are stored on the Grid semantic element as `GridLabel`; the resolved sequence index is stored as `GridSequenceIndex`;
- label uniqueness is case-insensitive across other Grid elements in the same project;
- the whole batch is validated before any label mutation, so a missing/non-Grid element, duplicate ID, duplicate external label or invalid sequence blocks the whole renumber operation;
- source geometry, source Handles and native CAD ownership are untouched by semantic numbering.

### `QS3DGRIDNUMBER` V25 interaction layer

The V25 adapter supplies the reviewed ordering explicitly instead of relying on selection-set order:

1. run `QS3DGRIDNUMBER`;
2. click each tracked Grid source one by one in the exact order that should receive labels;
3. press Enter after the final Grid;
4. choose `Numeric` or `Alphabetic`;
5. choose the start index; Numeric mode can also use zero-padding;
6. optionally enter prefix/suffix;
7. the adapter delegates the complete batch to `GridNamingService.Renumber(...)` only after input collection has finished.

The command accepts only CAD sources already tracked by a semantic Grid. Unknown CAD is not silently converted into a Grid; use `QS3DGRID` first. Duplicate picks are ignored with guidance, ambiguous semantic ownership fails closed, and the semantic mutation is wrapped in a project snapshot so command failure restores pre-command project state. Post-success Palette/status refresh is best-effort and cannot turn a successful semantic renumber into a false operation failure.

`QS3DGRIDNUMBER` remains semantic-only: it does not move/rotate source CAD, create dimensions, or infer left-to-right / bottom-to-top / radial ordering. The explicit click order is the authoritative ordering supplied by the user.

## Native Grid annotation

`GridAnnotationBuilder` provides source-implemented native endpoint annotation with explicit generated ownership:

- each endpoint receives an extension line, `Circle` bubble and centered `DBText` label;
- generated entities carry QS3D XData for project/element/category ownership;
- handles are stored in `GeneratedGridAnnotationHandles`;
- old live entities are erased only after `GeneratedGeometryService.RequireMatchingOwnership(...)` confirms the same project/Grid owner;
- the whole batch shares one native transaction plus a `ProjectStateSnapshot`; pre-commit native failure rolls CAD back and restores semantic metadata;
- post-commit `Editor.Regen()` / Palette refresh remains best-effort;
- `GeneratedGridAnnotationHealthService` checks persisted handle/label/owner/version/sizing consistency through comprehensive Health.

Geometry is plane-aware and fail-closed:

- planar LINE endpoints at the same WCS Z elevation use WCS Z as annotation normal;
- ARC bubble/text uses the ARC native plane normal;
- a 3D-sloped LINE is rejected because a lone LINE does not define a stable annotation plane; the builder does not silently project it to WCS-XY.

This is **source implementation, not V25 runtime certification**. Exact text alignment/readability, Undo/Redo, save/reopen, multi-DWG behavior and ownership replacement still require the local matrix in `docs/GRID-NATIVE-ANNOTATION.md`.

## Grid naming and generated-annotation health

`GridNamingHealthService` is included in `ComprehensiveModelHealthService` and reports semantic naming corruption:

- `GRID_LABEL_DUPLICATE` — Error on both Grid owners when labels collide case-insensitively;
- `GRID_LABEL_EMPTY` — Warning when a Grid explicitly carries an empty label property;
- `GRID_LABEL_TOO_LONG` — Error when external/manual mutation exceeds the 64-character semantic naming bound;
- `GRID_SEQUENCE_INVALID` — Error when `GridSequenceIndex` is not an integer in the supported range;
- `GRID_SEQUENCE_WITHOUT_LABEL` — Warning when a sequence index exists without a valid semantic label.

`GeneratedGridAnnotationHealthService` separately reports persisted generated-output corruption/staleness such as malformed/duplicate generated handles, owner mismatch, stale built labels and invalid bubble/text sizing. Core health intentionally does not pretend it can prove a particular DBText/Circle is live in a real DWG; live type/XData proof belongs to V25 runtime qualification.

A missing semantic Grid label is not itself an error because capture and naming are separate workflows. Health does not invent labels or mutate the model.

## Finite Grid intersections

`GridIntersectionPlanner` provides a bounded CAD-independent contract for finite `LINE`/`ARC` Grid references:

- LINE × LINE;
- LINE × ARC;
- ARC × ARC;
- duplicate semantic IDs, invalid/degenerate geometry, overlapping collinear LINEs, coincident ARC support circles and bounded-count overflow fail closed.

This planner reports intersection geometry only. Native extraction from V25 sources, intersection markers, constraints, dimensions and automatic structural hosting/snapping remain separate work.

## Product boundary

Current Grid source still does **not** mean the following are complete:

- automatic CAD spatial ordering for renumbering;
- rectangular/radial Grid-system authoring/discovery;
- Grid intersection constraints or associative dimensions;
- automatic native Grid intersection markers;
- Direct Draw Grid with transient jig/repeat authoring;
- automatic snapping/hosting of structure to Grid intersections;
- paper-space viewport annotation lifecycle;
- native Level heads/elevation symbols.

Those features must extend the existing `ElementCategory.Grid` semantic model rather than adding a competing Grid store. Runtime-sensitive visualization/editor behavior remains subject to `docs/REMOTE-AGENT-SCOPE.md` and local qualification.

## Floor / Level relation — current source

QS3D reuses the existing `ProjectFloor` / `FloorDefinition` catalog as its Level model; do not introduce a duplicate `LevelDefinition` merely for product parity.

Current Core supports opt-in vertical references:

- `BottomLevelId`
- `BottomLevelOffsetM`
- `TopLevelId`
- `TopLevelOffsetM`

`ProjectFloorService` owns Level reference assignment/lifecycle and `ElementVerticalPlacementService` defines the legacy-compatible bottom/top/effective-height resolution contract. `LevelReferenceHealthService` participates in Health All and Release Check.

This semantic Level contract is source-implemented, but native host/opening/curtain/rebar placement and Floor/Level assignment UI are intentionally not exposed as complete until all dependent native systems consume the same resolver. `LevelReferenceNativeIntegrationPolicy` keeps release fail-closed in the meantime. Exact integration/runtime proof remains in the local handoff.

## Native source-edit relation

If a tracked Grid `LINE`/`ARC` is edited with native BricsCAD tools, the authoritative-source reconcile path is `QS3DSYNCSOURCE`. The command refreshes tracked source-derived semantic state through the guarded source-reconcile contract. Generated Grid annotation may then be reported stale and should be explicitly rebuilt; source reconcile does not silently run destructive/native downstream rebuilds.

## Local V25 validation

A local-capable agent should include Grid in the exact-SHA runtime matrix:

1. capture one LINE and one ARC;
2. verify Grid semantic ownership and `LengthM`/`Count` after semantic regeneration;
3. edit/move a tracked Grid source and verify `QS3DSYNCSOURCE` preserves ownership while refreshing semantic state;
4. save/reopen and verify drawing-local source Handle provenance;
5. select generated/unrelated QS3D output and verify it cannot be recaptured as Grid;
6. select POLYLINE/Solid3d/text and verify the entire Grid capture operation fails before mutation;
7. test millimeter and metre drawings;
8. verify UI/selection sync and Locate behavior;
9. run `QS3DGRIDNUMBER`, verify Numeric/Alphabetic sequence, prefix/suffix/padding and duplicate-label fail-closed behavior;
10. verify cancelling during ordered picking/options leaves labels unchanged;
11. run `QS3DGRIDANNOTATE` and `QS3DGRIDANNOTATEALL`, repeat replacement and verify no duplicate native entities;
12. verify tilted ARC annotation stays on its native plane and 3D-sloped LINE fails with zero residue;
13. corrupt one generated ownership marker and verify replacement refuses to erase it;
14. verify save/reopen, Undo/Redo, multi-DWG isolation, Unicode labels and HiDPI visuals;
15. verify comprehensive Health reports malformed/duplicate naming and stale/corrupt generated annotation without mutating the model.

Until that runtime pass exists, describe Grid capture/naming/intersection/native-annotation code as source-implemented/statically guarded, not `LOCAL_PASS` or V25-runtime-certified.

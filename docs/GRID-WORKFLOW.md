# QS3D Grid / Trục reference workflow

Updated: 2026-08-10 (UTC+7)

## Current commands

`QS3DGRID`

Captures selected BricsCAD `LINE` or `ARC` geometry as `ElementCategory.Grid` semantic references.

`QS3DGRIDNUMBER`

Assigns deterministic semantic Grid labels to already tracked Grid sources using the user's **explicit click order**. The command does not infer spatial order from CAD geometry and does not create native Grid bubble/label drawing.

## Source contract

- `QS3DGRID` selection must contain only `LINE` / `ARC` sources;
- every source must expose a finite positive curve length;
- malformed/unsupported selection fails before semantic mutation;
- capture reuses `SemanticCaptureService`, including generated-output rejection, collision checks and project-state rollback;
- Grid uses the existing `GenericTakeoffRegenerator` and therefore carries semantic `LengthM` and `Count` quantities;
- the original DWG entity remains the source of truth and keeps its stable drawing-local CAD Handle provenance;
- Grid commands do not create or claim native Grid 3D geometry.

## Semantic Grid label sequencing

`GridNamingService` provides the CAD-independent, fail-closed naming contract over the **existing** `ElementCategory.Grid` elements. It does not create another Grid catalog.

Current semantics:

- caller supplies an explicit ordered list of Grid element IDs; Core does not pretend to infer spatial Grid order from CAD geometry;
- numeric sequences support prefix/suffix, start index and bounded zero-padding;
- alphabetic sequences use deterministic `A..Z, AA..AZ, BA...` numbering;
- labels are stored on the Grid semantic element as `GridLabel`; the resolved sequence index is stored as `GridSequenceIndex`;
- label uniqueness is case-insensitive across other Grid elements in the same project;
- the whole batch is validated before any label mutation, so a missing/non-Grid element, duplicate ID, duplicate external label or invalid sequence blocks the whole renumber operation;
- source geometry, source Handles and native CAD ownership are untouched.

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

`QS3DGRIDNUMBER` remains semantic-only: it does not move/rotate source CAD, generate Grid bubbles, create dimensions, or infer left-to-right / bottom-to-top / radial ordering. The explicit click order is the authoritative ordering supplied by the user.

## Grid naming health

`GridNamingHealthService` is included in `ComprehensiveModelHealthService`, so normal comprehensive Health/Release paths can surface semantic Grid naming corruption even before a native bubble/annotation layer exists.

Current checks:

- `GRID_LABEL_DUPLICATE` — Error on both Grid owners when labels collide case-insensitively;
- `GRID_LABEL_EMPTY` — Warning when a Grid explicitly carries an empty label property;
- `GRID_LABEL_TOO_LONG` — Error when external/manual mutation exceeds the 64-character semantic naming bound;
- `GRID_SEQUENCE_INVALID` — Error when `GridSequenceIndex` is not an integer in the supported range;
- `GRID_SEQUENCE_WITHOUT_LABEL` — Warning when a sequence index exists without a valid semantic label.

A missing label is not itself an error because `QS3DGRID` capture and semantic naming are separate workflows. **Health does not invent labels or mutate the model.** This health layer is Core/source functionality only; it does not imply native Grid annotation completion.

## Product boundary

The current Grid source does **not** yet mean the following are complete:

- native Grid bubble/label drawing and ownership/replacement lifecycle;
- automatic CAD spatial ordering for renumbering;
- rectangular/radial Grid systems;
- Grid intersection constraints;
- dimensions/annotations tied to Grid IDs;
- Direct Draw Grid with transient jig/repeat authoring;
- automatic snapping/hosting of structure to Grid intersections.

Those features must extend the existing `ElementCategory.Grid` semantic model rather than adding a competing Grid store. Source-only agents may advance deterministic Core/reference semantics; real bubbles, DrawJig/editor interaction, automatic spatial ordering and native visualization remain subject to the V25 runtime boundary in `docs/REMOTE-AGENT-SCOPE.md`.

## Floor / Level relation — current source

QS3D reuses the existing `ProjectFloor` / `FloorDefinition` catalog as its Level model; do not introduce a duplicate `LevelDefinition` merely for product parity.

Current Core supports opt-in vertical references:

- `BottomLevelId`
- `BottomLevelOffsetM`
- `TopLevelId`
- `TopLevelOffsetM`

`ProjectFloorService` owns Level reference assignment/lifecycle and `ElementVerticalPlacementService` defines the legacy-compatible bottom/top/effective-height resolution contract. `LevelReferenceHealthService` participates in Health All and Release Check.

This semantic Level contract is **source-implemented**, but native host/opening/curtain/rebar placement and Floor/Level assignment UI are intentionally not exposed as complete until all dependent native systems consume the same resolver. That integration is parked as P0 LOCAL_ONLY/runtime-sensitive work in `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

## Native source-edit relation

If a tracked Grid `LINE`/`ARC` is edited with native BricsCAD tools, the authoritative-source reconcile path is `QS3DSYNCSOURCE`. The command refreshes tracked source-derived semantic state through the guarded source-reconcile contract; it does not turn Grid into generated native 3D geometry or imply grip/jig parity.

## Local V25 validation

A local-capable agent should include Grid in the exact-SHA runtime matrix:

1. capture one LINE and one ARC;
2. verify Grid semantic ownership and `LengthM`/`Count` after `QS3DREGEN`;
3. edit/move a tracked Grid source and verify `QS3DSYNCSOURCE` preserves ownership while refreshing semantic state;
4. save/reopen and verify drawing-local source Handle provenance;
5. select generated/unrelated QS3D output and verify it cannot be recaptured as Grid;
6. select POLYLINE/Solid3d/text and verify the entire Grid capture operation fails before mutation;
7. test millimeter and metre drawings;
8. verify UI/selection sync and Locate behavior;
9. run `QS3DGRIDNUMBER`, click a reviewed Grid order, verify Numeric and Alphabetic sequences, prefix/suffix/padding, duplicate-label fail-closed behavior and save/reopen persistence;
10. verify cancelling during ordered picking/options leaves labels unchanged;
11. verify the command does not create native Grid bubbles or mutate source CAD;
12. verify `QS3DHEALTH` / comprehensive health reports malformed or duplicate Grid labels without changing them.

Until that runtime pass exists, describe `QS3DGRID`, `QS3DGRIDNUMBER`, `GridNamingService` and `GridNamingHealthService` as source-implemented/statically guarded, not `LOCAL_PASS` or V25-runtime-certified. Native Grid bubble/label drawing, automatic spatial ordering and Grid constraint systems remain separate product work.

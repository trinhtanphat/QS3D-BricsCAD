# LOCAL-005 Native Multi-Region Reinforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the existing Core multi-region polygon reinforcement planner to BricsCAD-native Slab/Foundation source association, ownership, atomic reconcile, materialization, and Health.

**Architecture:** Keep one semantic Slab/Foundation owner and reuse `PolygonalSlabMultiRegionMeshPlanner` as the only bar planner. Add a deterministic flat-loop-to-region assembler in Core, a bounded native closed-polyline reader, region-scoped generated ownership/metadata, and a dedicated native multi-region builder/Health path while preserving existing rectangle/single-polygon builders.

**Tech Stack:** C#/.NET 8 Core, .NET Framework 4.8 BricsCAD V25 adapter, Teigha/BricsCAD V25 managed API, Python source preflight guards.

**Spec:** `docs/superpowers/specs/2026-08-24-local005-native-multiregion.md`

## Global Constraints

- No direct writes to `main`; use canonical branch `agent/chatgpt/local005-multiregion-20260824` and Lane-Key `issue-3647`.
- Reuse `PolygonalSlabMultiRegionMeshPlanner`; do not introduce another reinforcement planning engine.
- Destructive reconciliation must validate complete ownership first and fail closed on partial, stale, duplicate, reused, or mixed ownership.
- One CAD transaction plus `ProjectStateSnapshot` per semantic element replacement; no partial source or generated-region commit.
- Preserve existing rectangle/single-polygon compatibility and aggregate generated-handle slots.
- No inferred anchorage/lap/hook/bend/detailing rules.
- Licensed V25 save/reopen, Undo/Redo, multi-DWG, and visual geometry proof remains `LOCAL_ONLY/PENDING_LOCAL`.
- New V25 source must remain covered by the V26 linked-source project.

---

### Task 1: Deterministic flat-loop region assembly

**Files:**
- Create: `src/QS3D.Core/Geometry/PolygonSourceLoopRegionAssembler.cs`
- Modify: `tests/QS3D.Core.SmokeTests/Program.cs`

**Interfaces:**
- Consumes: `PolygonRegionScanlineClipper.CreateRegion`, `PolygonRegionScanlineClipper.ContainsPoint`, `PolygonRegionSetTopology.NormalizeAndValidate`.
- Produces: `PolygonSourceLoop2`, `PolygonSourceRegion2`, `PolygonSourceLoopRegionAssembler.Assemble(IReadOnlyList<PolygonSourceLoop2>)`.

- [ ] Write deterministic smoke assertions for two disconnected islands, one outer plus one hole, reorder stability, duplicate source ids, deeper nesting, touching/overlap rejection.
- [ ] Run Core smoke tests and confirm RED because the assembler types do not exist.
- [ ] Implement bounded loop validation, canonical source ids, containment-depth grouping, stable `RegionId` derived from canonical outer source identity, final topology validation, and deterministic region/hole ordering.
- [ ] Run Core smoke tests and confirm GREEN.
- [ ] Commit the Core assembler and smoke coverage.

### Task 2: Native closed-polyline extraction

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Cad/ClosedPolygonSourceLoopReader.cs`
- Modify: `scripts/preflight-local005-native-multiregion.py`

**Interfaces:**
- Consumes: `Polyline`, Core `BulgedPolygonFootprintTessellator`, `CadUnitService`.
- Produces: a bounded reader result containing source handle, tessellated metre-space loop, drawing elevation, and deterministic geometry fingerprint.

- [ ] Add static RED assertions requiring a dedicated closed-loop reader, closed-polyline guard, bulge tessellation, OCS/WCS transform, horizontal-plane guard, finite/bounded point counts.
- [ ] Confirm feature preflight fails before implementation.
- [ ] Implement closed-loop extraction without weakening `CadPolylinePathReader`; allow straight and bulged horizontal loops, transform supported OCS to WCS, reject tilted/invalid/oversized geometry.
- [ ] Confirm static guard and V25 compile contract are GREEN.
- [ ] Commit reader implementation.

### Task 3: Region-scoped generated ownership

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Cad/GeneratedRebarRegionOwnershipService.cs`
- Create: `src/QS3D.BricsCAD.V25/Cad/MultiRegionRebarManifest.cs`
- Modify: `scripts/preflight-local005-native-multiregion.py`

**Interfaces:**
- Consumes: `GeneratedRebarNativeOwnershipService`, `GeneratedOwnershipIdentityToken`, `GeneratedHandleOwnershipPolicy`, CAD XData.
- Produces: `QS3D_REBAR_REGION` marker; deterministic parse/serialize helpers for source and generated manifests.

- [ ] Add RED contract requiring project/element/owner-slot/RegionId provenance and bounded deterministic manifests.
- [ ] Implement XData mark/match/require operations without changing legacy `QS3D_REBAR` marker format.
- [ ] Implement manifest parsing with duplicate ids/handles rejected and deterministic serialization.
- [ ] Run feature guard and compile contract.
- [ ] Commit ownership/manifest layer.

### Task 4: Atomic Slab/Foundation multi-region materialization

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Cad/SlabFoundationMultiRegionMeshSolidBuilder.cs`
- Modify minimally if required: `src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs`
- Modify minimally if required: `src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs`
- Modify: `scripts/preflight-local005-native-multiregion.py`

**Interfaces:**
- Consumes: `ClosedPolygonSourceLoopReader`, `PolygonSourceLoopRegionAssembler`, `PolygonalSlabMultiRegionMeshPlanner`, region ownership/manifest helpers, existing Slab/Foundation family/rebar semantics.
- Produces: full desired region-set replacement with aggregate and per-region generated metadata.

- [ ] Add RED static contract requiring exactly-one-element source association, `PolygonalSlabMultiRegionMeshPlanner.Plan`, 12,000 native bar cap, pre-erase complete ownership validation, one transaction, `ProjectStateSnapshot` rollback, dual ownership marking, deterministic source/generated manifests.
- [ ] Implement selection association anchored by current semantic source or previous source manifest; ambiguity fails before write.
- [ ] Resolve existing Slab/Foundation cover/faces/spacing/vertical-placement semantics and plan all regions in one Core call.
- [ ] Validate all previous generated handles before first erase; allow legacy aggregate-owned migration only after legacy ownership succeeds.
- [ ] Materialize every tagged region layout as native `Solid3d` bars and mark both aggregate and region ownership.
- [ ] Commit aggregate handles/count plus deterministic region/source/topology metadata and audit entry only within the atomic replacement.
- [ ] Run Core smoke/static guards and V25 compilation.
- [ ] Commit builder implementation.

### Task 5: Read-only Health and command surface

**Files:**
- Create: `src/QS3D.BricsCAD.V25/Cad/GeneratedMultiRegionRebarRuntimeHealthService.cs`
- Modify the existing rebar command file identified by repository conventions, or create a focused `MultiRegionRebarCommands.cs` if commands are split by feature.
- Modify: `scripts/preflight-local005-native-multiregion.py`

**Interfaces:**
- Consumes: persisted manifests, closed-loop reader, assembler, aggregate/region ownership services.
- Produces: Slab/Foundation multi-region build/refresh commands and a read-only Health command/report.

- [ ] Add RED contract for explicit Slab/Foundation multi-region commands and read-only Health.
- [ ] Implement Health source re-resolution, topology fingerprint verification, generated-count/manifest verification, per-region dual-ownership checks, duplicate-handle detection, and localized issue text; never repair in Health.
- [ ] Add commands following existing project-context/error-reporting conventions.
- [ ] Run feature guard and V25 compile.
- [ ] Commit Health/commands.

### Task 6: V26/static contract and tracker accuracy

**Files:**
- Create/complete: `scripts/preflight-local005-native-multiregion.py`
- Modify only if needed: existing V26 linked-source preflight contract.
- Update tracker documentation only where it currently claims the source layer is missing after this implementation.

**Interfaces:**
- Consumes: V26 project wildcard linked V25 source.
- Produces: a discoverable feature guard that proves required files/symbols, compatibility paths, fail-closed ownership, and V26 coverage remain present.

- [ ] Assert all new V25 files are linked into V26 and not excluded.
- [ ] Assert existing rectangle and single `PolygonalSlabMeshPlanner` code paths remain present.
- [ ] Assert issue #83 remains a LOCAL_ONLY runtime tracker rather than a source-completion claim.
- [ ] Run `python scripts/preflight-local005-native-multiregion.py` and aggregate `python scripts/preflight-all.py` where environment permits.
- [ ] Commit the final static/source contract.

### Task 7: Exact-head CI, race sync, review, and merge

**Files:** none unless CI/review exposes a concrete defect.

**Interfaces:**
- Consumes: canonical branch head and protected `main`.
- Produces: merged source child #3647; parent #83 updated to source-ready/PENDING_LOCAL.

- [ ] Open PR with `Closes #3647`, `Advances #83`, and Lane-Key `issue-3647`.
- [ ] Require exact-head branch/PR `preflight` and `core` SUCCESS, including Core smoke and BricsCAD V25 plugin build.
- [ ] If `main` advances, merge/sync `main` into the canonical branch without force-push and rerun exact-head CI.
- [ ] Recheck PR comments/review threads and mergeability immediately before merge.
- [ ] Merge through protected-main flow with expected-head guard.
- [ ] Verify merge SHA on `main` and #3647 closed.
- [ ] Comment on #83 with exact merged SHA and CI evidence; keep #83 OPEN with licensed runtime scenarios explicitly `PENDING_LOCAL`.
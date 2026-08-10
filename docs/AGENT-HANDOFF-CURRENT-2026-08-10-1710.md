# QS3D-BricsCAD — current agent handoff (refreshed 2026-08-10)

**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the short canonical current-state delta for fast-moving work. Fetch `main` before every write. If this note conflicts with current source, current source wins.

Read first:

- `AGENTS.md`
- `docs/PRODUCT-BOUNDARY.md`
- `docs/REMOTE-AGENT-SCOPE.md`
- `CI_POLICY.md`
- `docs/AGENT-HANDOFF-LATEST-2026-08-10.md`
- `docs/IMPLEMENTATION-STATUS.md`

Local/runtime work is parked in:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`
- `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`
- `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`

## 1. Product and execution boundary

QS3D remains a clean-room **BricsCAD V25 x64 .NET plugin**. BricsCAD owns the DWG/editor/viewport/native CAD lifecycle. Do not create a standalone QS3D CAD engine or commit BricsCAD runtime assemblies.

`BLT-like` / `BLT-style` means independently implemented workflow/UX familiarity only.

`docs/REMOTE-AGENT-SCOPE.md` is authoritative for remote work:

- source/Core/static contracts may move to `REMOTE_DONE`;
- real V25/Windows/native/private-DWG/signing/performance evidence is `LOCAL_ONLY`;
- remote `continue all` must not repeatedly re-audit parked LOCAL_ONLY gates;
- only exact local evidence may create `LOCAL_PASS`.

## 2. Direct Draw / Door / Opening — source state

Source authoring exists for the guarded P0/P1 set including Wall, Beam, Column, Slab, GlassWall, WallPier, StructuralWall, Foundation, Door and WallOpening.

Current contract includes:

- real BricsCAD source entity first, then semantic capture, then canonical native builder where applicable;
- active compatible Family/Type reuse;
- project/native rollback guards;
- planar translated/in-plane-rotated UCS support with tilted/3D UCS fail-closed;
- Door/Opening source + semantic + verified Auto Host without implicit physical cutting;
- `QS3DCUTSELECTEDOPENINGS` for explicit selected-only physical cut;
- post-commit UI refresh/regen failures are non-fatal.

Transient thickness/profile DrawJig preview, repeated authoring UX and broader native interaction proof remain LOCAL_ONLY/runtime-sensitive and are already parked in the local addendum.

### Manual host-link atomicity

`QS3DLINKHOST` now snapshots the full `ProjectState` before `HostLinkService.LinkOpening(...)`. Manual link mutation plus deterministic regeneration share one rollback boundary. If link/regeneration fails, the project snapshot is restored; if rollback itself fails, both failures are surfaced instead of silently leaving a half-mutated semantic project.

Opening host dependencies are canonicalized rather than accumulated as duplicate host references. Keep dependency mutations ownership-safe and preserve dependent propagation through `DependencyGraph`.

Keep palette/status refresh outside the semantic commit boundary. A post-commit UI failure must not turn a valid host-link commit into an apparent semantic failure.

### Physical opening-cut live freshness

Straight/selected and curved physical cuts now stamp live-input freshness metadata after a successful cut. `PhysicalOpeningCutLiveStateService` recomputes the current inputs so Health/Release can detect a host that still carries an older physical hole after host/opening CAD geometry, linked-opening membership or effective cut parameters changed.

The live fingerprint covers host source geometry and effective host dimensions; linked opening source extents plus Width/Height/Sill/Clearance; and curved-host project settings that affect cutter construction. Generated-host rebuild invalidation clears the whole `PhysicalOpeningCut*` family.

Keep this service wired into `QS3DHEALTHALL` and `QS3DRELEASECHECK`. Missing/stale/mismatched live-cut metadata is intentionally release-blocking; do not “upgrade” an old cut by stamping metadata without actually rebuilding/cutting it.

### Comprehensive Core health and semantic regeneration

`QS3DHEALTH` uses `ComprehensiveModelHealthService`. Its Core-only aggregate includes model/source health, Room Finish integrity, dependency health, Level-reference health, Grid naming integrity, generated ownership/stale/mode health, fabrication qualification and all current generated rebar/mesh/curtain output-family diagnostics.

`RegenerationEngine.RegenerateDirty(...)` and `RegenerateDirtySubset(...)` now share a project-snapshot transaction boundary. A semantic regenerator/rule failure restores the full project snapshot; if rollback itself fails, both errors are surfaced. Preserve this invariant when adding new regenerators. UI/native post-processing remains a separate boundary and must not be confused with Core semantic rollback.

Live BricsCAD-state checks such as curtain-frame live fingerprints and physical-opening live-cut freshness stay in `QS3DHEALTHALL` / `QS3DRELEASECHECK`; do not introduce BricsCAD runtime dependencies into Core just to make the composite broader.

## 3. Grid / reference model — source workflow materially advanced

`QS3DGRID` is source-implemented as guarded semantic capture for finite positive `LINE` / `ARC` Grid references. It reuses `ElementCategory.Grid`, transactional `SemanticCaptureService` and generic semantic quantities (`LengthM`, `Count`).

The semantic naming layer is also implemented:

- `GridNamingService` stores `GridLabel` and `GridSequenceIndex` on the existing Grid elements;
- numeric and alphabetic (`A..Z, AA...`) sequences are deterministic;
- the caller supplies explicit ordering; Core does not pretend to infer CAD spatial order;
- the whole batch is validated before mutation, with case-insensitive uniqueness against other Grid labels;
- `QS3DGRIDNUMBER` lets the user pick Grid source entities in explicit order, snapshots project state, renumbers atomically and records `grid.renumber` audit;
- comprehensive Health checks malformed/duplicate semantic Grid naming;
- the Project Ribbon exposes both `QS3DGRID` and `QS3DGRIDNUMBER` and static preflight guards their discoverability.

`GridIntersectionPlanner` provides a bounded CAD-independent finite intersection contract for explicit Grid `LINE`/`ARC` references, covering LINE×LINE, LINE×ARC and ARC×ARC. It fails closed on duplicate semantic IDs, invalid/degenerate geometry, overlapping collinear LINEs, coincident ARC support circles and bounded-count overflow. It reports intersection geometry only; it does not infer engineering constraints or mutate source CAD.

Still not complete / not to be overclaimed:

- native Grid bubble/label geometry and ownership/replacement lifecycle;
- automatic CAD spatial ordering for renumbering;
- V25 native LINE/ARC extraction into the Core intersection contract;
- rectangular/radial Grid-system authoring;
- Grid constraints, dimensions or native intersection markers;
- Direct Draw Grid / transient repeated authoring;
- automatic structure-to-grid hosting/snapping.

Extend the existing `ElementCategory.Grid` model rather than creating a competing Grid store. Read `docs/GRID-WORKFLOW.md` and `docs/GRID-INTERSECTIONS.md`.

## 4. Floor / Level semantics — Core implemented, native integration parked

Do **not** add a competing `LevelDefinition`. QS3D reuses the existing `ProjectFloor` / `FloorDefinition` catalog as the Level model.

Current Core supports opt-in:

- `BottomLevelId`
- `BottomLevelOffsetM`
- `TopLevelId`
- `TopLevelOffsetM`

`ProjectFloorService` owns assignment/lifecycle, `ElementVerticalPlacementService` defines legacy-compatible bottom/top/effective-height resolution, and `LevelReferenceHealthService` is wired into comprehensive Health, Health All and Release Check.

Legacy elements without Level references preserve source-relative placement semantics.

Native host/opening/curtain/rebar placement and Level assignment UI are **not** to be exposed as complete until all dependent native systems use the same resolver. `LevelReferenceNativeIntegrationPolicy` currently keeps every category unqualified; semantically valid Level refs therefore surface `LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING` and keep release blocked until coherent native integration plus exact-V25 proof exists.

Read `docs/LEVEL-REFERENCES.md`.

## 5. Authoritative source edit / reconcile — source implemented

`QS3DSYNCSOURCE` is the guarded reconcile bridge after a user edits authoritative tracked CAD with native BricsCAD tools.

Current source contract:

- rejects generated output and ambiguous/unknown ownership;
- snapshots full project state;
- invalidates owned dependent generated geometry while CAD is rollback-capable;
- refreshes source-derived semantic measurements/metadata;
- marks/regenerates semantics deterministically;
- restores project state on pre-CAD-commit failure;
- keeps post-commit Workspace/viewport sync best-effort;
- intentionally does **not** silently rebuild destructive/native downstream output.

Do not compose `QS3DSYNCSOURCE` + `QS3DBUILD3D` and call the combination atomic without a real shared/recovery contract.

Read `docs/SOURCE-EDIT-WORKFLOW.md`. Interactive MOVE/ROTATE/STRETCH/grip, UNDO, document-switch and save/reopen proof remains LOCAL_ONLY.

## 6. Semantic interchange JSON — source implemented, genuinely read-only command boundary

`QS3DINTERCHANGEJSON` exports deterministic `QS3D.SemanticSnapshot` format version 1 as UTF-8 semantic JSON.

The snapshot includes stable semantic project/catalog/element/reference/quantity data and excludes generated native ownership/runtime state. Drawing CAD handles remain explicitly drawing-local provenance; semantic element IDs are the portable identity.

The BricsCAD command preserves the read-only contract end-to-end:

1. Save dialog is shown before the active project is obtained, so Cancel performs no semantic mutation;
2. `ProjectStateSnapshot.CreateDetachedCopy(...)` creates a deep detached working state;
3. dirty semantic quantities are regenerated only on that detached copy;
4. `ProjectInterchangeJsonExporter` writes the detached snapshot through its existing temporary-file/replace atomic boundary;
5. live project object references are never restored/replaced, so modeless UI stays attached to the original live state.

Static preflight rejects regressions that call `RegenerateDirty(project)` or export the live project, and Core smoke proves detached project/element mutations do not leak into live state.

Version 1 does **not** claim JSON re-import/round-trip, IFC/Revit/BCF exchange or cloud/team synchronization. Any future importer must define identity collision, unit/schema validation, provenance, ownership reconstruction, migration and rollback before mutating live data.

Read `docs/INTERCHANGE-JSON.md`.

## 7. Semantic documentation layer — Core foundation implemented

`QS3D.Core.Documentation.SemanticTagRenderer` is source-implemented as the deterministic semantic-label renderer. It resolves bounded templates from a real project-owned semantic element and supports stable semantic tokens for ID/category/Family/Floor/Zone plus `P:` properties and `Q:` quantities.

Generated native ownership/runtime properties are intentionally not documentable semantic values. Unknown tokens and invalid referenced catalog identities fail closed.

This Core renderer does **not** mean native BricsCAD MText/MLeader/Table/Layout/Viewport workflows are complete. Native semantic tag placement, generated annotation ownership/replacement, DWG table generation and sheet/view lifecycle require exact V25 API/runtime design and qualification described in `docs/DOCUMENTATION-LAYER.md`.

Do not mark documentation issue #77 complete from the renderer alone.

## 8. Rebar/generated replacement hardening

Preserve cross-layer atomicity across generated rebar/mesh families: generated semantic ownership/count/mode/stale/audit state is published while the CAD transaction is still rollback-capable and project state is restored on pre-commit native failure.

Shape Rebar canonical audit event is:

```text
geometry.rebar.shape
```

The legacy post-commit `geometry.rebar3d.shape` command-layer audit has been removed. `scripts/preflight-generated-rebar-audit.py` guards all current generated rebar/mesh canonical audit paths and forbids reintroducing the duplicate Shape audit in the UI/post-commit layer.

Post-commit Palette/Editor refresh remains best-effort and must not convert a successful native/semantic commit into a false operation failure.

Fabrication-grade standards remain evidence/provenance only until an explicit governing standard/revision plus engineering approval exists.

## 9. Curtain truthfulness

Individual GlassWall host and Curtain LINE/path frame replacement families have guarded internal transaction/project rollback contracts.

`QS3DCURTAIN3D` still orchestrates multiple independent native phases. Keep the `Curtain 3D PARTIAL COMMIT` truthfulness boundary until either:

- one shared native transaction orchestration exists; or
- an ownership-safe persisted compensation/recovery journal exists and is proven on real V25.

Panel-by-panel backing glass and broader unsupported/freeform native path parity also remain separate native architecture/runtime gates. See the local addendum.

## 10. Customer diagnostics / support

Keep these commands distinct:

- `QS3DRUNTIMECHECK` — customer/runtime diagnostic;
- `QS3DRUNTIMEPROBE` — automation probe for the local harness;
- `QS3DSUPPORTBUNDLE` — privacy-safe support summary.

The Full Domain Hub user action uses `QS3DRUNTIMECHECK`, not the automation probe, and exposes `QS3DSUPPORTBUNDLE`. Preserve the diagnostics preflights.

Do not turn recorded package signing metadata into a claim of cryptographically verified Authenticode publisher/timestamp trust; real signing trust belongs to the release/local signing gate.

## 11. LOCAL_ONLY / external-policy gates already parked

Remote agents should not repeatedly re-audit these during normal `continue all`:

- exact-current-SHA V25 adapter build, NETLOAD/DemandLoad and interactive runtime proof;
- private-DWG save/reopen/multi-DWG regression;
- real Windows Ribbon/Palette/WPF/Unicode/HiDPI behavior;
- Direct Draw DrawJig/repeat/OSNAP/ORTHO/ESC/UNDO runtime behavior;
- coherent Level-reference native placement/UI integration;
- Curtain whole-command recovery and panel-by-panel backing glass;
- physical ownership-safe L/T/X/Multi wall-solid reconciliation;
- native semantic tag/MLeader/Table/Layout/Viewport documentation workflows;
- clean install/upgrade/uninstall and real Authenticode/timestamp trust;
- commercial license enforcement until real SKU/seat/trial/binding/offline/key-rotation policy is supplied;
- standard-specific fabrication-grade rebar until approved engineering inputs exist;
- legal/public/source distribution model until owner/legal policy is chosen;
- real large-model performance profiling.

Do not mark any of these `LOCAL_PASS` from source inspection.

## 12. Open product tracking — inspect current source before acting

Current issues include:

- #72 exact V25 qualification;
- #73 multi-owner wall solids / advanced geometry;
- #74 Direct Draw transient preview / repeated authoring;
- #75 production signing/install/update + licensing boundary;
- #76 fabrication-grade rebar / structural depth;
- #77 documentation layer — **partially advanced by `SemanticTagRenderer`; native tag/table/sheet workflows remain**;
- #79 Grid/reference model + richer Level constraints — **materially advanced by `QS3DGRID`, semantic Grid numbering/naming health, finite LINE/ARC intersection planning and current Bottom/Top Level Core semantics; native visualization/spatial ordering/constraints and Level native placement remain**;
- #80 native semantic modify/edit workflow — **materially advanced by `QS3DSYNCSOURCE`; richer interactive edit UX/runtime proof remains**;
- #81 large-model performance;
- #82 real V25 UI/DPI/context-menu/Ribbon polish;
- #83 generalized polygonal Slab/Foundation mesh;
- #84 interoperability/import-export — **materially advanced by detached read-only `QS3DINTERCHANGEJSON`; import/round-trip and broader formats remain**.

Do not close runtime/engineering/external-policy issues from source inspection alone.

## 13. CI / release

GitHub Actions remain manual-only under `CI_POLICY.md`. `continue all`, source review, docs/preflight changes, commits or local handoff preparation do **not** authorize workflow dispatch, rerun or GitHub Release publication.

A separate explicit owner instruction is required for CI/build/runtime/release execution. Never weaken BricsCAD `SECURELOAD`, Windows trust or signature validation to force a test to pass.

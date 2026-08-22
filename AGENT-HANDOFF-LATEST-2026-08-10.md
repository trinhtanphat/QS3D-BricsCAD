# QS3D-BricsCAD — canonical latest agent handoff

**Audit/update date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Branch:** `main`  
<<<<<<< Updated upstream
**Source reconciliation cutoff for this edition:** `ded0b605f5630851f5bfc8a383651acd32e0005d` (`docs: record auto host and review gated wall cleanup`)  
**Historical exhaustive session audit:** `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md`  
**Status:** this file is the **canonical current handoff**. If this file, an older chat message, or the historical handoff conflicts with newer `main`, **newer source wins**.
=======
**Repository/source reconciliation cutoff for this edition:** `904442c` (`test(preflight): guard typed editors and instance overrides`) plus rebased B4D/ED2/Handle commit `645b399` and the DWG-identity/generated-ownership hardening documented below.
**Historical exhaustive session handoff:** `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md`  
**Status of this file:** **canonical for current source status and continuation**. The older session-history handoff is retained as the detailed historical audit trail, but any source-status statement in that older file that conflicts with this file or newer `main` is superseded.
>>>>>>> Stashed changes

> The repository is being modified by multiple agents. Fetch `main` before work and again before every push. Inspect commits newer than the cutoff above rather than replaying an old feature branch over current `main`.

---

## 1. Review/evidence boundary

The historical audit already established:

- **377 / 377 accessible current-session records** were read sequentially; the terminal page reported **0 remaining**;
- **2 targeted prior-history retrievals** were also performed for earlier QS3D / BLT3D / BricsCAD V25 work;
- those chat/history findings were reconciled against GitHub source, not treated as source-of-truth by themselves.

That proves review of the material exposed to this session. It does not honestly certify deleted/inaccessible account-wide history or commits created after this document's cutoff.

---

## 2. Owner intent and non-negotiable architecture

QS3D is an original, clean-room **BLT3D-like semantic BIM / quantity takeoff plugin for BricsCAD V25**.

Non-negotiable requirements:

- target BricsCAD **V25**, Windows x64;
- adapter target **.NET Framework 4.8**; Core target `netstandard2.0`;
- native BricsCAD viewport remains the real 2D/3D canvas in the middle;
- Ribbon + WPF palettes provide QS3D workflow around the native viewport;
- dark, compact Vietnamese CAD UI inspired by the supplied BLT3D references without copying proprietary source/assets;
- keep deterministic business/domain logic in `QS3D.Core` and BricsCAD API calls in `QS3D.BricsCAD.V25`;
- visible UI actions should perform real work, not decorative mock behavior;
- BricsCAD/BLT proprietary DLLs, private DWG/DOCX fixtures and license materials must not be committed;
- do not call source/static/Core success “BricsCAD runtime verified”.

Requirement-document priorities that remain central:

- **TƯỜNG KT**;
- **HT_PHÒNG**;
- **Cửa / Lỗ mở**;
- **BQ → Excel**.

Private runtime fixture from the session: `260808.SHOP XAY TUONG_NHA NOI TRU.dwg`. Keep it private.

---

## 3. Multi-agent / CI rules

Read `AGENTS.md` and `CI_POLICY.md` before source edits.

Required integration pattern:

```text
fetch latest main
→ inspect concurrent commits
→ preserve their work
→ apply/merge new feature onto latest tree
→ resolve overlaps as a union, not ours/theirs blindly
→ review final diff
→ push without force
```

Never reset/force-push `main` backwards.

GitHub Actions policy remains **manual-only**. Release workflows use `workflow_dispatch`; commits, merges, docs, “continue all”, reviews or source changes do **not** authorize a CI dispatch.

Temporary CI gate branches may contain push-trigger helper workflows, but those temporary workflow commits must not be copied into release `main`.

---

## 4. Current project/persistence/domain foundation

Current source includes:

- Project / Zone / Floor / Family / semantic Element model;
- data-driven Family/instance properties and active project context;
- multi-DWG project cache keyed by `Document` identity;
- Save As drawing-identity synchronization;
- **QSDB schema v3**, deterministic `v1 → v2 → v3` migration;
- persisted dirty flags/timestamps;
- persisted QuantityRules and audit provenance;
- validated temp-save / atomic replacement where supported;
- `.bak` recovery and protected failure state;
- single-writer project locking;
- XML DTD/external-entity and file-size guards;
- non-finite/invalid persisted-state rejection;
- family reassignment that refreshes inherited defaults while preserving deliberate instance overrides;
- dependency graph + bounded fixed-point regeneration;
- finite/overflow-safe semantic quantity math with atomic quantity-map replacement on success only.

### QuantityRules

QuantityRules are project data, not UI hard-codes. The engine supports numeric Family/instance/current-quantity variables, dependency ordering, stale managed-output cleanup, provenance and cycle detection. Circular dependencies fail atomically rather than partially applying outputs.

---

## 5. Current Tường KT / wall workflow

### Semantic categories

- Tường Gạch / `ArchitecturalWall`;
- Vách Kính / `GlassWall`;
- Trụ Tường / `WallPier`.

### Native source paths

`QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, and `QS3DBUILD3D` now support TKT wall variants through:

- LINE centerlines;
- open plan-view POLYLINE centerlines;
- bulged POLYLINE segments via deterministic arc tessellation;
- `WallFootprintEngine` for miter joins with bevel fallback;
- self-intersection/reversal/degenerate-geometry rejection;
- far-origin-stable footprint area/perimeter math;
- guarded generated-Solid3d replacement and source/generated handle separation.

`WallMiterLimit` and `WallArcSagittaM` are project metadata controls used by this source path.

### Wall junction topology / review-gated cleanup

Current source includes deterministic wall-junction analysis:

- End / Straight / L / T / X / Multi classification;
- sweep/broad-phase + spatial candidate indexing;
- tolerance-aware endpoint/crossing detection;
- finite/extreme-coordinate guards;
- plan-view/coplanar CAD-selection guards;
- `QS3DWALLJUNCTIONS` diagnostic analysis;
- `WallJunctionAdjustmentPlanner` for reviewable endpoint snap proposals;
- current command output reports **SnapPlan** proposals instead of silently changing CAD.

Concurrent work after the initial planner also added the user-facing **Wall Snap Preview / Apply** workflow documented in `docs/COMMANDS.md`:

- preview first;
- review/apply is guarded by plan/source fingerprints and source-handle identity;
- apply is intended for review-gated endpoint cleanup, not blind auto-trimming.

Inspect current `WallJunction*` source and `docs/COMMANDS.md` before changing this workflow because it has been evolving concurrently.

---

## 6. Room / HT_Phòng

### `QS3DROOMAUTO`

Automatic room discovery is no longer limited to straight lines. Current source accepts plan-view:

- LINE;
- POLYLINE;
- bulged POLYLINE;
- ARC;
- SPLINE.

<<<<<<< Updated upstream
Core/adapter behavior includes:
=======
- Project / Zone / Floor / Family / semantic Element state;
- active Zone/Floor/Family context and data-driven property editing;
- family property propagation to member elements with derived quantities dirtied;
- multi-DWG live cache keyed by `Document` identity rather than mutable drawing filename;
- Save As drawing identity synchronization;
- live DWG identity based on BricsCAD `Database.FingerprintGuid`, with a same-path legacy migration and fail-closed rejection of copied/mismatched `.qsdb` Handle identities;
- **QSDB schema v3**, with deterministic **v1 → v2 → v3** migration;
- persisted `QuantityRule` definitions and audit provenance;
- persisted dirty flags and UTC update state;
- validated temp writes and atomic replacement where supported;
- `.bak` recovery and protected failure state rather than silent destructive overwrite;
- single-writer project lock;
- file-size and XML DTD/external-entity protection;
- validation of malformed/non-finite persisted data.
>>>>>>> Stashed changes

- drawing-unit normalization;
- configurable endpoint/topology tolerance;
- configurable minimum area;
- deterministic bulge/arc tessellation;
- `RoomBoundarySplineChordM` sampling for SPLINE with bounded maximum segment count;
- planarity/elevation validation;
- intersection/T-junction subdivision;
- endpoint snapping;
- dangling-bridge removal;
- bounded-face traversal;
- stable boundary keys, area/perimeter and boundary-source provenance;
- iterative bridge detection and source-evidence indexing for larger graphs;
- rollback of semantic/audit changes if regeneration fails;
- auto-room lifecycle reuse/stale handling;
- deterministic child-finish synchronization for existing auto rooms.

Important ownership invariant: auto-room boundary sources are provenance and must not be claimed as duplicate semantic `SourceHandles` ownership of wall entities.

### HT_Phòng

<<<<<<< Updated upstream
Current semantic finish workflow covers floor finish, waterproofing, skirting, wall finish and ceiling finish.
=======
- dependency graph and dirty propagation;
- bounded fixed-point regeneration;
- semantic regeneration preserves `Geometry` dirty state for native-solid categories; a successful committed CAD builder is the only path that clears that flag;
- explicit `QS3DREGEN`;
- BQ/BBS/Refresh regenerate deterministic dirty quantities before consuming them;
- guarded `QuantityMath` for finite/non-negative multiply/add/subtract/divide/hypotenuse/clamp operations;
- semantic/structural regeneration now stages calculations so an overflow throws without partially replacing the element's prior quantity map;
- smoke regressions explicitly verify wall, finish, beam, stair and earthwork overflow cases retain the pre-existing sentinel state instead of partially mutating quantities.
>>>>>>> Stashed changes

Safety invariant: finish untracking/removal must not erase unrelated CAD geometry.

---

## 7. Door / Opening workflow

Current source supports:

- `QS3DOPENING` / `QS3DDOOR` semantic capture;
- `QS3DLINKHOST` manual host linking;
- `QS3DAUTOLINKHOSTS` conservative automatic host matching;
- semantic opening deduction from host quantities;
- `QS3DCUTOPENINGS` physical boolean source path.

### Auto host matcher

Current matcher is deliberately conservative:

- compatible wall/vách host categories only;
- floor/zone constraints;
- live source geometry;
- distance/tolerance thresholds;
- ambiguity margin;
- elevation tolerance so openings are not linked to wrong-floor walls;
- already-linked openings are not silently reassigned;
- auto-link records audit provenance;
- it does **not** automatically run physical boolean cutting.

### Physical boolean cut

Current source supports generated compatible hosts with:

- LINE centerline host;
- **straight open POLYLINE** centerline host;
- source/fingerprint/proximity guards;
- `OpeningCutPlanner` / `PolylineOpeningCutPlanner`;
- cutter box + `BoolSubtract` inside a CAD transaction;
- idempotence metadata tied to generated host solid + opening/host geometry;
- rejection when geometry changed and a previously-cut solid must be rebuilt first.

Current limitation: **curved/bulged host POLYLINE physical cutting remains intentionally unsupported**. Do not claim semantic deduction equals a physical curved-host boolean.

---

## 8. Structural / native 3D

Semantic + deterministic quantity paths exist for:

- Beam;
- Slab;
- Column;
- StructuralWall;
- Foundation;
- Stair;
- Railing;
- Earthwork.

Source-level native 3D paths exist for TKT wall variants and supported structural forms through LINE and/or closed POLYLINE depending on category, with generated-geometry ownership and validation guards.

<<<<<<< Updated upstream
These native adapters remain **runtime-gated** until a real V25 build/NETLOAD regression proves the exact current SHA.
=======
- source/generated handles are distinct;
- generated geometry replacement uses guarded/two-phase behavior;
- generated entities receive versioned QS3D XData ownership (`ProjectId`, `ElementId`, category); erase/replacement and physical opening boolean modification require a matching live marker;
- health validates generated Solid3d ownership/liveness/category;
- erased/non-Entity source handles are not considered live;
- 3D builders reject ambiguous semantic ownership of one CAD source;
- one semantic element selected through multiple source objects is rejected rather than generating ambiguous duplicates;
- expected source geometry type is explicit: LINE where a line prism is required, closed POLYLINE where a footprint extrusion is required;
- non-finite/invalid geometry inputs and dimensions fail instead of creating corrupted solids.
>>>>>>> Stashed changes

---

## 9. Rebar / BBS / generated rebar geometry

### Deterministic BBS

<<<<<<< Updated upstream
Current Core includes:
=======
- stable Floor/Family ID grouping where appropriate;
- filtering and Locate;
- real recalculation callback;
- deterministic semantic regeneration before report consumption;
- persisted visible-column preferences in project metadata;
- XLSX export;
- finite/overflow-safe report accumulation rather than unchecked `+=`;
- filters/freeze/header behavior covered by deterministic source/tests.
- exported aggregate rows carry stable QS3D Element IDs, hexadecimal CAD handles and the owning DWG fingerprint;
- `QS3DED2` aliases the BQ/export workflow;
- `QS3DEXCELLOCATE` rejects a QS3D export whose fingerprint differs from the active DWG; the supplied legacy BLT hidden `$<decimal handle>` convention remains readable but requires explicit `YES` confirmation because it has no fingerprint;
- derived room-finish rows resolve handles transitively through their source-room dependency without duplicating semantic handle ownership.
>>>>>>> Stashed changes

- notation parsing and validation;
- guarded arithmetic;
- bar mark / shape / cutting-length concepts;
- lap/anchor/hook/waste fields used by schedule logic;
- kg/m, total length and total weight;
- `QS3DBBS` XLSX;
- `QS3DBBSVIEW` review/Locate;
- `QS3DBBSCSV` UTF-8 CSV with formula-injection/control-character/non-finite guards and atomic replacement.

### Column vertical rebar geometry

`QS3DREBAR3D` supports a guarded first native path for rectangular columns:

- closed 4-vertex rectangle POLYLINE;
- XY/orthogonality/bulge guards;
- `RebarNotation` one-diameter path;
- cover + explicit/inferred bars-along-width/depth;
- deterministic rectangular perimeter layout;
- vertical Solid3d bars.

### BBS shape geometry

Concurrent full-domain work added:

- `RebarShapePathBuilder` for deterministic STRAIGHT / L / U / S path generation;
- `QS3DREBAR3DSHAPE`;
- segmented-cylinder native shape solids with bounded per-element/per-batch counts;
- `GeneratedShapeRebarHandles` metadata.

### Ownership / health invariants

`GeneratedRebarOwnershipGuard` indexes both:

- `GeneratedRebarHandles`;
- `GeneratedShapeRebarHandles`.

Before destructive re-generation, a handle must be owned by the exact element/property key; cross-element/cross-key ownership conflicts are rejected instead of erased.

`GeneratedRebarHealthService` distinguishes column vs shape handle sets and supports separate liveness checks. Commands include:

- `QS3DREBARHEALTH`;
- `QS3DREBARSHAPEHEALTH`.

Keep physical BBS-shape geometry and BBS schedule semantics separate: a valid schedule is not proof every shape can be safely authored in native CAD for every host/category.

---

## 10. Recognition / templates / audit / revision

### Recognition

Recognition is deterministic/rule-based and review-oriented:

- layer/text/block/tag evidence;
- entity-type compatibility;
- Vietnamese normalization;
<<<<<<< Updated upstream
- token-boundary matching (e.g. normalized `dam` must not match inside `DAMAGE`);
- confidence + candidate margin;
- review UI;
- high-confidence auto-apply only;
- semantic collision rejection;
- project/company layer mappings override fallback heuristics;
- invalid/ambiguous mapping/confidence state rejected.
=======
- token/term boundary matching so `DAMAGE` is not mistaken for normalized `Dầm`/`dam`;
- confidence + top-candidate margin;
- `QS3DRECOGNIZE` review UI;
- `QS3DRECOGNIZEAUTO` high-confidence application;
- invalid confidence/margin and ambiguous mappings rejected;
- semantic category collision protection;
- **project/company layer mappings can override fallback heuristics deterministically**.
- `QS3DB4D` performs a bounded whole-Current-Space scan, excludes QS3D-generated mass/rebar/shape-rebar solids, reads Polyline/Region/Hatch/Solid3d metrics and auto-applies only high-confidence results while leaving ambiguous results in review. Rescan replaces prior source-derived metrics and `CAD.*` metadata instead of retaining values no longer exposed by the live entity.
- rescanning an already tracked object preserves its assigned Family/Floor/Zone instead of silently moving it to the active context.
>>>>>>> Stashed changes

### Templates

`.qstemplate` can carry company standards such as:

- Families;
- QuantityRules;
- recognition layer mappings;
- BQ visible columns;
- generic Family material/classification properties.

Template import is guarded by validation, backup/rollback logic, inherited-vs-instance property safety and audit provenance. Do not introduce implicit destructive project saves during failed import.

### Audit / Revision

Current source has:

- persisted project audit trail and Audit UI;
- revision baseline/diff persistence via `.qsrev`;
- Before/After/Delta/% rows + Locate;
- finite/overflow-safe revision arithmetic and duplicate-ID rejection.

---

## 11. UI / navigation currently implemented

Current source includes the dark BLT-like workspace structure, Ribbon, Workspace palettes and Full Domain Hub.

Important current UX additions include:

- typed property editors for booleans/choices/numbers;
- inherited-vs-instance override behavior;
- selection inspection;
- Focus / Isolate / Unisolate;
- Zone/Floor/Family/tree workflows;
- TKT variant capture;
- Wall junction analysis / preview/apply workflow;
- Room Auto;
- Opening auto host / physical cut;
- column + BBS-shape rebar 3D and health;
- BQ/BBS/Recognition/Revision/Template/Audit/Health access.

The native BricsCAD viewport remains the central viewport; UI mockups are design targets, not runtime screenshots.

---

## 12. Model Health / CAD ownership invariants

Model Health currently covers multiple layers:

- required semantic dimensions by category;
- material inheritance issues;
- host/dependency consistency;
- rebar definitions/lengths;
- source handle liveness;
- erased/non-Entity source rejection;
- generated host Solid3d format/ownership/category/liveness;
- generated-vs-source-handle separation;
- column generated rebar ownership/count/liveness;
- BBS shape generated rebar ownership/count via Core and dedicated shape-health liveness command.

Do not weaken Health just to make incomplete data appear valid.

---

## 13. BQ / reporting / Excel

Current BQ/reporting includes:

- stable Floor/Family grouping where appropriate;
- semantic regeneration before consumption;
- filters, Locate and real recalculate callback;
- visible-column preferences persisted in project metadata;
- finite/overflow-safe accumulation;
- real XLSX output with expected headers/filter/freeze behavior;
- drawing-unit-aware fallback takeoff rather than silent hard-coded millimeters.

Undefined/unsupported units must remain explicitly surfaced to users.

---

## 14. V25 package / DemandLoad / runtime probe

Current source includes:

- V25 release ZIP tooling;
- generated command manifest;
- package metadata + SHA-256 hashes;
- exclusion of BricsCAD-owned runtime DLLs;
- per-user V25 DemandLoad install/uninstall scripts;
- payload hash verification;
- optional Authenticode enforcement;
- staged replacement, `-WhatIf`/confirmation and safe uninstall;
- runtime probe that verifies actual palette visibility rather than command dispatch alone.

Historical GitHub-hosted successful runs include `31343984922`, `31346731964`, `31346906413` for their exact older snapshots.

They do **not** certify newer source committed after those heads.

Historical Gate C attempt `31341184031` remained queued because no matching `[self-hosted, windows, x64, bricscad-v25]` runner was available.

---

## 15. Manual preflight structure on current source

Manual workflows currently include source guards such as:

- `scripts/preflight.py`;
- `scripts/preflight-full-domain.py`;
- `scripts/preflight-room-lifecycle.py`;
- `scripts/preflight-geometry-completion.py`;
- `scripts/preflight-room-curve-sources.py`;
- `scripts/preflight-wall-junctions.py`;
- release PowerShell syntax checks;
- Core Release build/smokes when explicitly dispatched;
- V25 adapter/package/runtime steps only on the self-hosted V25 runner when explicitly dispatched.

Newest source was **not** automatically CI-run merely because these preflights were wired into the manual workflows.

---

## 16. Remaining truth gaps / work still not safe to call complete

Even though source breadth is now large, keep these gaps explicit:

1. newest exact `main` still needs real V25 adapter compile + NETLOAD/DemandLoad qualification;
2. private sample-DWG regression;
3. actual Ribbon/Palette/Domain Hub/typed-property/Focus/Isolate/Room Auto/Rebar/Opening-cut runtime verification;
4. Windows 100/125/150/200% DPI + Vietnamese Unicode visual acceptance;
5. wall polyline/freeform authoring and corner/junction behavior beyond currently guarded source paths;
6. wall Snap Apply must be runtime-proven before treating it as production-safe CAD mutation;
7. curved/bulged wall-host physical opening boolean;
8. larger real-world room-network performance and topology corpus;
9. broader physical rebar placement/host-aware ties/stirrups/bend-radius semantics;
10. production Authenticode signing/signed updater and optional commercial backend.

Cloudflare, if used later, is an optional backend for licensing/update/team metadata/package delivery, not the runtime host for the Windows BricsCAD plugin.

---

## 17. Local V25 acceptance sequence

A local/Windows agent with licensed BricsCAD V25 should prioritize:

1. fetch latest `main`, record exact SHA;
2. build Core + adapter Release/x64 against exact installed V25 assemblies;
3. package + DemandLoad/NETLOAD;
4. run `QS3DRUNTIMEPROBE` and retain safe evidence;
5. verify Ribbon + left/right palettes + Domain Hub;
6. test 100/125/150/200% DPI;
7. multi-document open/activate/Save As/close;
8. Xref/layer/select/Focus/Isolate;
9. TKT LINE/open-POLYLINE/bulge build + rebuild;
10. Wall Junction analysis, preview and guarded Apply;
11. Room Auto with LINE/POLYLINE/bulge/ARC/SPLINE and stale/split/merge lifecycle;
12. Opening manual/auto host, re-host, straight-POLYLINE physical cut, and changed-geometry rejection;
13. HT_Phòng sync/untracking safety;
14. structural native 3D source paths;
15. column `QS3DREBAR3D`, BBS-shape `QS3DREBAR3DSHAPE`, both health commands;
16. BQ XLSX, BBS XLSX/CSV;
17. Recognition + company mappings;
18. Template export/import rollback and inheritance behavior;
19. Revision/Audit/Model Health;
20. package install/uninstall on a clean V25 test profile;
21. private DWG + screenshot evidence.

Only then update runtime status as “verified”.

---

## 18. Agent start protocol

Every new agent:

```text
1. Read AGENTS.md
2. Read CI_POLICY.md
3. Fetch latest main
4. Inspect commits newer than ded0b605f5630851f5bfc8a383651acd32e0005d
5. Read this file
6. Read docs/IMPLEMENTATION-STATUS.md
7. Read docs/COMMANDS.md and docs/PLAN.md
8. Inspect actual source for the feature
9. Decide Core-safe vs real-V25-required
10. Implement on latest tree
11. Fetch main again before push and reconcile races
12. Do not dispatch Actions without explicit owner request
```

Use `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` when deeper chronology, screenshot evidence, early branch names or historical reasoning is needed.

---

## 19. Final continuation statement

Current QS3D source is well beyond the original UI shell: it includes schema-v3 persistence, deterministic regeneration/rules, TKT/Room/HT_Phòng/Cửa, structural semantics/native source paths, wall topology analysis/review-gated cleanup, automatic room discovery including direct curves, conservative automatic host matching, guarded physical opening cuts, BQ/XLSX, deterministic BBS + CSV, column and BBS-shape native rebar source paths, Recognition, Template, Revision, Audit, Model Health, Focus/Isolate, Xref/Layer/selection and V25 DemandLoad/package tooling.

The major remaining truth boundary is still **current-main execution inside a real licensed BricsCAD V25 environment**.

Precise wording remains mandatory:

<<<<<<< Updated upstream
**source-implemented / deterministic-Core-covered ≠ current-head compiled/NETLOAD/runtime-verified in BricsCAD V25.**
=======
- **Old:** `RebarCsvExporter.cs` / `QS3DBBSCSV` is absent.  
  **Current:** BBS CSV is present and has additional safety hardening.

- **Old:** automatic room-boundary discovery is wholly future work.  
  **Current:** deterministic straight-planar network discovery and `QS3DROOMAUTO` are implemented; curved/bulged/large-real-world proof remains future/runtime-gated.

- **Old:** packaging/installer is future only.  
  **Current:** V25 package + per-user DemandLoad install/uninstall source exists and has GitHub-hosted release-script validation; actual licensed V25 install/runtime remains unverified.

- **Old:** project templates/company recognition mappings are only planning.  
  **Current:** `.qstemplate`, project QuantityRules/audit provenance and layer mappings exist in source.

The older handoff remains valuable for chronology, screenshots, early architecture decisions, private fixture notes and detailed historical audit. Use this latest file for current implementation status.

---

## 15. High-value current files for the next agent

Read these before modifying related behavior:

```text
AGENTS.md
CI_POLICY.md
docs/AGENT-HANDOFF-LATEST-2026-08-10.md
docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md
docs/IMPLEMENTATION-STATUS.md
docs/PLAN.md
docs/COMMANDS.md
docs/UI-SPEC.md
docs/V25-INSTALL.md
docs/V25-RUNNER.md

scripts/preflight.py
scripts/preflight-full-domain.py
scripts/package-v25.ps1
scripts/install-v25-autoload.ps1
scripts/uninstall-v25-autoload.ps1
scripts/test-bricscad-v25-runtime.ps1

src/QS3D.BricsCAD.V25/Commands.cs
src/QS3D.BricsCAD.V25/ReviewCommands.cs
src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs
src/QS3D.BricsCAD.V25/DomainHubCommands.cs
src/QS3D.BricsCAD.V25/TemplateCommands.cs
src/QS3D.BricsCAD.V25/AuditCommands.cs
src/QS3D.BricsCAD.V25/BbsCsvCommands.cs
src/QS3D.BricsCAD.V25/RuntimeProbeCommands.cs
src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs
src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs
src/QS3D.BricsCAD.V25/Cad/CadGeometryGuard.cs
src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs
src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs
src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs
src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs
src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs

src/QS3D.Core/Domain/ProjectState.cs
src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs
src/QS3D.Core/Persistence/QsdbProjectStore.cs
src/QS3D.Core/Persistence/ProjectStateSnapshot.cs
src/QS3D.Core/Rules/QuantityRuleEngine.cs
src/QS3D.Core/Formulas/ExpressionEvaluator.cs
src/QS3D.Core/Templates/TemplateProfileStore.cs
src/QS3D.Core/Recognition/RecognitionEngine.cs
src/QS3D.Core/Recognition/ProjectRecognitionService.cs
src/QS3D.Core/Geometry/RoomBoundaryEngine.cs
src/QS3D.Core/Services/QuantityMath.cs
src/QS3D.Core/Services/SemanticRegenerators.cs
src/QS3D.Core/Services/StructuralRegenerator.cs
src/QS3D.Core/Services/HostLinkService.cs
src/QS3D.Core/Diagnostics/ModelHealthService.cs
src/QS3D.Core/Rebar/RebarSchedule.cs
src/QS3D.Core/Export/RebarCsvExporter.cs
src/QS3D.Core/Revisions/RevisionService.cs
src/QS3D.Core/Revisions/RevisionSnapshotStore.cs
src/QS3D.Core/Audit/AuditTrail.cs

Tests especially worth preserving:
tests/QS3D.Core.SmokeTests/WorkflowPersistenceSmoke.cs
tests/QS3D.Core.SmokeTests/WorkflowSafetySmoke.cs
tests/QS3D.Core.SmokeTests/ContinuationRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/HardeningRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/RevisionRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/RoomBoundaryRegressionSmoke.cs
tests/QS3D.Core.SmokeTests/SemanticOverflowSmoke.cs
```

---

## 16. Agent start protocol

Every new coding agent should:

1. read `AGENTS.md` and `CI_POLICY.md`;
2. fetch latest `main`;
3. compare latest `main` with this handoff cutoff `fc59fccc5d116b28758ddcbc77bdf10217f71f21`;
4. read this latest handoff;
5. read the older session-history handoff only when deeper chronology/early evidence is needed;
6. read current `IMPLEMENTATION-STATUS`, `PLAN` and `COMMANDS`;
7. inspect current source for the exact feature being changed;
8. decide whether the task is Core/repository-safe or requires real V25;
9. implement without overwriting concurrent work;
10. before push, fetch `main` again and reconcile any new commits;
11. do not run Actions unless the owner explicitly asks;
12. never claim V25 runtime success without real licensed-host evidence.

---

## 17. Evidence ledger

Session/history evidence retained from the prior audit:

- accessible current-session stream reviewed: **377 / 377**;
- pagination reached terminal **0 remaining**;
- targeted earlier project-history retrievals: **2**.

Historical integration evidence:

- `31343984922` — Core union gate: **success**.

Newer release validation evidence:

- `31346731964` — V25 DemandLoad release tooling: **success**.
- `31346906413` — integrated V25 DemandLoad release tree: **success**.

Runtime blocker/history:

- `31341184031` — historical V25 self-hosted integration attempt remained queued because the matching `bricscad-v25` runner was unavailable.

Current reconciliation cutoff for this document:

- `904442c` — typed editor/instance override guards plus project BBS shape planning, focus/isolate actions and unified rebar health;
- `645b399` — B4D whole-space scan and ED2 Excel/Handle round-trip rebased onto that mainline.

The older handoff cutoff `c987b34...` is an ancestor of later `main`; subsequent full-domain/release work was layered/merged on top rather than using a destructive reset.

---

## 18. Final continuation statement

QS3D is no longer merely a UI shell or early quantity prototype. Current source contains a broad semantic project model, schema-v3 persistence, recovery, deterministic regeneration/rules, Tường KT/HT_Phòng/Cửa, structural categories, native 3D source paths, B4D-style whole-drawing recognition, BQ/ED2 XLSX with reverse Handle lookup, BBS XLSX/CSV, Recognition, company layer mappings, templates, Revision, Model Health, automatic planar/curved-polyline room discovery, Audit, Domain Hub, Xref/Layer/selection integration and V25 release/DemandLoad tooling.

The largest remaining truth boundary is **real BricsCAD V25 execution on the newest exact source SHA**. Until that happens, terminology must stay precise:

**source-implemented / GitHub-hosted-Core-verified ≠ compiled/NETLOAD/runtime-verified in licensed BricsCAD V25.**
>>>>>>> Stashed changes

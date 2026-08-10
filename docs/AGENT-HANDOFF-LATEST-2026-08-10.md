# QS3D-BricsCAD — canonical latest agent handoff

**Audit/update date:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`  
**Foundation/rebar source reconciliation merge:** `df43d67286a2b972f7787961b0c11ed5e3529ae6` (`feat(rebar): finalize Foundation mesh integration`)  
**Historical exhaustive session audit:** `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md`  

This file is the **canonical current handoff**. If this file, older chat text, historical handoffs or an old feature branch conflicts with a newer `main`, **newer source wins**. Fetch `main` again before every integration write because this repository is actively modified by multiple agents.

---

## 1. Owner intent and architecture

QS3D is an original clean-room **BLT3D-like semantic BIM / quantity-takeoff plugin for BricsCAD V25**, not a copy of proprietary BLT source/assets.

Non-negotiable architecture:

- BricsCAD **V25**, Windows x64;
- adapter: **.NET Framework 4.8**;
- deterministic/domain logic: `QS3D.Core` on `netstandard2.0`;
- BricsCAD API/native geometry: `QS3D.BricsCAD.V25`;
- native BricsCAD viewport remains the real 2D/3D canvas;
- dark compact Vietnamese Ribbon/WPF workflow around the native viewport;
- no BricsCAD/BLT proprietary DLLs, private customer DWG/DOCX or license secrets in Git;
- visible actions should perform real work rather than decorative mock behavior.

Priority product domains remain **TƯỜNG KT, HT_PHÒNG, Cửa/Lỗ mở, BQ/Excel, semantic structure and guarded rebar authoring**.

---

## 2. Multi-agent and CI policy

Before edits/merge:

```text
fetch latest main
→ inspect concurrent commits
→ preserve concurrent work
→ apply/rebase onto latest tree
→ resolve overlaps as a union
→ review final diff
→ push/merge without force
```

Never reset or force-push `main` backwards.

GitHub Actions/release workflows remain **manual-only**. `continue all`, source review, merge, docs update or release-preparation text **does not authorize workflow dispatch**. This Foundation audit did **not** dispatch Actions.

---

## 3. Current persistence/domain foundation

Current source includes:

- Project / Zone / Floor / Family / semantic Element model;
- Family-vs-Instance property scope and inherited override handling;
- multi-DWG context keyed by live BricsCAD `Document` identity;
- Save As drawing-identity synchronization;
- `.qsdb` schema v3 with deterministic migration;
- persisted dirty flags/timestamps, QuantityRules and audit events;
- validated/atomic save path, backup recovery and project locking;
- XML/file-size/non-finite safety guards;
- dependency graph + bounded deterministic regeneration;
- `.qstemplate` import/export;
- revision baseline/diff persistence.

QuantityRules are project data, support dependency ordering and reject invalid/circular state rather than partially mutating outputs.

---

## 4. Architecture / Room / Door-Opening / Curtain source paths

### TƯỜNG KT

Current source has semantic + guarded native paths for:

- Tường Gạch / `ArchitecturalWall`;
- Vách Kính / `GlassWall`;
- Trụ Tường / `WallPier`;
- LINE and supported open-POLYLINE centerlines;
- bulged segments through deterministic tessellation where the current command supports them;
- wall-junction L/T/X/Multi analysis;
- review-gated wall endpoint **Preview → Apply** cleanup;
- finite/self-intersection/miter/bevel guards.

### Room / HT_PHÒNG

`QS3DROOMAUTO` supports guarded plan-view LINE/POLYLINE/ARC/SPLINE boundary discovery with deterministic snapping/intersection/T-junction handling, bridge removal, bounded-face traversal, stable provenance and non-destructive stale/reuse lifecycle.

HT_PHÒNG semantics include floor/waterproofing/skirting/wall/ceiling finish workflows. Boundary provenance must not become duplicate semantic ownership of wall source handles.

### Door / Opening

Current source includes:

- Door/Opening semantic capture;
- manual host link;
- conservative Auto Host with floor/zone/elevation/ambiguity guards;
- semantic opening deductions;
- physical opening cut paths for supported generated wall hosts;
- guarded straight-polyline and newer curved-host source paths where current source explicitly supports them;
- Door/Opening schedule/export UI added by concurrent main work.

Never generalize current guarded curved/opening support into a claim that arbitrary freeform corner-crossing booleans are solved.

### Curtain / Vách Kính

Concurrent `main` work includes dedicated Curtain Hub/frame overlay, opening-aware frame planning, generated Curtain-frame ownership/stale/health metadata and current release/schedule tooling. Inspect current Curtain source before changing it; it evolved materially during this audit.

---

## 5. Structure, quantity, recognition and reporting

Semantic structure/quantity paths exist for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork, with guarded native source paths depending on category.

Current reporting includes:

- deterministic semantic regeneration;
- BQ review/group/filter/Locate;
- drawing-unit-aware fallback takeoff;
- XLSX/CSV paths with spreadsheet/file safety guards;
- stable element/drawing references in current exports;
- Door/Opening and other schedule work added concurrently on `main`.

Recognition is deterministic/rule-based with review, confidence/margin handling, semantic collision rejection and project/company layer mappings. Whole-space B4D/recognition code must continue to exclude generated geometry families rather than recapturing generated solids as new semantic source CAD.

---

## 6. Current rebar/native generated families

Current source has guarded generated rebar families for:

1. Column longitudinal bars — `QS3DREBAR3D`;
2. Column ties — `QS3DREBARTIES3D`;
3. Beam longitudinal bars — `QS3DBEAMREBAR3D`;
4. Beam stirrups — `QS3DREBARSTIRRUP3D`;
5. supported BBS-shape geometry — `QS3DREBAR3DSHAPE`;
6. Slab X/Y mesh — `QS3DSLABREBAR3D`;
7. Structural Wall H/V mesh — `QS3DWALLREBAR3D`;
8. **Foundation X/Y mesh — `QS3DFOUNDATIONREBAR3D`**.

### Foundation Mesh — merged in `df43d672...`

Foundation Mesh deliberately reuses **`RectangularSlabMeshPlanner`** rather than forking another mesh math engine.

Native adapter contract:

- selected QS3D `Foundation` semantic source;
- one closed 4-vertex rectangular plan-view `POLYLINE` per Foundation element;
- rotated rectangles supported;
- bulged/arbitrary polygons rejected rather than placing straight bars outside the host;
- duplicate semantic ownership rejected before CAD mutation;
- bounded batch bar count;
- finite-safe coordinate/offset math;
- X and Y may use **independent diameter, count or spacing**;
- one direction cannot specify count and spacing simultaneously;
- `RebarFoundationFaces = Bottom | Top | Both`;
- `RebarFoundationXClosestToFace` controls layer ordering;
- native transaction commits before Foundation stale state is cleared.

Dedicated generated metadata starts with `GeneratedFoundationMesh*`, including handles/count/diameters/actual spacing/cover/faces/mode.

Detailed contract: `docs/FOUNDATION-REBAR3D.md`.

### Mesh Setup

`QS3DREBARMESHSETUP` now supports:

- Slab;
- StructuralWall;
- Foundation.

The setup UI validates **explicit user input** only. It does not recommend structural reinforcement. A previous artificial same-diameter restriction was removed: direction 1 and direction 2 may use independent diameter/count/spacing because the native planners support that contract.

### Beam consistency fix

Beam longitudinal native geometry now uses the same **5 mm near-horizontal planarity tolerance** as Beam Stirrup. This avoids the previous inconsistent state where the same slightly noisy Beam LINE could pass stirrup generation but fail longitudinal generation.

---

## 7. Generated ownership, invalidation and stale lifecycle

Generated rebar ownership is fail-closed. Before destructive replacement, a handle must be owned by the exact element/property family. Cross-element/cross-family handle conflicts are rejected rather than erased.

Current rebar-generated ownership families include:

- `GeneratedRebarHandles`;
- `GeneratedShapeRebarHandles`;
- `GeneratedTieRebarHandles`;
- `GeneratedBeamStirrupHandles`;
- `GeneratedSlabMeshHandles`;
- `GeneratedWallMeshHandles`;
- `GeneratedFoundationMeshHandles`.

Host geometry rebuild through `GeneratedDependentGeometryInvalidator` invalidates/erases owned dependent rebar sets, including Foundation Mesh, and preserves the current Curtain generated-frame lifecycle.

`ProjectElement` now tracks per-output stale snapshots for **nine generated output families**:

1. generated host solid;
2. longitudinal rebar;
3. BBS-shape rebar;
4. Column ties;
5. Beam stirrups;
6. Slab mesh;
7. Wall mesh;
8. Foundation mesh;
9. Curtain frame.

A semantic/source mutation marks only existing generated outputs stale. Replacing a handle set or explicitly completing that builder clears its own stale family without pretending unrelated outputs were rebuilt.

---

## 8. Health model

`QS3DREBARHEALTHALL` includes longitudinal, shape, ties, stirrups, Slab mesh, Wall mesh and Foundation mesh plus cross-family ownership checks.

`QS3DHEALTHALL` aggregates:

- semantic/model health;
- generated stale health;
- rebar family health;
- Curtain-frame health;
- rebar-specific cross-key ownership;
- generic generated-handle ownership (`GeneratedHandleOwnershipHealthService` from concurrent `main`);
- generated rebar mode/category metadata checks;
- dedupe + Locate.

`GeneratedRebarModeHealthService` was corrected so Slab/Wall/Foundation mesh validation reads their **dedicated handle and mode slots** rather than incorrectly depending on `GeneratedRebarHandles`.

Foundation health command: `QS3DFOUNDATIONREBARHEALTH`.

Do not weaken Health to make incomplete data appear valid.

---

## 9. BBS boundary

BBS schedule semantics and native mesh geometry are intentionally separate.

`ProjectRebarScheduleBuilder` relies on explicit semantic BBS/cutting/distribution data. Slab/Wall/Foundation native mesh geometry does **not** automatically invent fabrication hooks, anchorage, cutting lengths or schedule rows from footprint geometry alone.

Do not fabricate missing engineering/fabrication data merely to make every native mesh appear in BBS.

---

## 10. UI entry points

Current source exposes the main rebar/health workflow through:

- Ribbon QTY tab;
- Full Domain Hub;
- Rebar 3D Hub;
- Mesh Setup;
- Health All / Rebar Health All.

Foundation Mesh and Foundation Health are present beside Slab/Wall mesh. Concurrent `main` also contains current Release Readiness, Door schedule and Curtain tools; preserve those entries when editing Ribbon/Hub files.

---

## 11. Static/smoke regression source

Foundation integration added/extended source gates for:

- Foundation native source/ownership/health/UI contracts;
- generated stale snapshots;
- unified Rebar Health All;
- full Health All;
- mode/category health;
- Foundation-specific smoke registration;
- nine-family generated stale regression;
- generated-output snapshot health regression.

`preflight-all.py` auto-discovers `preflight-*.py`, so no separate Foundation workflow was added.

**Important validation boundary:** during this audit these new/modified preflight scripts and Core smoke tests were added/reconciled in source, but were **not executed in this chat**, and GitHub Actions were **not dispatched**.

---

## 12. V25 runtime / release boundary

The repository contains current package/DemandLoad/runtime-probe/release-readiness source. Concurrent `main` also added synthetic sample/release preparation work.

Historical green GitHub runs certify only their exact older snapshots. They do not certify the current `main`.

Historical V25 Gate C remained blocked/queued because no matching licensed `[self-hosted, windows, x64, bricscad-v25]` runner was available.

For the current source, still required before claiming V25 runtime completion:

- build adapter against the exact installed V25 `BrxMgd.dll` / `TD_Mgd.dll`;
- DemandLoad/NETLOAD on licensed BricsCAD V25;
- command/Ribbon/palette smoke regression;
- private-DWG regression for geometry, ownership, save/reopen/multi-DWG behavior;
- Foundation/Slab/Wall rebar native geometry verification in real drawing units;
- Unicode/HiDPI/screenshot comparison on the real runtime.

Precise wording remains mandatory:

**source-implemented / deterministic-Core-covered / static-regression-source-present ≠ NETLOAD/runtime-verified in BricsCAD V25.**

---

## 13. Remaining product work

Major remaining runtime/product gaps include:

- exact current V25 compile/NETLOAD/private-DWG proof;
- generalized clipped/polygonal Slab/Foundation mesh beyond the guarded rectangle adapter;
- broader structural rebar authoring such as advanced wall zones, multi-zone Beam reinforcement and editing/manipulation;
- fabrication hooks/bend radii/anchorage only when explicit engineering data exists;
- more production-grade Curtain/Pier authoring beyond current guarded paths;
- more complete wall-junction solid reconciliation for complex intersections;
- arbitrary freeform/corner-crossing opening booleans beyond current guarded source paths;
- real-runtime UI/DPI polish;
- production signing/updater/licensing infrastructure and certificates where applicable.

---

## 14. Next-agent checklist

1. Read `AGENTS.md`, `CI_POLICY.md` and this handoff.
2. Fetch current `main`; do not assume `df43d672...` is still HEAD.
3. Inspect commits newer than `df43d672...` before touching shared Ribbon/Hub/Health/release files.
4. Never reintroduce a second Slab/Foundation mesh math engine; reuse/generalize current planners.
5. Preserve independent mesh direction inputs.
6. Preserve per-output stale snapshots and fail-closed generated ownership.
7. Do not infer BBS fabrication data from native mesh geometry without explicit semantic inputs.
8. Do not run Actions unless the user explicitly authorizes CI/workflow execution.
9. Do not call current native paths runtime-verified without a licensed V25 build/NETLOAD/private-DWG proof.

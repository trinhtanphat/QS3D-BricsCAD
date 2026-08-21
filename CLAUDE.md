# CLAUDE.md — QS3D-BricsCAD Repository Guide

This file is the fast orientation guide for Claude / Claude Code and other coding agents working in this repository. It summarizes the product, architecture, business workflows, infrastructure, safety contracts, validation model, and repository execution rules.

**This file is not a second source of truth.** Current source and the canonical documents linked below win whenever this summary becomes stale. Dated audit/handoff files are historical evidence, not authority for current behavior.

---

## 1. Read this before changing anything

Before substantive work, read the current versions of these files from `main`:

1. `AGENTS.md`
2. `docs/MAIN-WRITE-AUTHORIZATION.md`
3. `docs/PRODUCT-BOUNDARY.md`
4. `CI_POLICY.md`
5. `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`
6. refresh the exact current `origin/main` SHA
7. `docs/AGENT-WORK-REGISTRATION.md`
8. `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md`
9. `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md`
10. the minimum relevant open Issue/PR reservation metadata for the intended lane
11. `docs/REMOTE-AGENT-SCOPE.md`
12. the current feature/domain documentation for the code being changed

Useful additional canonical references:

- `docs/SOURCE-OF-TRUTH.md`
- `docs/ARCHITECTURE.md`
- `docs/COMMANDS.md`
- `docs/PROJECT-SETUP.md`
- `docs/HEALTH-AND-PREFLIGHT.md`
- `docs/SCHEDULES.md`
- `docs/IMPLEMENTATION-STATUS.md`
- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-V26-QUALIFICATION.md`
- `docs/SECURE-UPDATES.md`
- `docs/QS3D-PLATFORM-MIGRATION.md`

### Rule precedence

When instructions conflict, do not guess. Re-read the canonical current files. In particular:

- `docs/MAIN-WRITE-AUTHORIZATION.md` controls who may merge/change `main`.
- `CI_POLICY.md` controls GitHub Actions behavior and protected check semantics.
- `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` controls duplicate-agent/Lane-Key ownership.
- `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md` controls continuation and terminal reporting.
- `docs/REMOTE-AGENT-SCOPE.md` controls what source-only agents may claim versus `LOCAL_ONLY` runtime evidence.
- Current source and canonical product docs take precedence over old `REVIEW-*`, `AUDIT-*`, `HANDOFF-*`, or dated status documents.

---

## 2. Product identity and hard boundary

**QS3D-BricsCAD is a Windows x64 plugin hosted by BricsCAD, not a standalone CAD executable.**

Current host lanes:

| Product assembly | Host | Target | Role |
| --- | --- | --- | --- |
| `QS3D.BricsCAD.V25.dll` | BricsCAD V25 | `.NET Framework 4.8` / `net48` | V25 host adapter, commands, WPF/Ribbon/CAD integration |
| `QS3D.BricsCAD.V26.dll` | BricsCAD V26 | `.NET 8 Windows` / `net8.0-windows` | V26 host build with host-major-specific boundaries |
| `QS3D.Core.dll` | host-neutral | `netstandard2.0` | deterministic domain, persistence, geometry, quantity, diagnostics, reporting and shared contracts |

BricsCAD owns the native DWG database, editor/document lifecycle, viewport, selection, CAD transactions and proprietary native/managed CAD APIs. QS3D adds semantic project state, commands, Ribbon, palettes/modeless WPF tools, deterministic calculations, reporting, recognition and guarded generated geometry.

The QS3D product-family split is deliberate:

- **`QS3D-BricsCAD`** — this repository; BricsCAD V25/V26 hosted plugin.
- **`QS3D-Platform`** — sibling vendor-neutral shared/domain layer being migrated incrementally.
- **`QS3D-CAD`** — sibling standalone desktop CAD/BIM/QS product.

Do not turn this repository into a standalone `QS3D.exe`, do not treat `QS3D-CAD` runtime evidence as BricsCAD evidence, and do not copy proprietary host SDK binaries/types across product boundaries.

BLT / BLT3D wording is a **clean-room workflow/UX reference only**. Never use BLT source, binaries, licenses, private APIs or proprietary assets.

---

## 3. System mental model

The shortest correct mental model is:

```text
BricsCAD V25/V26 host
  DWG database / Document / Editor / viewport / native CAD transactions
        |
        v
QS3D.BricsCAD.V25 / QS3D.BricsCAD.V26
  Commands / Ribbon / PaletteSet / modeless WPF
  selection + Handle adapters
  native CAD read/write + Solid3d builders
  drawing affinity / lifecycle / UI synchronization
        |
        v
QS3D.Core
  project/domain state
  geometry + units
  semantic rules + dependencies
  deterministic regeneration + quantities
  persistence / audit / revision / templates
  diagnostics / health / reporting / exports
        |
        v
.qsdb semantic/project sidecar
```

### Authoritative data

| Concern | Authority | Derived/cache examples |
| --- | --- | --- |
| native CAD geometry | active DWG in BricsCAD | normalized geometry/metrics in semantic elements |
| Project / Zone / Floor / Family | `.qsdb` | Workspace view models |
| semantic Element metadata and relationships | `.qsdb` | property panes, dependency indexes |
| formula/rule definitions | Core/rule catalog | BQ/schedule rows |
| quantity results | deterministic regeneration | UI/XLSX/CSV output |
| generated CAD identity | semantic ownership metadata + live DWG Handle validation | UI selection/highlight state |

Never persist a runtime BricsCAD `ObjectId` as cross-session identity. Persistent CAD references use drawing identity/fingerprint plus hexadecimal entity Handles.

Derived quantity/output may be rebuilt. Invalid or missing source geometry is a health problem, not permission to invent data.

---

## 4. Repository layout

```text
src/
  QS3D.Core/                 CAD-independent domain and deterministic logic
  QS3D.BricsCAD.V25/         BricsCAD V25 net48 host adapter + main shared host/UI source
  QS3D.BricsCAD.V26/         BricsCAD V26 net8 Windows host build / V26-specific boundaries

tests/
  QS3D.Core.SmokeTests/      deterministic CAD-independent regression/smoke executable

scripts/                     preflights, build/package/install/update/release/runtime helpers
samples/generated/           repository-owned synthetic fixtures only
docs/                        canonical product/workflow/runbook docs + historical evidence
.github/workflows/            shared validation, host lanes and release workflows

QS3D.sln                     V25-oriented solution
QS3D.V26.sln                 isolated V26/Core/smoke solution
```

`QS3D.Core` includes domain areas such as project/domain state, geometry, units, persistence, services, diagnostics/health, quantity/reporting, export/interchange, recognition/takeoff, schedules, rebar, revisions/review, templates, mapping, audit, documentation, coordination, cost/commercial and related shared infrastructure. Do not infer that every Core capability has identical user-facing UI maturity; check current command/UI wiring and feature docs before making a shipping claim.

### V25/V26 source sharing

V25 is the established host adapter. V26 is a real .NET 8 rebuild lane and links much of the V25 C#/XAML host source while replacing/excluding host-specific entry/update boundaries where needed.

Therefore any shared V25 host edit has two compatibility surfaces. Never relabel a V25 DLL as V26, mix V25/V26 BricsCAD assemblies, reuse V25 package/update identity for V26, or claim runtime parity from source sharing alone.

---

## 5. Core business/domain model

### Project

A QS3D project is drawing-bound semantic state persisted to `.qsdb`. Important responsibilities include:

- Project identity and drawing affinity/fingerprint.
- Zones and Floors/Levels.
- Families/Types and their default properties.
- Semantic Elements and instance overrides.
- source CAD Handle(s) and generated ownership Handle(s).
- dependency relationships and dirty/freshness state.
- rules, quantities and derived semantic values.
- audit/revision/persistence metadata.
- material catalog and other versioned metadata.

### Floor / Level

`ProjectFloorService` owns Floor semantics. Typical contracts include finite elevation, unique case-insensitive names, bounded project cardinality, active-floor rules, reference safety on delete, and dirty propagation when elevation or assignment changes.

Changing semantic Floor does **not** secretly translate native source CAD. Source geometry and semantic assignment are separate authorities.

### Zone

`ProjectZoneService` owns Zone CRUD, active Zone and assignment. Zone changes are semantic scope changes; they do not move CAD geometry. References and cardinality are validated fail-closed.

### Family / Type

`ProjectFamilyService` owns Family/Type lifecycle:

- ID/name uniqueness and category compatibility.
- create / duplicate / rename / delete.
- active Family selection.
- Family default properties.
- instance inheritance versus true overrides.
- safe assignment only to matching semantic categories.

When a Family default changes, inherited instance values may follow it while true instance overrides must survive. Avoid implementing a second Family store in UI or authoring code.

### Semantic Element

A semantic element binds business meaning to source CAD and may own generated output. Typical fields/relationships include:

- stable element ID and category;
- Floor / Zone / Family references;
- source Handles;
- generated Handles / ownership metadata;
- property map and effective Family/instance values;
- dependencies / host relationships;
- quantities and regeneration state;
- dirty flags and generated-stale state.

Object-based mutation services generally require the actual project-owned `ProjectElement`, not a foreign object with an equal ID.

### Material Catalog

`ProjectMaterialCatalog` provides built-in and custom materials. Custom catalog data is versioned in project metadata with validation/caps. Family and instance material references obey inheritance/override semantics. GlassWall distinguishes normal glass `Material` from `CurtainFrameMaterial`.

### Dependency / dirty / regeneration model

User or CAD changes update semantic state first, mark appropriate dirty scopes, propagate dependencies, then deterministic regenerators recalculate derived state. Generated CAD may become stale when geometry/properties/relations change; quantity-only dirtiness does not automatically mean native generated geometry is stale.

Always use the canonical dirty/generated-stale APIs and generated ownership policies instead of inventing feature-local truth.

---

## 6. Persistence and data-integrity infrastructure

`.qsdb` is product data, not a disposable cache. Treat persistence as correctness-critical.

The repository contains defensive contracts for:

- bounded project/XML input;
- hardened XML parsing and schema/current-state validation;
- canonical identifiers and reference validation;
- project/drawing identity checks;
- atomic/staged publication;
- backup/recovery behavior;
- project file locking;
- revision/baseline and stale-session protection;
- save-time validation;
- persistence stamps/checkpoints/freshness;
- audit and revision data;
- template/profile persistence.

A native CAD transaction and a `.qsdb` persistence operation are **separate durability domains**. Do not pretend they form a distributed transaction. Code that touches both must explicitly order validation, snapshot/rollback, native commit and persistence failure handling.

If malformed persisted state is intentionally recoverable, health may report it rather than making the project unreadable. Do not casually convert a repairable legacy path into total load failure without checking the canonical persistence contract.

---

## 7. Generated CAD ownership and safety

Generated native geometry is never just “whatever Handle is in a string property.” Destructive rebuild/erase must respect provenance and ownership.

Key principles:

- Generated output is owned by semantic element/category/slot metadata.
- Shared generated-handle ownership policy/index is preferred over hard-coded lists of `Generated*Handle(s)` fields.
- Before destructive replacement, validate that the expected old generated set is complete, live, of the expected native type and uniquely owned.
- Foreign/ambiguous generated geometry must fail closed before erase.
- Rebuild/rollback must not delete geometry merely because a textual Handle collides.
- Generated output should carry enough fingerprint/stale/configuration evidence to detect mismatched reruns where the feature contract requires it.
- Health/release checks must use canonical ownership/freshness rules.

This is especially important for native host solids, Curtain frames/panels and every Rebar generated family.

---

## 8. User/business capability map

The codebase is broad. The following is the working product map; verify exact current behavior against `docs/COMMANDS.md`, current source and the feature docs before changing or describing a specific command.

### 8.1 Start / Workspace / project navigation

Main UX surfaces include:

- QS3D Ribbon inside BricsCAD.
- docked Workspace palette.
- Project Tools.
- Full Domain Hub.
- Schedule Hub.
- Rebar 3D Hub.
- Curtain Hub.
- modeless BQ/schedule/review windows.
- Start/readiness/health entry points.

Modeless tools are **drawing-bound**. A window opened for DWG A must not silently mutate/export against DWG B after the user changes the active document.

The Workspace supports Family/Type scope and Instance scope, semantic selection synchronization, typed property editing, true instance overrides and override reset.

### 8.2 Project setup

Core setup capabilities:

- Project lifecycle tied to the active drawing.
- Floor/Level manager.
- Zone manager.
- Family/Type manager.
- Material Catalog.
- active context selection and semantic assignment.
- save/reload/refresh/regeneration.
- project inspection and health.

### 8.3 Direct Draw / authoring

Direct Draw creates **new native BricsCAD source geometry**, captures it into the normal semantic model and converges on existing regeneration/native builders. It is not a second CAD or semantic engine.

P0 Direct Draw:

- Architectural Wall.
- Beam.
- Column.
- Slab.

Guarded additional authoring:

- GlassWall.
- WallPier.
- StructuralWall.
- Foundation.
- Door.
- WallOpening.

Authoring contracts include Model Space checks, finite/unit-aware geometry, planarity tolerance, active Family/category compatibility, current-DWG revalidation, semantic regeneration before native mutation when possible, and ownership-scoped rollback.

Planar UCS is supported for translated/in-plane-rotated working UCS. Tilted/3D UCS is rejected where the Direct Draw contract cannot safely represent it. QS3D must not reset the user's UCS merely to simplify implementation.

### 8.4 Existing CAD capture / conversion

For CAD that already exists, capture/conversion workflows create semantic elements from supported native sources instead of redrawing them. Typical domains include:

- wall / glass wall / wall pier;
- beam / slab / column / structural wall / foundation;
- stair / railing / earthwork;
- room;
- door / opening;
- recognition/Quick Takeoff assisted flows.

Semantic capture is expected to be transactional at project-state level and must reject QS3D-generated outputs as new semantic input to prevent generated→source feedback loops.

### 8.5 Wall / Tường KT

Wall workflows cover:

- ArchitecturalWall / GlassWall / WallPier capture and Direct Draw.
- native 3D build/rebuild for supported semantics.
- wall quantity review.
- L/T/X/Straight/End/Multi junction analysis.
- fingerprinted wall-snap preview then apply.
- LINE/open-POLYLINE and guarded curved/bulged planning where implemented.

Do not invent physical multi-owner wall-solid union semantics. Current safe workflows use source-centerline cleanup plus ownership-aware generated invalidation/rebuild.

### 8.6 Door / WallOpening

Door/opening workflows include:

- capture and Direct Draw;
- automatic host matching;
- explicit manual host link;
- host compatibility / Floor / Zone / elevation / ambiguity checks;
- explicit physical boolean cut;
- selection-scoped cut and broader all-linked cut;
- dedicated guarded curved/bulged host cut path;
- Door/Opening schedule and XLSX with host provenance.

Direct Draw deliberately stops after source + semantic + verified host link. Physical host mutation remains explicit.

Host linkage uses semantic host identity (for example `HostWallId`) and native generated host validation. Re-host/unlink/property changes may stale dependent overlays or generated state.

### 8.7 Room and finishes

Room workflows include:

- manual Room capture;
- bounded automatic Room discovery from supported planar line/polyline/arc/spline networks;
- non-destructive provenance/stale-room lifecycle;
- room-linked finish generation/synchronization;
- Floor Finish, Waterproofing, Skirting, Wall Finish and Ceiling Finish domains;
- Room Finish schedule and XLSX.

Topology changes should preserve auditability; do not silently delete stale auto rooms just to make counts look clean.

### 8.8 Curtain Wall

Curtain capabilities include:

- GlassWall semantic/backing host.
- Curtain family/configuration UI.
- deterministic panel/grid/schedule calculations.
- perimeter/mullion/transom native frame overlays.
- native clear-glass panel pieces.
- opening-aware interruption/clipping.
- LINE and guarded open/bulged WCS-XY source-path support where current source allows it.
- independent generated frame and panel ownership slots/health.
- bounded native output counts before destructive replacement.
- Curtain schedule/XLSX and health.

The backing GlassWall remains the host solid used by opening booleans; frames and panels are separate generated ownership families.

### 8.9 Structure / earthwork

Semantic and quantity/native paths cover major structure categories including:

- Beam.
- Slab.
- Column.
- StructuralWall.
- Foundation.
- Stair.
- Railing.
- Earthwork.

Supported source conventions vary by category (for example LINE for linear members and closed POLYLINE for footprint-based elements). Inspect the exact builder before assuming a geometry type is supported.

### 8.10 Recognition / B4D / Quick Takeoff

Recognition and takeoff include:

- deterministic recognition + human review;
- confidence-gated auto-apply;
- bounded Current Space scanning;
- entity-type compatibility checks;
- project layer mappings;
- generated-output exclusion through shared ownership policy;
- Quick Takeoff with drawing-unit conversion;
- B4D-assisted quantity/recognition review.

Do not auto-capture semantically incompatible entities simply because layer/text matches. Do not let generated QS3D output feed back into source recognition.

### 8.11 Quantity / BQ

Quantity is deterministic Core business logic, not UI state.

Key capabilities include:

- recalculation/regeneration before reporting where required;
- quantity summary by semantic project data;
- filter/group/search by Floor/category/etc.;
- Locate/reveal back to source/model element;
- gross/net/length/area/volume/mass/count quantities where applicable;
- opening deductions and provenance where supported;
- stable source Handle + Element ID + drawing fingerprint traceability;
- XLSX exports;
- ED2 detailed and aggregate workbook workflows.

Unsupported or inapplicable quantities should not be filled with invented zeros. NaN/Infinity/overflow/invalid semantic values must fail closed rather than be silently clamped.

### 8.12 ED2 / Excel provenance

ED2 supports scoped export such as Selection/Floor/Zone/All and produces detailed/aggregate workbook views. Traceability includes semantic Element ID, live CAD Handle and drawing fingerprint.

Excel-driven Locate must validate that workbook provenance matches the active project/drawing and that required Handles still resolve before changing CAD selection. Never trust a workbook row as authority over current project/DWG state.

### 8.13 Material usage

Material usage groups effective material by domain context and preserves contributing Element IDs for audit. Instance material overrides beat Family defaults.

Primary quantity follows unit semantics (`m`, `m²`, `m³`, `kg` as appropriate). Export uses a real XLSX/OpenXML package and should validate/package atomically rather than leave a partially written output.

### 8.14 Schedules

`QS3DSCHEDULES` aggregates specialist review/export flows. Dedicated schedule domains include:

- BQ.
- Room Finish.
- Material Usage.
- Curtain Wall.
- Door/Opening.
- Rebar / BBS.

Shared schedule invariants:

- document-bound modeless behavior;
- checked finite arithmetic instead of unchecked aggregation;
- fail-closed invalid quantity data;
- lazy preferred/fallback quantity evaluation;
- deterministic grouping/provenance;
- regenerate/recalculate before export where the workflow contract requires it.

### 8.15 Rebar 3D and BBS

Implemented generated families include:

1. Column longitudinal bars.
2. Beam longitudinal bars.
3. BBS-shape-driven bars.
4. Beam stirrups.
5. Column ties.
6. Slab X/Y mesh.
7. StructuralWall horizontal/vertical mesh.
8. Foundation X/Y mesh.

Rebar infrastructure also includes:

- semantic setup/notation/cover/faces;
- BBS review/Locate;
- XLSX and UTF-8 CSV export;
- generated ownership/freshness/health;
- family-specific generated handles plus cross-family ownership diagnostics;
- fail-closed destructive replacement.

Do not invent fabrication hooks, bend radii, anchorage or code-specific dimensions when explicit business data is absent.

### 8.16 Review / viewport

QS3D exposes review helpers such as:

- highlight / unhighlight;
- focus / zoom selected / zoom all;
- isolate / restore;
- 3D/top/orbit views;
- semantic Locate;
- native section box / section plane / clip display where supported by the installed BricsCAD edition;
- semantic untrack operations that do not delete source CAD.

Review/viewport state is transient UI state, not project truth.

### 8.17 Revision / audit / templates

Core infrastructure includes:

- project audit trail;
- revision baseline and diff;
- deterministic snapshot/comparison/reporting contracts;
- template/profile persistence/apply paths;
- source/project freshness checks.

Identity, cardinality, numeric and serialization boundaries should be fail-closed and bounded.

### 8.18 Health / release readiness

`QS3DHEALTHALL` is the broad semantic/source/generated model integrity review. It covers evolving services for project references, dependencies, generated ownership/freshness, supported generated families and other model consistency checks.

`QS3DRELEASECHECK` is stricter and adds release-readiness/liveness/BOM/runtime-facing guards. Never weaken health/release guards merely to make incomplete data look green.

### 8.19 Cost / commercial / coordination / interoperability / documentation infrastructure

`QS3D.Core` also contains broader business/infrastructure modules such as Cost, Commercial, Coordination, Interoperability, Documentation/semantic sheets, Mapping and related workflows. These are shared deterministic domain services and may support estimating, rate/cost projections, clashes/coordination, mapping/interchange or documentation planning.

**Do not assume these modules are all on the current customer P0 UI path.** Before exposing/extending them, inspect current source, current Issues and domain-specific docs. The current BIM3D-QS product priority emphasizes model → quantity → review/locate → export correctness before claiming broad 4D/5D workflow completeness.

---

## 9. Important BricsCAD command families

The authoritative inventory is `docs/COMMANDS.md`; do not maintain a competing exhaustive manifest here. Major command families include:

### Workspace / project / health

`QS3D`, `QS3DHIDE`, `QS3DDOMAIN`, `QS3DPROJECTTOOLS`, `QS3DSCHEDULES`, `QS3DREFSEARCH`, `QS3DZONES`, `QS3DLEVELS`, `QS3DFAMILIES`, `QS3DMATERIALS`, `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN`, `QS3DINSPECT`, `QS3DHEALTH`, `QS3DHEALTHALL`, `QS3DRELEASECHECK`, `QS3DOWNERSHIPHEALTH`.

### Basic/context drawing and IFC delegation

`QS3DDRAWLINE`, `QS3DDRAWRECT`, `QS3DDRAWCIRCLE`, `QS3DDRAWBYCAD`, `QS3DDRAWPROFILE`, `QS3DFLOORSLOPE`, `QS3DSLABCUT`, `QS3DJOINCORNER`, `QS3DJOINTEE`, `QS3DIFCIMPORT`, `QS3DIFCIMPORTLIGHT`, `QS3DIFCREMOVE`, `QS3DIFCEXPORT`.

Several of these delegate intentionally to native BricsCAD commands instead of implementing a duplicate CAD/IFC engine.

### Direct Draw

`QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`, `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`, `QS3DDRAWDOOR`, `QS3DDRAWOPENING`.

### Room / finish

`QS3DROOM`, `QS3DROOMAUTO`, `QS3DFINISH`, `QS3DFINISHSCHEDULE`, `QS3DFINISHXLSX`.

### Wall / opening / native build

`QS3DWALL`, `QS3DGLASSWALL`, `QS3DWALLPIER`, `QS3DWALLQTY`, `QS3DWALLJUNCTIONS`, `QS3DWALLSNAPPREVIEW`, `QS3DWALLSNAPAPPLY`, `QS3DBUILD3D`, `QS3DOPENING`, `QS3DDOOR`, `QS3DAUTOLINKHOSTS`, `QS3DLINKHOST`, `QS3DCUTSELECTEDOPENINGS`, `QS3DCUTOPENINGS`, `QS3DCUTOPENINGSCURVED`, `QS3DDOORSCHEDULE`, `QS3DDOORXLSX`.

### Curtain

`QS3DCURTAIN`, `QS3DCURTAINXLSX`, `QS3DCURTAINFRAMES3D`, `QS3DCURTAINFRAMEHEALTH`, `QS3DCURTAIN3D`.

### Structure / takeoff / recognition / BQ

`QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`, `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`, `QS3DTAKEOFF`, `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`, `QS3DB4D`, `QS3DBQ`, `QS3DSETUPBLT`, `QS3DED2`, `QS3DEXCELLOCATE`, `QS3DMATERIALXLSX`.

### Rebar / BBS

`QS3DREBARHUB`, `QS3DREBARMESHSETUP`, `QS3DBBSVIEW`, `QS3DBBS`, `QS3DBBSCSV`, `QS3DREBAR3D`, `QS3DBEAMREBAR3D`, `QS3DREBAR3DSHAPE`, `QS3DREBARSTIRRUP3D`, `QS3DREBARTIES3D`, `QS3DSLABREBAR3D`, `QS3DWALLREBAR3D`, `QS3DFOUNDATIONREBAR3D`, plus family-specific and aggregate health commands.

### Review / revision

`QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT`, `QS3DFOCUS`, `QS3DISOLATE`, `QS3DUNISOLATE`, `QS3DSECTIONBOX`, `QS3DSECTIONPLANE`, `QS3DCLIPDISPLAY`, `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL`, `QS3DLOCATE`, `QS3DUNTRACK`, `QS3DUNTRACKFINISH`, `QS3DREVBASE`, `QS3DREVDIFF`.

Automation-only qualification probes also exist. They are guarded test infrastructure, **not normal user-facing product commands**, and must not be advertised as such.

---

## 10. Numerical, identity and input-integrity posture

QS3D generally prefers **fail closed** over silently publishing a plausible but wrong engineering result.

When changing Core boundaries, actively check:

- `NaN` / `Infinity` rejection;
- arithmetic overflow;
- swallowed non-zero floating-point addends/products where the established domain contract rejects representational loss;
- signed zero/canonical formatting where relevant;
- negative or nonsensical dimensions/quantities;
- explicit cardinality limits before expensive materialization;
- known `Count` validity and, where the API treats Count as integrity evidence, Count-versus-traversal agreement;
- null entries and malformed collection implementations;
- leading/trailing whitespace on canonical identity tokens;
- control characters/XML safety;
- case sensitivity versus case-insensitive identity according to the domain contract;
- duplicate identity and ambiguous ownership;
- bounded strings, files, archives and collection inputs.

Do not generalize a hardening pattern blindly. Preserve the public contract of the exact type/service and add a focused deterministic regression for the demonstrated defect.

---

## 11. Units and geometry

Core is CAD-independent and should receive normalized units where the host adapter owns drawing-unit interpretation.

Typical engineering output uses metres / square metres / cubic metres and related quantity units. Quick Takeoff and host commands must respect drawing unit conversion. Do not mix raw drawing units and engineering units casually.

Geometry code should be deterministic, finite and bounded. For native authoring:

- validate before native mutation when possible;
- obey Model Space / UCS / source-type contracts;
- do not assume every Polyline/bulge/freeform case is implemented;
- preserve source-versus-generated distinction;
- keep native transaction and semantic rollback ordering explicit.

A source/static proof of a geometry planner is not proof that BricsCAD `Solid3d`/Boolean behavior succeeds on a real DWG.

---

## 12. UI and multi-document safety

BricsCAD can host multiple open DWGs. Modeless QS3D windows/palettes must not redirect mutations when `MdiActiveDocument` changes.

For a drawing-bound window:

1. capture/bind the intended Document/DWG identity;
2. revalidate before project mutation, CAD selection, export or nested command execution;
3. fail closed or ask the user to reactivate the bound DWG instead of silently operating on another drawing;
4. keep post-commit UI refresh/focus best-effort when failure must not roll back already-valid CAD/project state.

Visible labels are UI text, not stable semantic dispatch identity. Prefer stable IDs/FeatureIds/commands/contracts for behavior.

---

## 13. Testing and validation

### Generic repository/source preflight

```bash
python scripts/preflight.py
```

### Aggregate focused preflights

```bash
python scripts/preflight-all.py
```

`preflight-all.py` discovers `scripts/preflight-*.py` gates in deterministic order. Feature gates are regression fences, not permission to weaken architecture just to satisfy string/source-shape checks.

### Core build and deterministic smoke

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

These cover CAD-independent domain, persistence, geometry, quantity, health, export/interchange and many regression cases.

### V25 host build

```powershell
$env:BRICSCAD_V25_DIR = '<BricsCAD V25 installation directory>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

### V26 host build

```powershell
$env:BRICSCAD_V26_DIR = '<BricsCAD V26 installation directory>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Do not commit BricsCAD proprietary assemblies. Do not point the V25 project at V26 assemblies or vice versa.

---

## 14. CI model

The shared non-publishing validation workflow is `.github/workflows/ci.yml`.

Current conceptual evidence layers are:

1. **automatic task-branch validation** for `agent/**` / `integration/**` pushes;
2. **protected PR merge-candidate validation** with stable required contexts `preflight` and `core`;
3. **combined integration validation** where an authorized multi-agent coordinator assembles an integration branch;
4. **exact-main release/cloud validation** when applicable after an integration-relevant authorized landing.

Important rules:

- CI evidence must be tied to the exact current SHA/candidate.
- Old green runs become stale after any head change/reconciliation.
- A known red branch/current-candidate failure is agent-owned remediation work.
- Do not create no-op commits, replace a correct canonical PR or bypass gates merely to manipulate CI identity/timing.
- Do not manually dispatch/re-run/cancel workflows unless current policy explicitly authorizes that action.
- A generic connector showing no classic commit status does not prove Actions are absent; follow `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md`.
- `preflight` and `core` on the current protected PR candidate are mandatory merge evidence.

CI is validation, not licensed BricsCAD runtime proof.

---

## 15. Packaging, install, update and release security

V25 and V26 have separate package/update identities. Never cross-use them.

V25 packaging/install/update infrastructure includes:

- `scripts/package-v25.ps1`;
- DemandLoad install/uninstall helpers;
- Mark-of-the-Web/unblock recovery helpers;
- package metadata and SHA-256 manifests;
- bounded ZIP validation;
- signed production update manifests;
- transactional staging/backup/rollback;
- version/downgrade checks;
- host-major isolation.

Production update security is intentionally strict. Executable payload trust includes plugin/Core DLLs and install/update/uninstall scripts. The updater validates HTTPS/origin rules, archive hash/size, safe entries, exact internal hash coverage, signer identity, version binding and rollback prerequisites before installation.

Do not weaken BricsCAD security settings such as `SECURELOAD`, package verification, path containment, signer checks or downgrade protection just to make installation easier.

Release/publication is a separate privilege/scope from ordinary code/docs merge. Do not claim `RELEASED` unless exact publication evidence is in scope and verified.

---

## 16. Remote-safe versus LOCAL_ONLY evidence

Source-only/hosted agents may implement and prove:

- Core/domain/geometry/persistence/reporting fixes;
- deterministic smoke tests;
- static/source preflights;
- source-visible ownership/rollback/transaction contracts;
- adapter source changes whose correctness can be reviewed statically;
- package/update validators not requiring real secrets;
- local qualification probes/scripts.

The following are `LOCAL_ONLY` unless actually executed in the required environment:

- licensed BricsCAD NETLOAD/DemandLoad;
- exact host-major proprietary API runtime behavior;
- native `Solid3d` / Boolean / DrawJig / Editor / UCS behavior on real BricsCAD;
- real multi-DWG/modeless UI lifecycle;
- Ribbon/WPF/HiDPI visual acceptance;
- private/customer DWG validation;
- clean-machine installer/update behavior requiring Windows/BricsCAD;
- Authenticode private-key/signing evidence;
- large-project native performance.

Never manufacture `LOCAL_PASS` from static source review or cloud CI. If a source change creates a new materially different local qualification requirement, update the canonical `docs/LOCAL-AGENT-INBOX.md` item instead of repeatedly rediscovering it remotely.

---

## 17. Required GitHub work lifecycle for an agent

Normal owner-requested repository work has a protected PR lifecycle. **Never write directly to `main`.**

### Before mutation

1. Fetch exact current `main`.
2. Identify the concrete semantic task.
3. Search the minimum open Issue/PR reservation metadata needed to detect a collision.
4. Determine one stable Lane-Key, normally `issue-<number>`.
5. If another ACTIVE canonical owner/carrier already owns the same lane, stop overlapping work as `DUPLICATE_CARRIER / NO MUTATION` unless the owner/coordinator explicitly reassigns it.
6. Otherwise register/reuse one Issue and create one canonical `agent/**` branch from the latest valid baseline.

### Implement

7. Keep source/tests/docs/claims for the task on that one carrier.
8. Make the smallest coherent change that fixes the demonstrated behavior.
9. Add focused regression coverage/source guard where appropriate.
10. Validate locally/source-side within actual capability.
11. Commit and push the same canonical branch; never force-push shared `main`.

### CI / PR / merge

12. Observe automatic exact-head branch CI and remediate real failures on the same carrier.
13. Refresh `main`; safely reconcile if required, without taking over another lane or silently dropping changes.
14. Open/update exactly one canonical PR with required metadata:

```text
Lane-Key: issue-123
Canonical owner/session: <stable-session-id>
Canonical carrier: agent/<agent-id>/<scope>
Supersedes: none
```

15. Observe protected current-candidate `preflight` + `core`.
16. Fix red checks when the root cause is safely inside the lane; do not merely report them.
17. Recheck current `main`, freshness, mergeability, review threads and expected head.
18. Under current standing same-task authorization, merge the **same task PR** through the protected PR path once all required gates are green/current/mergeable, unless the owner explicitly opted out or a real policy/tool/runtime blocker applies.
19. Refresh `main` and record the exact landed SHA.

The normal successful endpoint for a task is `MERGED_MAIN`, not `PR_OPEN` or “CI running.”

### Never do these

- direct contents/ref write to `main`;
- force-push/reset shared `main`;
- bypass protected checks;
- hide a known red failure behind a new PR/carrier;
- create a second carrier because the first is stale/red/queued/slow;
- take over another session's ACTIVE lane during a broad `continue all`;
- inspect/manage unrelated agents' work beyond the minimum collision metadata;
- manually trigger release/CI workflows without policy authorization;
- report stale green evidence as current.

---

## 18. Bug-fix discipline

When asked to “review/fix/continue all,” do not manufacture speculative backlog. Work from current source and reproducible evidence.

For each new defect:

1. reproduce or prove the exact source-level invariant failure;
2. collision-check semantic behavior and expected file/symbol ownership;
3. register a separate concrete Lane-Key if it is genuinely new;
4. keep scope narrow;
5. fix root cause, not only the symptom;
6. add deterministic regression that fails before the fix when practical;
7. preserve surrounding API/error/ordering semantics not implicated by the bug;
8. continue through CI/PR/merge instead of stopping at a report;
9. keep licensed host behavior explicitly `LOCAL_ONLY` when it was not run.

A broad umbrella Issue is not one shared implementation Lane-Key for every discovered fix.

---

## 19. Architectural anti-patterns to reject

Do not introduce:

- a standalone app shell in this repository;
- a second project/Family/material/quantity engine beside the canonical one;
- UI view-model state as authoritative project data;
- persisted runtime `ObjectId` identity;
- hidden trimming/normalization that aliases malformed canonical IDs unless the public contract explicitly requires normalization;
- unchecked engineering arithmetic that silently publishes NaN/Infinity/overflow or known-invalid quantities;
- unbounded caller-controlled enumeration/file/archive materialization where an established resource contract exists;
- feature-local generated-handle lists when shared ownership policy exists;
- destructive erase/rebuild based only on textual Handle values;
- automatic Door/Opening physical cuts hidden inside semantic authoring when the workflow requires explicit mutation;
- native CAD/IFC reimplementations where a deliberate BricsCAD delegation exists;
- packaging of proprietary BricsCAD assemblies;
- weakening secure update/install checks;
- claims that cloud/static validation equals licensed V25/V26 runtime qualification;
- duplicate agent Issues/branches/PRs for an already-owned lane.

---

## 20. Practical “where do I look?” map

| Need | Start here |
| --- | --- |
| product form / sibling boundary | `docs/PRODUCT-BOUNDARY.md` |
| system layering | `docs/ARCHITECTURE.md` |
| source/data authority | `docs/SOURCE-OF-TRUTH.md` |
| all BricsCAD command names/workflows | `docs/COMMANDS.md` |
| project/Floor/Zone/Family/Material | `docs/PROJECT-SETUP.md` |
| current source implementation snapshot | `docs/IMPLEMENTATION-STATUS.md` |
| health/preflight contracts | `docs/HEALTH-AND-PREFLIGHT.md` |
| BQ/domain schedules | `docs/SCHEDULES.md` |
| secure updater/install trust | `docs/SECURE-UPDATES.md` |
| current GitHub CI behavior | `CI_POLICY.md` |
| work reservation / branch / PR | `docs/AGENT-WORK-REGISTRATION.md` |
| duplicate lane races | `docs/AGENT-DUPLICATE-PROMPT-RACE-POLICY.md` |
| CI lookup/recovery | `docs/AGENT-BRANCH-CI-ACTIONS-LOOKUP.md` |
| continuation/reporting | `docs/AGENT-PROMPT-TO-RELEASE-CONTRACT.md` |
| merge/main authority | `docs/MAIN-WRITE-AUTHORIZATION.md` |
| remote/local evidence boundary | `docs/REMOTE-AGENT-SCOPE.md` |
| V25 licensed runtime | `docs/LOCAL-V25-QUALIFICATION.md` |
| V26 licensed runtime | `docs/LOCAL-V26-QUALIFICATION.md` |
| Platform migration | `docs/QS3D-PLATFORM-MIGRATION.md` |

---

## 21. Final operating principle

Optimize for **correct engineering state on current `main`**, not for producing a status report.

For product code, preserve the chain:

```text
live DWG source
  -> validated semantic/project state
  -> deterministic dirty/dependency/regeneration
  -> guarded generated ownership/native mutation
  -> validated .qsdb persistence
  -> health/review
  -> quantity/schedule/export derived output
```

For repository work, preserve the chain:

```text
current main
  -> collision-safe Lane-Key
  -> one canonical branch
  -> focused implementation + regression
  -> exact-head validation
  -> protected current PR candidate
  -> fresh preflight + core
  -> protected merge
  -> verify exact landed main SHA
```

When uncertain, inspect current source and the canonical document for that boundary before coding. Do not guess business rules, native runtime behavior, ownership, or merge authority.
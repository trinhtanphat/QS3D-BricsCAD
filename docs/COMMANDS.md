# QS3D command reference

Updated for the integrated source baseline on 2026-08-10. These names are **BricsCAD command-line/plugin commands after QS3D is loaded**, not standalone EXE or PowerShell commands. Commands that create/mutate native BricsCAD geometry remain subject to the licensed V25 runtime gate.

## Workspace, project and schedules

- `QS3D` — open the docked QS3D Workspace.
- `QS3DHIDE` — hide QS3D palettes.
- `QS3DDOMAIN` — open Full Domain Hub.
- `QS3DPROJECTTOOLS` — open the drawing-bound Project Tools hub.
- `QS3DSCHEDULES` — open the drawing-bound Schedule Hub for BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedules/exports.
- `QS3DREFSEARCH` — open the drawing-bound **Tham khảo thi công** launcher for Hình ảnh/Web/Video/Mua sắm/Video ngắn/Tin tức. QS3D URL-encodes the query, uses fixed HTTPS search URLs with SafeSearch and opens results in the Windows default browser; it does not scrape/embed result pages. See `CONSTRUCTION-REFERENCE-SEARCH.md`.
- `QS3DZONES` — Zone Manager: CRUD/active Zone/semantic assignment.
- `QS3DLEVELS` — Floor/Level project editor.
- `QS3DFAMILIES` — Family Manager: create/duplicate/rename/delete/properties/assignment while preserving true instance overrides. The **TẠO MỚI** Ribbon and Full Domain Hub expose it as the canonical Family / Type launcher before Direct Draw.
- `QS3DMATERIALS` — Material Catalog.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — persistence and deterministic regeneration.
- `QS3DINSPECT` — inspect current/prompted CAD selection and synchronize the Workspace.
- `QS3DHEALTH` — basic Model Health.
- `QS3DHEALTHALL` — aggregate semantic/source/generated/live-solid/stale/rebar/curtain health.
- `QS3DRELEASECHECK` — unified source/project release-readiness review. Includes safe generated ownership, all current generated rebar families including Foundation mesh, mode semantics, live CAD and BOM release guards. A clean result is still **not** a substitute for the licensed V25/private-DWG runtime gate.
- `QS3DOWNERSHIPHEALTH` — provenance-safe generated handle ownership review.
- `QS3DRUNTIMEPROBE` — V25 runtime identity/readiness probe.
- `QS3DBRCPROBE` — **automation-only clean-room diagnostic**, not a user-facing command. It runs only when the qualification harness supplies its result-marker environment variable and must be used only on a disposable reference copy of an authorized drawing, never on the original. The probe uses public BricsCAD APIs and emits sanitized aggregate capability/count data only; it does not emit drawing paths, CAD handles, layer/text/property values, call BLT APIs, or open/read BLT program binaries. Its purpose is to determine whether proxy/BRC entities expose supported public measurements; it is not a BLT compatibility or reverse-engineering path.
- `QS3DBRCROUNDTRIPPROBE` — **automation-only local qualification**, not a user-facing command. The guarded runner NETLOADs the exact V25 adapter, runs `QS3DB4D`, exports a newly created ED2 workbook, reads row 2 from `CHI_TIET`, validates Drawing Fingerprint + Element ID ↔ Handle provenance, resolves every Handle before changing PICKFIRST, and confirms metricless `ProxyEntity` candidates remain review-only/uncaptured. It accepts only a disposable `*.reference-copy.dwg`, refuses a pre-existing `.qsdb`, verifies the DWG SHA-256 before/after, writes only sanitized aggregate counts/booleans, and never calls BLT/private APIs or modifies the reference original.

- `QS3DLIFECYCLESEED`, `QS3DLIFECYCLEAFTERSAVE`, `QS3DLIFECYCLEPROBE`, `QS3DLIFECYCLECOMMANDPREP`, `QS3DLIFECYCLECOMMANDVERIFY` — **automation-only LOCAL-001 qualification**, not user-facing commands. `scripts/test-bricscad-v25-project-lifecycle.ps1` creates four disposable copies of the repository-generated sample, proves DWG `SaveComplete` sidecar persistence, cold-cache reopen/canonical binding, distinct ProjectIds and multi-DWG mutation isolation, and runs the real `QS3DREGEN`, `QS3DREFRESH` and `QS3DFINISH` commands against both a cold existing project and an absent-sidecar drawing. It also runs `QS3DBQ` to prove automatic legacy quantity-unit binding on the canonical project and native-unit resolution without a project, then runs `QS3DUNITS` with unresolved INSUNITS and a scope-validated one-shot automation confirmation to prove that an explicit accepted choice is the intentional project/bootstrap boundary. Normal user calls still use `Editor.GetKeywords`; physical keyboard/prompt UX remains part of the interactive matrix. A corrupt sidecar must fail closed and remain unchanged. Final evidence contains only nonce-bound booleans/counts and hashes; it does not emit drawing paths, ProjectIds, fingerprints, Handles, semantic names or raw exception text.
- `QS3DSIDECARREVISIONPROBE` — **automation-only LOCAL-001 qualification**, not a user-facing command. `scripts/test-bricscad-v25-sidecar-revision.ps1` uses one repository-generated disposable DWG copy and, with the project cache deliberately kept warm, tests backup appearance, primary replacement and primary removal. Read-only access, canonical bind, existing-project mutation, Interchange confirmation and Save must all refuse stale authority without changing semantic or DWG state; restoring the original bytes must recover the same canonical session. Marker evidence is nonce-bound booleans/counts/hashes only.

Project mutation APIs follow a shared integrity rule: object-based Floor/Zone/Family/Bulk Edit operations reject foreign `ProjectElement` objects even when their ID matches an element already stored in the project.

## BLT-style Family / Instance workflow

The Workspace property pane has two scopes:

- **Family / Type** — edit Family defaults; inherited values update while true instance overrides are preserved.
- **Đối tượng / Instance** — used when exactly one semantic element is selected; edits affect only that element and `↺` resets an override to the current Family value.

Typed controls include finite-number/text fields, boolean checkbox and editable choices. Semantic selection resolves source handles and generated owner slots, including slab/wall/Foundation mesh and Curtain frame handles.

## Direct Draw / Tạo mới

Direct Draw is the authoring path for **new geometry inside BricsCAD**. It creates a real BricsCAD source entity, captures it into the normal QS3D semantic model and reuses established semantic/native workflows. Legacy capture commands remain the path for CAD that was already drawn.

Use `QS3DFAMILIES` first when a different compatible Family / Type should become active. Direct Draw then consumes that active Family and prompts/validates the key instance values needed by the command.

### P0

- `QS3DDRAWWALL` — draw a new ArchitecturalWall from two or more plan-view points. Two points create a LINE source; longer paths create an open POLYLINE. Prompts/inherits wall thickness, height and bottom offset, then creates semantic + owned native 3D in one operation.
- `QS3DDRAWBEAM` — draw a new Beam from two plan-view points. Prompts/inherits width, height and bottom offset, then creates the semantic Beam + native prism.
- `QS3DDRAWCOLUMN` — pick the Column center, then prompt/inherit width, depth, height and bottom offset. QS3D creates a rectangular closed-POLYLINE source + semantic Column + native 3D.
- `QS3DDRAWSLAB` — interactively pick at least three plan-view boundary points and press Enter. Prompts/inherits slab thickness and bottom offset, then creates a closed-POLYLINE source + semantic Slab + native 3D.

P0 is intentionally guarded: Model Space only, unit-aware 5 mm planarity checks, semantic regeneration before native mutation, full project snapshot and verified cleanup of operation-owned source/generated CAD on failure.

### Guarded P1 native subset

- `QS3DDRAWGLASSWALL` — draw a GlassWall from two or more plan-view points, prompt/inherit thickness/height/bottom offset, capture semantic state and reuse `QS3DBUILD3D` for the backing native GlassWall host. Dedicated Curtain frames/panels remain a `QS3DCURTAIN3D` / Curtain Hub workflow.
- `QS3DDRAWWALLPIER` — pick exactly two plan-view points and create a LINE source, prompt/inherit thickness/height/bottom offset, then reuse the specialized WallPier dispatch. The LINE path preserves current Rectangular/Chamfered `WallPierProfileSolidBuilder` semantics; multi-segment Direct Draw is deliberately rejected until a deterministic profile-around-corners contract exists.
- `QS3DDRAWSTRUCTWALL` — draw a two-point StructuralWall LINE, prompt/inherit thickness/height/bottom offset and reuse canonical `QS3DBUILD3D` / structural builder behavior.
- `QS3DDRAWFOUNDATION` — draw a closed Foundation POLYLINE from at least three plan-view points, prompt/inherit thickness/bottom offset and reuse canonical `QS3DBUILD3D` / structural builder behavior.

P1 also runs in Model Space, uses the same 5 mm unit-aware plan-view tolerance, writes instance values through canonical `SetProperty()`, revalidates active-DWG affinity before nested build, and requires a live `GeneratedSolidHandle` after `QS3DBUILD3D`. Failure cleanup is ownership/XData-scoped and post-commit UI synchronization is non-destructive.

### Door / Opening Direct Draw

- `QS3DDRAWDOOR` — pick two plan-view door-edge points. QS3D creates a real LINE source whose plan length becomes `WidthM`, prompts/inherits `HeightM`, non-negative sill/bottom offset and `BooleanClearanceM`, captures exactly one Door, then runs selection-scoped Auto Host. Success requires a verified `HostWallId` and post-link semantic regeneration.
- `QS3DDRAWOPENING` — same guarded workflow for `WallOpening`.

Door/Opening Direct Draw is Model-Space/unit-aware, rejects explicitly invalid Family numerics, uses canonical `SetProperty()` writes, rechecks the active DWG around nested Auto Host, and rolls an unmatched/ambiguous new opening back instead of leaving an orphan source/semantic object.

**Physical boolean remains explicit.** The newly created source stays selected after successful authoring, so the safer follow-up is `QS3DCUTSELECTEDOPENINGS`, which resolves only selected Door/WallOpening semantic targets. `QS3DCUTOPENINGS` remains the broader all-linked operation. Neither is silently auto-invoked by Direct Draw.

### Planar UCS contract

P0, guarded P1 and Door/Opening Direct Draw share the current source-level UCS contract: point prompts stay in the active UCS; translated/in-plane-rotated UCS is allowed; persisted LINE/POLYLINE source geometry is transformed through `Editor.CurrentUserCoordinateSystem` before Model Space append; tilted/3D UCS is rejected before source creation. The user's UCS is not reset or mutated. See `docs/DIRECT-DRAW-UCS.md`. Exact World/translated/30°/45°/90° behavior still requires licensed V25 runtime qualification on the final SHA.

Ribbon **TẠO MỚI** and the Full Domain Hub expose Family / Type plus the current Direct Draw set and selected-opening cut. See `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`, `docs/DIRECT-DRAW-OPENINGS.md` and `docs/DIRECT-DRAW-UCS.md` for exact source/runtime boundaries.

## Room and finish workflow

- `QS3DROOM` — capture selected source as Room.
- `QS3DROOMAUTO` — discover bounded rooms from planar LINE/POLYLINE/ARC/SPLINE networks. Curve sampling, planarity, segment and minimum-area limits are guarded.
- `QS3DFINISH` — generate/synchronize room finish semantics.
- `QS3DFINISHSCHEDULE` — review Room Finish schedule.
- `QS3DFINISHXLSX` — export Room Finish schedule to XLSX.

Room Auto is non-destructive: stable provenance can reuse Rooms and topology changes can mark superseded Rooms stale instead of silently deleting audit data.

## Tường KT / Wall workflow

- `QS3DWALL` — capture Tường Gạch / ArchitecturalWall.
- `QS3DGLASSWALL` — capture Vách Kính / GlassWall and seed Curtain defaults.
- `QS3DWALLPIER` — capture Trụ Tường / WallPier and seed profile defaults.
- `QS3DWALLQTY` — open the drawing-bound Wall Quantity takeoff workspace: search/filter by floor/category, inspect one semantic wall per detail row, review visible-row totals, recompute on a detached snapshot, export the visible wall scope to XLSX, and use guarded default-on `Bám 3D` / explicit `Định vị 3D` to revalidate the current semantic wall + current source Handles before CAD select/zoom. The merged source path is not a substitute for licensed V25 modeless/viewport qualification; see `WALL-QUANTITY-TAKEOFF.md`.
- `QS3DWALLJUNCTIONS` — analyze L/T/X/Straight/End/Multi wall-centerline junctions and report a reviewable endpoint plan.
- `QS3DWALLSNAPPREVIEW` — calculate/fingerprint supported straight endpoint cleanup without mutation.
- `QS3DWALLSNAPAPPLY` — apply only the matching preview signature; stale preview/curved/bulged/nonsemantic source fails closed.
- `QS3DBUILD3D` — build/update native 3D for supported semantic source.

Physical L/T/X multi-owner wall-solid union/reconciliation is not implemented by guessing. Current wall snap is a safe source-centerline cleanup workflow followed by ownership-aware generated invalidation/rebuild.

## Door / Opening workflow

- `QS3DOPENING` — capture WallOpening from pre-existing CAD.
- `QS3DDOOR` — capture Door from pre-existing CAD.
- `QS3DDRAWOPENING` — author a new WallOpening LINE directly and require verified Auto Host.
- `QS3DDRAWDOOR` — author a new Door LINE directly and require verified Auto Host.
- `QS3DAUTOLINKHOSTS` — safe automatic host matching using compatibility, surface gap, Floor/Zone, ambiguity and elevation gates. It does not silently cut the host.
- `QS3DLINKHOST` — explicit manual host link.
- `QS3DCUTSELECTEDOPENINGS` — resolve the current CAD/semantic selection to a deduplicated Door/WallOpening target set and physically cut only that requested subset. Every selected target/host is prevalidated before native mutation; stale generated hosts fail closed. Same-fingerprint reruns are idempotent; a different cut state on the same already-cut generated host requires host rebuild first.
- `QS3DCUTOPENINGS` — guarded broader physical cut for all currently linked openings on supported hosts; keep this an explicit mutation step after Direct Draw/capture.
- `QS3DCUTOPENINGSCURVED` — dedicated curved/bulged open-POLYLINE host path; plans/fingerprints before `BoolSubtract` and keeps identical reruns idempotent.
- `QS3DDOORSCHEDULE` — drawing-bound Door/Opening schedule with host provenance.
- `QS3DDOORXLSX` — Door/Opening XLSX export.

Opening link/re-host/unlink and relevant opening property changes stale dependent GlassWall frame overlays without unnecessarily stale-marking the backing wall host.

## Curtain Wall / Vách Kính

- `QS3DCURTAIN` — Curtain Wall Hub/Family editor.
- `QS3DCURTAINXLSX` — deterministic Curtain schedule export.
- `QS3DCURTAINFRAMES3D` — generate/update supported perimeter/mullion/transom frame overlays. Current source includes deterministic LINE plus guarded open/bulged WCS-XY path support; exact V25 behavior remains runtime-gated.
- `QS3DCURTAINFRAMEHEALTH` — frame handle/live-solid/count/grid/config/live-geometry/ownership health.
- `QS3DCURTAIN3D` — one-shot backing GlassWall host + supported frame-overlay + panel-by-panel clear-glass workflow for guarded LINE/open-bulged path sources.

Curtain frames and panel cells are interrupted/clipped deterministically around linked Door/Opening rectangles according to the supported source-path planners. The backing GlassWall remains the single host solid used by opening booleans; frame pieces own `GeneratedCurtainFrameHandles` and native panel pieces own the independent `GeneratedCurtainPanelHandles` slot. Panel replacement is bounded to 4,096 native pieces per element and 8,192 per batch before destructive replacement. Source/static wiring is not exact-SHA BricsCAD V25 runtime proof; see `docs/CURTAIN-NATIVE-PANELS.md` and LOCAL-002.

Curtain destructive and health ownership indexes use the shared generated-owner policy, so newly added generated families are protected without updating a manual slot list. Do not call current open/bulged-path frame work runtime-verified until the licensed V25 gate is executed.

## Structure / earthwork capture

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff with drawing unit conversion.

Native source conventions include LINE for supported linear structure and closed POLYLINE for supported footprint-based structure. Exact behavior remains part of the V25 runtime gate.

## Recognition and BQ

- `QS3DRECOGNIZE` — deterministic recognition + review.
- `QS3DRECOGNIZEAUTO` — auto-apply only sufficiently confident recognition.
- `QS3DB4D` — bounded Current Space scan. It rejects layer/text matches whose CAD entity type is incompatible, excludes every generated owner-slot handle through the shared ownership policy, and keeps planar area separate from `Solid3d` total surface area. For recognized material solids, native mass-properties volume is authoritative over default prism estimates.
- `QS3DBQ` — quantity summary/filter/group/Locate/XLSX.
- `QS3DED2` — choose `Selection`, active `Floor`, active `Zone` or `All`; regenerate that semantic scope, then export `CHI_TIET` (one element per row) and Zone-aware `TONG_HOP` in one newly created workbook. Both sheets preserve Element ID/Handle/fingerprint provenance and expose element name, category, effective material, Family ID, Floor/Zone, engineering quantities, optional `DensityKgM3`/derived or explicit mass, and notes. Missing density stays blank rather than being guessed.
- `QS3DEXCELLOCATE` — locate a `CHI_TIET`/QS3D workbook row only when Element ID, CAD Handle and DWG fingerprint provenance agree with the active project and every Handle still resolves. Legacy BLT `$decimal` Handle rows are the only no-fingerprint path and require explicit `YES`; failures preserve the current CAD selection.

## Material schedules

- `QS3DMATERIALS` — Material Catalog.
- `QS3DMATERIALXLSX` — material usage/export workflow.

## Rebar schedule and setup

- `QS3DREBARHUB` — Rebar 3D Hub.
- `QS3DREBARMESHSETUP` — edit selected Slab/StructuralWall/Foundation mesh notation/cover/faces using semantic properties; changes stale existing generated mesh output.
- `QS3DBBSVIEW` — BBS review/Locate.
- `QS3DBBS` — BBS XLSX export.
- `QS3DBBSCSV` — UTF-8 CSV export with spreadsheet-safety guards.

## Rebar 3D

### Longitudinal and shapes

- `QS3DREBAR3D` — supported rectangular-column longitudinal bars.
- `QS3DBEAMREBAR3D` — supported Beam LINE longitudinal bars.
- `QS3DREBAR3DSHAPE` — supported straight/L/U/Z/custom BBS-shape-driven geometry.
- `QS3DREBARHEALTH` — longitudinal generated health.
- `QS3DREBARSHAPEHEALTH` — shape generated health.

### Beam stirrups / Column ties

- `QS3DREBARSTIRRUP3D` — bounded beam-stirrup loop distribution.
- `QS3DREBARSTIRRUPHEALTH` — beam-stirrup health.
- `QS3DREBARTIES3D` — bounded rectangular Column tie generation.
- `QS3DREBARTIEHEALTH` — Column tie health.

### Slab mesh

- `QS3DSLABREBAR3D` — X/Y mesh on supported rectangular Slab footprint.
- `QS3DSLABREBARHEALTH` — count, independent X/Y diameter/spacing, cover, faces, ownership and live-solid health.

### StructuralWall mesh

- `QS3DWALLREBAR3D` — horizontal/vertical mesh on supported StructuralWall LINE path.
- `QS3DWALLREBARHEALTH` — independent direction diameter/spacing, cover, faces, category/ownership/live-solid health.

### Foundation mesh

- `QS3DFOUNDATIONREBAR3D` — dedicated Foundation X/Y mesh using the deterministic rectangular mesh planner with Foundation-specific ownership/stale metadata.
- `QS3DFOUNDATIONREBARHEALTH` — Foundation count, X/Y diameter/spacing, cover, faces, category, ownership and live-solid health.

### Unified rebar health

- `QS3DREBARHEALTHALL` — aggregate longitudinal, shape, ties, stirrups, slab mesh, wall mesh and **Foundation mesh**, plus cross-family ownership diagnostics.
- `QS3DHEALTHALL` and `QS3DRELEASECHECK` add semantic/generated stale/mode/live/BOM checks.

Destructive rebar/tie/curtain guards use the shared generated ownership policy so foreign generated geometry fails closed before erase. Current stirrup/tie geometry does not invent fabrication hooks, bend radii or code-specific anchorage when explicit dimensions are absent.

## Review / viewport

- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT` — transient review highlight.
- `QS3DFOCUS` — focus/zoom current selection.
- `QS3DISOLATE`, `QS3DUNISOLATE` — temporary isolate/restore.
- `QS3DSECTIONBOX` — native BIM Section Detail workflow when the installed edition supports it.
- `QS3DSECTIONPLANE` — native section plane.
- `QS3DCLIPDISPLAY` — native clip display toggle.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport controls.
- `QS3DLOCATE` — semantic Locate.
- `QS3DUNTRACK`, `QS3DUNTRACKFINISH` — remove semantic tracking without deleting source CAD.

## Revision

- `QS3DREVBASE`, `QS3DREVDIFF` — revision baseline/delta workflow.

## Packaging and autoload

- `scripts/package-v25.ps1` packages `src/QS3D.BricsCAD.V25/bin/x64/Release/net48`.
- It requires `QS3D.BricsCAD.V25.dll` + `QS3D.Core.dll`, excludes BricsCAD-owned assemblies, includes install/uninstall/update helpers, synthetic sample fixtures, metadata and SHA-256 hashes.
- `COMMANDS.txt` is generated directly from current source `[CommandMethod]` declarations, so new commands do not rely on a manually maintained release manifest.
- GitHub Actions remain manual-only. `release-v25.yml` additionally requires explicit `confirm_release=RELEASE` before publication.

See [`REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md`](REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md), [`DIRECT-DRAW-P0-IMPLEMENTATION.md`](DIRECT-DRAW-P0-IMPLEMENTATION.md), [`DIRECT-DRAW-P1-IMPLEMENTATION.md`](DIRECT-DRAW-P1-IMPLEMENTATION.md), [`DIRECT-DRAW-OPENINGS.md`](DIRECT-DRAW-OPENINGS.md), [`DIRECT-DRAW-UCS.md`](DIRECT-DRAW-UCS.md), [`CURTAIN-PATH-FRAMES.md`](CURTAIN-PATH-FRAMES.md) and [`ADVANCED-GEOMETRY.md`](ADVANCED-GEOMETRY.md) for current boundaries.
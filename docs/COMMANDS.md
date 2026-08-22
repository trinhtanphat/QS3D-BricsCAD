# QS3D command reference

Updated for the integrated source baseline on 2026-08-10. Commands that create/mutate native BricsCAD geometry remain subject to the licensed V25 runtime gate.

## Workspace, project and schedules

- `QS3D` — open the docked QS3D Workspace.
- `QS3DHIDE` — hide QS3D palettes.
- `QS3DDOMAIN` — open Full Domain Hub.
- `QS3DPROJECTTOOLS` — open the drawing-bound Project Tools hub.
- `QS3DSCHEDULES` — open the drawing-bound Schedule Hub for BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedules/exports.
- `QS3DZONES` — Zone Manager: CRUD/active Zone/semantic assignment.
- `QS3DLEVELS` — Floor/Level project editor.
- `QS3DFAMILIES` — Family Manager: create/duplicate/rename/delete/properties/assignment while preserving true instance overrides.
- `QS3DMATERIALS` — Material Catalog.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — persistence and deterministic regeneration.
- `QS3DINSPECT` — inspect current/prompted CAD selection and synchronize the Workspace.
- `QS3DHEALTH` — basic Model Health.
- `QS3DHEALTHALL` — aggregate semantic/source/generated/live-solid/stale/rebar/curtain health.
- `QS3DRELEASECHECK` — unified source/project release-readiness review. Includes safe generated ownership, all current generated rebar families including Foundation mesh, mode semantics, live CAD and BOM release guards. A clean result is still **not** a substitute for the licensed V25/private-DWG runtime gate.
- `QS3DOWNERSHIPHEALTH` — provenance-safe generated handle ownership review.
- `QS3DRUNTIMEPROBE` — V25 runtime identity/readiness probe.

Project mutation APIs follow a shared integrity rule: object-based Floor/Zone/Family/Bulk Edit operations reject foreign `ProjectElement` objects even when their ID matches an element already stored in the project.

## BLT-style Family / Instance workflow

The Workspace property pane has two scopes:

- **Family / Type** — edit Family defaults; inherited values update while true instance overrides are preserved.
- **Đối tượng / Instance** — used when exactly one semantic element is selected; edits affect only that element and `↺` resets an override to the current Family value.

Typed controls include finite-number/text fields, boolean checkbox and editable choices. Semantic selection resolves source handles and generated owner slots, including slab/wall/Foundation mesh and Curtain frame handles.

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
- `QS3DWALLJUNCTIONS` — analyze L/T/X/Straight/End/Multi wall-centerline junctions and report a reviewable endpoint plan.
- `QS3DWALLSNAPPREVIEW` — calculate/fingerprint supported straight endpoint cleanup without mutation.
- `QS3DWALLSNAPAPPLY` — apply only the matching preview signature; stale preview/curved/bulged/nonsemantic source fails closed.
- `QS3DBUILD3D` — build/update native 3D for supported semantic source.

Physical L/T/X multi-owner wall-solid union/reconciliation is not implemented by guessing. Current wall snap is a safe source-centerline cleanup workflow followed by ownership-aware generated invalidation/rebuild.

## Door / Opening workflow

- `QS3DOPENING` — capture WallOpening.
- `QS3DDOOR` — capture Door.
- `QS3DAUTOLINKHOSTS` — safe automatic host matching using compatibility, surface gap, Floor/Zone, ambiguity and elevation gates. It does not silently cut the host.
- `QS3DLINKHOST` — explicit manual host link.
- `QS3DCUTOPENINGS` — guarded physical cuts on supported straight host paths.
- `QS3DCUTOPENINGSCURVED` — dedicated curved/bulged open-POLYLINE host path; plans/fingerprints before `BoolSubtract` and keeps identical reruns idempotent.
- `QS3DDOORSCHEDULE` — drawing-bound Door/Opening schedule with host provenance.
- `QS3DDOORXLSX` — Door/Opening XLSX export.

Opening link/re-host/unlink and relevant opening property changes stale dependent GlassWall frame overlays without unnecessarily stale-marking the backing wall host.

## Curtain Wall / Vách Kính

- `QS3DCURTAIN` — Curtain Wall Hub/Family editor.
- `QS3DCURTAINXLSX` — deterministic Curtain schedule export.
- `QS3DCURTAINFRAMES3D` — generate/update supported LINE perimeter/mullion/transom frame overlays.
- `QS3DCURTAINFRAMEHEALTH` — frame handle/live-solid/count/grid/config/live-geometry/ownership health.
- `QS3DCURTAIN3D` — one-shot backing GlassWall host + supported LINE frame overlay workflow.

Curtain LINE frames can be interrupted deterministically around linked Door/Opening rectangles. The backing GlassWall remains the single host solid used by opening booleans; frame pieces own separate `GeneratedCurtainFrameHandles`.

Curtain destructive and health ownership indexes use the shared generated-owner policy, so newly added generated families are protected without updating a manual slot list. Curved/open-POLYLINE native frame overlays remain product/runtime work.

## Structure / earthwork capture

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff with drawing-unit conversion.

Native source conventions include LINE for supported linear structure and closed POLYLINE for supported footprint-based structure. Exact behavior remains part of the V25 runtime gate.

## Recognition and BQ

- `QS3DRECOGNIZE` — deterministic recognition + review.
- `QS3DRECOGNIZEAUTO` — auto-apply only sufficiently confident recognition.
- `QS3DB4D` — bounded Current Space scan. It reads supported CAD metrics and now excludes **every generated owner-slot handle via the shared generated ownership policy**, preventing QS3D output from being re-ingested as source CAD when new generated families are added.
- `QS3DBQ` — quantity summary/filter/group/Locate/XLSX.
- `QS3DED2` — ED2-style Excel/Handle workflow.
- `QS3DEXCELLOCATE` — locate workbook rows with DWG fingerprint safety; legacy no-fingerprint handle rows require explicit confirmation.

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

See [`REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md`](REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md) for the current deep review and [`ADVANCED-GEOMETRY.md`](ADVANCED-GEOMETRY.md) for geometry-specific limits.

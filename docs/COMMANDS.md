# QS3D command reference

Updated for the current source baseline on 2026-08-10. Commands that create or mutate native BricsCAD geometry remain subject to the licensed V25 runtime gate.

## Workspace and project

- `QS3D` — open the docked QS3D workspace.
- `QS3DHIDE` — hide QS3D palettes.
- `QS3DDOMAIN` — open the Full Domain Hub.
- `QS3DSAVE`, `QS3DRELOAD`, `QS3DREFRESH`, `QS3DREGEN` — project persistence and deterministic regeneration.
- `QS3DINSPECT` — inspect current/prompted CAD selection and synchronize the workspace review/property pane.
- `QS3DHEALTH` — run the basic Model Health checks.
- `QS3DHEALTHALL` — aggregate model/source/generated-solid/stale-state, generated-rebar ownership/mode, slab/wall mesh and curtain-frame health into one review window with Locate support.
- `QS3DRUNTIMEPROBE` — inspect V25 runtime availability/identity.

## BLT-style property workflow

The workspace property pane has two scopes:

- **Family / Type** — edits the selected Family defaults. Existing elements that still equal the previous Family value inherit the change; true instance overrides are preserved.
- **Đối tượng / Instance** — automatically selected when exactly one semantic element matches the CAD selection. Edits affect only that element. The `↺` action resets an override back to the current Family value.

Common boolean fields use a checkbox, mode/material/classification-like fields use editable choices, and other fields use text/numeric editors with finite-value validation. Selection matching uses semantic reference handles, so auto-room boundary provenance can participate without duplicating ownership handles.

## Semantic capture

### Room / finishes

- `QS3DROOM` — capture selected CAD source as Room.
- `QS3DROOMAUTO` — discover bounded room faces from selected planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges are tessellated deterministically; SPLINE is sampled by bounded chord length before the Core engine splits intersections/T-junctions, snaps endpoints, removes dangling bridges and calculates room area/perimeter.
- ARC/POLYLINE plan-view orientation is validated and LINE/ARC/POLYLINE/SPLINE sampled elevations must remain within `RoomBoundaryToleranceM`.
- Room Auto metadata: `RoomBoundaryToleranceM` (default `0.005`), `RoomBoundaryMinimumAreaM2` (default `0.5`), `RoomBoundaryArcSagittaM` (default `0.002`), `RoomBoundarySplineChordM` (default `0.02`). SPLINE sampling is capped.
- Room Auto lifecycle is non-destructive: stable source provenance reuses rooms when possible; topology split/merge can mark superseded rooms `Stale` instead of deleting them.
- `QS3DFINISH` — generate/synchronize room finish semantics.

### Tường KT / Cửa

- `QS3DWALL` — capture Tường Gạch / ArchitecturalWall.
- `QS3DGLASSWALL` — capture Vách Kính / GlassWall and seed curtain-grid/frame defaults for newly created GlassWall families.
- `QS3DWALLPIER` — capture Trụ Tường / WallPier and seed rectangular/chamfered profile defaults.
- `QS3DWALLJUNCTIONS` — analyze selected LINE/open-POLYLINE wall centerlines and classify L/T/X/Straight/End/Multi junction nodes. The report also includes a reviewable endpoint snap plan.
- `QS3DWALLSNAPPREVIEW` — calculate endpoint cleanup for **QS3D semantic wall source** LINE/open straight POLYLINE only. It records a SHA-256 preview signature; curved/bulged polylines are review-only and are not moved automatically.
- `QS3DWALLSNAPAPPLY` — apply the previously previewed endpoint moves only when the current selection/geometry/tolerances still hash to the same preview signature. Changed source geometry requires previewing again. Affected semantic owners are marked dirty and generated dependent geometry is invalidated through ownership guards.
- Wall snap metadata: `WallJunctionToleranceM` (default `0.005`), `WallJunctionPlanarityToleranceM` (defaults to junction tolerance), `WallJunctionSnapEpsilonM` (small movement/no-op epsilon).
- `QS3DOPENING` — capture Lỗ Mở Vách.
- `QS3DDOOR` — capture Cửa Đi.
- `QS3DAUTOLINKHOSTS` — safely match selected Door/Opening semantics to the nearest compatible wall host. Matching uses wall surface gap rather than centerline distance alone, groups tessellated segments by semantic host, rejects near-tie ambiguity, respects Floor/Zone when assigned, and applies an independent source-elevation gate before linking. It does **not** silently run the physical boolean cut.
- Auto Host metadata: `AutoHostMaxGapM` (default `0.25`), `AutoHostAmbiguityM` (default `0.02`), `AutoHostElevationToleranceM` (default `0.25`), plus `WallArcSagittaM` for curved polyline centerline matching.
- `QS3DLINKHOST` — manually link selected Door/Opening semantics to a selected wall host; this remains the explicit override workflow.
- `QS3DCUTOPENINGS` — physically subtract linked Door/Opening cutters from generated LINE hosts and supported straight, non-bulged open-POLYLINE host segments when the opening safely projects to one segment and does not cross a corner/junction.
- `QS3DCUTOPENINGSCURVED` — plan and subtract cutters against supported bulged open-POLYLINE wall centerlines. The service prepares all footprints/vertical plans and a deterministic fingerprint **before** `BoolSubtract`; rerunning the same solid/fingerprint is idempotent, while changed geometry on the same already-cut host requires a host rebuild first.
- Physical-cut invalidation clears handle/fingerprint/count/**mode** metadata so an old straight/curved mode cannot survive source replacement.

### Vách Kính / Curtain Wall

- `QS3DCURTAIN` — open the dedicated Curtain Wall Hub/Family editor.
- `QS3DCURTAINXLSX` — export deterministic curtain schedule data.
- `QS3DCURTAINFRAMES3D` — generate/update mullion/transom/perimeter-frame `Solid3d` overlays for supported horizontal GlassWall `LINE` sources. Generated frames use dedicated ownership and do **not** replace the backing wall host.
- `QS3DCURTAINFRAMEHEALTH` — review generated curtain-frame handles, live solids, ownership, frame/grid counts, stored dimensions and deterministic configuration fingerprint.
- `QS3DCURTAIN3D` — one-shot GlassWall workflow: build/update the backing host for selected GlassWall LINE/open-POLYLINE sources, then add frame overlays for supported LINE sources and regenerate semantic quantities.
- Curtain Family controls include `CurtainMaxPanelWidthM`, `CurtainMaxPanelHeightM`, `CurtainPerimeterFrameWidthM`, `CurtainMullionWidthM`, `CurtainTransomWidthM`, `CurtainFrameDepthM` and `CurtainFrameMaterial`.
- Frame metadata uses `GeneratedCurtainFrameHandles` plus a `GeneratedCurtainFrameConfigFingerprint`. Changing panel grid/frame depth/bottom offset/current dimensions is detected as a stale frame snapshot even if semantic quantity regeneration has already completed.
- The current native curtain implementation intentionally keeps **one backing GlassWall host** for Door/Opening booleans and a separate frame overlay. Door/Opening cuts do not yet interrupt frame solids, and open/curved POLYLINE hosts do not yet get curved frame overlays.

### Structure / earthwork

- `QS3DBEAM`, `QS3DSLAB`, `QS3DCOLUMN`, `QS3DSTRUCTWALL`, `QS3DFOUNDATION`.
- `QS3DSTAIR`, `QS3DRAILING`, `QS3DEARTHWORK`.
- `QS3DTAKEOFF` — Quick Takeoff from current CAD selection.

## Native 3D

- `QS3DBUILD3D` — create/update generated `Solid3d` for supported selected semantic elements.
- Tường Gạch / GlassWall backing host: LINE or open POLYLINE centerline; bulges are tessellated and guarded miter/bevel footprint logic is used.
- WallPier LINE uses the specialized deterministic rectangular/chamfered profile builder; open POLYLINE WallPier currently falls back to the generic Tường KT footprint pipeline.
- Use `QS3DCURTAIN3D` when a GlassWall LINE also needs curtain-frame overlays.
- LINE source: beam, structural wall, railing.
- Closed POLYLINE source: slab, column, foundation, stair footprint mass, earthwork footprint mass.
- Earthwork is extruded downward by `DepthM`.

## Recognition, quantity and rebar

- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO` — deterministic recognition + review/auto-accept.
- `QS3DB4D` — bounded scan of the active Current Space. It reads curve/closed-area/Region/Hatch/Solid3d metrics, excludes QS3D-generated mass/rebar geometry, auto-applies only high-confidence recognition and leaves ambiguous results for review.
- `QS3DBQ`, `QS3DED2` — quantity summary, filtering/grouping/Locate/XLSX. Export rows include stable QS3D Element IDs, hexadecimal CAD handles and the owning DWG fingerprint.
- `QS3DEXCELLOCATE` — read a chosen workbook row, reject a fingerprint that differs from the active DWG, then select/zoom the resolved entities. Legacy BLT `$<decimal handle>` rows remain readable but require typing `YES` because those workbooks carry no fingerprint.
- `QS3DBBSVIEW` — BBS review/Locate window.
- `QS3DBBS` — BBS XLSX export.
- `QS3DBBSCSV` — UTF-8 CSV export with spreadsheet formula-injection guards.
- `QS3DREBAR3D` — guarded rectangular-column longitudinal-bar `Solid3d` generation.
- `QS3DBEAMREBAR3D` — guarded beam longitudinal-bar generation for supported Beam `LINE` source; it uses the protected `GeneratedRebarHandles` ownership path shared by generated longitudinal bars.
- `QS3DREBARHEALTH` — verify generated longitudinal-bar ownership/handle/count state.
- `QS3DREBAR3DSHAPE` — generate supported BBS-shape-driven 3D bars. Source supports straight and configured L/U/Z/custom leg/turn paths; shape metadata and total cutting length are validated before geometry mutation.
- `QS3DREBARSHAPEHEALTH` — verify generated shape-bar ownership/handle state.
- `QS3DREBARSTIRRUP3D` — generate rectangular beam-stirrup loop solids along supported horizontal Beam `LINE` source. Distribution is driven by deterministic beam-stirrup planning using count or spacing plus section/end cover and diameter. Generated handles use dedicated ownership metadata; batch/element counts are bounded.
- `QS3DREBARSTIRRUPHEALTH` — review generated beam-stirrup handles/ownership/live-solid state and Locate affected solids.
- `QS3DREBARTIES3D` — generate rectangular column tie loop solids for supported Column semantic elements with closed 4-vertex rectangular POLYLINE source. Tie diameter, count/spacing and cover/clearance inputs are validated before native geometry mutation.
- `QS3DREBARTIEHEALTH` — review generated column tie ownership/live-solid state and Locate affected solids.
- `QS3DSLABREBAR3D` — generate X/Y slab mesh for a supported closed 4-vertex rectangular Slab POLYLINE. `RebarSlabXNotation` and `RebarSlabYNotation` may use independent diameters/count/spacing; generated state is stored under dedicated `GeneratedSlabMesh*` metadata.
- `QS3DSLABREBARHEALTH` — review slab-mesh count, independent X/Y diameters, actual spacing, cover, Top/Bottom/Both faces, ownership and live-solid state.
- `QS3DWALLREBAR3D` — generate horizontal/vertical StructuralWall mesh on supported near-horizontal LINE sources. Horizontal/vertical notation may use independent diameters/count/spacing; Near/Far/Both faces are supported through the deterministic planner and dedicated `GeneratedWallMesh*` ownership.
- `QS3DWALLREBARHEALTH` — review wall-mesh count, diameters, spacing, cover, faces, ownership/category/live-solid state.
- `QS3DREBARHEALTHALL` — aggregate generated longitudinal, BBS-shape, column-tie, beam-stirrup, slab-mesh and wall-mesh health plus cross-family ownership diagnostics into one review window.
- Beam stirrup and column tie loops currently use guarded segmented-cylinder geometry. They do **not** invent fabrication hooks, bend radii or code-specific anchorage where explicit dimensions are absent.

See [`ADVANCED-GEOMETRY.md`](ADVANCED-GEOMETRY.md) for shape/stirrup/tie/mesh/curtain metadata and current geometric limits.

## Review / viewport

- `QS3DHIGHLIGHT`, `QS3DUNHIGHLIGHT` — transient selected-object highlighting.
- `QS3DFOCUS` — focus/zoom the current selection with review emphasis.
- `QS3DISOLATE`, `QS3DUNISOLATE` — temporarily isolate selected objects and restore hidden objects.
- `QS3DSECTIONBOX` — launch BricsCAD's native `BIMSECTION` **Detail** workflow to create an interactive detail-section volume/box. Existing implied selection is highlighted as visual context only. This command requires an edition/runtime that provides `BIMSECTION`; source presence is not treated as proof that the current BricsCAD license exposes the BIM command.
- `QS3DSECTIONPLANE` — launch native `SECTIONPLANE` for a standard interactive section entity when a full BIM Detail volume is not desired.
- `QS3DCLIPDISPLAY` — launch native `CLIPDISPLAY` to toggle clipping for a selected/prompted section entity.
- Native command wrappers deliberately use English command/option names with the `_` localization prefix (`_BIMSECTION _Detail`, `_SECTIONPLANE`, `_CLIPDISPLAY`) rather than hard-coding localized command text.
- `QS3DVIEW3D`, `QS3DVIEWTOP`, `QS3DORBIT`, `QS3DZOOMSELECTED`, `QS3DZOOMALL` — viewport controls.
- `QS3DLOCATE` — locate a semantic element by ID/reference handles.
- `QS3DUNTRACK`, `QS3DUNTRACKFINISH` — remove semantic tracking without deleting source CAD.

## Revision

- `QS3DREVBASE`, `QS3DREVDIFF` — revision baseline/delta workflow.

## UI entry points

The main palette, Ribbon and Full Domain Hub expose the major product flows consistently: Room Auto, Tường KT, Curtain Hub/Curtain 3D, WallPier profile workflow, Giao tường + review-gated snap cleanup, Auto/Manual Door-Opening host linking, straight/curved physical cuts, Build 3D, Focus/Isolate/Section Box, BQ/BBS, column/beam longitudinal rebar, BBS-shape rebar, beam stirrups, column ties, slab mesh, wall mesh and unified health. The goal is to minimize command-line memorization while preserving explicit commands for power users and test harnesses.

## Packaging and autoload

- `scripts/package-v25.ps1` creates the V25 release ZIP, excludes proprietary BricsCAD assemblies, generates `COMMANDS.txt` from current QS3D `CommandMethod` declarations, records metadata and SHA-256 hashes.
- The package includes DemandLoad install/uninstall helpers with hash verification and optional Authenticode enforcement.
- DemandLoad/NETLOAD remain part of the licensed V25 runtime gate; source presence is not treated as runtime verification.

# BLT-style completion review — 2026-08-10

This review records the source-level completion batch applied directly to `main` after a full repository pass. It preserves concurrent work already merged by other agents and does not claim licensed BricsCAD V25 runtime verification.

## Review scope

- Core/domain contracts used by the UI and geometry adapters.
- Main WPF workspace, Family/property inspector, semantic tree, selected-object review, Ribbon and Full Domain Hub.
- Tường KT, Cửa/Lỗ mở, Room Auto, native wall geometry, physical opening boolean workflow and current rebar geometry path.
- Static geometry/full-domain/room-curve preflight and documentation/status consistency.
- Concurrent `main` activity was re-read before writes; stale SHA writes were allowed to fail rather than force-overwriting newer work.

## Completed in this batch

### BLT-style main workflow

- Added **Bóc chọn** to the Family/Type pane so users can select a semantic category/Family and capture the current BricsCAD selection without memorizing the corresponding command.
- The category dispatcher covers Room, Tường Gạch, Vách Kính, Trụ Tường, Door/Opening, structure/earthwork and HT_Phòng workflows.
- Family properties now use clearer Vietnamese labels, logical BLT-style groups and unit display for common geometry/rebar fields.
- Numeric validation covers numeric properties even when a field does not have a visible unit suffix, and still rejects NaN/Infinity/invalid text.
- The compact action row wraps instead of forcing all controls into one fixed horizontal line.

### Tường KT completion

- Added explicit `QS3DGLASSWALL` and `QS3DWALLPIER` semantic capture commands with safe starter Family defaults.
- Generalized the existing guarded LINE wall builder to ArchitecturalWall, GlassWall and WallPier.
- Generalized the open-POLYLINE/bulge `WallFootprintEngine` builder to the same three categories.
- Updated `QS3DBUILD3D` so the active/selected Tường Gạch, Vách Kính or Trụ Tường category is passed end-to-end into the correct native builder rather than only accepting ArchitecturalWall.
- Kept the current Vách Kính/Trụ Tường native path intentionally generic: it is a Tường KT centerline extrusion, not a claim of full curtain-wall framing/panel semantics or specialized pier profiles.

### Room Auto completion preserved and guarded

- Preserved concurrent direct planar ARC and bounded SPLINE adapters added while this review was in progress.
- `QS3DROOMAUTO` now accepts planar LINE/POLYLINE/ARC/SPLINE source networks. Direct ARC and polyline bulges are tessellated by sagitta; SPLINE is sampled by configurable chord length with a hard segment cap.
- ARC/POLYLINE plan-view orientation and all LINE/ARC/POLYLINE/SPLINE sample elevations are validated so mixed-Z/non-planar boundaries are not silently flattened.
- Room boundary provenance continues through a shared semantic reference resolver so Locate/BQ/Health/BBS/revision navigation does not require duplicate semantic ownership handles.
- Static guards cover ARC/SPLINE adapter wiring, planarity, bounded sampling and Room Auto lifecycle/rollback/performance contracts.

### Cửa/Lỗ mở and rebar integration

- Preserved the newer physical opening boolean implementation already merged concurrently: cutter preparation happens before mutation and the idempotence fingerprint includes live host/opening geometry, so moving an opening cannot be silently treated as an already-applied cut.
- Physical cutting now covers compatible generated LINE-host solids for Tường Gạch/ArchitecturalWall, Vách Kính/GlassWall, Trụ Tường/WallPier and Vách BTCT/StructuralWall.
- Surfaced `QS3DCUTOPENINGS` in Ribbon and Full Domain Hub together with capture and host-link actions.
- Surfaced the current guarded rectangular-column `QS3DREBAR3D` path in Ribbon and Full Domain Hub while keeping its scope explicit.

### UI discoverability

- Ribbon now exposes Tường Gạch, Vách Kính, Trụ Tường, Cửa/Lỗ, Link Host, Khoét Cửa/Lỗ and Cốt thép 3D instead of leaving important flows command-line-only.
- Full Domain Hub now contains the same major Tường KT/Cửa/rebar entry points.
- Main workspace, Ribbon and Domain Hub therefore share the same product workflow vocabulary.

### Regression/documentation guards

- `scripts/preflight-geometry-completion.py` now requires the Tường KT variant commands, workspace wiring, Ribbon/Hub buttons, category-aware wall builders, `QS3DBUILD3D` category forwarding and the four compatible LINE-wall boolean host categories.
- Room static guards cover direct planar ARC and bounded SPLINE Room Auto source adapters.
- README, command reference, implementation status, master plan and UI specification were refreshed to distinguish implemented source paths from runtime-verified behavior.

## Preserved concurrent fixes

During the review, `main` advanced repeatedly. The batch deliberately preserved newer concurrent work, including:

- position/host-aware opening-cut fingerprints and expanded LINE-wall host categories;
- rectangular column rebar geometry and generated-bar ownership health checks;
- Room Auto lifecycle/topology hardening, direct planar ARC and bounded SPLINE sampling;
- shared semantic reference-handle Locate behavior;
- far-origin wall/opening numeric stability guards;
- geometry-completion and curve-source preflight wiring.

No force update was used to overwrite these changes.

## Remaining high-value BLT parity work

1. Licensed V25 compile + NETLOAD/DemandLoad and private-DWG regression on the newest head.
2. Real V25 screenshots at 100/125/150/200% DPI and follow-up spacing/icon/context-menu/focus polish.
3. Production-grade Vách Kính curtain-wall framing/panels and specialized Trụ Tường profiles/material display behavior.
4. Wall-to-wall joins/T-junction cleanup, freeform/closed-loop profiles and more advanced level/elevation constraints.
5. Generalized Door/Opening booleans beyond LINE hosts, especially curved/polyline hosts.
6. General rebar authoring beyond rectangular-column longitudinal bars.
7. Native non-planar curve projection only if product requirements justify it; direct planar ARC and bounded SPLINE support are already implemented in source.
8. Transient highlight/isolate/section-box UX proven against BricsCAD V25.

GitHub Actions remain manual-only. This review updates source and static guards but does not dispatch CI or substitute for licensed BricsCAD V25 runtime proof.

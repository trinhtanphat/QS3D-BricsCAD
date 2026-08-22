# QS3D BricsCAD V25 master plan

Updated 2026-08-10 for the current `main` source baseline.

## Implemented source baseline

- clean-room layered architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI;
- `.qsdb` semantic source-of-truth with schema v3 migration, locking, validated temp save, backup/recovery, persisted dirty state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, project rule catalog, Model Health and revision foundations;
- active Zone/Floor/Family semantic property flow and multi-DWG project cache keyed by `Document` identity;
- BLT-style three-pane workspace with semantic tree, Family/Type list, grouped Vietnamese property inspector, selected-object review, HT_Phòng, plus live Xref/Drawing and Layer management;
- category-aware **Bóc chọn** flow in the workspace so model capture no longer depends on memorizing command names;
- Room / Tường Gạch / Vách Kính / Trụ Tường / Opening / Door / HT_Phòng semantic capture and quantity workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths;
- native Tường KT source paths for Tường Gạch, Vách Kính and Trụ Tường from LINE and open POLYLINE centerlines using deterministic wall footprint generation, guarded miter/bevel joining and bulge tessellation;
- native structural Solid3d source paths isolated behind the V25 adapter and two-phase generated-geometry replacement;
- Door/Opening host linking plus guarded physical boolean subtraction for compatible generated LINE-host solids across Tường Gạch, Vách Kính, Trụ Tường and Vách BTCT, with live host/opening geometry included in the idempotence fingerprint;
- rectangular-column longitudinal rebar 3D source path built from deterministic rebar layout planning, plus generated-bar ownership health diagnostics;
- Quick Takeoff Length/Area/Volume/Count with `INSUNITS` conversion;
- BQ stable-ID grouping/filtering/Locate/XLSX, real recalc and persisted visible-column preferences;
- rebar notation/BBS model, XLSX/CSV export and BBS review/Locate UI;
- persisted revision baseline/diff workflow using `.qsrev`;
- deterministic recognition review + confident auto-apply, with project layer mappings overriding fallback heuristics;
- `.qstemplate` company-standard import/export for Families, QuantityRules, layer mappings, BQ columns and generic material/classification properties;
- deterministic planar room-boundary engine with iterative bridge detection/source lookup: intersection/T-junction subdivision, endpoint snapping, dangling-bridge removal, bounded-face traversal, stable boundary keys and Area/Perimeter calculation;
- `QS3DROOMAUTO` accepts planar LINE/POLYLINE/ARC/SPLINE networks. Direct ARC and polyline bulges use configurable sagitta; SPLINE uses bounded chord sampling with a hard segment cap; source elevations are checked before face discovery;
- Room Auto lifecycle is non-destructive and quantity-safe: same normalized source provenance reuses the existing Room, topology split/merge marks superseded Rooms `Stale`, stale Rooms/direct dependents are excluded from BQ, and audit records remain available for review/recovery;
- Room Auto boundary provenance is resolved by the adapter without claiming duplicate semantic `SourceHandles`; shared semantic reference resolution keeps BQ/Health/Locate/BBS/revision navigation working for boundary-derived elements;
- Tường KT/Cửa/physical-cut/rebar-3D workflows are exposed consistently through commands, the main palette, Ribbon and Full Domain Hub;
- V25 release package + per-user DemandLoad install/uninstall source with hashes/signature policy and proprietary-runtime exclusion;
- generic/full-domain/room-lifecycle/geometry-completion/room-curve static preflights plus deterministic Core smoke coverage, including end-to-end guards for all three Tường KT native 3D variants, compatible LINE-wall opening hosts and direct ARC/SPLINE Room Auto wiring;
- manual-only GitHub Actions and V25 self-hosted NETLOAD/runtime/screenshot harness.

## Next validation gates

1. Run all static/source preflights on the newest head when an explicitly approved validation run is requested.
2. Core Release build + deterministic smoke suite on the newest geometry/Room/UI head. Earlier green runs do not automatically validate later commits.
3. Licensed Windows BricsCAD V25 compile on `[self-hosted, windows, x64, bricscad-v25]`.
4. `NETLOAD`/DemandLoad and command/Ribbon/palette regression, including `QS3DROOMAUTO`, `QS3DCUTOPENINGS`, `QS3DREBAR3D`, Tường Gạch/Vách Kính/Trụ Tường capture + `QS3DBUILD3D`, recognition/template/revision/BBS/domain/audit workflows.
5. Private sample DWG regression: Tường KT LINE/open-POLYLINE/curved centerlines for all three variants; Room Auto mixed LINE/POLYLINE/ARC/SPLINE plus non-planar rejection; physical cuts on all supported LINE-wall hosts; room/finish/structural/takeoff/BQ/BBS/rebar/template/save/reopen; Room Auto split/merge/reuse and moved-opening rebuild/re-cut cases.
6. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode and narrow/wide palette sizes.
7. Performance/multi-DWG open-activate-SaveAs-close corpus plus large planar boundary-network/SPLINE sampling and large BQ/rebar corpus.
8. Only after these gates are green, consider automatic PR CI/release-candidate automation.

## Runtime/product completion still remaining

- production-grade Vách Kính curtain-wall framing/panel semantics and specialized Trụ Tường profiles/material/display behavior beyond the current generic Tường KT centerline extrusion path;
- wall-to-wall automatic joins/T-junction cleanup, closed-loop/freeform wall profiles and complex elevation/level constraints beyond the current guarded centerline extrusion path;
- generalized physical opening/door cutting beyond compatible LINE-host solids, especially curved/polyline hosts and edit/rebuild UX proven on V25;
- V25/private-DWG proof and performance tuning of automatic room-boundary discovery; direct planar ARC and bounded SPLINE sampling are implemented in source, while native non-planar curve projection remains future work;
- general rebar authoring beyond the current rectangular-column longitudinal-bar path: beam/slab/wall bars, stirrups, hooks/bends, shape editing and broader BBS-to-geometry synchronization;
- transient highlight/isolate/section-box UX proven against V25 editions;
- further BLT parity polish after real user screenshots: Ribbon grouping/icons, palette density, keyboard focus, context menus, empty/error states and DPI behavior;
- Authenticode production signing and signed updater;
- optional Cloudflare license/update/team-sync backend;
- future AutoCAD adapter reusing `QS3D.Core`.

These remaining items must not be marked complete from source review alone when their correctness depends on BricsCAD V25 runtime, private DWG data, signing infrastructure or external deployment.

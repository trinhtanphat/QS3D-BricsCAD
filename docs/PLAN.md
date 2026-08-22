# QS3D BricsCAD V25 master plan

Updated 2026-08-10 for the current `main` source baseline.

## Implemented source baseline

- clean-room layered architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI;
- `.qsdb` semantic source-of-truth with schema migration, locking, validated temp save, backup/recovery, persisted dirty state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, Model Health, revision baseline/diff and company template import/export;
- multi-DWG project context keyed by live `Document` identity;
- BLT-style three-pane workspace with semantic tree, Family/Type list, selected-object review, HT_Phòng, Xref/Drawing and Layer management;
- category-aware **Bóc chọn** flow so semantic capture does not depend on command-line memorization;
- typed property inspector with Vietnamese groups/units, boolean/choice/text editors and explicit **Family / Type** versus **Đối tượng / Instance** scope;
- exactly one semantic selection opens Instance scope; instance overrides are preserved when Family defaults change and can be reset to the current Family value;
- semantic-reference selection matching supports normal sources, Auto Room boundary provenance and generated-solid fallback without duplicating ownership handles;
- review actions are available in Workspace/Ribbon/Domain Hub: Highlight, Focus, Cô lập and Khôi phục;
- Room / Tường Gạch / Vách Kính / Trụ Tường / Opening / Door / HT_Phòng semantic workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths and guarded native adapters;
- native Tường KT source paths for all three variants from LINE/open POLYLINE centerlines using deterministic wall-footprint generation, guarded miter/bevel joining and bulge tessellation;
- `QS3DWALLJUNCTIONS` classifies L/T/X/Straight/End/Multi nodes and produces reviewable endpoint-cleanup plans;
- `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY` implement review-gated centerline endpoint cleanup for tracked wall LINE/open straight POLYLINE source. Preview hashes geometry/targets/tolerances; Apply rejects stale previews, curved/bulged sources and nonsemantic wall geometry. Affected semantic owners are dirtied after mutation, and generated geometry can be invalidated atomically before safe rebuild;
- manual Door/Opening host linking plus `QS3DAUTOLINKHOSTS`, which matches selected openings to compatible semantic walls using surface gap, Floor/Zone scope, ambiguity rejection and an independent elevation gate; automatic host matching never silently executes the physical boolean cut;
- guarded physical Door/Opening subtraction for compatible LINE hosts and safe straight/non-bulged POLYLINE segments; curved/bulged/corner-crossing cuts fail closed;
- Quick Takeoff, BQ stable-ID grouping/filtering/Locate/XLSX and deterministic recognition/review/auto-apply;
- Room Auto from planar LINE/POLYLINE/ARC/SPLINE with sagitta/chord controls, planarity checks, bounded sampling, topology split/merge lifecycle, stale-room handling and rollback;
- rebar notation/BBS, XLSX/CSV, rectangular-column 3D bars, linear distribution planning, protected ownership/health checks and BBS-shape-driven 3D bars for supported straight/L/U/Z/custom leg/turn paths;
- Ribbon, Workspace and Full Domain Hub expose the main product workflows consistently, including Giao tường/snap preview+apply, Auto/Manual Host, Focus/Isolate and both rebar geometry paths;
- V25 release package + per-user DemandLoad install/uninstall source with hashes/signature policy and proprietary-runtime exclusion;
- generic/full-domain/room/geometry/advanced static preflights and deterministic Core smoke coverage, including safe Auto Host, wall-snap review gating, XAML well-formedness and Family/Instance inspector contracts;
- GitHub Actions on `main` remain manual-only.

## Next validation gates

1. Run all source/static preflights on the newest head only when an explicitly approved validation run is requested.
2. Core Release build + deterministic smoke suite on the newest Room/wall/opening/rebar/UI head; older green runs do not validate later commits automatically.
3. Compile the V25 adapter on `[self-hosted, windows, x64, bricscad-v25]` against the exact installed BricsCAD V25 managed assemblies.
4. NETLOAD/DemandLoad regression for workspace, Ribbon, Domain Hub, Family/Instance property scope, Focus/Isolate, Giao tường, wall-snap preview/apply, Auto Host, Room Auto, opening cuts, recognition/template/revision/BQ/BBS and both rebar 3D paths.
5. Private-DWG regression: Tường KT LINE/open-POLYLINE/curved centerlines; L/T/X networks and snap cleanup; Door/Opening Auto Host with ambiguous/elevation cases; Room Auto mixed LINE/POLYLINE/ARC/SPLINE; opening cuts on LINE and safe straight-POLYLINE hosts; structure/takeoff/BQ/BBS; shape rebar; save/reopen and multi-DWG lifecycle.
6. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode, narrow/wide palettes, Family/Instance selector, typed controls and error/disabled states.
7. Performance tests for large room-boundary networks, wall-junction graphs, Auto Host candidate sets, BQ tables, SPLINE sampling and rebar batches.
8. Only after these gates are green, consider broader automatic PR/release validation.

## Runtime/product completion still remaining

- production-grade Vách Kính curtain-wall framing/panel semantics and specialized Trụ Tường profiles/material presentation beyond the generic Tường KT extrusion;
- **physical wall-solid reconciliation/union** at L/T/X/Multi junctions; guarded source-centerline endpoint cleanup is implemented, but generated solids are not yet automatically unioned/reshaped as a complete junction system;
- closed-loop/freeform wall profiles and more complex level/elevation constraints;
- physical opening/door cutting on curved/bulged polyline wall hosts and complex corner-spanning openings;
- broader rebar authoring/editing for beam/slab/wall bars, stirrups, hooks, bend radii and interactive shape manipulation beyond the deterministic current source paths;
- section-box and deeper transient isolate/highlight workflows proven against V25;
- richer specialized editors such as level picker/material catalog rather than generic editable choices;
- context menus, shortcuts, commercial icon set/Ribbon grouping, persisted palette splitter sizes, accessibility/focus order and DPI polish from real V25 screenshots;
- Authenticode production signing, signed updater and optional commercial licensing/team-sync backend;
- future AutoCAD adapter reusing `QS3D.Core`.

Items depending on BricsCAD V25 runtime, private drawings, signing infrastructure or external services must not be marked complete from repository inspection alone.

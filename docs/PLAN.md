# QS3D BricsCAD V25 master plan

Updated 2026-08-10 for the current `main` source baseline.

## Implemented source baseline

- clean-room layered architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI;
- `.qsdb` semantic source-of-truth with schema migration, locking, validated temp save, backup/recovery, persisted dirty/generated-snapshot state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, Model Health / Full Health, revision baseline/diff and company template import/export;
- multi-DWG project context keyed by live `Document` identity;
- BLT-style three-pane workspace with semantic tree, Family/Type list, selected-object review, HT_Phòng, Xref/Drawing and Layer management;
- category-aware **Bóc chọn** flow so semantic capture does not depend on command-line memorization;
- typed property inspector with Vietnamese groups/units, boolean/choice/text editors and explicit **Family / Type** versus **Đối tượng / Instance** scope; exactly one semantic selection opens Instance scope and true instance overrides survive Family edits;
- semantic-reference selection matching supports normal sources, Auto Room provenance and generated-solid fallback without duplicating ownership handles;
- review actions in Workspace/Ribbon/Domain Hub: Highlight, Focus, Cô lập/Khôi phục, Section Box/Plane and clip display;
- Room / Tường Gạch / Vách Kính / Trụ Tường / Opening / Door / HT_Phòng semantic workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths and guarded native adapters;
- generic Tường KT LINE/open-POLYLINE wall-footprint pipeline, deterministic bulge tessellation and guarded miter/bevel joining;
- specialized WallPier LINE rectangular/chamfered profile planning/native build while retaining generic open-POLYLINE fallback;
- `QS3DWALLJUNCTIONS` + review-gated `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY`; generated dependents are ownership-invalidated before source mutation/rebuild;
- manual Door/Opening host linking plus `QS3DAUTOLINKHOSTS` using compatible semantic walls, surface gap, Floor/Zone scope, ambiguity rejection and independent elevation gating;
- guarded straight physical Door/Opening subtraction plus `QS3DCUTOPENINGSCURVED` for supported bulged open-POLYLINE hosts. Curved cutter planning/fingerprint validation runs before `BoolSubtract`, identical reruns are idempotent and changed fingerprints on the same solid fail closed;
- Room Auto from planar LINE/POLYLINE/ARC/SPLINE with sagitta/chord controls, planarity checks, bounded sampling, topology split/merge lifecycle, stale-room handling and rollback;
- **Curtain Wall** panel-grid quantities/schedule/XLSX plus native GlassWall LINE frame overlays. `QS3DCURTAIN3D` keeps one backing host for opening booleans and adds dedicated mullion/transom/perimeter frame solids; frame depth is Family-editable, ownership/invalidation is guarded and a deterministic config fingerprint detects stale grid/depth/offset snapshots;
- Quick Takeoff, bounded Current-Space B4D scan, BQ stable-ID grouping/filtering/Locate/XLSX, ED2/Excel Locate and deterministic recognition/review/auto-apply;
- rebar notation/BBS/XLSX/CSV, column/beam longitudinal bars, BBS-shape bars, beam stirrups and column ties;
- dedicated rectangular **Slab X/Y mesh** with independent X/Y diameter/distribution and dedicated generated ownership/health;
- dedicated **StructuralWall horizontal/vertical mesh** with independent H/V diameter/distribution, Near/Far/Both faces and dedicated generated ownership/health;
- `QS3DREBARHEALTHALL` aggregates six generated-rebar families and cross-family ownership; `QS3DHEALTHALL` adds model/source/generated-solid/stale/mode and curtain-frame health with issue-specific Locate;
- Ribbon, Workspace, Full Domain Hub, Curtain Hub and Geometry Extensions expose the main product workflows consistently, including Curtain 3D, Giao tường/snap, Auto/Manual Host, curved cuts, slab/wall mesh and Full Health;
- V25 release package + per-user DemandLoad install/uninstall source with hashes/signature policy and proprietary-runtime exclusion;
- feature-specific static preflights cover Room curves/lifecycle, wall junction/snap, Auto Host, straight/curved opening, WallPier profile, dedicated slab/wall mesh, curtain native frame/config fingerprint, unified health, command uniqueness, XAML well-formedness and Family/Instance inspector contracts;
- `scripts/preflight-all.py` discovers the feature preflights; GitHub Actions on `main` remain **manual-only**.

## Next validation gates

1. Run all source/static preflights on the newest head only when an explicitly approved validation run is requested.
2. Core Release build + deterministic smoke suite on the newest curtain/wall/opening/rebar/UI head; older green runs do not validate later commits automatically.
3. Compile the V25 adapter on `[self-hosted, windows, x64, bricscad-v25]` against the exact installed BricsCAD V25 managed assemblies.
4. NETLOAD/DemandLoad regression for workspace, Ribbon, Domain Hub, Curtain Hub, Family/Instance/Floor-Level editing, Focus/Isolate, Giao tường, wall snap, Auto Host, Room Auto, straight/curved opening cuts, recognition/template/revision/BQ/BBS and all generated-rebar families.
5. Private-DWG regression: Tường KT LINE/open-POLYLINE/curves; WallPier profiles; L/T/X networks and snap cleanup; Door/Opening ambiguous/elevation/curved-cut cases; Curtain host/frame overlay and stale fingerprint; Room Auto mixed LINE/POLYLINE/ARC/SPLINE; structure/takeoff/BQ/BBS; slab/wall mesh; shape/stirrup/tie/longitudinal rebar; save/reopen and multi-DWG lifecycle.
6. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode, narrow/wide palettes, Family/Instance/Floor-Level controls, typed editors and error/disabled states.
7. Performance tests for large room-boundary networks, wall-junction graphs, Auto Host candidate sets, curtain grids, BQ tables, SPLINE sampling and rebar/mesh batches.
8. Only after these gates are green, consider broader automatic PR/release validation.

## Runtime/product completion still remaining

- **physical wall-solid reconciliation/union** at L/T/X/Multi junctions. Guarded source-centerline endpoint cleanup is implemented, but generated wall bodies are not yet automatically unioned/reshaped into a complete junction system;
- curtain opening-aware mullion/transom interruption, panel-by-panel glass solids and curved/open-POLYLINE frame overlays;
- WallPier specialized profile generation for open POLYLINE/freeform paths beyond the current LINE specialization;
- closed-loop/freeform wall profiles and richer multi-level/elevation constraints;
- more complex corner-spanning curved opening booleans beyond the current guarded tessellated footprint planner;
- fabrication-grade rebar hooks, bend radii, lap/anchorage/code-specific detailing and richer interactive editing beyond the current deterministic bars/shape/tie/stirrup/mesh paths;
- real V25 proof for Section Box/BIM command availability, transient isolate/highlight lifecycle and palette behavior;
- richer material/catalog/level editing and commercial icon/context-menu/Ribbon grouping/persisted splitter/accessibility/DPI polish based on real V25 screenshots;
- Authenticode production signing, signed updater and optional commercial licensing/team-sync backend;
- future AutoCAD adapter reusing `QS3D.Core`.

Items depending on BricsCAD V25 runtime, private drawings, signing infrastructure or external services must not be marked complete from repository inspection alone.

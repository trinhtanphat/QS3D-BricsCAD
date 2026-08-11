# QS3D BricsCAD V25 master plan

Updated 2026-08-10 for the current `main` source baseline plus the full-repository ownership/capture/release hardening audit.

## Locked product direction

QS3D remains a **BricsCAD V25 x64 plugin product**. The current roadmap does not target a standalone `QS3D.exe`, a QS3D-owned DWG/CAD engine or a separate native viewport. BricsCAD remains the runtime host and the release form remains plugin DLLs loaded by DemandLoad/`NETLOAD`.

`QS3D.Core` stays CAD-independent for deterministic tests/reuse and future hosted adapters; that separation must not be reinterpreted as a standalone desktop product. `BLT-style`/`BLT3D-familiar` roadmap language means workflow/UX parity only. See `docs/PRODUCT-BOUNDARY.md`.

Any future standalone direction requires an explicit owner requirement and a new architecture/build/licensing/release plan; it is not part of this master plan.

## Implemented source baseline

- clean-room layered plugin architecture: `QS3D.Core` + BricsCAD V25 adapter + WPF/Ribbon UI hosted by BricsCAD;
- `.qsdb` semantic source-of-truth with schema migration, locking, validated temp save, backup/recovery, persisted dirty/generated-snapshot state, project QuantityRules and audit provenance;
- dependency/fixed-point regeneration, formula engine, Model Health / Full Health, revision baseline/diff and company template import/export;
- **detached Rule Preview** for element/project quantity-rule deltas with Add/Change/Remove classification, before/after provenance, exact project ownership checks and stale-preview rejection;
- **detached Regeneration Preview** that runs the real Core `RegenerateDirty` path on a copied project, reuses `RevisionService` for semantic deltas and reports before/after Health regressions without mutating live state;
- guarded rule/regeneration Apply APIs use project snapshots and can roll back when a live apply introduces a new Model Health Error; adapter mutation/confirmation UX remains a V25 qualification gate;
- deterministic **Model Health baseline/diff** classifies New / Resolved / Persistent issues so operation quality can be measured rather than inferred from a final count;
- privacy-safe `QS3D.DiagnosticSummary` v1 aggregate export omits project/DWG identity, CAD handles, semantic IDs/names, properties, quantities and health messages; `QS3DDIAGSUMMARY` exposes this source-side support workflow;
- `QS3DRULEPREVIEW` and `QS3DREGENPREVIEW` expose the new read-only dry-run workflows without mutating the live project;
- dependency cycles are detected as explicit Model Health errors and therefore block `QS3DRELEASECHECK` instead of leaving regeneration as an opaque stall;
- multi-DWG project context keyed by live `Document` identity;
- BLT-style three-pane workspace with semantic tree, Family/Type list, selected-object review, HT_Phòng, Xref/Drawing and Layer management;
- category-aware **Bóc chọn** flow so semantic capture does not depend on command-line memorization;
- typed property inspector with Vietnamese groups/units, boolean/choice/text editors and explicit **Family / Type** versus **Đối tượng / Instance** scope; exactly one semantic selection opens Instance scope and true instance overrides survive Family edits;
- semantic capture is transactional: QS3D-generated output is rejected as source before mutation and single/batch capture plus HT_Phòng generation/synchronization restore a full `ProjectStateSnapshot` if regeneration/validation fails;
- generic starter Families for ArchitecturalWall/GlassWall/WallPier are aligned with specialized capture defaults, including wall-axis offsets, Curtain frame depth and WallPier profile/chamfer defaults;
- generated output ownership has one Core contract: `PhysicalOpeningCutSolidHandle` and every `Generated*Handle(s)` owner slot are normalized, parsed and enumerated centrally; selection, ownership health, BOM validation, semantic capture, B4D and Release Check consume the shared policy;
- generated destructive guards for rebar/tie/curtain and major dedicated health indexes use the shared owner-slot policy instead of feature-local lists;
- review actions in Workspace/Ribbon/Domain Hub: Highlight, Focus, Cô lập/Khôi phục, Section Box/Plane and clip display;
- Room / Tường Gạch / Vách Kính / Trụ Tường / Opening / Door / HT_Phòng semantic workflows;
- Beam / Slab / Column / StructuralWall / Foundation / Stair / Railing / Earthwork deterministic quantity paths and guarded native adapters;
- generic Tường KT LINE/open-POLYLINE wall-footprint pipeline, deterministic bulge tessellation and guarded miter/bevel joining;
- specialized WallPier LINE rectangular/chamfered profile planning/native build while retaining generic open-POLYLINE fallback;
- `QS3DWALLJUNCTIONS` + review-gated `QS3DWALLSNAPPREVIEW` / `QS3DWALLSNAPAPPLY`; generated dependents are ownership-invalidated before source mutation/rebuild;
- manual Door/Opening host linking plus `QS3DAUTOLINKHOSTS` using compatible semantic walls, surface gap, Floor/Zone scope, ambiguity rejection and independent elevation gating;
- guarded straight physical Door/Opening subtraction plus `QS3DCUTOPENINGSCURVED` for supported bulged open-POLYLINE hosts; curved cutter planning/fingerprint validation runs before mutation and identical reruns are idempotent;
- Room Auto from planar LINE/POLYLINE/ARC/SPLINE with sagitta/chord controls, planarity checks, bounded sampling, topology split/merge lifecycle and stale-room handling;
- **Curtain Wall** panel-grid quantities/schedule/XLSX plus native host, perimeter/mullion/transom overlays and panel-by-panel clear-glass solids. `QS3DCURTAIN3D` keeps one backing host for opening booleans and independent frame/panel owner slots;
- supported LINE and guarded open/bulged Curtain frames/panels are opening-aware: linked Door/Opening rectangles interrupt frames and clip panel cells deterministically; opening property/re-host/unlink changes stale both dependent output families without falsely rebuilding the backing host;
- Quick Takeoff, bounded Current-Space B4D scan, BQ stable-ID grouping/filtering/Locate/XLSX, ED2/Excel Locate and deterministic recognition/review/auto-apply;
- B4D excludes generated geometry through canonical `CollectOwnerHandles(project)`, so owner classification, parsing and dedupe do not drift when generated families are added;
- rebar notation/BBS/XLSX/CSV, column/beam longitudinal bars, BBS-shape bars, beam stirrups and column ties;
- dedicated rectangular **Slab X/Y mesh** with independent X/Y diameter/distribution and generated ownership/health;
- dedicated **StructuralWall horizontal/vertical mesh** with independent H/V diameter/distribution, Near/Far/Both faces and generated ownership/health;
- dedicated **Foundation X/Y mesh** with independent X/Y diameter/count/spacing, Bottom/Top/Both faces, dedicated generated ownership/stale/mode metadata and health;
- `QS3DREBARHEALTHALL` aggregates the current generated-rebar health families and cross-family ownership; `QS3DHEALTHALL` adds model/source/generated-solid/stale/mode/curtain/dependency health;
- `QS3DRELEASECHECK` includes Foundation mesh health, generated-rebar mode/category health, dependency-cycle health, provenance-safe ownership, live CAD, stale-state and BOM release guards;
- Project Tools and Full Domain Hub expose drawing-bound `QS3DSCHEDULES`; Schedule Hub routes BQ, Room Finish, Material, Curtain, Door/Opening and rebar schedule/export workflows;
- semantic JSON interchange now rejects malformed source handle/dependency values instead of silently dropping, trimming or case-insensitively deduplicating them during export;
- V25 release package builds from `bin/x64/Release/net48`, derives `COMMANDS.txt` from `[CommandMethod]`, excludes BricsCAD-owned assemblies and includes hashes/install/update helpers plus reviewed synthetic samples;
- per-user DemandLoad install/replace is transactional: failed install restores the prior files/registry state or removes a partial new install;
- updater version decisions are bound to the cryptographically verified signed manifest; expected-version substitution/replay mismatch is rejected before install;
- feature-specific static preflights cover Room curves/lifecycle, wall junction/snap, Auto Host, opening cuts, WallPier, slab/wall/foundation mesh, Curtain opening-aware lifecycle, unified/release health, canonical generated ownership/B4D exclusion, transactional semantic capture/default parity, dependency cycles, installer rollback, updater version binding, command uniqueness, preview/diagnostic contracts and XAML contracts;
- `scripts/preflight-all.py` discovers feature preflights, including the product-boundary documentation/source guard; GitHub Actions on `main` remain **manual-only**.

## Next validation gates

1. Run aggregate source/static preflights on the exact newest head only when the owner explicitly approves a validation run.
2. Core Release build + deterministic smoke suite on that same SHA; older green runs do not validate later commits.
3. Compile the V25 adapter on `[self-hosted, windows, x64, bricscad-v25]` against the exact installed BricsCAD V25 managed assemblies.
4. NETLOAD/DemandLoad regression for Workspace/Ribbon/hubs, project editors, Focus/Isolate/Section, wall snap, Auto Host, Room Auto, opening cuts, recognition/template/revision/BQ/BBS, generated-source rejection, `QS3DRULEPREVIEW`, `QS3DREGENPREVIEW`, `QS3DDIAGSUMMARY` and all generated-rebar families.
5. Representative private-DWG regression: wall/WallPier; L/T/X cleanup; ambiguous/elevation host cases; straight/curved cuts; Curtain backing host + opening-aware LINE frames; Room Auto mixed curves; structure/BQ/BBS; slab/wall/foundation mesh; shape/stirrup/tie/longitudinal rebar; failed capture/finish rollback; dependency-cycle reporting; rule/regeneration preview read-only behavior; save/reopen/multi-DWG lifecycle.
6. Qualify confirmation/Undo/session behavior before exposing guarded Rule/Regeneration Apply as a production V25 mutation command; preview commands remain read-only source features until this is proven.
7. Qualify transactional installer rollback and signed-manifest updater version binding using an actual signed release package and prior installed version.
8. Visual regression at 100/125/150/200% DPI with Vietnamese Unicode, narrow/wide palettes, Family/Instance/Floor-Level controls, typed editors and error/disabled states.
9. Performance tests for large room-boundary networks, wall-junction graphs, Auto Host candidate sets, Curtain grids, BQ tables, SPLINE sampling, owner registries, preview/diff workloads and rebar/mesh batches.
10. Only after these gates are green should broader automatic validation or production rollout be considered; changing current manual-only Actions policy still requires explicit owner approval.

## Runtime/product completion still remaining

- **physical wall-solid reconciliation/union** at L/T/X/Multi junctions. Guarded source-centerline endpoint cleanup is implemented, but generated wall bodies are not yet automatically unioned/reshaped under a safe multi-owner contract;
- Curtain exact-V25 qualification for current LINE/open-bulged host/frame/panel output, including outer/nested rollback, opening clipping, owner selection, Undo and save/reopen; broader tilted/closed/arbitrary freeform paths remain unsupported product work;
- WallPier specialized profile generation for open POLYLINE/freeform paths beyond the current LINE specialization;
- richer closed/freeform wall profiles and multi-level/elevation constraints;
- complex corner-spanning curved opening booleans beyond the current guarded tessellated footprint planner;
- fabrication-grade rebar hooks, bend radii, lap/anchorage/code-specific detailing and richer interactive editing beyond current deterministic bars/shape/tie/stirrup/mesh paths;
- production V25 mutation UX for guarded Rule/Regeneration Apply, including explicit confirmation, Undo/session semantics and post-apply locate/review;
- real V25 proof for Section Box/BIM command availability, transient isolate/highlight lifecycle and palette behavior;
- richer commercial icon/context-menu/Ribbon grouping/persisted splitter/accessibility/DPI polish based on real V25 screenshots;
- production Authenticode certificate/signing operations, signed release publication and optional commercial licensing/team-sync backend. Source-side manifest verification/version binding/rollback is implemented, but production signing infrastructure is still external release work;
- future AutoCAD **plugin adapter** reusing `QS3D.Core`.

Items depending on BricsCAD V25 runtime, private drawings, production signing infrastructure or external services must not be marked complete from repository inspection alone.

See `docs/SOURCE-PRODUCT-PLAN-2026-08-10.md`, `docs/PRODUCT-BOUNDARY.md`, `docs/REVIEW-2026-08-10-CONTINUE-ALL-AUDIT.md` and `docs/FULL-REPO-AUDIT-2026-08-10.md` for product logic, product boundary, audit rationale and validation boundaries.

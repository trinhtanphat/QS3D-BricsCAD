# QS3D-BricsCAD — current agent handoff

**Updated:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the short **current-state delta handoff** for fast-moving work. It travels with the source it describes. Always fetch the newest `main` before a write; if a newer source commit conflicts with this document, current source wins.

For the larger historical/source baseline, also read `docs/AGENT-HANDOFF-LATEST-2026-08-10.md`. For the full session chronology, use `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` only when needed.

## 1. Locked product form

QS3D is a **BricsCAD V25 x64 .NET plugin**, not a standalone QS3D CAD executable.

- BricsCAD V25 is required at runtime.
- `QS3D.BricsCAD.V25.dll` is loaded through DemandLoad or `NETLOAD`.
- BricsCAD owns DWG/database/editor/viewport/native CAD lifecycle.
- QS3D adds commands, Ribbon, palettes/modeless WPF tools, semantic/project state, quantities and guarded generated geometry inside BricsCAD.
- `QS3D.Core` being CAD-independent exists for deterministic logic/testability/reuse and does not imply `QS3D.exe`.
- BLT/BLT3D terminology is clean-room workflow/UX reference only.

`docs/PRODUCT-BOUNDARY.md` is authoritative for product-form ambiguity.

## 2. Direct Draw is now a first-class plugin workflow

The owner-required authoring direction is no longer only `draw CAD first → Bóc chọn/capture → QS3DBUILD3D`. QS3D also exposes direct authoring inside the BricsCAD host.

Current source authoring commands:

### P0

- `QS3DDRAWWALL`
- `QS3DDRAWBEAM`
- `QS3DDRAWCOLUMN`
- `QS3DDRAWSLAB`

P0 has explicit key dimension prompts, source-relative bottom offsets, Model-Space gating, unit-aware 5 mm planarity checks, semantic regeneration before native mutation, generated-result selection, XData-based failed-output discovery and verified CAD rollback cleanup.

### Guarded P1 native subset

- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`

P1 creates one real DWG source, captures exactly one semantic element, writes user-confirmed instance properties through canonical `ProjectElement.SetProperty()`, revalidates the active DWG before nested build, and reuses canonical `QS3DBUILD3D` rather than forking native builder semantics. Because `QS3DBUILD3D` reports its own failures instead of throwing them outward, the P1 wrapper additionally requires a live `GeneratedSolidHandle`; otherwise it performs ownership-scoped CAD cleanup before restoring project state.

### Door / Opening Direct Draw

- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`

The user picks two plan-view edge points; QS3D creates a real Model-Space LINE whose plan length becomes `WidthM`, prompts/inherits `HeightM`, non-negative sill/bottom offset and `BooleanClearanceM`, captures exactly one semantic Door/WallOpening, regenerates, selects only that new source, and reuses the established selection-scoped Auto Host path. A valid authoring commit requires a non-empty `HostWallId` plus post-link deterministic regeneration. No-host or ambiguous-host placement rolls back the operation-created source and project state rather than leaving an orphan semantic opening.

A targeted physical-cut follow-up is source-implemented:

- `QS3DCUTSELECTEDOPENINGS` resolves current CAD/semantic selection to a deduplicated Door/WallOpening ID set;
- every selected target and generated host is prevalidated before native mutation, including stale/ownership/source-shape checks;
- only the requested subset is passed to `OpeningBooleanService`;
- same host/fingerprint rerun is idempotent; a different cut state on the same already-cut generated host fails closed until rebuild;
- legacy `QS3DCUTOPENINGS` remains the broader all-linked operation.

Direct Draw still does **not** silently auto-cut host solids. Physical mutation remains explicit until real V25 transaction/rollback UX is qualified.

Read:

- `docs/DIRECT-DRAW-WORKFLOW.md`
- `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`
- `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`
- `docs/DIRECT-DRAW-OPENINGS.md`
- `docs/DIRECT-DRAW-UCS.md`

## 3. Planar UCS source contract

The complete current Direct Draw set — P0, guarded P1, Door and WallOpening — now shares one source-level UCS contract.

- Prompt points stay in the active BricsCAD UCS.
- World UCS, translated UCS and in-plane rotated UCS are accepted by source guards.
- Persistent LINE/POLYLINE source geometry is transformed through `Editor.CurrentUserCoordinateSystem` before Model Space append so downstream semantic/native/host workflows consume database/WCS geometry.
- Column footprints rotate with the user's planar UCS rather than remaining hard-wired to WCS X/Y.
- Door/Opening `WidthM` is calculated from UCS-local prompt geometry before the persisted source LINE is transformed for Auto Host.
- Tilted/3D UCS is rejected before source creation because current native/host builders still carry WCS-planar assumptions.
- QS3D does not reset or mutate the user's UCS.

This is **source-implemented / statically guarded**, not runtime-certified. World/translated/30°/45°/90° planar UCS and tilted-UCS rejection still require interactive V25 proof on the exact release SHA.

## 4. Important Direct Draw boundaries

Do not overclaim these paths:

- GlassWall Direct Draw builds/captures the backing GlassWall host. Curtain frame/panel behavior remains governed by `QS3DCURTAIN3D` / Curtain Hub and its dedicated source/runtime contracts.
- **Curtain path-frame/panel support is source-implemented** for guarded horizontal LINE and open/bulged WCS-XY POLYLINE paths using bounded tessellation/station mapping, opening clipping, separate generated ownership and live-fingerprint checks. This is not licensed-runtime proof. Read `docs/CURTAIN-PATH-FRAMES.md` and `docs/CURTAIN-NATIVE-PANELS.md`.
- `QS3DCURTAIN3D` captures a command-level `ProjectStateSnapshot` and encloses the canonical LINE/path host, frame and panel builders in one outer native transaction. A pre-commit phase failure aborts the outer transaction and restores the semantic snapshot; live-fingerprint/UI work stays post-commit and warning-only. `scripts/preflight-curtain-orchestration-boundary.py` and `scripts/preflight-curtain-native-panels.py` guard that source structure.
- This outer/nested transaction contract is **source-implemented, not V25-runtime-qualified**. Do not restore obsolete `PARTIAL COMMIT` wording, and do not promote it to `LOCAL_PASS` until LOCAL-002 injects failures at every phase on the exact final SHA. Panel ownership/runtime status is tracked separately in `docs/CURTAIN-NATIVE-PANELS.md`.
- WallPier P1 is deliberately two-point LINE-only so native dispatch stays on the specialized profile builder. Do not claim arbitrary multi-segment/freeform profile parity.
- StructuralWall P1 uses the existing supported LINE structural path.
- Foundation P1 uses the existing supported closed-POLYLINE structural path.
- Door/Opening Direct Draw completes **source + semantic + verified Auto Host**. It intentionally does **not** invoke physical boolean as an implicit side effect.
- `QS3DCUTSELECTEDOPENINGS` solves explicit-target selection, but it does not turn physical boolean history into a fully incremental journal: a different cut state on the same already-cut generated solid still requires rebuild.
- Do not auto-call targeted cut from Direct Draw until licensed V25 proves host-Solid3d transaction/rollback behavior and product UX explicitly opts into automatic destructive mutation.

Legacy Bóc chọn/capture and `QS3DBUILD3D` remain fully supported for existing CAD drawings.

## 5. Build3D/current safety direction

Preserve current `QS3DBUILD3D` hardening:

- semantic/generated host selection resolves back to stable source handles;
- missing/stale live source CAD stops the operation before replacement;
- mixed native categories are rejected in one logical build;
- mixed LINE/open-POLYLINE wall batches are rejected when builder transaction boundaries differ;
- semantic regeneration runs before native builder commit;
- generated output selection does not redefine source-of-truth ownership;
- generated host aliases must not broaden host selection to rebar/mesh/detail output families;
- PaperSpace provenance must not be silently rebuilt into Model Space;
- specialized WallPier dispatch and other category-specific native paths must not be collapsed into a generic wall builder by accident.

## 6. Plugin UI discoverability and Family / Type

The BricsCAD Ribbon contains a `TẠO MỚI` authoring tab and Full Domain Hub contains `TẠO MỚI / DIRECT DRAW`. These are plugin UI hosted by BricsCAD, not a separate application shell.

The current authoring UI includes **Family / Type** wired to canonical `QS3DFAMILIES`, P0/P1 native categories, **Vẽ Cửa**, **Vẽ Lỗ Mở**, and **Khoét Cửa/Lỗ đang chọn** wired to `QS3DCUTSELECTEDOPENINGS`. Users can activate/edit the compatible Family first; Direct Draw then consumes the active Family and prompts/validates required instance values. Do not create a competing Direct-Draw-only family store/editor.

Current point acquisition uses BricsCAD base-point rubber-band feedback where applicable. A richer thickness/profile `DrawJig` preview or persistent repeated-authoring reactor still requires exact V25 managed-API compile and interactive proof; do not claim it from static source alone.

## 7. Validation boundary

This continuation work is based on GitHub source/static review. The execution environment used for this pass does not provide licensed interactive BricsCAD V25 runtime proof, GitHub Actions were not dispatched, and a local container clone attempt could not resolve GitHub networking. Newly added static preflight source is present but was not executable-run from that container in this pass.

Use precise status language:

**source-implemented / statically guarded ≠ compiled and NETLOAD/runtime-verified on the exact current SHA.**

Still required before production claims:

- exact current V25 adapter compile against installed managed assemblies;
- NETLOAD/DemandLoad unique command registration;
- real Ribbon/palette/Domain Hub behavior, including Family / Type and selected-opening cut;
- P0/P1/Door/Opening World/translated/30°/45°/90° planar-UCS behavior plus tilted/3D-UCS rejection;
- successful and forced-failure rollback tests;
- representative private-DWG save/reopen/multi-DWG regression;
- GlassWall/Curtain, WallPier, StructuralWall and Foundation native geometry checks;
- Curtain phase-failure regression proving the outer transaction leaves no host/frame partial commit and restores semantic state for every pre-commit failure; post-commit fingerprint/UI failure must remain a truthful warning;
- Door/Opening valid-host/no-host/ambiguous-host behavior, Floor/Zone/elevation/gap gates and sill/clearance persistence;
- `QS3DCUTSELECTEDOPENINGS` with one/multiple selected openings, multiple hosts, mixed unrelated CAD selection, stale hosts, same-fingerprint rerun and different-fingerprint fail-closed behavior;
- legacy `QS3DCUTOPENINGS` compatibility;
- guarded LINE/open/bulged Curtain host/frame/panel generation, opening clipping and six-phase rollback in real V25;
- representative testing against a private copy of owner-provided `MB MONG.dwg` without committing that file;
- Unicode/HiDPI visual tests and large-model performance.

## 8. CI/release policy

All GitHub Actions workflows remain **manual-only** (`workflow_dispatch`). `continue all`, source changes, docs updates, commits or reviews do not authorize CI/build/runtime/release dispatch. A separate explicit owner request is required.

Do not add automatic push/tag/PR triggers and do not publish a release without explicit owner approval plus the existing release confirmation gate.

## 9. Remaining product/runtime work

After current runtime qualification, the remaining substantial authoring/product gaps are:

1. optional one-shot Door/Opening + physical cut UX only after targeted-cut transaction/rollback is proven on V25; do not silently auto-cut;
2. transient thickness/profile DrawJig preview + repeated authoring mode proven against BricsCAD V25 editor/jig behavior;
3. optional compact in-command Family picker only if real UX testing shows the existing Family / Type launcher is too slow; canonical `QS3DFAMILIES` remains source of truth;
4. physical multi-owner wall-solid L/T/X/Multi reconciliation under a safe ownership/unmerge/rebuild model;
5. richer multi-segment WallPier profile authoring and further Curtain/product geometry only where guarded builders/planners support it safely;
6. real-runtime Ribbon/icon/context-menu/DPI polish;
7. production signing/update infrastructure and optional commercial licensing/backend only when real credentials/product requirements exist.

Never mark runtime-gated items complete from source inspection alone.

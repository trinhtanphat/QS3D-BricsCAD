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

P0 has received concurrent hardening beyond the initial implementation: explicit key dimension prompts, bottom offsets, Model-Space gating, unit-aware 5 mm planarity checks, semantic regeneration before native mutation, generated-result selection, XData-based failed-output discovery and verified CAD rollback cleanup.

### Guarded P1 native subset

- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`

P1 creates one real DWG source, captures exactly one semantic element, writes user-confirmed instance properties through canonical `ProjectElement.SetProperty()`, revalidates the active DWG before nested build, and reuses canonical `QS3DBUILD3D` rather than forking native builder semantics. Because `QS3DBUILD3D` reports its own failures instead of throwing them outward, the P1 wrapper additionally requires a live `GeneratedSolidHandle`; otherwise it performs ownership-scoped CAD cleanup before restoring project state.

### Door / Opening Direct Draw

- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`

These commands are source-implemented and statically guarded. The user picks two plan-view edge points; QS3D creates a real Model-Space LINE whose plan length becomes `WidthM`, prompts/inherits `HeightM`, non-negative sill/bottom offset and `BooleanClearanceM`, captures exactly one semantic Door/WallOpening, regenerates, selects only that new source, and reuses the established selection-scoped Auto Host path. A valid authoring commit requires a non-empty `HostWallId` plus post-link deterministic regeneration. No-host or ambiguous-host placement rolls back the operation-created source and project state rather than leaving an orphan semantic opening.

Door/Opening instance writes use `SetProperty()`, active-DWG affinity is rechecked before and after nested Auto Host, CAD cleanup uses the exact source `ObjectId`, cleanup occurs before project snapshot restore, and post-commit UI synchronization is best-effort/non-destructive.

A targeted physical-cut follow-up is also source-implemented:

- `QS3DCUTSELECTEDOPENINGS` resolves the current CAD/semantic selection to a deduplicated Door/WallOpening id set and invokes only the targeted `OpeningBooleanService` overload;
- legacy `QS3DCUTOPENINGS` remains the broader all-linked command;
- the same host/fingerprint rerun remains idempotent, while trying a different opening set on the same already-cut generated host fails closed until host rebuild.

Read:

- `docs/DIRECT-DRAW-WORKFLOW.md`
- `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`
- `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`
- `docs/DIRECT-DRAW-OPENINGS.md`

## 3. Important Direct Draw boundaries

Do not overclaim these paths:

- GlassWall Direct Draw builds/captures the backing GlassWall host. Curtain frame/panel behavior remains governed by `QS3DCURTAIN3D` / Curtain Hub and its dedicated source/runtime contracts.
- **Curtain path-frame support is source-implemented** for guarded horizontal LINE and open/bulged WCS-XY POLYLINE paths using bounded tessellation/station mapping, generated ownership and live-fingerprint checks. This is not licensed-runtime proof and does not create panel-by-panel backing glass solids. Read `docs/CURTAIN-PATH-FRAMES.md`.
- WallPier P1 follows the currently supported wall dispatch, including specialized behavior where the current builder supports it. Do not claim arbitrary freeform profile parity.
- StructuralWall P1 uses the existing supported LINE structural path.
- Foundation P1 uses the existing supported closed-POLYLINE structural path.
- Door/Opening Direct Draw completes **source + semantic + verified Auto Host**. It intentionally does **not** invoke physical boolean as an implicit side effect.
- `QS3DCUTSELECTEDOPENINGS` solves explicit-target selection so the user can cut only chosen Door/WallOpening elements instead of sending every explicit cut through the broader all-linked path. It does **not** yet turn physical boolean history into an incremental journal: a different cut set on the same already-cut generated solid still requires rebuild before a new cut fingerprint can be committed.
- Do not auto-call targeted cut from Direct Draw until licensed V25 proves the host-Solid3d transaction/rollback behavior and product UX explicitly opts into automatic destructive mutation.

Legacy Bóc chọn/capture and `QS3DBUILD3D` remain fully supported for existing CAD drawings. Existing `QS3DDOOR`, `QS3DOPENING`, Auto/Manual Host and physical cut commands remain the conversion/review path.

## 4. Build3D/current safety direction

Concurrent `main` hardening around `QS3DBUILD3D` must be preserved:

- semantic/generated host selection resolves back to stable source handles;
- missing/stale live source CAD stops the operation before replacement;
- mixed native categories are rejected in one logical build;
- mixed LINE/open-POLYLINE wall batches are rejected when builder transaction boundaries differ;
- semantic regeneration runs before native builder commit;
- generated output selection does not redefine source-of-truth ownership;
- generated host aliases must not broaden host selection to rebar/mesh/detail output families;
- PaperSpace provenance must not be silently rebuilt into Model Space;
- specialized WallPier dispatch and other category-specific native paths must not be collapsed into a generic wall builder by accident.

Do not regress these invariants while extending Direct Draw.

## 5. Plugin UI discoverability and Family / Type

The BricsCAD Ribbon contains a `TẠO MỚI` authoring tab and Full Domain Hub contains `TẠO MỚI / DIRECT DRAW`. These are plugin UI hosted by BricsCAD, not a separate application shell.

The current authoring UI includes **Family / Type** wired to canonical `QS3DFAMILIES`, plus P0, guarded P1 native categories, **Vẽ Cửa**, **Vẽ Lỗ Mở**, and **Khoét Cửa/Lỗ đang chọn** wired to `QS3DCUTSELECTEDOPENINGS`. Users can activate/edit the compatible Family first; Direct Draw then consumes the active Family and prompts/validates required instance values. Do not create a competing Direct-Draw-only family store/editor.

Current point acquisition already uses BricsCAD base-point rubber-band feedback where applicable. A richer thickness/profile `DrawJig` preview or persistent repeated-authoring reactor still requires exact V25 managed-API compile and interactive proof; do not claim it from static source alone.

Door/Opening UI text must continue to distinguish Auto Host completion from the separate intentional physical-cut step. Keep major Direct Draw commands discoverable in both Ribbon and Domain Hub while avoiding an overcrowded generic Workspace palette.

## 6. Validation boundary

This continuation work is based on GitHub source/static review. The execution environment used for this pass does not provide licensed interactive BricsCAD V25 runtime proof, GitHub Actions were not dispatched, and a local container clone attempt could not resolve GitHub networking. Therefore the newly added static preflight source is present but was not executable-run from that container in this pass.

Use precise status language:

**source-implemented / statically guarded ≠ compiled and NETLOAD/runtime-verified on the exact current SHA.**

Still required for Direct Draw and the broader plugin before production claims:

- exact current V25 adapter compile against the installed managed assemblies;
- NETLOAD/DemandLoad unique command registration;
- real Ribbon/palette/Domain Hub behavior, including Family / Type activation and selected-opening cut;
- successful and forced-failure rollback tests;
- representative private-DWG save/reopen/multi-DWG regression;
- GlassWall/Curtain, WallPier, StructuralWall and Foundation native geometry checks;
- Door/Opening width correctness in millimeter and meter drawings;
- Door/Opening valid-host, no-host and ambiguous-host behavior, including Floor/Zone/elevation/gap gates;
- Door/Opening sill/bottom offset and boolean-clearance persistence;
- `QS3DCUTSELECTEDOPENINGS` with one/multiple selected openings, multiple hosts, mixed unrelated CAD selection, same-fingerprint rerun and different-fingerprint fail-closed behavior;
- legacy `QS3DCUTOPENINGS` behavior after adding the overload;
- guarded LINE/open/bulged Curtain path-frame generation in real V25;
- World UCS and representative rotated-UCS authoring behavior;
- representative testing against a private copy of owner-provided `MB MONG.dwg` without committing that file;
- Unicode/HiDPI visual tests and large-model performance.

## 7. CI/release policy

All GitHub Actions workflows remain **manual-only** (`workflow_dispatch`). `continue all`, source changes, docs updates, commits or reviews do not authorize CI/build/runtime/release dispatch. A separate explicit owner request is required.

Do not add automatic push/tag/PR triggers and do not publish a release without explicit owner approval plus the existing release confirmation gate.

## 8. Next safe product work

After current runtime qualification, the most useful remaining authoring/product gaps are:

1. if one-shot Door/Opening authoring + boolean is desired, add an explicit opt-in UX only after the new targeted subset transaction is proven on V25; do not silently auto-cut;
2. transient thickness/profile preview + repeated authoring mode proven against BricsCAD V25 editor/jig behavior;
3. optional compact in-command Family picker only if real UX testing shows the existing Family / Type launcher is too slow; canonical `QS3DFAMILIES` remains source of truth;
4. physical multi-owner wall-solid L/T/X reconciliation under a safe ownership model;
5. richer WallPier profile authoring and further Curtain/product geometry only where existing guarded planners/builders support it safely;
6. real-runtime Ribbon/icon/context-menu/DPI polish;
7. production signing/update infrastructure only when release requirements are explicit.

Never mark runtime-gated items complete from source inspection alone.

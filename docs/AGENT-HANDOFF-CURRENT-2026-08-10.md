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

### Guarded P1 subset

- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`

P1 creates one real DWG source, captures exactly one semantic element, applies user-confirmed dimensions and reuses canonical `QS3DBUILD3D` rather than forking native builder semantics. Because `QS3DBUILD3D` reports its own failures instead of throwing them outward, the P1 wrapper additionally requires a live `GeneratedSolidHandle`; otherwise it rolls project/CAD state back.

Read:

- `docs/DIRECT-DRAW-WORKFLOW.md`
- `docs/DIRECT-DRAW-P0-IMPLEMENTATION.md`
- `docs/DIRECT-DRAW-P1-IMPLEMENTATION.md`

## 3. Important Direct Draw boundaries

Do not overclaim these paths:

- GlassWall Direct Draw builds/captures the backing GlassWall host; dedicated Curtain perimeter/mullion/transom frame work remains `QS3DCURTAIN3D` / Curtain Hub.
- WallPier P1 follows the currently supported wall compatibility source/build path. Do not claim arbitrary freeform/specialized open-POLYLINE profile completion.
- StructuralWall P1 uses the existing supported LINE structural path.
- Foundation P1 uses the existing supported closed-POLYLINE structural path.
- Door/Opening Direct Draw is intentionally deferred until host/link/elevation/boolean/rollback UX is specified. Do not add `QS3DDRAWDOOR`/`QS3DDRAWOPENING` by guessing.

Legacy Bóc chọn/capture and `QS3DBUILD3D` remain fully supported for existing CAD drawings.

## 4. Build3D/current safety direction

Concurrent `main` hardening around `QS3DBUILD3D` must be preserved:

- semantic/generated host selection resolves back to stable source handles;
- missing/stale live source CAD stops the operation before replacement;
- mixed native categories are rejected in one logical build;
- mixed LINE/open-POLYLINE wall batches are rejected because builder transactions differ;
- semantic regeneration runs before native builder commit;
- generated output selection does not redefine source-of-truth ownership;
- generated host aliases must not broaden host selection to rebar/mesh/detail output families.

Do not regress these invariants while extending Direct Draw.

## 5. Plugin UI discoverability

The BricsCAD Ribbon contains a `TẠO MỚI` authoring tab and Full Domain Hub contains `TẠO MỚI / DIRECT DRAW`. These are plugin UI hosted by BricsCAD, not a separate application shell.

Keep major Direct Draw commands discoverable in both places while avoiding an overcrowded generic Workspace palette.

## 6. Validation boundary

This continuation work is based on GitHub source/static review. The execution environment used for this pass does not provide licensed interactive BricsCAD V25 runtime proof, and GitHub Actions were not dispatched.

Therefore use precise status language:

**source-implemented / statically guarded ≠ compiled and NETLOAD/runtime-verified on the exact current SHA.**

Still required for Direct Draw and the broader plugin before production claims:

- exact current V25 adapter compile;
- NETLOAD/DemandLoad command registration;
- real Ribbon/palette/Domain Hub behavior;
- successful and forced-failure rollback tests;
- representative private-DWG save/reopen/multi-DWG regression;
- GlassWall/Curtain, WallPier, StructuralWall and Foundation native geometry checks;
- Unicode/HiDPI visual tests and large-model performance.

## 7. CI/release policy

All GitHub Actions workflows remain **manual-only** (`workflow_dispatch`). `continue all`, source changes, docs updates, commits or reviews do not authorize CI/build/runtime/release dispatch. A separate explicit owner request is required.

Do not add automatic push/tag/PR triggers and do not publish a release without explicit owner approval plus the existing release confirmation gate.

## 8. Next safe product work

After current Direct Draw runtime qualification, the most useful remaining authoring/product gaps are:

1. explicit Door/Opening Direct Draw UX/transaction contract before code;
2. transient preview + repeated authoring mode proven against BricsCAD V25 editor/jig behavior;
3. richer Family/type chooser in Direct Draw without duplicating project editors;
4. physical multi-owner wall-solid L/T/X reconciliation under a safe ownership model;
5. curved/open-POLYLINE Curtain frame overlays and richer WallPier profile authoring;
6. real-runtime Ribbon/icon/context-menu/DPI polish;
7. production signing/update infrastructure only when release requirements are explicit.

Never mark runtime-gated items complete from source inspection alone.

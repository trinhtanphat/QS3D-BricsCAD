# QS3D-BricsCAD — current agent handoff

**Updated:** 2026-08-10 (UTC+7)  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the short **current-state delta handoff** for fast-moving work. Always fetch the newest `main` before a write; if newer source conflicts with this document, current source wins.

For the broader baseline also read `docs/AGENT-HANDOFF-LATEST-2026-08-10.md`. Use `docs/AGENT-HANDOFF-SESSION-HISTORY-2026-08-10.md` only when historical chronology is needed.

## 1. Locked product form

QS3D is a **BricsCAD V25 x64 .NET plugin**, not a standalone QS3D CAD executable.

- BricsCAD V25 is required at runtime.
- `QS3D.BricsCAD.V25.dll` is loaded through DemandLoad or `NETLOAD`.
- BricsCAD owns DWG/database/editor/viewport/native CAD lifecycle.
- QS3D adds commands, Ribbon, palettes/modeless WPF tools, semantic/project state, quantities and guarded generated geometry inside BricsCAD.
- `QS3D.Core` is CAD-independent for deterministic logic/testability/reuse; that does not imply a `QS3D.exe`.
- BLT/BLT3D terminology is clean-room workflow/UX reference only.

`docs/PRODUCT-BOUNDARY.md` is authoritative for product-form ambiguity.

## 2. Direct Draw is first-class authoring

QS3D now exposes source-backed authoring directly inside BricsCAD while preserving legacy capture for existing CAD.

### P0

- `QS3DDRAWWALL`
- `QS3DDRAWBEAM`
- `QS3DDRAWCOLUMN`
- `QS3DDRAWSLAB`

P0 includes explicit key dimensions, source-relative bottom offsets, Model-Space gating, unit-aware 5 mm planarity checks, semantic regeneration before native mutation, generated-result selection, XData-based failed-output discovery and verified CAD rollback cleanup.

### Guarded P1

- `QS3DDRAWGLASSWALL`
- `QS3DDRAWWALLPIER`
- `QS3DDRAWSTRUCTWALL`
- `QS3DDRAWFOUNDATION`

P1 creates one real DWG source, captures exactly one semantic element, applies user-confirmed dimensions and reuses canonical `QS3DBUILD3D`. It verifies the generated result and rolls project/CAD state back on failure rather than accepting a reported-but-unbuilt result.

### Door / Opening Direct Draw

- `QS3DDRAWDOOR`
- `QS3DDRAWOPENING`

These commands are **implemented in source**. The picked plan-view LINE is real DWG provenance and its plan length is authoritative `WidthM`; height, sill/bottom offset and boolean clearance remain guarded semantic properties. The command performs selection-scoped Auto Host for only the newly created opening and requires a valid host relation or rolls back.

Physical host cutting is intentionally separate. Direct Draw does **not** silently invoke `QS3DCUTOPENINGS`; the user explicitly chooses straight/curved cut after reviewing host and dimensions. Read `docs/DIRECT-DRAW-OPENINGS.md`.

## 3. Family / Type authoring flow

`QS3DFAMILIES` is the canonical Family Manager. Do not create a competing Direct-Draw-only family database/editor.

The BricsCAD-hosted **TẠO MỚI** Ribbon and Full Domain Hub expose **Family / Type** alongside Direct Draw. Users can activate/edit the compatible Family first; Direct Draw then consumes that active Family and validates/prompts the required instance values.

## 4. Current preview boundary

P0/P1 point acquisition uses BricsCAD `PromptPointOptions` base-point rubber-band behavior where applicable, so the current source has native line/path feedback while picking points.

Do **not** claim a thickness/profile `DrawJig` or a persistent repeat-mode authoring reactor as implemented or V25-proven. Those require exact BricsCAD V25 managed API compilation and interactive validation before source should depend on them.

## 5. Curtain path frames are source-implemented

The old statement that curved/open-POLYLINE Curtain frames are entirely future work is stale.

`QS3DCURTAINFRAMES3D` and `QS3DCURTAIN3D` now have source support for:

- horizontal LINE source;
- guarded open plan-view POLYLINE with +Z normal;
- bulged segments through bounded tessellation;
- deterministic station mapping and frame splitting across path segments;
- linked Door/Opening interruptions;
- generated ownership, live fingerprint/stale checks and bounded piece budgets.

This remains **source-implemented, runtime qualification pending**. It does not create panel-by-panel backing glass solids. Read `docs/CURTAIN-PATH-FRAMES.md`.

## 6. Important remaining geometry boundary

Physical L/T/X multi-owner wall-solid union/reconciliation is still not something to add by guessing. Current safe workflow remains:

`QS3DWALLJUNCTIONS` → `QS3DWALLSNAPPREVIEW` → `QS3DWALLSNAPAPPLY` → ownership-aware rebuild.

A destructive union needs an explicit ownership model, deterministic regeneration/unmerge behavior, failure rollback and licensed V25 boolean tests. Keep this as a runtime/design gate until those invariants exist.

WallPier richer/specialized profile authoring is also separate from the generic compatibility path; do not overclaim freeform profile parity.

## 7. Build3D safety direction

Preserve concurrent `QS3DBUILD3D` hardening:

- semantic/generated host selection resolves back to stable source handles;
- missing/stale live source CAD stops before replacement;
- mixed native categories are rejected in one logical build;
- mixed LINE/open-POLYLINE wall batches are rejected when builder transactions differ;
- semantic regeneration occurs before native builder commit;
- generated output selection does not redefine source-of-truth ownership;
- generated host aliases must not broaden host selection to rebar/mesh/detail output families.

## 8. Static integration guard

`scripts/preflight-direct-draw-authoring-integration.py` protects the cross-feature authoring state:

- `ProjectStateSnapshot` namespace availability through adapter `GlobalUsings.cs`;
- unique Family/Type and Direct Draw command registration;
- Family / Type discoverability in TẠO MỚI Ribbon + Domain Hub;
- Door/Opening source status and no stale “not implemented” command docs;
- Curtain path-frame source status;
- current source-backed rubber-band preview boundary without fake DrawJig claims.

`scripts/preflight-all.py` auto-discovers this gate. GitHub Actions remain manual-only.

## 9. Validation boundary

This continuation work is based on GitHub source/static review. It does not provide licensed interactive BricsCAD V25 runtime proof, and GitHub Actions were not dispatched.

Use precise status language:

**source-implemented / statically guarded ≠ compiled and NETLOAD/runtime-verified on the exact current SHA.**

Still required before production claims:

- exact current V25 adapter compile;
- `NETLOAD` / DemandLoad command registration;
- Ribbon/Domain Hub Family / Type + all Direct Draw invocation;
- successful and forced-failure rollback tests;
- Door/Opening no-host/ambiguous-host rollback and explicit physical cut;
- LINE/open-POLYLINE/bulged Curtain path frames in real V25;
- save/reopen, multi-DWG and representative private-DWG regression;
- Unicode/HiDPI visual tests and large-model performance;
- richer DrawJig/repeat authoring only after compile/runtime proof.

## 10. CI/release policy

All GitHub Actions workflows remain **manual-only** (`workflow_dispatch`). `continue all`, source changes, docs updates, commits or reviews do not authorize CI/build/runtime/release dispatch. A separate explicit owner request is required.

Do not add automatic push/tag/PR triggers and do not publish a release without explicit owner approval plus the existing release confirmation gate.

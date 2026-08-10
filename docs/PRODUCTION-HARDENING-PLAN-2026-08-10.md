# QS3D BricsCAD V25 — production hardening and completion plan

Date: 2026-08-10

## Purpose

This document is the current source-to-product gap map for QS3D. It separates:

1. source defects that can be fixed and regression-guarded in the repository;
2. product work that needs an explicit geometry/ownership/licensing contract;
3. runtime gates that cannot be truthfully closed without licensed interactive BricsCAD V25 on Windows x64.

`source implemented` is not the same as `runtime verified` or `production ready`.

## Current product surface

The repository is already beyond a takeoff prototype. Current source includes:

- drawing-bound semantic Project/Zone/Floor/Family/Instance data and `.qsdb` persistence;
- category-aware capture and BLT-style Direct Draw;
- native host geometry for walls and structural families;
- room/finish workflows;
- Door/WallOpening linking and guarded boolean cutting, including curved-host planning paths;
- Curtain host/frame generation, schedules, opening-aware frame interruption and open/bulged polyline path-frame mapping;
- 3D reinforcement families, BBS, quantity and health flows;
- BQ/Quick Takeoff/ED2/XLSX/CSV schedules and locate round trips;
- generated-handle ownership, stale-state, dependency and release-readiness diagnostics;
- transactional installer/update source paths and signed-manifest verification;
- public-key-only RSA-SHA256 offline license verification in Core;
- manual-only CI/release workflows.

Direct Draw source coverage currently includes:

- P0: `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWSLAB`, `QS3DDRAWCOLUMN`;
- P1: `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`;
- host-aware openings: `QS3DDRAWDOOR`, `QS3DDRAWOPENING`.

## Defects fixed in this hardening batch

### P0 — canonical Build3D false-failure boundary

`QS3DBUILD3D` used to perform Palette refresh, viewport regen, generated selection, status update and `QS3DVIEW3D` dispatch inside the main operation `try` after native geometry had already committed. A UI failure could therefore report `QS3DBUILD3D lỗi` even though the native rebuild had succeeded.

Fixed contract:

- native/semantic build completion remains the operation boundary;
- post-commit UI synchronization is best-effort and non-fatal;
- status/error reporting itself is best-effort so a Palette failure cannot mask the original operation result.

### P0 — wall source-type cardinality

Wall Build3D dispatch used `.Single()` after validation that rejected only `> 1` source types. The zero-source-type state is now rejected explicitly before native dispatch with an actionable Health/Locate message.

### P0 — centralized native build capability

Native category support is now centralized in `Cad/NativeBuildCapability.cs` and consumed by both canonical Build3D and Workspace compatibility checks. This prevents UI/command capability drift when a category is added or removed from native support.

### P0 — cross-layer generated host replacement atomicity

The canonical generated host builders no longer commit CAD first and semantic generated-handle ownership afterward. The following families now capture a deep `ProjectStateSnapshot`, apply generated semantic ownership while the BricsCAD transaction is still rollback-capable, commit CAD only after semantic mutation succeeds, and restore project state when the CAD transaction does not commit:

- LINE ArchitecturalWall/GlassWall host replacement;
- open-POLYLINE ArchitecturalWall/GlassWall/WallPier replacement;
- specialized LINE WallPier profile replacement;
- structural native replacement for Beam, Slab, Column, StructuralWall, Foundation, Stair, Railing and Earthwork.

This specifically closes the old split-brain window where a new `Solid3d` could survive a successful DB transaction while `GeneratedSolidHandle`/semantic ownership failed to advance. It deliberately does **not** claim whole-command atomicity for multi-transaction orchestrators such as Curtain host + frame generation.

### P0 — Curtain frame replacement atomicity

The separate LINE and open/bulged-path Curtain frame builders now use the same cross-layer principle internally:

- capture a deep `ProjectStateSnapshot` before replacement;
- erase previous owned frame solids and create replacements inside one BricsCAD transaction;
- publish `GeneratedCurtainFrameHandles`, counts, configuration/path metadata, stale-state clearing and audit while that CAD transaction can still abort;
- restore the project snapshot when the native transaction does not commit;
- keep `project.Touch()` and live-geometry fingerprint stamping post-commit, because timestamp/UI/live-stamp failure must not misreport an otherwise-valid geometry commit as failed.

`CurtainWallFrameLiveStateService.TryStampSelected(...)` intentionally treats post-commit live fingerprint stamping as best-effort. A missing/pending live fingerprint remains visible to health/readiness instead of turning committed frame geometry into a false command failure.

This closes the per-frame-builder CAD/semantic split-brain window for both LINE and guarded open/bulged path overlays. It still does **not** make the higher-level `QS3DCURTAIN3D` host+frame sequence one native transaction.

### P0 — physical opening prevalidation

Selected physical opening cuts now prevalidate their selected hosts/readiness and fail closed before starting destructive cut work when the selected set is not safe to process as a unit. This reduces partial physical-cut risk without pretending separate host transactions are one global transaction.

### Regression guards

`scripts/preflight-build3d-canonical.py` locks:

- exactly one canonical `QS3DBUILD3D` owner;
- centralized `NativeBuildCapability` consumption rather than duplicate category tables;
- one valid wall source type before `.Single()` dispatch;
- deterministic WallPier LINE/profile vs POLYLINE routing;
- no Curtain detail transaction piggybacking into canonical Build3D;
- post-commit UI synchronization remaining non-fatal.

Generated replacement atomicity is guarded across the four canonical host builder families by dedicated `preflight-generated-replacement-atomic*.py` gates. `scripts/preflight-curtain-frame-atomicity.py` separately locks LINE/path frame replacement ordering, snapshot rollback and non-fatal post-commit live fingerprint stamping. `preflight-all.py` discovers feature gates by filename, so these checks participate automatically when aggregate preflight is explicitly run.

## P0 — remaining sell-ready blockers

### 1. Licensed BricsCAD V25 runtime qualification

Must be completed on the exact release SHA in licensed interactive BricsCAD V25 x64:

- Release/x64 adapter compile against exact V25 managed assemblies;
- `NETLOAD` and DemandLoad;
- Ribbon, Palette, modeless windows and command discovery;
- Direct Draw Wall/Beam/Column/Slab plus P1 and Door/Opening flows;
- cancel/ESC/undo and selection synchronization;
- save/reopen and multi-DWG lifecycle;
- representative private DWGs;
- native `Solid3d` regeneration/ownership/boolean regressions;
- `QS3DHEALTHALL` and `QS3DRELEASECHECK` on representative data.

This is a runtime gate, not a source-code TODO that should be faked by mocks.

### 2. Curtain whole-command orchestration transaction boundary

`QS3DCURTAIN3D` intentionally composes host generation and frame-overlay generation, and those operations still use separate native transaction families. **Each canonical host replacement family and each LINE/path frame replacement builder is now cross-layer atomic on its own.** The remaining gap is only the orchestration boundary between those individually-safe stages.

Before claiming the whole command is all-or-nothing, define and prove an explicit orchestration journal/compensation contract or a shared native transaction design that can safely restore both the prior backing host and prior frame family if a later stage fails.

Until then:

- source/builders must continue fail-closed on foreign ownership;
- semantic/rule validation must happen before the first native mutation;
- Release Readiness must surface stale/inconsistent frame or host state;
- post-commit live fingerprint stamping stays best-effort/non-fatal and missing state remains health-visible;
- canonical `QS3DBUILD3D` must not absorb Curtain frame transactions;
- documentation must not describe the **combined** host+frame command as all-or-nothing even though each underlying replacement builder is internally cross-layer atomic.

### 3. Commercial license enforcement wiring

Core already verifies signed offline licenses with public-key-only RSA-SHA256 logic and deterministic smoke/preflight coverage. The BricsCAD adapter startup currently does not enforce or activate that license.

Before paid distribution, explicitly choose:

- product/SKU names;
- seat vs machine vs user binding policy;
- trial/expiry/grace policy;
- public verification key and key rotation strategy;
- offline-only vs optional activation service;
- license file location and admin/user installation scope;
- support/recovery flow for machine replacement.

Only after those product decisions should adapter command gating/startup enforcement be wired. Private signing/license keys must never be committed.

### 4. Production signing and publisher trust

The preview build is unsigned. Production release requires:

- Authenticode code-signing certificate operations outside the repository;
- signed package/update manifest qualification;
- timestamping and certificate-expiry procedure;
- install/update rollback qualification against a signed package.

## P1 — product completeness

### Direct Draw

Source coverage is broad, including planar-UCS support across the P0/P1/opening authoring families, but runtime acceptance must prove every Direct Draw family creates a real DWG source, semantic owner and expected native result with no partial state after cancellation/failure.

Future Direct Draw candidates should be driven by real customer workflow, not command-count parity. Stair/Railing/Earthwork should not get guessed geometry merely to increase coverage.

### Curtain Wall

Current source already supports deterministic frame overlays for LINE and open/bulged WCS-XY POLYLINE GlassWall paths, including linked-opening interruption and live fingerprints. Both frame-replacement builders are internally cross-layer atomic as described above. Documentation that still says open-polyline native frame overlays are entirely missing, or that the frame builders still commit CAD before semantic ownership, is stale.

Still not claimed complete:

- fabrication-grade panel-by-panel backing glass ownership/boolean model;
- runtime validation of path-frame solids on representative curved/bulged DWGs;
- whole-command host+frame rollback journal/shared transaction contract.

### Wall junctions

Keep the current preview/fingerprint/apply source-centerline cleanup. Do not blindly boolean-union L/T/X/Multi host solids until a multi-owner replacement contract defines which semantic wall owns the resulting physical solid and how openings/health/regeneration survive rebuilds.

### Rebar fabrication detail

Current 3D/BBS geometry must not infer code-specific laps, development length, anchorage, hook or bend rules that were never configured. To call output fabrication-grade, add an explicit standards/rules profile with deterministic versioning and schedule provenance, then runtime-qualify it.

### Performance and UX

Qualification matrix should include:

- Unicode Vietnamese labels and file paths;
- 100/125/150/200% DPI;
- light/dark BricsCAD host themes where relevant;
- large selections and bounded batch limits;
- large semantic models and XLSX exports;
- Palette/Hub document affinity while switching DWGs;
- viewport focus/isolate/section flows on large drawings.

## P1 — commercial repository hygiene

The repository is public. Before serious paid distribution, choose intentionally between:

- public source/open-core;
- public docs/samples + private production source;
- source-available commercial license.

Do not add a legal `LICENSE` file by assumption. The owner must choose the legal licensing model. Release binaries, public samples and customer/private project data should remain separated.

## P2 — optional differentiation

After P0/P1 are green:

- richer BLT-familiar direct authoring previews and repeated-draw workflows;
- configurable company templates/material libraries;
- standards-aware rebar profiles;
- controlled panel-level Curtain representation;
- customer-specific schedule/BQ templates;
- telemetry/crash diagnostics only with an explicit privacy policy and opt-in/enterprise configuration.

## Acceptance matrix before calling 1.0 production-ready

A release candidate is production-ready only when all of these are true for the exact SHA/package:

- aggregate source preflights pass;
- Core Release build/smoke pass;
- V25 Release/x64 adapter build passes against exact V25 assemblies;
- signed package installs via DemandLoad and rolls back on failed replacement;
- commands/Ribbon/Palette load cleanly;
- Direct Draw and capture compatibility paths pass save/reopen/regen/undo/cancel checks;
- Wall/WallPier/GlassWall/Opening/Curtain/Structure/Rebar native regressions pass;
- schedules/XLSX/CSV/ED2 round trips pass;
- generated ownership, stale state, dependency health and Release Readiness are clean;
- Unicode/HiDPI and large-model tests pass;
- commercial licensing/signing policy is intentionally configured if the build is sold.

## CI policy

Do not dispatch GitHub Actions merely because source/docs were updated. Repository workflows remain owner-controlled and manual-only. An explicit build/runtime/release request is required before any workflow dispatch.

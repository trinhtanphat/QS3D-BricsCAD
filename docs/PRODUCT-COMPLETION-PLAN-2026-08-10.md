# QS3D-BricsCAD — production completion & BLT-style parity plan

Date: 2026-08-10 (UTC+7)

This plan defines what “complete” means for QS3D as an original clean-room BricsCAD V25 plugin. It is not a claim that proprietary BLT source or internals are available. `BLT-like` means comparable workflow ergonomics and quantity/BIM authoring outcomes built independently on the BricsCAD API.

## 1. Product target

QS3D should become a production-grade BricsCAD V25 x64 plugin where a customer can:

- install a signed release without Visual Studio or source code;
- open an ordinary DWG and author/capture semantic architectural and structural elements;
- create native/generated 3D in the BricsCAD viewport;
- edit Family/Instance properties with deterministic invalidation/regeneration;
- create/link/cut supported Door/Opening geometry;
- produce quantities, schedules, Excel/BBS outputs and locate rows back in CAD;
- create guarded reinforcement for supported members;
- save/reopen/move between drawings without corrupting project ownership;
- run Health/Release/Runtime checks and get actionable failures instead of silent corruption;
- upgrade/uninstall securely with package/version/publisher verification.

The native BricsCAD viewport remains the CAD engine. QS3D must not duplicate BricsCAD as a standalone CAD application.

## 2. Current source status

Implemented source areas include:

- semantic Project/Zone/Floor/Family/Element model and `.qsdb` persistence;
- dependency regeneration, stale state and generated-handle ownership;
- architectural/structural capture workflows;
- Direct Draw for the current Wall/Beam/Column/Slab and guarded P1 categories documented in `DIRECT-DRAW-WORKFLOW.md`;
- Room/HT_PHÒNG, Door/Opening host workflows, Curtain workflows and supported native geometry;
- BQ/Excel/Locate/reporting and current schedules;
- Column/Beam/Slab/Wall/Foundation supported rebar families and health checks;
- Ribbon, Workspace, property panels, Family Manager, Hubs and per-user layout persistence;
- manual-only build/runtime/release workflows;
- secure package installer/updater, package hashes and Authenticode signing/finalization path;
- `QS3DRELEASECHECK` and `QS3DRUNTIMECHECK` source-level diagnostics.

Source implementation is not equivalent to licensed BricsCAD runtime certification.

## 3. P0 — production release blockers

A stable customer release is forbidden until every item below is green on the exact release SHA.

### P0.1 Exact-SHA build and static gates

Required:

- `python scripts/preflight-ci-manual-only.py`;
- `python scripts/preflight.py`;
- `python scripts/preflight-all.py` including customer-release/version/signing/ownership/atomicity gates;
- `dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release`;
- deterministic Core smoke suite;
- V25 adapter build against the installed licensed V25 `BrxMgd.dll` and `TD_Mgd.dll`.

No historical green run qualifies a newer SHA.

### P0.2 Licensed BricsCAD V25 runtime gate

The self-hosted Windows runner must have the `windows`, `x64`, `bricscad-v25` labels and a licensed BricsCAD V25 installation.

Runtime acceptance suite must verify at minimum:

- NETLOAD and Registry DemandLoad;
- `QS3D`, Ribbon, Workspace, Hubs and property panels;
- `QS3DRUNTIMECHECK` reports V25/x64 and package/assembly consistency;
- Direct Draw Wall/Beam/Column/Slab;
- capture -> edit -> `QS3DBUILD3D` compatibility path;
- Door/Opening unique-host, no-host and ambiguous-host behavior;
- explicit supported physical opening cuts;
- Room/HT_PHÒNG generation;
- Curtain generated ownership;
- BQ/BBS/Excel export + Locate;
- supported rebar generation and Health All;
- ESC/cancel/exception paths do not leave semantic or CAD half-commits;
- undo/redo where the command contract supports it;
- save/reopen and project reload;
- multi-DWG switching and Save As identity synchronization;
- representative millimeter/metre drawing-unit cases;
- Unicode Vietnamese text and HiDPI layouts.

### P0.3 Stable signing and package identity

Stable release requires:

- Authenticode code-signing certificate available to the self-hosted release runner;
- SHA-256 signing and trusted HTTPS timestamping;
- signed plugin/Core/installer/uninstaller/updater executable payloads;
- finalized package metadata records `pluginSignatureStatus=Valid`, signer thumbprint and signed assembly version;
- plugin/Core source `<Version>` values are identical;
- `RELEASE_TAG` equals the full source product version exactly, including prerelease suffix;
- ZIP SHA-256 and internal `SHA256SUMS.txt` are generated after final signing/finalization;
- no BricsCAD proprietary runtime DLL is bundled.

### P0.4 Clean-machine install/upgrade/uninstall proof

Test on a Windows user profile that did not build QS3D:

1. BricsCAD V25 compatible edition installed and launched once.
2. Download/extract QS3D release ZIP.
3. Run the packaged DemandLoad installer.
4. Launch BricsCAD and invoke `QS3D` without manual NETLOAD.
5. Run `QS3DRUNTIMECHECK`.
6. Upgrade from the previous signed release with `-Force` or the signed updater path.
7. Uninstall and verify registration/payload cleanup without changing unrelated BricsCAD security settings.

The customer must not need Visual Studio, the repository, BricsCAD SDK DLL copies or a local compiler.

## 4. P1 — BLT-style authoring parity

These areas provide the biggest product-value gain after P0 runtime qualification.

### P1.1 Level/Grid/reference model

Implement first-class Levels and Grids so element vertical placement is not represented only as source-relative offsets.

Acceptance:

- Level CRUD with stable IDs;
- bottom/top level references plus offsets;
- Floor/Zone/element filtering by level;
- level changes invalidate only dependent elements;
- deterministic quantity/regeneration behavior;
- migration of current offset-only data without guessing absolute elevations.

### P1.2 Native modify workflow

Add BricsCAD-native editing ergonomics rather than forcing delete/recreate:

- move/stretch supported semantic sources;
- grip/jig or equivalent preview for key dimensions;
- Family/Instance edit -> targeted invalidate -> native rebuild;
- selection synchronization in both directions;
- command-level undo/cancel contract;
- repeat-command authoring for high-frequency objects.

### P1.3 Wall topology and joins

Finish physical solid reconciliation for complex L/T/X/Multi intersections.

Acceptance:

- deterministic join ownership;
- no duplicate volume at intersections;
- safe cleanup/rebuild after source edits;
- curved/open-polyline cases only where mathematically supported;
- explicit fail-closed result for unsupported freeform geometry.

### P1.4 Door/Opening booleans

Extend physical cuts without weakening safety:

- targeted cut transaction for the selected/new opening;
- corner-crossing and curved host handling where proven;
- cut ownership and rollback across host replacement;
- no global destructive recut when a local operation is sufficient.

### P1.5 Curtain/Pier production geometry

Continue from current frame overlay/path work to:

- broader curved-path support;
- deterministic mullion/transom segmentation;
- panel-by-panel glass solids;
- opening-aware panel clipping;
- quantities separated for frame/glass/panel where semantically explicit.

## 5. P2 — structural/rebar depth

Do not infer engineering reinforcement from geometry alone. Add detail only when explicit design/fabrication data exists.

Target areas:

- multi-zone Beam reinforcement;
- advanced Structural Wall zones/boundaries;
- non-rectangular clipped Slab/Foundation mesh through a generalized clipping planner;
- explicit hook/bend-radius/anchorage/lap semantics;
- bar editing/manipulation with stable bar marks;
- BBS cutting lengths derived from explicit fabrication semantics;
- conflict/cover/host containment health checks.

## 6. P2 — documentation and construction output

To become stronger than a quantity-only plugin, add a documentation layer on top of the semantic model:

- tags/labels linked to stable semantic IDs;
- generated plan/elevation/section helpers;
- quantity table placement into DWG;
- revision-aware schedule deltas;
- sheet/view metadata and export presets;
- deterministic re-generation of annotations after model edits.

Generated annotations must have explicit ownership and must not be recaptured as new semantic model inputs.

## 7. P2 — interoperability and large-project workflow

After core runtime stability:

- Xref-aware read-only federation and quantity boundaries;
- multi-DWG project aggregation without cross-drawing Handle confusion;
- IFC/BIM import/export only through documented BricsCAD/API-compatible contracts;
- template/company standards for layers, Families, quantity rules and naming;
- incremental scanning/cache for large DWGs;
- performance budgets and cancellation for long operations;
- diagnostic/support export with privacy-safe defaults.

## 8. UI/UX target

The desired BLT-style feel is achieved by workflow speed, not visual copying.

Required characteristics:

- CAD viewport remains primary;
- compact dark Vietnamese-first workspace;
- modeless panels where editing while drawing is useful;
- consistent Family/Instance inspector;
- obvious Direct Draw vs Capture distinction;
- visible Health/Release status;
- persistent user layout;
- searchable command/action surface;
- disabled actions explain why they are unavailable;
- no decorative buttons that do not execute real behavior.

## 9. Release ladder

### Preview

Use `vX.Y.Z-preview.N` only for internal/controlled testing. It may be explicitly unsigned or skip runtime only when the manual release workflow is intentionally configured as a prerelease; never present such a build as customer production.

### Release Candidate

Use `vX.Y.Z-rc.N` after:

- exact-SHA source/Core/V25 build passes;
- licensed V25 runtime suite passes;
- installer/upgrade/uninstall passes on a clean profile;
- package is signed/timestamped;
- no known P0 data-loss/ownership/release bug remains.

### Stable

Use `vX.Y.Z` only after the same RC artifact family completes representative private-DWG regression and all P0 gates are green. Stable release workflow must keep `run_runtime=true`, `sign_package=true`, `prerelease=false`, and the source `<Version>` must be `X.Y.Z` before dispatch.

## 10. Definition of “complete”

Do not define completion by number of commands or by a date.

For a sellable V1, “complete” means:

- all P0 release blockers pass on exact SHA;
- major architecture/structure/direct-draw paths used in the supported sales scope are runtime-qualified;
- current guarded limitations are documented and fail closed;
- installer/update/uninstall works for a non-developer customer;
- no unsigned stable package;
- no known semantic/CAD ownership half-commit;
- no release/tag/version ambiguity;
- support can identify the installed product/runtime using `QS3DRUNTIMECHECK`;
- customer data remains local unless a future feature explicitly documents otherwise.

P1/P2 features can continue after V1, but a feature must not be advertised as supported until its own runtime/health/save-reopen acceptance suite passes.

## 11. Immediate next release decision

The existing `v0.1.0-preview.1` release is a historical preview. Do not overwrite that tag or asset.

For the next build:

1. finish/review the current source batch;
2. choose the next product version and update `<Version>` consistently in plugin/Core;
3. provision/confirm the licensed V25 self-hosted runner and signing certificate variables;
4. manually dispatch the V25 release workflow with runtime enabled;
5. publish as prerelease/RC until the clean-machine/private-DWG runtime matrix is green;
6. only then cut the first stable customer release.

The repository remains manual-workflow-only; source commits do not authorize Actions by themselves.

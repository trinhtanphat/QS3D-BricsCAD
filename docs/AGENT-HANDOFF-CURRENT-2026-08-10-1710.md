# QS3D-BricsCAD — current agent handoff (2026-08-10 17:10 UTC+7)

**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Canonical branch:** `main`

This is the newest short current-state delta for fast-moving work. Fetch `main` before every write. If current source conflicts with this note, current source wins.

Read this together with `docs/AGENT-HANDOFF-LATEST-2026-08-10.md`, `docs/LOCAL-V25-QUALIFICATION.md`, `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`, `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md` and `docs/LEVEL-REFERENCES.md`.

## 1. Product boundary

QS3D remains a clean-room **BricsCAD V25 x64 .NET plugin**. BricsCAD owns the DWG/editor/viewport/native CAD lifecycle. Do not create a standalone QS3D CAD application or duplicate BricsCAD runtime assemblies.

`BLT-like` means workflow/UX familiarity and independently implemented quantity/semantic/native behavior, not proprietary source/assets.

## 2. New source capability in the latest continue-all review

### Dependency-safe semantic untrack

`QS3DUNTRACK` / `QS3DUNTRACKFINISH` now resolve both source and generated selections through canonical semantic ownership, reject ambiguous ownership and block removal while transitive semantic dependents remain outside the selected batch. A complete dependent batch can be untracked together. CAD geometry remains untouched by untrack.

### Grid semantic reference capture

New command:

```text
QS3DGRID
```

Current contract:

- selection-only semantic capture;
- accepts only `LINE` / `ARC` sources with finite positive length;
- validates the whole selection before semantic mutation;
- reuses transactional `SemanticCaptureService` and canonical generated-output rejection;
- reuses existing `ElementCategory.Grid` + `GenericTakeoffRegenerator` (`LengthM`, `Count`);
- post-capture Workspace/UI refresh is non-fatal and must not turn a valid committed semantic capture into a reported command failure;
- does not claim native 3D Grid geometry.

Read `docs/GRID-WORKFLOW.md`.

Still not implemented/qualified: Grid bubbles/naming/renumbering, rectangular/radial systems, Grid constraints, Direct Draw Grid jig/repeat mode, structure-to-grid hosting.

### Manual host-link atomicity — source blocker closed

`QS3DLINKHOST` now snapshots the full `ProjectState` before `HostLinkService.LinkOpening(...)`. Manual link mutation and deterministic regeneration share one rollback boundary. If linking/regeneration fails, the snapshot is restored; if restore itself fails, both failures are surfaced rather than silently keeping a half-mutated project.

Preserve the rule that semantic commit succeeds before palette/status refresh. Post-commit UI refresh is not allowed to turn a valid host-link commit into an apparent semantic failure.

### Physical opening cut live freshness

Straight/selected and curved physical opening cuts now stamp a live-input fingerprint after a successful cut. Health/Release can detect a host that still carries an old hole after host/opening CAD geometry, linked opening membership or effective cut parameters changed.

The fingerprint covers host CAD source geometry and effective host dimensions, linked opening source extents plus width/height/sill/clearance, and the curved-host settings that affect curved cutter construction. Host rebuild invalidation still clears the entire `PhysicalOpeningCut*` metadata family.

Keep `PhysicalOpeningCutLiveStateService` wired into `QS3DHEALTHALL` and `QS3DRELEASECHECK`. A legacy cut without live fingerprint is intentionally a release-blocking warning until rebuilt/cut again; do not silently auto-upgrade stale CAD geometry by stamping metadata only.

### Comprehensive Core health

`QS3DHEALTH` now uses `ComprehensiveModelHealthService`. The Core composite includes model/source health, Room Finish integrity, dependency health, Level-reference health, generated ownership/stale/mode health, fabrication qualification and all generated rebar/mesh/curtain families.

Adapter/live-CAD-specific checks such as curtain-frame live fingerprints and physical-opening live-cut fingerprints remain in `QS3DHEALTHALL` / `QS3DRELEASECHECK`; do not move those into Core by duplicating BricsCAD runtime dependencies.

### Runtime diagnostics truthfulness

`QS3DRUNTIMECHECK` reports V25/x64/version/package consistency. Package signing information shown there is explicitly **recorded metadata only**; cryptographic Authenticode publisher/timestamp verification remains the responsibility of the signed installer/release gate. Do not change this command back into a misleading `signature=signed` claim based only on JSON metadata.

### Privacy-safe support diagnostics

New command:

```text
QS3DSUPPORTBUNDLE
```

Default report may contain runtime/product/schema/category/count/dirty-state information, but intentionally excludes DWG names/paths, CAD handles, semantic IDs, Family identity, project metadata, user name, machine name, private geometry and secrets. Read `docs/SUPPORT-DIAGNOSTICS.md` and preserve `preflight-support-bundle.py`.

## 3. Local exact-SHA V25 qualification is now a first-class handoff

Canonical local runner:

```text
scripts/run-local-v25-qualification.ps1
```

Canonical runtime matrix:

```text
docs/LOCAL-V25-QUALIFICATION.md
```

The runner requires a clean exact SHA and coordinates source preflights, Core build/smoke, V25 adapter build against installed `BrxMgd.dll`/`TD_Mgd.dll`, licensed runtime probing and scoped local evidence. `-SkipRuntime` is diagnostic only and cannot qualify a customer release.

Runtime evidence belongs under the gitignored `artifacts/` tree. Use only sanitized evidence handoff files for Git commits.

Recent concurrent work also added scoped signed-package qualification support and sanitized exact-SHA evidence export. Inspect the current runner/scripts before changing their schema or status semantics.

## 4. Rebar/native replacement state

Preserve recent cross-layer atomicity hardening across generated rebar/mesh families. Semantic ownership/audit state is updated inside the same pre-CAD-commit logical operation and restored from project snapshots if the native transaction fails before commit. Post-commit UI refresh failures must remain non-fatal.

Shape Rebar now records the canonical audit event:

```text
geometry.rebar.shape
```

alongside its generated handle/count/mode/stale update before the CAD transaction commits. Static audit guards cover generated rebar/mesh families.

The standards-neutral fabrication qualification gate is evidence/provenance validation only. It is not an engineering-code compliance engine. Standard-specific hooks/laps/anchorage/bend rules require an explicit approved governing standard + revision and engineering sign-off.

## 5. Curtain boundary remains intentionally truthful

Individual GlassWall host and Curtain LINE/path frame replacement families have guarded transaction/project rollback contracts. However:

```text
QS3DCURTAIN3D
```

still orchestrates multiple independent native phases. A later phase can fail after an earlier phase legitimately committed; current source reports `Curtain 3D PARTIAL COMMIT` rather than pretending whole-command rollback.

Do not remove the warning. Whole-command completion needs either a shared native transaction orchestration or a persisted ownership-safe compensation/recovery journal proven on real V25. See `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`.

Panel-by-panel native backing glass also remains a local/native architecture gate; do not add it as an unrelated best-effort third transaction.

## 6. Floor / Level model clarification — P1.1 Core foundation merged

Do not add a duplicate `LevelDefinition` merely for visual parity with another product. Existing `ProjectFloor` is the semantic Level catalog.

Current Core now has:

- `FloorDefinition` with stable ID/name/elevation;
- `ActiveFloorId` and legacy element `FloorId`;
- opt-in element `BottomLevelId`, `BottomLevelOffsetM`, `TopLevelId`, `TopLevelOffsetM` semantics owned by `ProjectFloorService`;
- `ElementVerticalPlacementService` as the single Core contract for legacy source-relative vs Level-referenced vertical placement;
- legacy no-Level behavior preserved exactly as source base + legacy `BottomOffsetM` + legacy `HeightM`;
- Bottom-only behavior resolving absolute bottom from Level elevation + explicit offset while retaining legacy height;
- Bottom+Top behavior deriving effective height from explicit Level elevations + offsets;
- fail-closed validation for Top-without-Bottom, missing Level IDs, non-finite offsets and top <= bottom;
- Floor update/reference-count/delete lifecycle covering `FloorId` plus Bottom/Top Level references;
- `LevelReferenceHealthService` wired into comprehensive Health, Health All and Release Check;
- Core smoke + static preflight protecting compatibility and invalid-reference behavior.

Important boundary: **native CAD placement and Bottom/Top Level assignment UI are not enabled yet**. Do not expose Level-picker buttons that imply CAD placement until host solids, physical openings, curtain geometry and rebar/dependent outputs consume the same vertical-placement resolver coherently. There is no implicit `FloorId -> BottomLevelId` migration and no double application of legacy `BottomOffsetM` after a Bottom Level ref is present.

Read `docs/LEVEL-REFERENCES.md`.

## 7. Diagnostics Hub integration — source resolved

Keep these commands distinct:

- `QS3DRUNTIMECHECK` — customer/runtime diagnostic;
- `QS3DRUNTIMEPROBE` — automation probe used by the local runtime harness.

Current `main` now wires the Full Domain Hub user-facing `Kiểm tra runtime V25` action to `QS3DRUNTIMECHECK` and exposes `QS3DSUPPORTBUNDLE` as `Tạo Support Bundle` in the same `KIỂM TRA / RELEASE` section. The automation-only `QS3DRUNTIMEPROBE` command remains separate for the local runtime harness.

Preserve `scripts/preflight-domain-hub-diagnostics.py`: it parses the XAML, guards the customer-facing command tags, confirms both command implementations still exist and fails if the runtime button is accidentally routed back to `QS3DRUNTIMEPROBE`.

This is a **source/static integration result**. Real button execution, host-theme rendering and diagnostic output still belong to the exact-SHA local V25 matrix.

## 8. Remaining local / native / policy gates

Source-only agents must not fake completion of:

- exact-current-SHA V25 build/NETLOAD/DemandLoad/runtime proof;
- Direct Draw/planar-UCS/ESC/UNDO/repeat authoring and future DrawJig behavior;
- Curtain whole-command recovery and panel-by-panel native glass;
- physical ownership-safe L/T/X/Multi wall-junction solids;
- native Level-referenced placement across host solids + physical openings + curtain + rebar/dependent generated geometry, and the matching Bottom/Top Level assignment UI;
- representative private-DWG save/reopen/multi-DWG regression;
- Unicode/HiDPI and large-model performance;
- clean install/upgrade/uninstall;
- actual Authenticode certificate/timestamp/package trust evidence;
- commercial-license enforcement until owner supplies real SKU/seat/trial/binding/offline/rotation policy;
- standard-specific fabrication-grade rebar until governing standard/revision + engineering inputs exist;
- legal/public/source distribution model until owner/legal policy is chosen;
- Grid bubbles/naming/renumbering, rectangular/radial systems, constraints, Direct Draw Grid and structure-to-grid hosting beyond the current semantic capture source;
- broader documentation/interoperability only after explicit supported semantics/formats are defined.

Canonical details:

- `docs/LOCAL-V25-QUALIFICATION.md`
- `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`
- `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`

GitHub tracking created for continuation/local agents:

- #72 exact V25 qualification;
- #73 multi-owner wall solids / advanced geometry;
- #74 Direct Draw transient preview / repeated authoring;
- #75 production signing/install/update + optional licensing boundary;
- #76 fabrication-grade rebar / structural depth;
- #77 documentation layer;
- #79 Grid/reference model + richer level constraints — **partially advanced by `QS3DGRID` semantic capture and the P1.1 Core Level-reference foundation; native placement/UI remain open**;
- #80 native semantic modify/edit workflow;
- #81 large-model performance;
- #82 real V25 UI/DPI/context-menu/Ribbon polish;
- #83 generalized polygonal Slab/Foundation mesh;
- #84 broader interoperability/import-export.

Before acting on any issue, inspect current `main` because concurrent source work may have partially or fully advanced it beyond the issue's original description. Do not close runtime/engineering/external issues from source inspection alone.

## 9. CI / release rule

GitHub Actions remain manual-only. `continue all`, source review, commits, merges, docs or handoff updates do **not** authorize workflow dispatch or GitHub Release publication.

A separate explicit owner instruction is required for CI/build/runtime/release execution. Never weaken BricsCAD `SECURELOAD`, Windows trust or signature validation to force a package/runtime test to pass.

# QS3D local-agent inbox

**Updated:** 2026-08-11 (UTC+7)

This file is the **single live queue for LOCAL_ONLY work**. Detailed runbooks remain in the linked local qualification/handoff documents, but a local agent should start here before opening those longer files.

## Mandatory handoff contract

- A remote/hybrid agent that discovers a new LOCAL_ONLY requirement must add or update the matching item in this file **in the same source/docs batch that introduced or exposed the requirement**.
- Do not create a second live queue. Historical `docs/LOCAL-AGENT-*.md` files are supporting detail/evidence; this inbox is the current priority index.
- Local agents work `P0` before `P1` before `P2`, always from a clean checkout of the newest intended SHA.
- `LOCAL_PASS` requires real evidence tied to the exact tested SHA. Source review, static preflight, mock tests, `-SkipRuntime`, or a remote build cannot manufacture `LOCAL_PASS`.
- Never commit proprietary BricsCAD DLLs, private/customer DWGs, signing keys, credentials, or unsanitized runtime captures.
- When an item passes, set `Status: PASS`, replace `Evidence: PENDING_LOCAL` with a sanitized evidence summary, and record the exact SHA under `Evidence`.
- When source changes alter a local scenario, update this inbox immediately instead of relying on an older handoff paragraph.

Valid priorities: `P0`, `P1`, `P2`.  
Valid statuses: `OPEN`, `IN_PROGRESS`, `PASS`, `BLOCKED`.

## LOCAL-001 — exact V25 build/load baseline

- Priority: P0
- Status: OPEN
- Area: BricsCAD V25 adapter / packaging baseline
- Why local: Requires licensed BricsCAD V25 x64, installed managed references, Windows desktop, NETLOAD/DemandLoad, and native command execution.
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Also cold-start/reopen a drawing with an existing `.qsdb`, invoke one existing-project mutation before another command has warmed the cache, verify the mutation binds the canonical project, then save/reopen. Repeat with the sidecar absent and verify ownership-dependent mutation refuses without leaving a new project in the live cache.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache existing-sidecar ProjectId continuity, absent-sidecar refusal/no-new-project result, sanitized failure log if any.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-002 — Curtain whole-command recovery and native panels

- Priority: P0
- Status: OPEN
- Area: Curtain Wall
- Why local: Final proof needs native V25 Solid3d/transaction behavior, failure injection across host/frame/panel phases, and save/reopen ownership verification.
- Scenario: Qualify whole-command recovery/compensation for `QS3DCURTAIN3D`; materialize panel-by-panel glass from `CurtainWallDetailPlanner.Panels` with bounded ownership, opening interruption, stale/health/release integration, and deterministic replacement.
- Evidence required: Exact SHA; injected-failure matrix after each logical phase; panel ownership/count checks; opening/door clipping checks; save/reopen result; no foreign-object deletion.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-003 — shared Level Z-chain in native geometry

- Priority: P0
- Status: OPEN
- Area: Structural / Wall / Opening / Rebar vertical placement
- Why local: Correctness depends on native V25 geometry, cutters, generated rebar alignment, and save/reopen behavior after Level edits.
- Scenario: Qualify the shared `ElementVerticalPlacementService` chain across wall families, Beam/Column/Slab/Foundation, Door/WallOpening, Curtain frames/panels, and generated reinforcement. Cover legacy/no-Level, Bottom-only, Bottom+Top, Top-only fail-closed, deleted/renamed Level, and dependent invalidation.
- Evidence required: Exact SHA; before/after Z measurements; host-opening-rebar alignment; health/release blocker behavior; save/reopen and Level-edit invalidation results.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-004 — source reconcile native atomicity

- Priority: P0
- Status: OPEN
- Area: Source Reconcile / Modify
- Why local: Requires real LINE/POLYLINE edits, native generated-object cleanup, transaction/undo behavior, and document switching.
- Scenario: Exercise `QS3DSYNCSOURCE` after source edits; verify generated dependents invalidate safely, generated/ambiguous selections fail closed, forced failure restores project/native state, undo/redo is coherent, and document switches never mutate another project.
- Evidence required: Exact SHA; source edit/reconcile results; injected failure evidence; undo/redo notes; multi-DWG result; save/reopen result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-005 — polygon Slab/Foundation native reinforcement

- Priority: P1
- Status: OPEN
- Area: Rebar 3D / Slab / Foundation
- Why local: Core outer+holes+disconnected multi-region topology/planning is REMOTE_DONE. Remaining proof requires native source-loop/RegionId association, native rebar materialization/ownership, straight/bulged extraction and OCS/WCS behavior, limits, Undo/save-reopen, multi-DWG, and exact V25 geometry.
- Scenario: Qualify the current Core region plans through native Slab/Foundation extraction/materialization. Cover convex/concave/disconnected regions and holes, straight/bulged source loops, impossible-cover rejection, bounded bar/object counts, rectangle compatibility, per-region ownership/stale/health, cross-layer rollback, Undo/Redo, and multi-DWG. Do not concatenate islands or treat islands as holes.
- Evidence required: Exact SHA; representative region/hole/bulge matrix; source-loop ↔ RegionId ↔ native-owner checks; cover/spacing/count measurements; limit rejection; rollback/Undo result; save-reopen/multi-DWG result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/POLYGON-REGION-HOLES.md`; `docs/POLYGONAL-SLAB-MESH.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-006 — native documentation objects

- Priority: P1
- Status: OPEN
- Area: Documentation / Tags / Tables / Sheets
- Why local: Semantic MText tags and generic/authoritative project Table source paths already exist; licensed V25 is required to prove runtime ownership/rendering/refresh plus the remaining MLeader, custom-schedule interaction, Layout, Viewport and PaperSpace behavior.
- Scenario: Qualify the existing `QS3DTAG` / `QS3DTAGREFRESH` / `QS3DTAGREMOVE` / `QS3DTAGHEALTH` lifecycle and the existing native generic/BQ/Door-Opening/Room-Finish/Material/BBS Table lifecycles first; do not reimplement them. Include a cold-cache reopen with a valid `.qsdb`, then create/refresh/remove a tag and each representative native Table before any other QS3D command warms the project cache; verify generated ownership/metadata lands on the canonical project and survives save/reopen. With no sidecar, verify these ownership-dependent commands refuse rather than creating an empty project. Then implement/qualify only the remaining native MLeader/custom-schedule interaction and Sheet/Layout/Viewport/title-block/PaperSpace workflows. Verify stable semantic/project ownership, deterministic refresh, user-object protection, Unicode/HiDPI, styles, scale/lock, Undo, and save/reopen.
- Evidence required: Exact SHA; semantic-tag and each implemented Table ownership/refresh/health checks; cold-cache ProjectId/metadata continuity; absent-sidecar refusal; user-object protection; Unicode/HiDPI result; Layout/Viewport scale/lock result when implemented; Undo/save-reopen/multi-DWG result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/SEMANTIC-TAGS.md`; `docs/COMMANDS-NATIVE-DOCUMENTATION-TABLES.md`; `docs/DOCUMENTATION-LAYER.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Why local: Core now defines deterministic multi-owner identity/dependency/rebuild plans, but safe native Solid3d materialization, booleans, replacement and ownership verification still require V25.
- Scenario: Materialize only from current `WallJunctionOwnershipPlanner` output. Treat all plans sharing one `GroupToken` (`WJP1:`) as one replacement/rebuild unit; never assume an individual occurrence index remains long-lived when group topology/membership changes. Persist/verify dedicated `OwnerToken` (`WJX1:`) plus `InputFingerprint` (`WJF1:`), never reuse one wall's generated-solid owner. Cover L/T/X/Multi, 2/3/4+ owners, multiple occurrences, mixed thicknesses, incompatible vertical ranges, source/profile/elevation changes, owner add/remove, stale-extra output cleanup, foreign/corrupt ownership refusal, and Door/Opening host retention.
- Evidence required: Exact SHA; whole-`GroupToken` membership/replacement check; `OwnerToken`/dependency/fingerprint persistence checks; L/T/X/Multi geometry matrix; invalidation/rebuild after fingerprint or group-membership changes; stale-extra cleanup; no cross-DWG/project mutation; opening host-retention result; save/reopen and Undo/Redo result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/WALL-JUNCTION-OWNERSHIP.md`; `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-008 — Direct Draw transient preview and repeated mode

- Priority: P1
- Status: OPEN
- Area: Direct Draw UX
- Why local: DrawJig/transient/editor/UCS lifecycle, ESC cleanup, document switches, and native palette behavior require interactive V25.
- Scenario: Qualify transient thickness/profile preview and repeated authoring; ESC/cancel leaves no residue, active planar UCS is respected, only safe last values are reused, document switch cancels safely, and final source+semantic+native commit remains atomic.
- Evidence required: Exact SHA; cancel/ESC evidence; UCS matrix; repeated-mode result; document-switch result; no persistent preview residue.
- Evidence: PENDING_LOCAL
- Related docs: `docs/DIRECT-DRAW-WORKFLOW.md`; `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-009 — clean-machine install/sign/update qualification

- Priority: P1
- Status: OPEN
- Area: Packaging / Release / Trust
- Why local: Production signing certificate, Windows trust chain, timestamp, clean-machine install/update/rollback/uninstall, and BricsCAD SECURELOAD behavior cannot be proven remotely.
- Scenario: On an authorized customer-like Windows/V25 machine, sign approved binaries, verify signer/timestamp, finalize hashes/manifest/ZIP only after verification, then test clean install, upgrade, rollback, uninstall, DemandLoad, and trust behavior without weakening SECURELOAD.
- Evidence required: Exact SHA/tag; signer/timestamp verification summary; package hashes; install/upgrade/rollback/uninstall result; DemandLoad/SECURELOAD result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/MANUAL-BUILD-RELEASE.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-010 — large-model performance and UI matrix

- Priority: P2
- Status: OPEN
- Area: Performance / UI / HiDPI
- Why local: Representative timings, native palette responsiveness, GPU/driver effects, and DPI/layout behavior need real hardware and V25.
- Scenario: Measure DependencyGraph/regeneration, rooms, wall junctions, Auto Host, Curtain, BQ/BBS/ED2/Interchange, ownership/Health, rebar limits, plus 100/125/150/200% DPI and narrow/normal/wide palettes on representative large projects.
- Evidence required: Exact SHA; hardware/OS/V25 build; project sizes; timings; DPI/layout results; sanitized bottleneck notes.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-011 — staged native rollback and post-commit UI isolation

- Priority: P1
- Status: OPEN
- Area: Fault injection / rollback / modeless UI
- Why local: Core can prove semantic `ProjectStateSnapshot` restoration remotely, but native transaction abort/compensation, DocumentLock and multi-DWG behavior, generated-object cleanup, and post-commit WPF/modeless failures require interactive licensed BricsCAD V25.
- Scenario: Starting from the Core staged matrix in `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`, inject failures before native commit, during native cleanup/materialization, immediately after native commit, and during post-commit palette/modeless refresh. Verify semantic/native state ownership remains coherent, committed native work is not falsely rolled back by UI failure, another DWG is never mutated, and stale modeless callbacks fail closed. Specifically open Recognition review, unload/forget or otherwise make its original project unavailable without closing the bound DWG, then invoke Apply: it must not create a replacement empty project. Repeat after cold-cache rebind of the same valid sidecar and verify Apply uses the canonical current project.
- Evidence required: Exact SHA; failure stage and exception; before/after semantic snapshot summary; native object/owner counts; transaction/Undo result; active-DWG identity; stale Recognition Apply no-project-creation/canonical-rebind result; post-commit UI result; save/reopen result; sanitized screenshots/logs where useful.
- Evidence: PENDING_LOCAL
- Related docs: `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.
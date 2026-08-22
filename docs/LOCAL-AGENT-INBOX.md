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
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, sanitized failure log if any.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
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
- Why local: Core polygon topology/planning can be source-tested remotely, but final bar-centerline cover, native materialization, ownership, limits, and save/reopen require V25.
- Scenario: Qualify convex/concave/disconnected regions and holes, impossible-cover rejection, bounded bar/object counts, rectangle compatibility, aggregate ownership/stale/health semantics, and cross-layer rollback before CAD commit.
- Evidence required: Exact SHA; representative geometry matrix; cover/spacing/count measurements; limit rejection; rollback result; save/reopen result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-006 — native documentation objects

- Priority: P1
- Status: OPEN
- Area: Documentation / Tags / Tables / Sheets
- Why local: MText/MLeader, Table, Layout, Viewport, Paper/Model Space, styles, Unicode, scale, and native ownership must be proven against the installed V25 API.
- Scenario: Implement/qualify one semantic tag path and one native schedule table path first, then sheet/layout/viewport ownership. Verify stable semantic owner IDs, deterministic refresh, user-object protection, Unicode, styles, scale/lock, Model/Paper Space, and save/reopen.
- Evidence required: Exact SHA; native object ownership/refresh checks; user-object protection; Unicode/HiDPI result; save/reopen result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/DOCUMENTATION-LAYER.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Why local: Safe multi-owner physical reconciliation depends on native Solid3d geometry/booleans and ownership behavior in V25.
- Scenario: Qualify dedicated junction-owned output without ambiguously reassigning one solid to multiple walls. Cover L/T/X, 2/3/4 owners, mixed thicknesses, incompatible vertical ranges, source changes, removal/rebuild, and Door/Opening host retention.
- Evidence required: Exact SHA; junction ownership/dependency checks; geometry matrix; invalidation/rebuild result; opening host-retention result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
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

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.

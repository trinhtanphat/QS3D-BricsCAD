# QS3D local-agent inbox

**Updated:** 2026-08-25 (UTC+7)

This file is the **single live queue for LOCAL_ONLY work**. Historical detail and the pre-compaction evidence ledger are preserved byte-for-byte in `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`.

## Mandatory handoff contract

- A remote/hybrid agent that discovers a new LOCAL_ONLY requirement must add or update the matching item in this file in the same source/docs batch that introduced or exposed the requirement.
- Do not create a second live queue. Historical local-agent files are supporting detail/evidence; this inbox is the current priority index.
- Every `OPEN`, `IN_PROGRESS`, or `BLOCKED` LOCAL_ONLY item has implicit remote disposition `DO_NOT_RETRY_REMOTE` unless current source materially changes the scenario or the agent actually gains the missing local capability.
- Local agents work `P0` before `P1` before `P2`, always from a clean checkout of the newest intended exact SHA.
- `LOCAL_PASS` requires real licensed/runtime evidence tied to the exact tested SHA. Source review, static preflight, mock tests, `-SkipRuntime`, or hosted CI cannot manufacture `LOCAL_PASS`.
- Never commit proprietary BricsCAD DLLs, private/customer DWGs, signing keys, credentials, or unsanitized runtime captures.
- When source changes alter a local scenario, update this inbox immediately instead of relying on an older handoff paragraph.

Valid priorities: `P0`, `P1`, `P2`.  
Valid statuses: `OPEN`, `IN_PROGRESS`, `PASS`, `BLOCKED`.

## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork

- Priority: P0
- Status: OPEN
- Area: StructuralWall live-BREP concrete-contact / formwork licensed qualification
- Remote disposition: SOURCE_READY / LOCAL_RUN_ONLY
- Exact runtime checkout: `a4f1a53683a9296532a0290fcb79bc49b9d4b892`
- Minimum source-ready ancestor: `c64eb8c1b83761e155da670904a72e64669464b7`
- Runner: `scripts/run-local-v25-wall-contact-3681.ps1`
- Local contract: fetch and checkout the exact runtime SHA, run the committed runner unchanged on licensed BricsCAD V25, and publish only sanitized `LOCAL_PASS`, `LOCAL_FAIL`, or `NO_RESULT` evidence. The local worker must not edit production source; hosted/static CI cannot manufacture `LOCAL_PASS`.
- Evidence: PENDING_LOCAL after merged source/harness fix #3846 / PR #3854.

## LOCAL-001 — exact V25 build/load baseline

- Priority: P0
- Status: PASS
- Area: BricsCAD V25 adapter / packaging baseline
- Why local: Final native load, command, save/reopen and desktop lifecycle proof requires licensed BricsCAD V25 on Windows.
- Scenario: Preserve the already-qualified exact V25 baseline; route any newly changed native scenario to a bounded local row instead of reinterpreting static evidence as runtime PASS.
- Evidence required: Exact SHA, V25/plugin identity, build/load/runtime marker and sanitized cleanup evidence.
- Evidence: Baseline PASS evidence is recorded at exact SHA `0ae7fb4369172198d25347b9b0d75bdbceead2bb`; detailed lifecycle evidence remains in the archived ledger.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-002 — Curtain whole-command atomicity and native panels

- Priority: P0
- Status: OPEN
- Area: Curtain Wall
- Why local: Broad H.1, modeless/native teardown and remaining interactive parity require real licensed V25 despite bounded P01-P12 and Family-editor evidence.
- Scenario: Run only the remaining broad Curtain/H.1 matrix on an exact current merged SHA; do not rerun already bounded cells unless source materially changes them.
- Evidence required: Exact SHA, whole-command/native ownership, rollback/Undo, save-reopen, modeless/document-lifetime and cleanup evidence.
- Evidence: Bounded P01-P12 and Family-editor rows have local evidence; overall broad LOCAL-002 remains `PENDING_LOCAL`.
- Related docs: `docs/CURTAIN-NATIVE-PANELS.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-003 — shared Level Z-chain in native geometry

- Priority: P0
- Status: IN_PROGRESS
- Area: Structural / Wall / Opening / Rebar vertical placement
- Why local: Full-family dual-unit native geometry, Undo/save-reopen, multi-DWG and private-DWG coverage require licensed V25.
- Scenario: Continue the remaining complete-family Millimeter/Meter Level lifecycle from exact current source; keep bounded representative probe PASS separate from full qualification.
- Evidence required: Exact SHA/ProductVersion, before/after Z measurements, generated ownership, Health/Release, Undo, save-reopen, multi-DWG and cleanup evidence.
- Evidence: Representative automated probes are PASS; complete-family/full lifecycle remains `PENDING_LOCAL`.
- Related docs: `docs/LEVEL-REFERENCES.md`; `docs/LOCAL-LEVEL-Z-QUALIFICATION-2026-08-11.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-004 — source reconcile native atomicity

- Priority: P0
- Status: IN_PROGRESS
- Area: Source Reconcile / Modify
- Why local: Native LINE/POLYLINE edits, generated cleanup, native Undo/Redo and broader category/dependent failure coverage require licensed V25.
- Scenario: Continue only the matrix beyond already-qualified P01-P05 cells on exact current source, including remaining topology/category/dependent/failure/multi-DWG cases.
- Evidence required: Exact SHA, source/semantic/native before-after state, generated ownership, rollback, Undo/Redo, save-reopen, multi-DWG and cleanup evidence.
- Evidence: Consolidated P01-P05 native cells are PASS; parent #80 broader matrix remains pending.
- Related docs: `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-005 — polygon Slab/Foundation native reinforcement

- Priority: P1
- Status: OPEN
- Area: Rebar 3D / Slab / Foundation
- Why local: Native RegionId/source-loop association, rebar materialization, bulges/OCS-WCS, ownership and Undo/save-reopen require licensed V25.
- Scenario: Qualify convex/concave/disconnected regions and holes through native Slab/Foundation extraction/materialization without concatenating islands or treating islands as holes.
- Evidence required: Exact SHA, region/hole/bulge matrix, RegionId/native-owner checks, cover/spacing/count, rollback/Undo, save-reopen and multi-DWG evidence.
- Evidence: PENDING_LOCAL
- Related docs: `docs/POLYGON-REGION-HOLES.md`; `docs/POLYGONAL-SLAB-MESH.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-006 — native documentation objects

- Priority: P1
- Status: OPEN
- Area: Documentation / Tags / Tables / Sheets
- Why local: Source implementation is complete; native MText/MLeader/Table/Layout/Viewport/title-block lifecycle and modeless behavior still require licensed BricsCAD.
- Scenario: Follow the committed LOCAL-006 qualification runbook on an exact intended SHA; do not reimplement source unless licensed evidence exposes a new bounded defect.
- Evidence required: Exact SHA, tag/table/schedule/sheet ownership and lifecycle, cancellation/freshness, Undo/Redo, save-cold-reopen, multi-DWG and cleanup evidence.
- Evidence: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Related docs: `docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md`; `docs/SEMANTIC-TAGS.md`; `docs/SEMANTIC-SCHEDULES.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Why local: Physical multi-owner junction materialization/replacement/rebuild and native Undo/save-reopen require V25.
- Scenario: Preserve P01 analysis and P02 Wall Snap PASS; continue the physical-output/integration and remaining L/T/X/Multi ownership matrix from exact current source.
- Evidence required: Exact SHA, GroupToken/OwnerToken/fingerprint ownership, geometry/rebuild, no-cross-DWG, opening retention, save-reopen and Undo/Redo evidence.
- Evidence: P01 analysis and P02 Wall Snap are LOCAL_PASS; remaining physical-output/integration scope is pending.
- Related docs: `docs/WALL-JUNCTION-OWNERSHIP.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-008 — Direct Draw transient preview and repeated mode

- Priority: P1
- Status: OPEN
- Area: Direct Draw UX
- Why local: Editor prompts, DrawJig, ESC, UCS, document switches, Auto Host/reference and modeless/Ribbon behavior require licensed interactive hosts.
- Scenario: Preserve P01/P03 PASS; complete quick/advanced per-prompt cancel/drift, Auto Host/reference and remaining Ribbon/UI matrix on exact current source.
- Evidence required: Exact SHA, prompt/cancel/drift matrix, ProjectId freshness, UCS/unit/document isolation, Auto Host/reference, Ribbon/repeated mode and cleanup evidence.
- Evidence: P01 and repeated-mode P03 are LOCAL_PASS; remaining interactive matrix is PENDING_LOCAL.
- Related docs: `docs/DIRECT-DRAW-WORKFLOW.md`; `docs/DIRECT-DRAW-CANCEL-PROJECT-LIFECYCLE.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-009 — clean-machine install/sign/update qualification

- Priority: P1
- Status: OPEN
- Area: Packaging / Release / Trust
- Why local: Signing certificate, trust chain, SECURELOAD and clean-machine install/upgrade/rollback/uninstall require authorized Windows hosts.
- Scenario: Sign only approved binaries, verify timestamp/trust, then test clean install, upgrade, rollback, uninstall, DemandLoad and SECURELOAD without weakening security policy.
- Evidence required: Exact SHA/tag, signer/timestamp verification, package hashes, install/upgrade/rollback/uninstall and DemandLoad/SECURELOAD evidence.
- Evidence: PENDING_LOCAL
- Related docs: `docs/MANUAL-BUILD-RELEASE.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-010 — large-model performance and UI matrix

- Priority: P2
- Status: OPEN
- Area: Performance / UI / HiDPI
- Why local: Representative timings, GPU/driver behavior, palette responsiveness and DPI/layout need real hardware.
- Scenario: Measure the major regeneration/query/export flows plus 100/125/150/200% DPI and narrow/normal/wide palettes on representative projects.
- Evidence required: Exact SHA, hardware/OS/V25 build, model sizes, timings, DPI/layout results and sanitized bottleneck notes.
- Evidence: PENDING_LOCAL
- Related docs: `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-011 — staged native rollback and post-commit UI isolation

- Priority: P1
- Status: OPEN
- Area: Fault injection / rollback / modeless UI / generated replacement atomicity
- Why local: Native transaction abort/compensation, DocumentLock, ObjectId/XData ownership and post-commit WPF failures require licensed V25.
- Scenario: Execute the staged rollback matrix for stale/missing/malformed generated owner sets, modeless cache/document changes, unavailable-project palettes and another-DWG isolation.
- Evidence required: Exact SHA, failure stage, semantic/native snapshots, expected/live owner sets, rollback/Undo, foreign-object protection, save-reopen and cleanup evidence.
- Evidence: PENDING_LOCAL
- Related docs: `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-012 — Project Browser native workspace and CAD selection bridge

- Priority: P1
- Status: IN_PROGRESS
- Area: Project Browser / Workspace / modeless selection
- Why local: Live implied selection, ObjectId/Handle re-resolution, modeless palette lifecycle, document switching, focus/zoom and HiDPI require licensed V25.
- Scenario: Continue the broader Browser/modeless/DPI/document/cache/property matrix while preserving already-qualified eligible single-selection CAD→Excel→CAD→Workspace bridge evidence.
- Evidence required: Exact SHA, CAD↔Browser selection, Workspace scope/reset freshness, unavailable-project recovery, active-DWG isolation, save-reopen and DPI evidence.
- Evidence: Eligible single-selection bridge is LOCAL_PASS; wider modeless/browser matrix remains PENDING_LOCAL.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/REMOTE-AGENT-SCOPE.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-013 — clean-room BRC public capability and eligible CAD quantity round-trip

- Priority: P0
- Status: IN_PROGRESS
- Area: BRC public capability / Recognition / eligible CAD B4D / ED2 / Excel Locate
- Why local: Licensed hosts, authorized BRC-containing reference content and native public proxy/entity behavior are required; proprietary/internal BRC APIs remain out of bounds.
- Scenario: Continue only authorized clean-room public-API capability and eligible CAD quantity/selection rows; opaque proxies remain review-only and fail closed.
- Evidence required: Exact SHA, sanitized reference hashes, public capability/counts, B4D/ED2 workbook summary, Locate/refusal matrix and proof originals were unchanged.
- Evidence: Eligible CAD round-trip and refusal matrix have bounded PASS evidence; current BRC proxy parity/visual follow-up remains pending.
- Related docs: `docs/PRODUCT-BOUNDARY.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-014 — Plan-to-3D preview-to-commit and batch compensation

- Priority: P1
- Status: OPEN
- Area: QS3DCONVERT2D / QS3DPLAN2WALLS / QS3DCONVERT2DADV
- Why local: Model Space/UCS state, live source changes during prompts, native Solid3d ownership, editor selection and compensation require licensed V25.
- Scenario: Preserve P01/P02 PASS; complete advanced prompt drift/cancel, rollback injection, Undo/Redo, save-reopen and remaining quick/advanced lifecycle.
- Evidence required: Exact SHA, prompt count/defaults, source geometry/freshness, semantic/native ownership, compensation rollback, Undo/Redo and save-reopen evidence.
- Evidence: P01/P02 bounded quick-path cells are LOCAL_PASS; remaining matrix is PENDING_LOCAL.
- Related docs: `docs/PLAN-TO-3D-WORKFLOW.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-015 — Construction Reference Search browser/modeless runtime

- Priority: P2
- Status: OPEN
- Area: QS3DREFSEARCH / modeless browser launcher
- Why local: Requires licensed V25, Windows default-browser association and modeless WPF/document lifecycle.
- Scenario: Verify all fixed HTTPS result categories, URL encoding/SafeSearch, input bounds, document affinity/reactivation/close and zero semantic/CAD mutation.
- Evidence required: Exact SHA, Windows/V25/browser identity, category/encoding/refusal matrix, document lifecycle and no-mutation evidence.
- Evidence: PENDING_LOCAL
- Related docs: `docs/CONSTRUCTION-REFERENCE-SEARCH.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-016 — BricsCAD V26 native authoring and dependent-output qualification

- Priority: P0
- Status: IN_PROGRESS
- Area: V26 .NET 8 native authoring / semantic / generated geometry lifecycle
- Why local: Requires licensed V26 x64, interactive Windows, exact net8.0-windows plugin and fresh-process native save/reopen.
- Scenario: Preserve bounded V26 Beam/repeated/LINE cells and continue the broader V26 native/private-DWG matrix on exact current source.
- Evidence required: Exact SHA, V26 host/CLR/x64/PDB identity, native lifecycle booleans, save/cold-reopen and zero-process cleanup.
- Evidence: Bounded P01 and repeated/LINE workflows are LOCAL_PASS; broader V26 matrix remains PENDING_LOCAL.
- Related docs: `docs/LOCAL-V26-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-017 — BricsCAD V26 native Slab POLYLINE qualification

- Priority: P0
- Status: PASS
- Area: V26 .NET 8 native Slab source-edit lifecycle
- Why local: Bounded proof required licensed V26 native STRETCH, reconcile/rebuild and fresh-process save/cold-reopen.
- Scenario: Preserve the completed bounded P02 cell; parent #80/#1462 broader scopes remain separate.
- Evidence required: Exact clean SHA, V26/PDB/plugin identity, native STRETCH/reconcile/rebuild, save/cold-reopen and cleanup evidence.
- Evidence: LOCAL_PASS at exact SHA `54b7fce6127208085817f20dd0781b580a18e4bd`.
- Related docs: `docs/LOCAL-V26-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-018 — exact V26 LINE and repeated Direct Draw lifecycle

- Priority: P0
- Status: PASS
- Area: V26 native editor/document/repeated lifecycle
- Why local: Bounded proof required licensed V26 editor commands, physical ESC, document switching and fresh-process reopen.
- Scenario: Preserve the completed #3578/#3612 bounded lifecycle; broader parent scopes remain separate.
- Evidence required: Exact source/PDB/plugin identity, DrawJig/ESC/UCS/document-switch/Undo/Redo and LINE reconcile/save/reopen evidence.
- Evidence: LOCAL_PASS repeated-mode source SHA `9a77d329e90809a2006d8e4dc1bafc995c0a8ca2`, with predecessor LINE lifecycle evidence retained in the archive.
- Related docs: `docs/LOCAL-V26-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate

- Priority: P0
- Status: PASS
- Area: QS3DREVIEWEXPORT / QS3DREVIEWLOCATE; BricsCAD V25 + V26 host bridge
- Why local: Final acceptance required licensed V25/V26 NETLOAD, real XLSX export, native PICKFIRST selection and stale-trace refusal.
- Scenario: Preserve the completed bounded V25/V26 six-sheet export/Locate round-trip and its negative refusal matrix.
- Evidence required: Exact clean pushed SHA, host/plugin identity, six-sheet order, locate/refusal counts, unchanged source DWG and cleanup evidence.
- Evidence: LOCAL_PASS on exact clean pushed source SHA `9cfff87262d7a7117c5ef1f03b486271a0723fa3` for both licensed V25.2.10 and V26.2.07.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/LOCAL-V26-QUALIFICATION.md`; `docs/archive/LOCAL-AGENT-INBOX-2026-08-25-pre-a4f1.md`
- Updated: 2026-08-25

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires separate explicit release authorization. This inbox records local engineering qualification truth; detailed historical evidence remains in the archived pre-compaction ledger.

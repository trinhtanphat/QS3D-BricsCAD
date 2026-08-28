# QS3D local-agent inbox

**Updated:** 2026-08-27 (UTC+7)

This file is the **single live queue for LOCAL_ONLY work**. Detailed runbooks remain in the linked local qualification/handoff documents, but a local agent should start here before opening those longer files.

## Mandatory handoff contract

- A remote/hybrid agent that discovers a new LOCAL_ONLY requirement must add or update the matching item in this file **in the same source/docs batch that introduced or exposed the requirement**.
- Do not create a second live queue. Historical `docs/LOCAL-AGENT-*.md` files are supporting detail/evidence; this inbox is the current priority index.
- Every `OPEN`, `IN_PROGRESS`, or `BLOCKED` LOCAL_ONLY item in this inbox has implicit remote disposition **`DO_NOT_RETRY_REMOTE`**. Subsequent remote/non-local agents must skip its execution/re-audit unless current source materially changes the scenario, the owner explicitly asks for a fresh remote source investigation, or the agent actually gains the missing local capability.
- Before adding an item, remote agents must search this inbox and update the existing matching item instead of duplicating the same unavailable work. Lack of local capability is a handoff condition, not a reason to retry from another equivalent remote agent.
- Local agents work `P0` before `P1` before `P2`, always from a clean checkout of the newest intended SHA.
- `LOCAL_PASS` requires real evidence tied to the exact tested SHA. Source review, static preflight, mock tests, `-SkipRuntime`, or a remote build cannot manufacture `LOCAL_PASS`.
- Never commit proprietary BricsCAD DLLs, private/customer DWGs, signing keys, credentials, or unsanitized runtime captures.
- When an item passes, set `Status: PASS`, replace `Evidence: PENDING_LOCAL` with a sanitized evidence summary, and record the exact SHA under `Evidence`.
- When source changes alter a local scenario, update this inbox immediately instead of relying on an older handoff paragraph.

Valid priorities: `P0`, `P1`, `P2`.  
Valid statuses: `OPEN`, `IN_PROGRESS`, `PASS`, `BLOCKED`.

## P0 — #3681 StructuralWall live-BREP concrete-contact/formwork

- Priority: P0
- Status: PASS
- Area: StructuralWall live-BREP concrete-contact / formwork licensed qualification
- Remote disposition: COMPLETED / NO_RERUN
- Exact runtime checkout: `a4f1a53683a9296532a0290fcb79bc49b9d4b892`
- Minimum source-ready ancestor: `c64eb8c1b83761e155da670904a72e64669464b7`
- Runner: `scripts/run-local-v25-wall-contact-3681.ps1`
- Local contract: licensed BricsCAD V25 qualification is complete for this bounded scenario. Keep the committed runner only as a regression reference; do not rerun unless a material source change explicitly reopens qualification. Hosted/static CI cannot manufacture or replace `LOCAL_PASS`.
- Evidence: `LOCAL_PASS` on exact runtime source `a4f1a53683a9296532a0290fcb79bc49b9d4b892`; sanitized evidence PR #3849 merged as `7fec6f36a7c1181d7113f0e7220ea3dafca66e29`. #3681 is CLOSED/completed.

## LOCAL-001 — exact V25 build/load baseline

- Priority: P0
- Status: IN_PROGRESS
- Area: BricsCAD V25 adapter / packaging baseline
- 2026-08-21 exact issue-72 continuation: clean candidate `0ae7fb4369172198d25347b9b0d75bdbceead2bb` on BricsCAD V25.2.10 passed the official qualification runner with manual-CI/generic preflight, all `962/962` aggregate gates, Core Release `0/0`, Core smoke `ALL PASS`, V25 `Release|x64` `0/0`, offline WPF and licensed NETLOAD/Ribbon/Palette. Matching adapter/Core ProductVersion is `0.1.0-preview.10081+0ae7fb4369172198d25347b9b0d75bdbceead2bb`; SHA-256 values are `B725F335AA71E90E9584EA1A6940A6889ACA2E2FDB22D88C2CB3713047268D01` / `2A5DCE45CC74EB9248A7079E02835DA81DEFD5A492AAF318CF21FB001CB44A2A`. The same exact candidate passed schema-3 Project Lifecycle across four disposable documents: SaveComplete/cold identity/canonical bind/detached and multi-DWG isolation, absent/corrupt sidecar fail-closed, nine REGEN/REFRESH/FINISH phases, legacy/native unit boundaries and explicit unbound Meter override resolution. Package/signing were not requested; the full interactive/private-DWG matrix remains `NOT_RUN` and customer-release qualification remains false.
- Current evidence reading rule: later exact-SHA lifecycle paragraphs in this item supersede only the baseline `NOT RUN` statements they explicitly name. They do not promote LOCAL-001 to `PASS`; all remaining scenarios stay `PENDING_LOCAL`.
- Why local: Requires licensed BricsCAD V25 x64, installed managed references, Windows desktop, NETLOAD/DemandLoad, and native command execution.
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory and complete the remaining canonical existing-project, modeless, Interchange, Save, selection, multi-DWG and lifecycle matrix described by `docs/LOCAL-V25-QUALIFICATION.md` and `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`.
- Evidence required: Exact QS3D SHA, Windows/V25/plugin identity, canonical ProjectId continuity for true writes, fail-closed no-project/replaced-sidecar outcomes, modeless live-state invariants, Interchange freshness/rollback, save/reopen/multi-DWG results, and sanitized cleanup evidence.
- Evidence: `PENDING_LOCAL` for the still-open current-candidate matrix. Historical bounded exact-SHA PASS evidence remains recorded in the linked runbooks/issues and is not promoted to overall LOCAL-001 completion.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`; `scripts/test-bricscad-v25-project-lifecycle.ps1`; `scripts/test-bricscad-v25-sidecar-revision.ps1`.
- Updated: 2026-08-25

## LOCAL-002 — Curtain whole-command atomicity and native panels

- Priority: P0
- Status: OPEN
- Area: Curtain Wall
- Source-side status: REMOTE_DONE for whole-command source atomicity, native panels, exact-set ownership/replacement and modeless Family-editor work. Bounded licensed cells P01-P12 and Family-editor evidence exist on their exact tested SHAs; broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Why local: Native V25 Solid3d/nested-transaction behavior, modeless Curtain Hub, native ownership, shutdown behavior and complete broad H.1 proof require licensed interactive BricsCAD.
- Scenario: Follow `docs/CURTAIN-NATIVE-PANELS.md` and the active H.1/local runbook on one exact eligible SHA; do not relabel historical bounded cells as broad current-candidate qualification.
- Evidence required: Exact tested SHA, V25/plugin identity, native ownership/rollback/Undo/save-reopen/multi-DWG/modeless results and sanitized cleanup.
- Evidence: Bounded P01-P12/Family-editor evidence exists; broad H.1 and overall LOCAL-002 remain `PENDING_LOCAL`.
- Related source/docs: `docs/CURTAIN-NATIVE-PANELS.md`; `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`.
- Updated: 2026-08-24

## LOCAL-003 — shared Level Z-chain in native geometry

- Priority: P0
- Status: IN_PROGRESS
- Area: Structural / Wall / Opening / Rebar vertical placement
- Source-side status: `SOURCE_INTEGRATED / AUTOMATED_RUNTIME_PROBE_PASS / FULL_LOCAL_MATRIX_PENDING`.
- Why local: Correctness depends on native V25 geometry, cutters, generated rebar alignment, Level edits, Undo/save/reopen and multi-DWG behavior.
- Scenario: Run the guarded current Level-Z probe and then the wider shared `ElementVerticalPlacementService` matrix across wall/structural/opening/Curtain/reinforcement families in mm and m drawings, including fail-closed Level configurations, source reconcile, Health/Release, Undo, save/reopen and multi-DWG isolation.
- Evidence required: Exact SHA/plugin identity; sanitized aggregate marker; before/after Z measurements; host-opening-Curtain-rebar alignment; Health/Release behavior; drawing hash/no-sidecar proof for the probe; Undo/save/reopen/multi-DWG results.
- Evidence: Representative automated probe and several bounded lifecycle/unit rows are `LOCAL_PASS` on their exact historical SHAs; complete family dual-unit lifecycle, broader multi-DWG and authorized private-DWG evidence remain `PENDING_LOCAL`.
- Related docs: `docs/LEVEL-REFERENCES.md`; `docs/LOCAL-LEVEL-Z-QUALIFICATION-2026-08-11.md`; `scripts/test-bricscad-v25-level-z.ps1`; `scripts/test-bricscad-v25-level-z-lifecycle.ps1`.
- Updated: 2026-08-25

## LOCAL-004 — source reconcile native atomicity

- Priority: P0
- Status: IN_PROGRESS
- Area: Source Reconcile / Modify
- Why local: Requires real LINE/POLYLINE edits, generated-object cleanup, native transaction/Undo behavior, document switching and fresh-process persistence.
- Scenario: Follow `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md` and the current issue #80 matrix; preserve the exact-source and disposable-drawing boundary.
- Evidence required: Exact SHA/plugin identity; reconcile/refusal/rollback results; semantic/native Undo/Redo coherence; save/reopen; multi-DWG; process/script/private-state cleanup.
- Evidence: Multiple bounded native cells P01-P06 have `LOCAL_PASS` on their exact tested SHAs; the broader topology/category/dependent/failure matrix remains open under #80, so LOCAL-004 remains `IN_PROGRESS`.
- Related docs: `docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md`; `scripts/test-bricscad-v25-source-reconcile.ps1`; `scripts/preflight-source-reconcile-runtime-probe.py`.
- Updated: 2026-08-25

## LOCAL-005 — polygon Slab/Foundation native reinforcement

- Priority: P1
- Status: OPEN
- Area: Rebar 3D / Slab / Foundation
- Why local: Core topology/planning is REMOTE_DONE; native source-loop/RegionId association, rebar materialization/ownership, OCS/WCS behavior, limits, Undo/save-reopen and multi-DWG require V25.
- Scenario: Qualify convex/concave/disconnected regions and holes, straight/bulged loops, cover/limit rejection, per-region ownership, rollback, Undo/Redo and multi-DWG without concatenating islands or treating islands as holes.
- Evidence required: Exact SHA; topology/RegionId/native-owner matrix; cover/spacing/count measurements; limit refusal; rollback/Undo; save-reopen/multi-DWG.
- Evidence: PENDING_LOCAL
- Related docs: `docs/POLYGON-REGION-HOLES.md`; `docs/POLYGONAL-SLAB-MESH.md`.
- Updated: 2026-08-11

## LOCAL-006 — native documentation objects

- Priority: P1
- Status: OPEN
- Area: Documentation / Tags / Tables / Sheets
- Source-side status: `SOURCE_COMPLETE / REMOTE_DONE`; bounded Semantic Tag Undo/Redo is `LOCAL_PASS` on exact source `a572ab0a350f54f8e994ac1e91f825907646af9c`, while broader MLeader/Table/custom-schedule/Sheet/Layout/Viewport/Unicode/HiDPI/save-reopen/multi-DWG/V26 coverage remains pending.
- Why local: Final integration depends on licensed BricsCAD native documentation objects, rendering, modeless behavior and persistence.
- Scenario: Follow `docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md` for Semantic Tag/MLeader, Table/custom schedules and Sheet/Layout/PaperSpace/Viewport/title-block lifecycles including cancellation/freshness, atomic rollback, Undo/Redo, save/cold-reopen and multi-DWG.
- Evidence required: Exact SHA/plugin identity; ownership/refresh/remove/health; modeless detached/live-state boundaries; schedule/table/sheet/layout/viewport behavior; Undo/Redo/save-reopen/multi-DWG and sanitized cleanup.
- Evidence: broader LOCAL-006 remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Related docs: `docs/LOCAL-006-NATIVE-DOCUMENTATION-QUALIFICATION.md`; `docs/SEMANTIC-TAGS.md`; `docs/SEMANTIC-SCHEDULES.md`.
- Updated: 2026-08-24

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Why local: Native multi-owner Solid3d materialization, booleans, replacement, save/reopen and Undo/Redo require licensed V25.
- Scenario: Preserve the completed P01 analysis/P02 Wall Snap evidence, then qualify the remaining physical output/group ownership matrix from `docs/WALL-JUNCTION-OWNERSHIP.md`.
- Evidence required: Exact SHA; GroupToken/OwnerToken/fingerprint ownership; geometry matrix; stale/rebuild/foreign refusal; opening host retention; save/reopen and Undo/Redo.
- Evidence: P01/P02 bounded `LOCAL_PASS`; physical output/integration remains `PENDING_LOCAL`.
- Related docs: `docs/WALL-JUNCTION-OWNERSHIP.md`.
- Updated: 2026-08-23

## LOCAL-008 — Direct Draw transient preview and repeated mode

- Priority: P1
- Status: OPEN
- Area: Direct Draw UX
- Source-side status: REMOTE_DONE for current quick/advanced/project-preview and prompt-freshness contracts. Bounded P01/P03/P04 and executable P05 submatrix have local evidence; remaining Auto Host/reference/Ribbon/UI/drift cells remain pending.
- Why local: DrawJig/editor/UCS lifecycle, physical ESC, document switching and Auto Host ambiguity require interactive V25/V26.
- Scenario: Follow `docs/DIRECT-DRAW-WORKFLOW.md` and current local matrix for cancellation, preview/project drift, quick/ADV defaults, Window Auto Host/reference, Ribbon/repeated mode, planar UCS and document switching.
- Evidence required: Exact SHA/plugin identity; prompt/cancel/drift matrix; same-ProjectId success; no-residue proof; Auto Host/reference/Ribbon/repeated/document-switch results.
- Evidence: bounded cells are `LOCAL_PASS`; remaining matrix is `PENDING_LOCAL`.
- Related docs: `docs/DIRECT-DRAW-WORKFLOW.md`; `docs/DIRECT-DRAW-PREVIEW-PROJECT-FRESHNESS.md`.
- Updated: 2026-08-25

## LOCAL-009 — clean-machine install/sign/update qualification

- Priority: P1
- Status: OPEN
- Area: Packaging / Release / Trust
- Why local: Production signing certificate, Windows trust chain, timestamp, clean-machine lifecycle and BricsCAD SECURELOAD behavior cannot be proven remotely.
- Scenario: On an authorized clean/customer-like machine, verify approved signed binaries/manifest/package, install/upgrade/rollback/uninstall, DemandLoad and SECURELOAD without weakening trust policy.
- Evidence required: Exact SHA/tag; signer/timestamp; package hashes; install/upgrade/rollback/uninstall; DemandLoad/SECURELOAD.
- Evidence: PENDING_LOCAL
- Related docs: `docs/MANUAL-BUILD-RELEASE.md`.
- Updated: 2026-08-21

## LOCAL-010 — large-model performance and UI matrix

- Priority: P2
- Status: OPEN
- Area: Performance / UI / HiDPI
- Source/runner status: `SOURCE_READY / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Runner: `scripts/run-local-v25-local-010.ps1`
- Runbook: `docs/LOCAL-010-PERFORMANCE-UI-QUALIFICATION.md`
- Why local: Representative timings, native palette responsiveness, GPU/driver effects and DPI/layout behavior need real hardware/V25.
- Scenario: Run the canonical one-command runner on a clean exact intended SHA.
- Evidence required: Exact SHA; hardware/OS/V25 identity; project sizes; timings; DPI/layout and sanitized bottleneck notes.
- Evidence: PENDING_LOCAL
- Updated: 2026-08-11

## LOCAL-011 — staged native rollback and post-commit UI isolation

- Priority: P1
- Status: PASS
- Area: Fault injection / rollback / modeless UI / generated replacement atomicity
- Remote disposition: REMOTE_DONE / LOCAL_PASS / DO_NOT_RETRY_REMOTE
- Exact tested SHA: `fbf1e7923cbde9037637e5b6b1339b31f491c87a`
- Evidence: `LOCAL_PASS` for the bounded 21-row canonical matrix. Do not rerun unless a material source change explicitly reopens it.
- Related docs: `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`.
- Updated: 2026-08-25

## LOCAL-012 — Project Browser native workspace and CAD selection bridge

- Priority: P1
- Status: IN_PROGRESS
- Area: Project Browser / Workspace / modeless selection
- Source-side status: REMOTE_DONE for several Workspace/Properties/Foundation paths; current broader Browser/modeless/DPI/document/cache/property integration still contains source/runtime handoffs described by issues #3936/#4032 and remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` where source-ready.
- Why local: Real BricsCAD implied selection/ObjectId resolution, modeless WPF palettes, document/cache switching and HiDPI rendering require licensed V25.
- Scenario: Follow the current LOCAL-012 issue/runbook matrix for CAD↔semantic selection, Instance/Family scope, live-Family Reset, stale/unavailable-project refusal, Foundation subtype Add/Solid3D and dedicated Properties palette behavior.
- Evidence required: Exact SHA/plugin identity; selection/scope/reset/stale/cache/document/DPI results; no cross-DWG mutation.
- Evidence: partial bounded local evidence exists; overall LOCAL-012 remains `IN_PROGRESS`.
- Updated: 2026-08-26

## LOCAL-013 — clean-room BRC public capability and eligible CAD quantity round-trip

- Priority: P0
- Status: IN_PROGRESS
- Area: BRC public capability / Recognition / eligible CAD B4D / ED2 / Excel Locate
- Why local: Requires licensed BricsCAD V25, authorized BRC-containing content, public proxy/entity behavior and a real workbook/CAD selection round-trip.
- Scenario: Use disposable copies only; preserve the clean-room/public-API boundary, run the current BRC probe and eligible CAD B4D→ED2→Locate matrix, and keep opaque proxies review-only when no finite positive public metric exists.
- Evidence required: Exact SHA; sanitized source-copy identity/hashes; public capability counts; B4D/ED2/Locate positive/refusal results; explicit opaque-proxy fail-closed evidence; workbook inspection when an authorized qualifying input is available.
- Evidence: eligible-CAD round-trip and historical BRC public-proxy evidence exist on exact historical SHAs; exact-current BRC workbook visual follow-up remains pending, so LOCAL-013 stays `IN_PROGRESS`.
- Updated: 2026-08-23

## LOCAL-014 — Plan-to-3D preview-to-commit and batch compensation

- Priority: P1
- Status: OPEN
- Area: `QS3DCONVERT2D` / `QS3DPLAN2WALLS` / `QS3DCONVERT2DADV`
- Source-side status: REMOTE_DONE for current quick/advanced freshness and ownership-scoped compensation contracts.
- Why local: Model Space/UCS/unit/source drift during prompts, native Solid3d ownership, editor selection, rollback, Undo/save-reopen and multi-DWG need licensed V25.
- Scenario: Preserve bounded P01-P04 evidence and qualify remaining document/space/source-delete-retype/project-replacement drift, compensation/rollback, Undo/Redo, save/cold-reopen and multi-DWG isolation.
- Evidence required: Exact SHA/V25 identity; prompt/default/drift matrix; semantic/native ownership; compensation/rollback; Undo/Redo/save-reopen/multi-DWG.
- Evidence: P01-P04 bounded `LOCAL_PASS`; overall LOCAL-014 remains `PENDING_LOCAL`.
- Updated: 2026-08-25

## LOCAL-015 — Construction Reference Search browser/modeless runtime

- Priority: P2
- Status: OPEN
- Area: `QS3DREFSEARCH` / modeless browser launcher
- Source-side status: REMOTE_DONE; exact Windows/BricsCAD/browser behavior remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Why local: Requires licensed BricsCAD V25, Windows default browser association, modeless WPF/document lifecycle and active-DWG switching.
- Scenario: Follow the current reference-search runbook for HTTPS category mapping, encoding/SafeSearch, Enter/quick-query behavior, empty/oversize refusal, active-DWG/document-close lifecycle and no semantic/CAD mutation.
- Evidence required: Exact SHA; Windows/V25/browser identity; sanitized category/encoding/SafeSearch/lifecycle results; no project/CAD/audit mutation.
- Evidence: PENDING_LOCAL
- Updated: 2026-08-11

## LOCAL-016 — BricsCAD V26 native authoring and dependent-output qualification

- Priority: P0
- Status: IN_PROGRESS
- Area: issue `#1462`; V26 `.NET 8` native authoring/semantic/generated-geometry lifecycle
- Why local: Requires licensed interactive BricsCAD V26 x64 and matching `net8.0-windows` plugin/native lifecycle.
- Scenario: Preserve completed bounded V26 P01/package/repeated/native-line evidence and qualify the remaining parent #1462 private-DWG, trust/signing/package/update/UI and broader native matrix on one exact eligible SHA.
- Evidence required: Exact SHA/V26/plugin identity; native lifecycle and remaining parent-matrix results; sanitized cleanup.
- Evidence: bounded rows have `LOCAL_PASS`; broader V26 matrix remains `PENDING_LOCAL`.
- Updated: 2026-08-25

## LOCAL-017 — BricsCAD V26 native Slab POLYLINE qualification

- Priority: P0
- Status: PASS
- Area: issues `#80`, `#1462`, bounded carrier `#3576`
- Evidence: P02 `LOCAL_PASS` on exact clean SHA `54b7fce6127208085817f20dd0781b580a18e4bd`; this closes only the bounded Slab POLYLINE cell.
- Updated: 2026-08-22

## LOCAL-018 — exact V26 LINE and repeated Direct Draw lifecycle

- Priority: P0
- Status: PASS
- Area: issues `#80`, `#1462`, completed `#3578`, carrier `#3612`
- Evidence: bounded repeated/native LINE lifecycle `LOCAL_PASS`; parent #80/#1462 broader matrices remain separately open.
- Updated: 2026-08-23

## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate

- Priority: P0
- Status: PASS
- Area: issue `#3536`; `QS3DREVIEWEXPORT` / `QS3DREVIEWLOCATE`; V25 + V26
- Evidence: `LOCAL_PASS` on exact clean pushed SHA `9cfff87262d7a7117c5ef1f03b486271a0723fa3`; V25/V26 six-sheet export, locate and four refusal cases passed with sanitized cleanup.
- Updated: 2026-08-24

## LOCAL-021 — Móng Bè workflow and Quantity Insight viewport highlight

- Priority: P0
- Status: IN_PROGRESS
- Area: issue `#4041`; BricsCAD V25 Móng Bè Add/Edit/native 3D/Quantity Insight viewport highlight
- Remote disposition: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Source/artifact status: source successor after the consumed startup NO_RESULT is required/eligible only under the current #72 host-allocation contract.
- Why local: Requires licensed interactive V25, Workspace/Properties rendering, native Solid3d/highlight and save/cold-reopen.
- Scenario: Follow the active #4041 exact-artifact runtime matrix only after fresh local host authorization.
- Evidence: no overall LOCAL-021 `LOCAL_PASS`; consumed historical artifacts remain `NO_RESULT / DO_NOT_RETRY`.
- Updated: 2026-08-28

## LOCAL-022 — Móng đơn placement/edit/save-reopen on V25/V26

- Priority: P0
- Status: BLOCKED
- Area: issue `#4034`; BricsCAD V25/V26 Móng đơn Add/placement/edit/regenerate/save-reopen
- Remote disposition: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Why local: Final acceptance requires licensed V25/V26 placement/native geometry/edit/persistence/cold-reopen.
- Runtime gate: consumed candidates are non-retryable; wait for an explicitly authorized exact successor source/artifact and fresh #72 allocation.
- Evidence: no LOCAL-022 `LOCAL_PASS`.
- Updated: 2026-08-27

## LOCAL-020 — Grid pair-owned intersection marker native lifecycle

- Priority: P1
- Status: OPEN
- Area: Grid / pair-owned native intersection markers (#3771)
- Source/runner status: `SOURCE_READY / MERGED_MAIN`; minimum source/guard ancestor `707ba4f2991e6ab47a81d9de80a32c19e55fca79`.
- Remote disposition: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Runbook: `docs/LOCAL-GRID-INTERSECTION-MARKER-QUALIFICATION.md`
- Why local: Requires licensed V25/V26 native Circle/XData/ownership, Undo/Redo, save/cold-reopen and multi-DWG/document/owner-space behavior.
- Evidence: `PENDING_LOCAL`
- Updated: 2026-08-27

## LOCAL-023 — Beam formwork behavior matrix on preview.10228

- Priority: P1
- Status: PASS
- Area: issue `#4093`; BricsCAD V25 Beam formwork M1–M8 behavior matrix
- Remote disposition: TERMINAL / DO_NOT_RETRY_REMOTE
- Exact runtime artifact: `v0.1.0-preview.10228`; source `7dacdce17a6403d19681732ca7bad22cdb6f1499`.
- Evidence: `100% / LOCAL_PASS / BEAM_BEHAVIOR_MATRIX`; exact detailed values remain in `docs/evidence/2026-08-27-issue4093-preview10228-beam-matrix.json`.
- Updated: 2026-08-27

## P1 — #3480 Quantity Review exact native BREP face highlight

- Priority: P1
- Status: OPEN
- Area: Quantity Review / formwork exact native BREP subentity highlight
- Source-side status: `REMOTE_DONE / MERGED_MAIN`.
- Remote disposition: PENDING_LOCAL_AGENT / DO_NOT_RETRY_REMOTE
- Runbook: `docs/FEATURE-RUNBOOKS/issue-3480-quantity-exact-face.md`
- Why local: Requires licensed BricsCAD V25 native BREP subentity highlighting and persistence checks.
- Evidence: `PENDING_LOCAL_AGENT`.
- Updated: 2026-08-26

## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification

- Priority: P0
- Status: OPEN
- Area: Production ChatGPT MCP onboarding + full CAD agent, issue #4352 / PR #4425
- Source/handoff status: source-hardening on canonical PR #4425; runtime qualification remains PENDING_LOCAL.
- Remote disposition: PENDING_LOCAL / DO_NOT_RETRY_REMOTE
- Why local: Final proof must pin one exact candidate SHA and run on licensed BricsCAD V25/V26 with real Windows UI, Cloudflare browser login/named tunnel/public HTTPS endpoint, ChatGPT MCP tool discovery, native CAD mutations/recovery, save/reopen/plot and process cleanup. Static or hosted source validation cannot establish this result.
- Scenario: Run `scripts/test-mcp-loopback-readonly.py`, then the complete click-first Cloudflare + ChatGPT + full CAD/UI matrix in `docs/MCP-FULL-CAD-AGENT.md` against the same exact candidate SHA. Cover V25/V26 load, auth/session/protocol handling, ChatGPT discovery, read/inspect, direct geometry CRUD/layers/transforms, bounded CAD command workflows, BricsCAD-process-confined mouse/keyboard, emergency stop/cancel/resume, audit, save/close/reopen, plot/export, tunnel restart/autostart and shutdown cleanup.
- Evidence required: Exact candidate SHA and matching V25/V26 host/plugin identity; sanitized loopback/session/tunnel/ChatGPT results; native tool/mutation/recovery/persistence results; proof no bearer token, Cloudflare credential, private/customer DWG/path, proprietary binary or unsanitized screenshot is published.
- Evidence: `PENDING_LOCAL`
- Related: issue #4352; PR #4425; `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md`; `docs/MCP-FULL-CAD-AGENT.md`; `scripts/test-mcp-loopback-readonly.py`.
- Updated: 2026-08-29

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.

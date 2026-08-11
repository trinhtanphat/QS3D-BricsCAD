# QS3D local-agent inbox

**Updated:** 2026-08-11 (UTC+7)

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

## LOCAL-001 — exact V25 build/load baseline

- Priority: P0
- Status: IN_PROGRESS
- Area: BricsCAD V25 adapter / packaging baseline
- Why local: Requires licensed BricsCAD V25 x64, installed managed references, Windows desktop, NETLOAD/DemandLoad, and native command execution.
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Cold-start/reopen a drawing with an existing `.qsdb`: true writes must bind the canonical same-ProjectId project, while one regeneration-based CSV/XLSX export and one modeless Door/Room refresh/export must use detached regenerated state and leave live project dirty/change-version/timestamp/audit state unchanged. Explicitly exercise `QS3DREGEN` and `QS3DREFRESH` after cache forget/reload: `QS3DREGEN` must bind the canonical existing project before regeneration; `QS3DREFRESH` may regenerate only after the same canonical binding. Repeat those commands with the sidecar/project absent: `QS3DREGEN` must refuse without creating/caching a replacement project, while `QS3DREFRESH` must remain a non-creating UI refresh with no semantic mutation. Exercise `QS3DFINISH` with a selected existing Room after cache forget/reload and verify generated finish semantics stay on the canonical project; repeat with the sidecar/project absent and verify the command refuses without creating/caching a replacement project or finish Family/Element state. Exercise automatic legacy unit binding from a unit-dependent command such as `QS3DBQ`: on a valid legacy project with semantic elements it must bind/update/save the canonical project; on a drawing with supported INSUNITS but no QS3D project it must not create/cache a project merely while resolving units. Separately verify explicit `QS3DUNITS` still intentionally creates/saves project unit state when the user confirms a project override. Repeat ownership-dependent writes with the sidecar absent and verify refusal without leaving a replacement project. For `QS3DINTERCHANGEIMPORT`, review a policy plan, then forget/reload the project cache or replace/remove the sidecar before confirmation; freshness confirmation must refuse the stale plan without creating/caching a replacement project and without applying any import mutation. Repeat the same post-preview cache/reload/sidecar-replacement test with standalone `QS3DINTERCHANGEAPPEND`: its initial target bootstrap before preview is allowed, but after the Yes/No review it must refuse a stale/replaced target through the non-creating freshness guard and must not append any semantic state.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache ProjectId continuity for true writes, `QS3DREGEN`/`QS3DREFRESH` existing-project and absent-sidecar lifecycle results with proof of no replacement project, `QS3DFINISH` canonical-project success plus absent-sidecar refusal/no-new-project/no-finish-mutation result, automatic legacy unit-binding existing-project persistence plus no-project/no-cache result, explicit `QS3DUNITS` bootstrap/persistence result, before/after live-state invariants for detached refresh/export, absent-sidecar refusal/no-new-project result, generic Interchange stale-confirmation refusal with proof of no replacement project/import mutation, standalone Append initial-bootstrap identity plus stale-confirmation refusal with proof of no second/replacement project and no appended mutation, sanitized failure log if any.
- Evidence: Automated baseline PASS at exact SHA `3a8ae9fc5165fda588ac1377545ad9b31c85982e`: clean-tree/manual-CI/source preflights, all 365 aggregate feature gates, Core Release build and deterministic smoke, exact-V25 adapter Release build with zero warnings/errors, offline WPF theme/Workspace/RightPanel smoke, and licensed V25 NETLOAD/Ribbon/Palette runtime probe. Sanitized runtime identity recorded BricsCAD `25.2.10`, x64 CLR `4.0.30319.42000`, with Ribbon and both palettes ready. The screenshot runner captured only the BricsCAD HWND through `PrintWindow(hwnd)`, cleaned its process environment, left no BricsCAD process/root dump, and the locally inspected image contained only the target host window. The same exact adapter binary (`A78FDC7F9E5300EEBA3E553D9C571F15E38075D8570FB00AE70C3679DCA3A991`) was packaged locally, installed with `OnCommand` registration, and passed a clean BricsCAD start plus `QS3DRUNTIMEPROBE` with `load_mode=DemandLoad`; its generated script contained no `NETLOAD`. Scope remains `source-build+runtime-smoke`; the full interactive matrix, save/reopen, multi-DWG isolation, `QS3DREGEN`/`QS3DREFRESH`, `QS3DFINISH`, legacy/explicit unit-binding lifecycle and generic/standalone-Append Interchange stale-confirmation scenarios were **NOT RUN**, so this item remains `IN_PROGRESS` and customer-release qualification remains false.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`; `scripts/test-bricscad-v25-project-lifecycle.ps1`
- Updated: 2026-08-11

## LOCAL-002 — Curtain whole-command recovery and native panels

- Priority: P0
- Status: OPEN
- Area: Curtain Wall
- Why local: Final proof needs native V25 Solid3d/transaction behavior, failure injection across host/frame/panel phases, modeless Curtain Hub/regeneration behavior, and save/reopen ownership verification.
- Source-side status: REMOTE_DONE for canonical Curtain Hub Family-property writes at source commit `ea9a15e02057425099f8f774b73448bde8e21fbc`; `CurtainWallWindow` routes all saved Family values through `ProjectFamilyService.SetProperty`, removes the unconditional Save-side `project.Touch()`, and the Curtain preflight rejects a return to direct `family.Properties[...]` mutation. Local execution must use the exact final `main` SHA containing the equivalent merged diff, not assume this source-only proof is `LOCAL_PASS`.
- Scenario: Qualify whole-command recovery/compensation for `QS3DCURTAIN3D`; materialize panel-by-panel glass from `CurtainWallDetailPlanner.Panels` with bounded ownership, opening interruption, stale/health/release integration, and deterministic replacement. Also qualify the modeless `QS3DCURTAIN` Family Save path on a clean current project: use a GlassWall Family with at least one inherited instance and one explicit instance override, change numeric values plus `Material`/`CurtainFrameMaterial`, and verify inherited values follow the Family while explicit overrides remain unchanged and are not dirtied solely by the Family change. Attempt a material/frame-material value longer than the canonical 1000-character Family-property limit and verify Save fails/rolls back without semantic/native partial mutation. With the project clean and all form values already equal to the Family, Save again and verify no `ChangeVersion`/`UpdatedUtc` advance attributable solely to a no-op Family write; any regeneration must correspond only to genuinely pre-existing dirty work. Then save/reopen and verify the accepted Family values and generated Curtain output remain consistent.
- Evidence required: Exact final tested `main` SHA containing source commit-equivalent `ea9a15e02057425099f8f774b73448bde8e21fbc`; Windows/BricsCAD V25 build; injected-failure matrix after each logical Curtain phase; panel ownership/count checks; opening/door clipping checks; inherited-instance versus explicit-override before/after property and dirty-state evidence; >1000-character Material/FrameMaterial rejection with before/after semantic/native state; clean no-op Save before/after `ChangeVersion` and `UpdatedUtc`; save/reopen result; no foreign-object deletion.
- Evidence: PENDING_LOCAL
- Related source/docs: `src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs`; `src/QS3D.Core/Domain/ProjectFamilyService.cs`; `scripts/preflight-curtain-wall-ui-export.py`; `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
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
- Scenario: Qualify existing Semantic Tag and native generic/BQ/Door-Opening/Room-Finish/Material/BBS Table lifecycles first; do not reimplement them. On cold-cache valid `.qsdb`, true tag/Table ownership writes must bind canonical state and persist through save/reopen. Separately open BQ, Door/Opening, Room Finish and BBS review windows: BQ column preference writes must bind canonical state, while Door/Room refresh/export and BBS XLSX refresh/export must regenerate detached snapshots and leave live dirty/change-version/timestamp/audit state unchanged. Read-only load/filter/Locate paths remain non-creating. Then implement/qualify only remaining MLeader/custom-schedule interaction and Sheet/Layout/Viewport/title-block/PaperSpace workflows.
- Evidence required: Exact SHA; tag/Table ownership/refresh/health checks; BQ preference persistence; before/after live-state invariants for Door/Room/BBS detached regeneration/export; absent-sidecar behavior; user-object protection; Unicode/HiDPI result; Layout/Viewport scale/lock result when implemented; Undo/save-reopen/multi-DWG result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/SEMANTIC-TAGS.md`; `docs/COMMANDS-NATIVE-DOCUMENTATION-TABLES.md`; `docs/DOCUMENTATION-LAYER.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Why local: Core now defines deterministic multi-owner identity/dependency/rebuild plans, but safe native Solid3d materialization, booleans, replacement and ownership verification still require V25.
- Scenario: First qualify the existing Wall Snap lifecycle on the exact candidate SHA. With a valid `.qsdb`, forget/reload the cache and run `QS3DWALLSNAPPREVIEW` then `QS3DWALLSNAPAPPLY`: both commands must bind the canonical same-ProjectId project before metadata/audit or CAD/semantic mutation, and Apply must preserve the current source-fingerprint, generated-dependent invalidation, native transaction and semantic rollback boundaries. Repeat with the project/sidecar absent or replaced after the source selection is prepared: both commands must fail closed and must not create/cache a replacement project, write preview metadata/audit, move LINE/POLYLINE vertices, invalidate generated ownership, or mutate another DWG. Then materialize only from current `WallJunctionOwnershipPlanner` output. Treat all plans sharing one `GroupToken` (`WJP1:`) as one replacement/rebuild unit; never assume an individual occurrence index remains long-lived when group topology/membership changes. Persist/verify dedicated `OwnerToken` (`WJX1:`) plus `InputFingerprint` (`WJF1:`), never reuse one wall's generated-solid owner. Cover L/T/X/Multi, 2/3/4+ owners, multiple occurrences, mixed thicknesses, incompatible vertical ranges, source/profile/elevation changes, owner add/remove, stale-extra output cleanup, foreign/corrupt ownership refusal, and Door/Opening host retention.
- Evidence required: Exact SHA; Wall Snap valid-sidecar canonical ProjectId continuity; absent/replaced-sidecar Preview/Apply refusal with proof of no replacement project, no preview metadata/audit change and no native/semantic mutation; successful Preview→Apply proof that source-fingerprint validation, generated invalidation, native transaction and semantic rollback behavior remain intact; whole-`GroupToken` membership/replacement check; `OwnerToken`/dependency/fingerprint persistence checks; L/T/X/Multi geometry matrix; invalidation/rebuild after fingerprint or group-membership changes; stale-extra cleanup; no cross-DWG/project mutation; opening host-retention result; save/reopen and Undo/Redo result.
- Evidence: PENDING_LOCAL
- Related docs: `docs/WALL-JUNCTION-OWNERSHIP.md`; `docs/LOCAL-AGENT-REMAINING-GATES-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-008 — Direct Draw transient preview and repeated mode

- Priority: P1
- Status: OPEN
- Area: Direct Draw UX
- Why local: DrawJig/transient/editor/UCS lifecycle, ESC cleanup, document switches, native palette behavior, and proof that editor cancellation does not cross the project-creation boundary require interactive V25.
- Scenario: First qualify the source-defined cancel/project lifecycle on a new disposable DWG with no QS3D project or sidecar. For each of `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWSLAB`, `QS3DDRAWCOLUMN`, `QS3DDRAWGLASSWALL`, `QS3DDRAWWALLPIER`, `QS3DDRAWSTRUCTWALL`, `QS3DDRAWFOUNDATION`, `QS3DDRAWDOOR`, `QS3DDRAWOPENING`, and `QS3DDRAWWALLREF`, complete point/reference acquisition and cancel at every numeric parameter prompt in separate runs. Verify each cancel leaves no newly-created/cached QS3D project, no new sidecar/project persistence, no command-owned source CAD, no semantic Element, and no generated/native output. Then repeat representative commands with an existing valid project and compatible Family defaults and verify the prompt defaults still come from that project without mutation before commit. After that, qualify transient thickness/profile preview and repeated authoring; ESC/cancel leaves no residue, active planar UCS is respected, only safe last values are reused, document switch cancels safely, and final source+semantic+native commit remains atomic.
- Evidence required: Exact SHA; per-command/per-prompt cancel matrix; proof of no project/cache/sidecar/source/semantic/native residue on clean DWG; existing-project Family-default continuity; UCS matrix; repeated-mode result; document-switch result; no persistent preview residue.
- Evidence: PENDING_LOCAL
- Related docs: `docs/DIRECT-DRAW-WORKFLOW.md`; `docs/DIRECT-DRAW-CANCEL-PROJECT-LIFECYCLE.md`; `docs/LOCAL-AGENT-OPEN-WORK-ADDENDUM-2026-08-10.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
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
- Area: Fault injection / rollback / modeless UI / generated replacement atomicity
- Why local: Core can prove semantic `ProjectStateSnapshot` restoration remotely, but native transaction abort/compensation, DocumentLock and multi-DWG behavior, generated-object cleanup, stale/erased ObjectId behavior, native XData ownership and post-commit WPF/modeless failures require interactive licensed BricsCAD V25.
- Source-side status: REMOTE_DONE for Grid annotation exact-set replacement (`761b9b92f5dd3638b18d281c273a406e41069511`), Curtain LINE/PATH exact-set replacement (`ffd26294f3f27d03de1050643aa0aeb894dcb0f2`), the shared seven-family Rebar exact-set ownership guard (`1850f02382c8ccf71f04e3ea9daa28455aaae08f`), and the dedicated Column Tie exact-set guard follow-up (`b22eacd681230f231e0f970fb670e8f89769c35e`). Runtime qualification remains `PENDING_LOCAL`; remote/non-local agents must not re-run or re-claim this V25 proof unless the source scenario materially changes.
- Scenario: Starting from `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`, inject failures before/during/after native commit and during post-commit UI refresh. Verify stale Recognition Apply never creates a replacement project and true modeless writes either bind current canonical same-ProjectId state or fail closed. Keep Door/Opening, Room Finish, BBS and BQ windows open across cache forget/reload and active-DWG switches: Door/Room/BBS read-only regeneration/export must operate on detached snapshots without changing live project state; BQ preference writes must rebind canonical state or refuse. Keep `RebarMeshSetupWindow` open across `QS3DRELOAD`/cache replacement and then click Save: it must fail closed before any property/dirty/change-version/audit/native mutation, must not create/cache a replacement project, and must require reopening the window against the current canonical project. With Workspace/Right palettes visible, also activate a drawing whose QS3D project cannot be loaded (for example a sanitized drawing/sidecar identity-mismatch fixture): verify the old project's palette callbacks/content are torn down, palette visibility/layout is preserved, no click can mutate the prior drawing/project, and a later valid drawing activation rebinds cleanly. For generated replacement atomicity, on an exact tested SHA that includes the four source-side commits above, create controlled generated owner sets where metadata stores N handles but exactly one expected handle is stale/missing/erased while the remaining old generated entities are still live. Exercise Grid `GeneratedGridAnnotationHandles`; Curtain `GeneratedCurtainFrameHandles` for both LINE and PATH builders; and representative Rebar owner slots covering longitudinal bars, stirrups/ties/shape bars and slab/wall/foundation mesh families. Each replacement attempt must refuse before committing any destructive erase for that owner set, create no partial replacement objects, preserve surviving old entities plus semantic handle metadata/ownership, and leave another DWG untouched. Also inject malformed and duplicate-canonical Rebar handle metadata and verify fail-closed behavior. Then restore a complete live set and prove exact replacement succeeds, old owned entities are removed exactly once, new handles/native ownership are complete, foreign/unmarked objects are never deleted, Undo is coherent, and save/reopen retains the final ownership state. Another DWG must never be mutated.
- Evidence required: Exact tested SHA including `761b9b92f5dd3638b18d281c273a406e41069511`, `ffd26294f3f27d03de1050643aa0aeb894dcb0f2`, `1850f02382c8ccf71f04e3ea9daa28455aaae08f`, and `b22eacd681230f231e0f970fb670e8f89769c35e`; Windows/BricsCAD V25 build; failure stage/exception; before/after semantic snapshot summary; for each generated owner-slot case record stored expected handles, live CAD handles/counts, native owner/XData identity and object counts before/after; proof stale/missing/malformed/duplicate-canonical cases leave surviving objects and metadata unchanged with no partial new objects; full-live replacement success; foreign-object protection; transaction/Undo result; active-DWG identity and no cross-DWG mutation; Recognition no-project-creation result; Door/Room/BBS detached live-state invariants; BQ canonical preference result; Rebar Mesh Setup reload/Save refusal with proof of no replacement project and no semantic/native mutation; unavailable-project palette before/after state and proof that stale callbacks cannot mutate the prior project; post-commit UI result; save/reopen result; sanitized evidence where useful.
- Evidence: PENDING_LOCAL
- Related source/docs: `src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs`; `src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs`; `src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs`; `src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs`; `src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs`; `scripts/preflight-generated-geometry.py`; `scripts/preflight-curtain-frame-native-ownership.py`; `scripts/preflight-rebar-native-ownership.py`; `docs/PROJECT-ROLLBACK-FAILURE-MATRIX.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-V25-QUALIFICATION.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-11

## LOCAL-012 — Project Browser native workspace and CAD selection bridge

- Priority: P1
- Status: OPEN
- Area: Project Browser / Workspace / modeless selection
- Why local: Core query/grouping/virtualization/selection/workspace-state coordination is source-safe, but final integration depends on real BricsCAD Editor implied selection, live ObjectId/handle resolution, modeless WPF palette lifecycle, document switching, focus/zoom behavior and Unicode/HiDPI rendering.
- Scenario: Starting from the exact SHA containing `ProjectBrowserWorkspaceCoordinator`, wire/qualify the native Workspace/Project Browser adapter without persisting CAD ObjectIds/handles in Core state. CAD selection must re-resolve to stable semantic IDs, reveal/expand the correct Browser paths and update the multi-selection inspector. Browser node/element selection must re-resolve the current canonical project and live CAD handles at action time, select/zoom only the bound active DWG, and fail closed for stale/deleted/ambiguous IDs. Keep the modeless UI open while switching DWGs, forgetting/reloading project cache, deleting selected semantics, changing grouping/filter/query, paging large nodes, saving/reopening, and cancelling operations. Verify presentation-only browser state never increments semantic `ChangeVersion` or invalidates quantity/regeneration previews.
- Evidence required: Exact QS3D SHA; Windows and BricsCAD V25 build; CAD→Browser and Browser→CAD selection matrix; single/multi-selection; stale/deleted/ambiguous ID refusal; active-DWG/document-affinity result; cache reload result; paging/filter/grouping state result; before/after semantic `ChangeVersion`; save/reopen; 100/125/150/200% DPI screenshots or sanitized notes; no cross-DWG mutation.
- Evidence: PENDING_LOCAL
- Related docs: `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceCoordinator.cs`; `src/QS3D.Core/Navigation/ProjectBrowserSelectionPlanner.cs`; `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`; `docs/REMOTE-AGENT-SCOPE.md`; `docs/LOCAL-V25-QUALIFICATION.md`
- Updated: 2026-08-11

## LOCAL-013 — clean-room public BRC proxy measurement and quantity round-trip

- Priority: P0
- Status: PASS
- Area: BRC proxy measurement / Recognition / B4D / ED2 / Excel Locate
- Why local: Requires licensed BricsCAD V25, authorized BRC-containing drawing content and native public proxy/entity behavior. All source drawings and workbooks are reference-only; qualification must create disposable drawing copies and a new workbook/output before any command runs, and must never inspect BLT binaries or proprietary/internal BLT APIs.
- Scenario: On disposable copies only, run the automation-only `QS3DBRCPROBE` clean-room diagnostic and record only sanitized public-API capability/count evidence. Determine whether supported public BricsCAD APIs provide finite positive category-appropriate Length, Area or Volume for BRC/proxy entities. Then exercise `QS3DB4D` → a newly created `CHI_TIET`/`TONG_HOP` workbook → Excel Handle locate, verifying quantity/provenance and Handle ↔ active BRC/CAD object round-trip without modifying either reference original. If a proxy remains opaque or lacks a finite positive primary metric, keep it review-only and fail closed: do not auto-accept/capture it and do not invent geometry or quantities.
- Source/runner status: `QS3DBRCPROBE`, `QS3DBRCROUNDTRIPPROBE`, `scripts/test-bricscad-v25-brc-probe.ps1`, `scripts/test-bricscad-v25-brc-quantity-roundtrip.ps1` and their static preflights are implemented and qualified at exact tested SHA `cd96507f942471d2030b3dbe0acc61f5fabfd5a7`. Both runners are automation-only, require an explicit disposable `*.reference-copy.dwg`, isolate BricsCAD runtime artifacts, restore process environment variables and record before/after DWG hashes. The round-trip runner refuses a pre-existing sidecar, produces a new workbook and validates complete Excel-to-CAD selection.
- Evidence required: Exact QS3D SHA; hashes or other non-sensitive identity for each disposable reference copy and confirmation that originals were not modified; sanitized aggregate `QS3DBRCPROBE` marker; public measurement/count results; B4D recognition/capture decision; ED2 new-workbook row/provenance summary; Excel Locate success/refusal result; and explicit opaque-proxy fail-closed evidence where applicable. `PASS` is allowed only with sanitized evidence tied to the exact tested SHA.
- Evidence: PASS at exact tested SHA `cd96507f942471d2030b3dbe0acc61f5fabfd5a7` on BricsCAD V25.2.10 x64. The disposable reference copy retained SHA-256 `7B5D54E620500564ADA20B8DDDE0FCA129E6A687644CE07D0BFB2B4A0D8B4B66` before and after both runs; the reference originals were not modified. The public-API probe opened all 352/352 current-space entities with zero read failures and found 25 proxy entities. No proxy exposed a finite positive direct Length, plan Area, surface Area or Volume; 24 proxies produced non-empty public `Explode` results with surface-only evidence across 304 positive Face parts, while exploded plan Area and Volume remained zero. Therefore all 25 proxies stayed review-only and no geometry or quantity was inferred. The B4D/ED2 probe produced 31 project elements, 31 `CHI_TIET` rows, one `TONG_HOP` row and 31 live export Handle references; the modern two-sheet 25-column workbook preserved drawing fingerprint plus Element ID/Handle provenance, and Excel Locate resolved and selected exactly one active CAD object. Its 25 proxy snapshots yielded zero capture-ready, zero auto-accepted and zero captured proxy owners. Artifact-tool inspection found no spreadsheet formula-error tokens and the rendered `CHI_TIET`/`TONG_HOP` sheets were legible. Evidence is sanitized aggregate data only; no private path, raw Handle list, customer drawing or workbook is committed. This PASS closes only LOCAL-013 and does not qualify full BLT parity, the remaining local matrix or customer release.
- Related docs: `src/QS3D.BricsCAD.V25/BrcPublicProbeCommands.cs`; `src/QS3D.BricsCAD.V25/BrcQuantityRoundTripProbeCommands.cs`; `scripts/test-bricscad-v25-brc-probe.ps1`; `scripts/test-bricscad-v25-brc-quantity-roundtrip.ps1`; `docs/PRODUCT-BOUNDARY.md`; `docs/COMMANDS.md`; `docs/LOCAL-V25-QUALIFICATION.md`
- Updated: 2026-08-11

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.

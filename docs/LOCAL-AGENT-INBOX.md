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
- 2026-08-23 exact #3612 continuation: refreshed `origin/main@bacfc918982570475d3ab1369310dfbf5119d2a0` is represented by the task branch. Repeated-mode source candidate `9a77d329e90809a2006d8e4dc1bafc995c0a8ca2` passed licensed `NETLOAD` and exact-plugin identity on BricsCAD V25.2.10 and V26.2.07. Each host ran independent two-segment Wall and Beam sequences, observed eight hosted `DrawJig.WorldDraw` callbacks per Family, and passed Enter, physical ESC, planar UCS, active-document switch refusal/isolation, native/semantic Undo/Redo, QS3D/DWG save, sidecar persistence and fresh-process Wall/Beam cold reopen. On predecessor candidate `e5725e96eed6dcebb46370c33e6f8a88e2cc2b68`, V26 CLR 8.0.29 identity/native LINE and the V25 eligible CAD -> B4D -> ED2 XLSX -> Excel Locate -> real PICKFIRST -> visible Workspace bridge passed; the Workspace showed one selected semantic Instance/Family and `1 chọn`, while wrong fingerprint, unknown element, stale Handle and partial resolution all failed closed without corrupting selection/state. This closes only the bounded #3612 cells; private/customer DWG, missing historical BRC-proxy parity, signing/package and the remaining full interactive/release matrix stay pending under their parent issues.
- Google Sheet row 1 Windows smoke-executable bounded PASS (2026-08-15): exact committed/pushed candidate `9b25be9df47459f72acecfa96443d94204410f2f` passed the guarded-entrypoint/failure-containment/manual-CI/local-handoff gates and Release build with `0 warnings / 0 errors`. Apphost ProductVersion/SHA-256 were `1.0.0+9b25be9df47459f72acecfa96443d94204410f2f` / `C65D39BAD9BDBB8F61671478EDA47A182C16C1061F110C8E42E0CDD3E2192606`; Core ProductVersion/SHA-256 were `0.1.0-preview.10045+9b25be9df47459f72acecfa96443d94204410f2f` / `82786A401B2315AD18783B92F476D998B55B02690E3A1EE98BC34D1D58D5E377`. The direct Windows `.exe` run exited `0` in about 19.63 seconds with 30 stdout lines, exactly one `ALL PASS`, no failure token and empty stderr; no Application Error/.NET window, WerFault process, matching Application event, timeout or process residue appeared. Negative managed-failure injection is `NOT_RUN` because no repository-approved external trigger exists; no injection/source/test change was made. Logs remain ignored/local and no Actions/private data/BricsCAD operation was involved. This closes only Sheet row 1 and does not change overall LOCAL-001 `IN_PROGRESS` status.
- Why local: Requires licensed BricsCAD V25 x64, installed managed references, Windows desktop, NETLOAD/DemandLoad, and native command execution.
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Cold-start/reopen a drawing with an existing `.qsdb`: true writes must bind the canonical same-ProjectId project, while one regeneration-based CSV/XLSX export and one modeless Door/Room refresh/export must use detached regenerated state and leave live project dirty/change-version/timestamp/audit state unchanged. Explicitly exercise `QS3DREGEN` and `QS3DREFRESH` after cache forget/reload: `QS3DREGEN` must bind the canonical existing project before regeneration; `QS3DREFRESH` may regenerate only after the same canonical binding. Repeat those commands with the sidecar/project absent: `QS3DREGEN` must refuse without creating/caching a replacement project, while `QS3DREFRESH` must remain a non-creating UI refresh with no semantic mutation. Exercise `QS3DFINISH` with a selected existing Room after cache forget/reload and verify generated finish semantics stay on the canonical project; repeat with the sidecar/project absent and verify the command refuses without creating/caching a replacement project or finish Family/Element state. Exercise automatic legacy unit binding from a unit-dependent command such as `QS3DBQ`: on a valid legacy project with semantic elements it must bind/update/save the canonical project; on a drawing with supported INSUNITS but no QS3D project it must not create/cache a project merely while resolving units. Separately verify explicit `QS3DUNITS` still intentionally creates/saves project unit state when the user confirms a project override. Repeat ownership-dependent writes with the sidecar absent and verify refusal without leaving a replacement project. For `QS3DINTERCHANGEIMPORT`, review a policy plan, then forget/reload the project cache or replace/remove the sidecar before confirmation; freshness confirmation must refuse the stale plan without creating/caching a replacement project and without applying any import mutation. Repeat the same post-preview cache/reload/sidecar-replacement test with standalone `QS3DINTERCHANGEAPPEND`: its initial target bootstrap before preview is allowed, but after the Yes/No review it must refuse a stale/replaced target through the non-creating freshness guard and must not append any semantic state.
- Additional scenario: Exercise `QS3DLINKHOST` with an empty/cancelled selection before any semantic lookup; it must return without binding, creating, or caching a QS3D project. With one valid opening plus wall source after cache forget/reload, it must bind the same canonical project; with the sidecar absent after selection, it must fail closed without CAD or semantic mutation.
- Additional evidence required: `QS3DLINKHOST` empty/cancelled-selection proof of no project bind/create/cache, valid-selection canonical project continuity, and absent-sidecar refusal with no host/CAD/semantic mutation.
- Additional scenario: Run `scripts/test-bricscad-v25-sidecar-revision.ps1` on the repository-generated disposable fixture. With the canonical project cache warm, make the `.bak` appear, replace the `.qsdb` bytes, and remove the `.qsdb` in separate restored phases. Every read/bind/existing-mutation/Interchange-confirmation/Save boundary must fail closed; the semantic project and DWG hashes must remain unchanged, and byte restoration must recover the same canonical session.
- Additional evidence required: Exact tested SHA, BricsCAD V25/plugin identity, PASS booleans for all three `.qsdb/.bak` warm-cache changes and all five authority boundaries, unchanged disposable/reference DWG SHA-256, restored-session continuity, sanitized marker, environment/process cleanup, and no private path/ProjectId/Handle/fingerprint output.
- Additional scenario: Exercise the rebar selection boundary on the same exact candidate SHA. `QS3DREBAR3D` and `QS3DREBARTIES3D` are PICKFIRST-only: no implied selection must return without opening a new selection prompt and without binding, creating, or caching a project. For `QS3DBEAMREBAR3D`, `QS3DREBARSTIRRUP3D`, `QS3DSLABREBAR3D`, `QS3DFOUNDATIONREBAR3D`, `QS3DWALLREBAR3D`, and `QS3DREBAR3DSHAPE`, cancel/empty interactive selection must return before project binding/creation/cache. Repeat valid selected runs after cache forget/reload and verify canonical same-ProjectId binding plus unchanged generated ownership, exact replacement, rollback and UI finalization.
- Additional evidence required: Rebar selection matrix proving no project bind/create/cache on empty/cancel for all eight guarded 3D commands; proof that `QS3DREBAR3D`/`QS3DREBARTIES3D` remain PICKFIRST-only; canonical ProjectId continuity on successful selected runs; and unchanged generated ownership/replacement/rollback behavior.
- Additional scenario: Exercise `QS3DSAVE` on a cold-cache drawing with a valid existing `.qsdb`; it must bind and persist the canonical same-ProjectId project. On a drawing without a QS3D project/sidecar, Save must fail closed without creating, caching, or persisting a replacement project. After cache forget with a valid sidecar, it must rebind the existing project rather than creating a default one.
- Additional evidence required: `QS3DSAVE` cold-cache canonical same-ProjectId persistence plus absent-project refusal proving no replacement project/cache/sidecar creation.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache ProjectId continuity for true writes, `QS3DREGEN`/`QS3DREFRESH` existing-project and absent-sidecar lifecycle results with proof of no replacement project, `QS3DFINISH` canonical-project success plus absent-sidecar refusal/no-new-project/no-finish-mutation result, automatic legacy unit-binding existing-project persistence plus no-project/no-cache result, explicit `QS3DUNITS` bootstrap/persistence result, before/after live-state invariants for detached refresh/export, absent-sidecar refusal/no-new-project result, generic Interchange stale-confirmation refusal with proof of no replacement project/import mutation, standalone Append initial-bootstrap identity plus stale-confirmation refusal with proof of no second/replacement project and no appended mutation, sanitized failure log if any.
- Evidence: Automated baseline PASS at exact SHA `3a8ae9fc5165fda588ac1377545ad9b31c85982e`: clean-tree/manual-CI/source preflights, all 365 aggregate feature gates, Core Release build and deterministic smoke, exact-V25 adapter Release build with zero warnings/errors, offline WPF theme/Workspace/RightPanel smoke, and licensed V25 NETLOAD/Ribbon/Palette runtime probe. Sanitized runtime identity recorded BricsCAD `25.2.10`, x64 CLR `4.0.30319.42000`, with Ribbon and both palettes ready. The screenshot runner captured only the BricsCAD HWND through `PrintWindow(hwnd)`, cleaned its process environment, left no BricsCAD process/root dump, and the locally inspected image contained only the target host window. The same exact adapter binary (`A78FDC7F9E5300EEBA3E553D9C571F15E38075D8570FB00AE70C3679DCA3A991`) was packaged locally, installed with `OnCommand` registration, and passed a clean BricsCAD start plus `QS3DRUNTIMEPROBE` with `load_mode=DemandLoad`; its generated script contained no `NETLOAD`. Scope remains `source-build+runtime-smoke`; the full interactive matrix, save/reopen, multi-DWG isolation, `QS3DREGEN`/`QS3DREFRESH`, `QS3DFINISH`, legacy/explicit unit-binding lifecycle and generic/standalone-Append Interchange stale-confirmation scenarios were **NOT RUN**, so this item remains `IN_PROGRESS` and customer-release qualification remains false.
  Additional exact-SHA lifecycle evidence: `scripts/test-bricscad-v25-project-lifecycle.ps1` PASS at `604982a506aa07ba3eae047d282847333d529314` on BricsCAD `25.2.10`. Four repository-generated disposable DWG copies proved DWG `SaveComplete` sidecar persistence, cold-cache identity continuity, canonical existing-project binding, detached-snapshot immutability, distinct A/B project identity, isolated per-DWG mutations, a second persisted cold reload, absent-sidecar non-creation, and corrupt-sidecar fail-closed behavior with the corrupt file unchanged. The fixture SHA-256 was unchanged, final evidence contained no raw paths/ProjectIds/Handles/fingerprints, the state file was removed, and no BricsCAD process or probe environment remained. This narrows but does not close LOCAL-001: `QS3DREGEN`/`QS3DREFRESH`, `QS3DFINISH`, legacy/explicit unit binding, modeless refresh/export invariants, Interchange confirmation freshness, save/reopen UI flows and the full interactive/private-DWG matrix remain **NOT RUN**.
  Superseding the earlier **NOT RUN** status for these three commands: the expanded lifecycle runner PASSed at exact SHA `f627e9667e58c036df0be5b094a6f9cc494abaeb` on BricsCAD `25.2.10`. It executed the real `QS3DREGEN`, `QS3DREFRESH`, and `QS3DFINISH` commands in six phases: cold existing project plus absent-sidecar drawing for each command. Existing-project phases preserved canonical project identity and pending semantic mutation; REGEN/REFRESH cleared the probe Room's semantic dirty state; FINISH created every canonical Room Finish category with its Room dependency. All absent-sidecar phases remained non-creating with no cached/pending project state. The first run exposed passive Workspace refresh calling `GetOrCreate` on the absent-sidecar path; the follow-up lifecycle review also caught an intermediate read-only snapshot that could detach modeless edits on a cold cache. The final fix binds only an existing canonical project through `ExistingProjectMutationContext`, and the clean exact-SHA rerun passed all six phases, kept the generated fixture SHA-256 unchanged, removed state/environment/process residue, and emitted no raw path, ProjectId, Handle, fingerprint, or error text in marker/JSON evidence. LOCAL-001 remains `IN_PROGRESS` for unit binding, modeless refresh/export runtime invariants, Interchange/Host Link confirmation paths, Save As UI coverage, and the full interactive/private-DWG matrix.
  Superseding the unit-binding **NOT RUN** status: the nine-phase lifecycle runner PASSed at exact SHA `1dfbf35fbc9745b880133f7ac0cdfb50887391b1` on BricsCAD `25.2.10`. Real `QS3DBQ` on a cold legacy project bound the native unit to the same canonical project, persisted it and left no pending state; real `QS3DBQ` on a supported-INSUNITS drawing without a sidecar resolved units but created neither cache nor QSDB; real `QS3DUNITS` on unresolved INSUNITS consumed a scope-validated one-shot automation confirmation, persisted a Meter project override and intentionally bootstrapped exactly one empty project without semantic elements. The run exposed and drove fixes for unit inspection calling `GetOrCreate` and for BQ XAML checkbox events mutating live column preferences during `InitializeComponent`. All nine phases passed, the generated fixture SHA-256 remained unchanged, cleanup/privacy checks passed, and no GitHub Actions ran. Normal user calls still use `Editor.GetKeywords`; physical keyboard/prompt UX remains **NOT RUN** in the interactive matrix. LOCAL-001 remains `IN_PROGRESS` for modeless refresh/export runtime invariants, Interchange/Host Link confirmation paths, Save As UI coverage, the physical `QS3DUNITS` prompt and the full interactive/private-DWG matrix.
  Warm-cache sidecar revision evidence (2026-08-15): the automation-only matrix PASSed on clean, committed and pushed exact SHA `cfc80fe80f1bf866fdec27111eb5fdf1977a3305` in BricsCAD `25.2.10` x64. Focused sidecar/Save/static/manual-CI/handoff gates, PowerShell AST, full Core smoke and the installed-reference V25 `Release|x64` build passed first with `0 warnings / 0 errors`; plugin/Core ProductVersion was `0.1.0-preview.10040+cfc80fe80f1bf866fdec27111eb5fdf1977a3305`, adapter SHA-256 `E06C477F04DC18546D89B8DC0C291783D4FFDF82820F2E8D0F68D8EC9C68CA68`, and Core SHA-256 `6F7220CF5318A1E70A1F09B59E449E64A16FF744A4DAF82A35CECB66CAFF0685`. With one canonical project cache kept warm, backup appearance, primary byte replacement and primary removal each made read-only access, canonical bind, existing-project mutation, Interchange confirmation and Save fail closed. Semantic state stayed unchanged, restoring the original bytes recovered the same canonical session, and no DWG write was requested. Fixture and disposable-copy SHA-256 stayed `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; zero process, environment, script, QSDB, backup, project-lock and drawing-lock residue was verified. The bounded runner/probe corrections only grouped cleanup paths atomically, finalized the exact host before DWG hashing, shortened private scratch names below net48 MAX_PATH, and emitted allowlisted failure stage/kind tokens; production persistence/Core behavior was not changed. This closes only the warm-cache revision row. LOCAL-001 remains `IN_PROGRESS` for the other current-candidate scenarios and customer-release qualification remains false.
- Current evidence reading rule: the later exact-SHA lifecycle paragraphs supersede only the baseline `NOT RUN` statements they name. They do not promote LOCAL-001 to `PASS`; all remaining scenarios in the final current-candidate block below stay `PENDING_LOCAL`.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`; `scripts/test-bricscad-v25-project-lifecycle.ps1`; `scripts/test-bricscad-v25-sidecar-revision.ps1`; `scripts/preflight-save-project-lifecycle.py`
- Historical extension snapshot (superseded by the consolidated current-candidate scenario below):
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Cold-start/reopen a drawing with an existing `.qsdb`: true writes must bind the canonical same-ProjectId project, while one regeneration-based CSV/XLSX export and one modeless Door/Room refresh/export must use detached regenerated state and leave live project dirty/change-version/timestamp/audit state unchanged. Explicitly exercise `QS3DREGEN` and `QS3DREFRESH` after cache forget/reload: `QS3DREGEN` must bind the canonical existing project before regeneration; `QS3DREFRESH` may regenerate only after the same canonical binding. Repeat those commands with the sidecar/project absent: `QS3DREGEN` must refuse without creating/caching a replacement project, while `QS3DREFRESH` must remain a non-creating UI refresh with no semantic mutation. For `QS3DFINISH`, cancel/empty selection must return before canonical project bind/cache and leave project, finish, audit and CAD state unchanged. Then exercise `QS3DFINISH` with a selected existing Room after cache forget/reload and verify generated finish semantics stay on the canonical project; repeat with the sidecar/project absent and verify the command refuses without creating/caching a replacement project or finish Family/Element state. Exercise automatic legacy unit binding from a unit-dependent command such as `QS3DBQ`: on a valid legacy project with semantic elements it must bind/update/save the canonical project; on a drawing with supported INSUNITS but no QS3D project it must not create/cache a project merely while resolving units. Separately verify explicit `QS3DUNITS` still intentionally creates/saves project unit state when the user confirms a project override. Repeat ownership-dependent writes with the sidecar absent and verify refusal without leaving a replacement project. For `QS3DINTERCHANGEIMPORT`, review a policy plan, then forget/reload the project cache or replace/remove the sidecar before confirmation; freshness confirmation must refuse the stale plan without creating/caching a replacement project and without applying any import mutation. Exercise `QS3DLINKHOST` with an empty/cancelled selection before any semantic lookup: it must return without binding, creating, or caching a QS3D project. Then repeat with exactly one Door/WallOpening plus one valid wall/vách source after cache forget/reload: selection acquisition must complete first, the mutation must bind the canonical same-ProjectId project, and host-link regeneration/rollback behavior must remain intact; with the sidecar/project absent after selection, the command must fail closed without creating a replacement project or mutating CAD/semantic state. Also exercise the rebar selection boundary on the same exact candidate SHA. `QS3DREBAR3D` and `QS3DREBARTIES3D` are PICKFIRST-only: with no implied selection they must return without opening a new selection prompt and without binding, creating, or caching a QS3D project. `QS3DBEAMREBAR3D`, `QS3DREBARSTIRRUP3D`, `QS3DSLABREBAR3D`, `QS3DFOUNDATIONREBAR3D`, `QS3DWALLREBAR3D`, and `QS3DREBAR3DSHAPE` retain interactive selection fallback: cancel/empty selection must return before project binding/creation/cache. For each family, repeat a valid selected run after cache forget/reload and verify the mutation binds the canonical same-ProjectId project while existing generated-ownership, exact-replacement, rollback and UI-finalization behavior remains intact. Also qualify `QS3DGRIDNUMBERAUTO`: after selecting valid semantic Grid LINE sources, cancel independently at ordering-axis start/end, naming mode/start/padding/prefix/suffix, and decline/cancel the final confirmation. Read-only preview may inspect the detached existing sidecar, but every such exit must occur before canonical project bind/cache and must leave Grid names, `ChangeVersion`, audit and CAD state unchanged. On a cold-cache valid `.qsdb`, complete the preview and confirm: the command must then bind the canonical same-ProjectId project, re-resolve the selected semantic ownership, re-read the authoritative live LINE geometry, re-order using the confirmed axis, verify the preview ElementId/projected-coordinate plan is still current, and only then renumber. Change Grid ownership/project identity or move a selected Grid LINE across the ordering axis between preview and commit and verify freshness rejection occurs before any renumber/audit mutation.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache ProjectId continuity for true writes, `QS3DREGEN`/`QS3DREFRESH` existing-project and absent-sidecar lifecycle results with proof of no replacement project, `QS3DFINISH` cancel/empty proof of no canonical project bind/cache and no project/finish/audit/CAD mutation plus canonical-project success and absent-sidecar refusal/no-new-project/no-finish-mutation result, automatic legacy unit-binding existing-project persistence plus no-project/no-cache result, explicit `QS3DUNITS` bootstrap/persistence result, before/after live-state invariants for detached refresh/export, absent-sidecar refusal/no-new-project result, Interchange stale-confirmation refusal with proof of no replacement project/import mutation, `QS3DLINKHOST` empty/cancelled selection proof of no project bind/create/cache plus valid-selection canonical ProjectId continuity and absent-sidecar refusal with no host/CAD/semantic mutation, rebar selection matrix proving no-project-bind/create/cache on empty/cancel for all eight guarded 3D commands, proof that `QS3DREBAR3D`/`QS3DREBARTIES3D` do not gain an interactive prompt, canonical ProjectId continuity on successful selected runs, unchanged generated ownership/replacement/rollback behavior, `QS3DGRIDNUMBERAUTO` per-prompt/confirm cancel matrix proving no canonical bind/cache or Grid/audit mutation after detached preview, successful cold-cache canonical ProjectId continuity plus live authoritative LINE re-read, stale ProjectId/ownership/order-coordinate rejection before renumber, sanitized failure log if any.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `scripts/preflight-room-finish-project-lifecycle.py`; `scripts/preflight-grid-auto-number-project-lifecycle.py`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Historical Room Auto extension snapshot (superseded by the consolidated current-candidate scenario below):
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Cold-start/reopen a drawing with an existing `.qsdb`: true writes must bind the canonical same-ProjectId project, while one regeneration-based CSV/XLSX export and one modeless Door/Room refresh/export must use detached regenerated state and leave live project dirty/change-version/timestamp/audit state unchanged. Explicitly exercise `QS3DREGEN` and `QS3DREFRESH` after cache forget/reload: `QS3DREGEN` must bind the canonical existing project before regeneration; `QS3DREFRESH` may regenerate only after the same canonical binding. Repeat those commands with the sidecar/project absent: `QS3DREGEN` must refuse without creating/caching a replacement project, while `QS3DREFRESH` must remain a non-creating UI refresh with no semantic mutation. For `QS3DFINISH`, cancel/empty selection must return before canonical project bind/cache and leave project, finish, audit and CAD state unchanged. Then exercise `QS3DFINISH` with a selected existing Room after cache forget/reload and verify generated finish semantics stay on the canonical project; repeat with the sidecar/project absent and verify the command refuses without creating/caching a replacement project or finish Family/Element state. Exercise automatic legacy unit binding from a unit-dependent command such as `QS3DBQ`: on a valid legacy project with semantic elements it must bind/update/save the canonical project; on a drawing with supported INSUNITS but no QS3D project it must not create/cache a project merely while resolving units. Separately verify explicit `QS3DUNITS` still intentionally creates/saves project unit state when the user confirms a project override. Repeat ownership-dependent writes with the sidecar absent and verify refusal without leaving a replacement project. For `QS3DINTERCHANGEIMPORT`, review a policy plan, then forget/reload the project cache or replace/remove the sidecar before confirmation; freshness confirmation must refuse the stale plan without creating/caching a replacement project and without applying any import mutation. Exercise `QS3DLINKHOST` with an empty/cancelled selection before any semantic lookup: it must return without binding, creating, or caching a QS3D project. Then repeat with exactly one Door/WallOpening plus one valid wall/vách source after cache forget/reload: selection acquisition must complete first, the mutation must bind the canonical same-ProjectId project, and host-link regeneration/rollback behavior must remain intact; with the sidecar/project absent after selection, the command must fail closed without creating a replacement project or mutating CAD/semantic state. Also exercise the rebar selection boundary on the same exact candidate SHA. `QS3DREBAR3D` and `QS3DREBARTIES3D` are PICKFIRST-only: with no implied selection they must return without opening a new selection prompt and without binding, creating, or caching a QS3D project. `QS3DBEAMREBAR3D`, `QS3DREBARSTIRRUP3D`, `QS3DSLABREBAR3D`, `QS3DFOUNDATIONREBAR3D`, `QS3DWALLREBAR3D`, and `QS3DREBAR3DSHAPE` retain interactive selection fallback: cancel/empty selection must return before project binding/creation/cache. For each family, repeat a valid selected run after cache forget/reload and verify the mutation binds the canonical same-ProjectId project while existing generated-ownership, exact-replacement, rollback and UI-finalization behavior remains intact. Also qualify `QS3DGRIDNUMBERAUTO`: after selecting valid semantic Grid LINE sources, cancel independently at ordering-axis start/end, naming mode/start/padding/prefix/suffix, and decline/cancel the final confirmation. Read-only preview may inspect the detached existing sidecar, but every such exit must occur before canonical project bind/cache and must leave Grid names, `ChangeVersion`, audit and CAD state unchanged. On a cold-cache valid `.qsdb`, complete the preview and confirm: the command must then bind the canonical same-ProjectId project, re-resolve the selected semantic ownership, re-read the authoritative live LINE geometry, re-order using the confirmed axis, verify the preview ElementId/projected-coordinate plan is still current, and only then renumber. Change Grid ownership/project identity or move a selected Grid LINE across the ordering axis between preview and commit and verify freshness rejection occurs before any renumber/audit mutation. Also qualify `QS3DROOMAUTO` preview-to-commit freshness. On an initially projectless drawing, prepare valid accepted Room boundary geometry, then make a valid QS3D project/sidecar visible before the command reaches commit: the command must refuse and require rerun before `GetOrCreate`, `ProjectStateSnapshot`, Room/audit mutation, regeneration or CAD-side follow-up. With an existing project, change `RoomBoundaryToleranceM`, `RoomBoundaryArcSagittaM`, `RoomBoundarySplineChordM`, `RoomBoundaryMinimumAreaM2`, or the effective drawing-unit policy after boundary selection/diagnostics but before mutation and verify fail-closed refusal. The successful no-project path must remain creation-capable only when accepted topology exists and no project appears before commit.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache ProjectId continuity for true writes, `QS3DREGEN`/`QS3DREFRESH` existing-project and absent-sidecar lifecycle results with proof of no replacement project, `QS3DFINISH` cancel/empty proof of no canonical project bind/cache and no project/finish/audit/CAD mutation plus canonical-project success and absent-sidecar refusal/no-new-project/no-finish-mutation result, automatic legacy unit-binding existing-project persistence plus no-project/no-cache result, explicit `QS3DUNITS` bootstrap/persistence result, before/after live-state invariants for detached refresh/export, absent-sidecar refusal/no-new-project result, Interchange stale-confirmation refusal with proof of no replacement project/import mutation, `QS3DLINKHOST` empty/cancelled selection proof of no project bind/create/cache plus valid-selection canonical ProjectId continuity and absent-sidecar refusal with no host/CAD/semantic mutation, rebar selection matrix proving no-project-bind/create/cache on empty/cancel for all eight guarded 3D commands, proof that `QS3DREBAR3D`/`QS3DREBARTIES3D` do not gain an interactive prompt, canonical ProjectId continuity on successful selected runs, unchanged generated ownership/replacement/rollback behavior, `QS3DGRIDNUMBERAUTO` per-prompt/confirm cancel matrix proving no canonical bind/cache or Grid/audit mutation after detached preview, successful cold-cache canonical ProjectId continuity plus live authoritative LINE re-read, stale ProjectId/ownership/order-coordinate rejection before renumber, `QS3DROOMAUTO` no-project-preview/project-appears refusal with proof of no project creation/cache replacement and no Room/audit/CAD mutation, per-setting and effective-unit freshness rejection before snapshot/mutation, successful accepted-topology no-project creation path, sanitized failure log if any.
- Historical disposition: `PENDING_LOCAL`; the authoritative current disposition is recorded after the consolidated FieldMerge/current-candidate scenario below.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `scripts/preflight-room-finish-project-lifecycle.py`; `scripts/preflight-grid-auto-number-project-lifecycle.py`; `scripts/preflight-room-auto-project-lifecycle.py`; `docs/ROOM-AUTO-PREVIEW-COMMIT-FRESHNESS.md`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Authoritative current-candidate extension:
- Source-side status: REMOTE_DONE for the dedicated reviewed native FieldMerge source path: `QS3DINTERCHANGEFIELDMERGE` previews the existing target through `TryGetReadOnly`, completes all 11 precedence prompts and the final confirmation before canonical mutation binding, rechecks ProjectId/drawing fingerprint/ChangeVersion, then `InterchangeFieldMergeImportService` performs generated-dependent native prepare -> exact authorized Core field apply -> ownership metadata sweep -> CAD commit with semantic snapshot rollback on pre-commit failure. `scripts/preflight-interchange-field-merge-execution.py` guards this source contract. Exact licensed V25 transaction/failure/Undo/save-reopen/multi-DWG proof remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
- Scenario: Run `scripts/run-local-v25-qualification.ps1` from a clean exact SHA with the real V25 install directory; prove Core Release build, Core smoke, adapter exact-V25 build, NETLOAD, DemandLoad, command registration, save/reopen, and multi-DWG isolation. Cold-start/reopen a drawing with an existing `.qsdb`: true writes must bind the canonical same-ProjectId project, while one regeneration-based CSV/XLSX export and one modeless Door/Room refresh/export must use detached regenerated state and leave live project dirty/change-version/timestamp/audit state unchanged. Explicitly exercise `QS3DREGEN` and `QS3DREFRESH` after cache forget/reload: `QS3DREGEN` must bind the canonical existing project before regeneration; `QS3DREFRESH` may regenerate only after the same canonical binding. Repeat those commands with the sidecar/project absent: `QS3DREGEN` must refuse without creating/caching a replacement project, while `QS3DREFRESH` must remain a non-creating UI refresh with no semantic mutation. For `QS3DFINISH`, cancel/empty selection must return before canonical project bind/cache and leave project, finish, audit and CAD state unchanged. Then exercise `QS3DFINISH` with a selected existing Room after cache forget/reload and verify generated finish semantics stay on the canonical project; repeat with the sidecar/project absent and verify the command refuses without creating/caching a replacement project or finish Family/Element state. Exercise automatic legacy unit binding from a unit-dependent command such as `QS3DBQ`: on a valid legacy project with semantic elements it must bind/update/save the canonical project; on a drawing with supported INSUNITS but no QS3D project it must not create/cache a project merely while resolving units. Separately verify explicit `QS3DUNITS` still intentionally creates/saves project unit state when the user confirms a project override. Repeat ownership-dependent writes with the sidecar absent and verify refusal without leaving a replacement project. For `QS3DINTERCHANGEIMPORT`, review a policy plan, then forget/reload the project cache or replace/remove the sidecar before confirmation; freshness confirmation must refuse the stale plan without creating/caching a replacement project and without applying any import mutation. For `QS3DINTERCHANGEFIELDMERGE`, use a valid snapshot against a cold-cache existing project and cancel independently at each of the 11 field-group precedence prompts and at the final confirmation: every cancel must remain read-only, must not canonical-bind/cache a project, and must leave semantic/native/audit state unchanged. Then confirm a reviewed UseSource-containing plan and verify the command canonical-binds the same ProjectId/drawing fingerprint/ChangeVersion before native prepare. Replace/remove/reload the sidecar/project or change target fingerprint/version after review but before confirmation and verify fail-closed refusal before native erase or semantic mutation. Inject failures after generated-dependent native Prepare, during Core authorized apply, after metadata sweep and before native commit; prove the CAD transaction aborts and `ProjectStateSnapshot` restores the semantic target with no cross-DWG or foreign-object deletion. A successful FieldMerge must leave affected generated outputs explicitly invalidated for rebuild; it must not silently auto-build3D/cut/rebar/curtain/grid/save. Exercise `QS3DLINKHOST` with an empty/cancelled selection before any semantic lookup: it must return without binding, creating, or caching a QS3D project. Then repeat with exactly one Door/WallOpening plus one valid wall/vách source after cache forget/reload: selection acquisition must complete first, the mutation must bind the canonical same-ProjectId project, and host-link regeneration/rollback behavior must remain intact; with the sidecar/project absent after selection, the command must fail closed without creating a replacement project or mutating CAD/semantic state. Also exercise the rebar selection boundary on the same exact candidate SHA. `QS3DREBAR3D` and `QS3DREBARTIES3D` are PICKFIRST-only: with no implied selection they must return without opening a new selection prompt and without binding, creating, or caching a QS3D project. `QS3DBEAMREBAR3D`, `QS3DREBARSTIRRUP3D`, `QS3DSLABREBAR3D`, `QS3DFOUNDATIONREBAR3D`, `QS3DWALLREBAR3D`, and `QS3DREBAR3DSHAPE` retain interactive selection fallback: cancel/empty selection must return before project binding/creation/cache. For each family, repeat a valid selected run after cache forget/reload and verify the mutation binds the canonical same-ProjectId project while existing generated-ownership, exact-replacement, rollback and UI-finalization behavior remains intact. Also qualify `QS3DGRIDNUMBERAUTO`: after selecting valid semantic Grid LINE sources, cancel independently at ordering-axis start/end, naming mode/start/padding/prefix/suffix, and decline/cancel the final confirmation. Read-only preview may inspect the detached existing sidecar, but every such exit must occur before canonical project bind/cache and must leave Grid names, `ChangeVersion`, audit and CAD state unchanged. On a cold-cache valid `.qsdb`, complete the preview and confirm: the command must then bind the canonical same-ProjectId project, re-resolve the selected semantic ownership, re-read the authoritative live LINE geometry, re-order using the confirmed axis, verify the preview ElementId/projected-coordinate plan is still current, and only then renumber. Change Grid ownership/project identity or move a selected Grid LINE across the ordering axis between preview and commit and verify freshness rejection occurs before any renumber/audit mutation. Also qualify `QS3DROOMAUTO` preview-to-commit freshness. On an initially projectless drawing, prepare valid accepted Room boundary geometry, then make a valid QS3D project/sidecar visible before the command reaches commit: the command must refuse and require rerun before `GetOrCreate`, `ProjectStateSnapshot`, Room/audit mutation, regeneration or CAD-side follow-up. With an existing project, change `RoomBoundaryToleranceM`, `RoomBoundaryArcSagittaM`, `RoomBoundarySplineChordM`, `RoomBoundaryMinimumAreaM2`, or the effective drawing-unit policy after boundary selection/diagnostics but before mutation and verify fail-closed refusal. The successful no-project path must remain creation-capable only when accepted topology exists and no project appears before commit. Also exercise `QS3DSAVE`: on a cold-cache drawing with a valid existing `.qsdb`, Save must bind and persist the canonical same-ProjectId project; on a drawing with no QS3D project/sidecar, Save must fail closed without creating, caching, or persisting a replacement project; after cache forget with a valid sidecar it must rebind the existing project rather than creating a default one.
- Evidence required: Exact QS3D SHA, Windows build, BricsCAD V25 build, .NET/MSBuild version, command/load results, cold-cache ProjectId continuity for true writes, `QS3DREGEN`/`QS3DREFRESH` existing-project and absent-sidecar lifecycle results with proof of no replacement project, `QS3DFINISH` cancel/empty proof of no canonical project bind/cache and no project/finish/audit/CAD mutation plus canonical-project success and absent-sidecar refusal/no-new-project/no-finish-mutation result, automatic legacy unit-binding existing-project persistence plus no-project/no-cache result, explicit `QS3DUNITS` bootstrap/persistence result, before/after live-state invariants for detached refresh/export, absent-sidecar refusal/no-new-project result, Interchange stale-confirmation refusal with proof of no replacement project/import mutation, `QS3DINTERCHANGEFIELDMERGE` per-prompt/final-confirm cancel proof of no canonical bind/cache or semantic/native/audit mutation, successful cold-cache canonical ProjectId/fingerprint/ChangeVersion continuity, stale/replaced-target refusal before erase/mutation, native Prepare/Core apply/metadata sweep/pre-commit failure-injection evidence proving CAD abort plus semantic snapshot restoration, successful explicit invalidation/rebuild-required result and no foreign/cross-DWG deletion, `QS3DLINKHOST` empty/cancelled selection proof of no project bind/create/cache plus valid-selection canonical ProjectId continuity and absent-sidecar refusal with no host/CAD/semantic mutation, rebar selection matrix proving no-project-bind/create/cache on empty/cancel for all eight guarded 3D commands, proof that `QS3DREBAR3D`/`QS3DREBARTIES3D` do not gain an interactive prompt, canonical ProjectId continuity on successful selected runs, unchanged generated ownership/replacement/rollback behavior, `QS3DGRIDNUMBERAUTO` per-prompt/confirm cancel matrix proving no canonical bind/cache or Grid/audit mutation after detached preview, successful cold-cache canonical ProjectId continuity plus live authoritative LINE re-read, stale ProjectId/ownership/order-coordinate rejection before renumber, `QS3DROOMAUTO` no-project-preview/project-appears refusal with proof of no project creation/cache replacement and no Room/audit/CAD mutation, per-setting and effective-unit freshness rejection before snapshot/mutation, successful accepted-topology no-project creation path, `QS3DSAVE` cold-cache canonical same-ProjectId persistence plus absent-project refusal proving no replacement project/cache/sidecar creation, sanitized failure log if any.
- Evidence: PENDING_LOCAL. Exact current-main-derived candidate
  `8b5ece70d1aaf489e14ac68d7606053def1d08ba` (parent
  `6aa3270fb71b68d3039e19569d2d89e74e294712`) passed manual-CI/generic
  preflight, all `960/960` aggregate gates, Core Release build and deterministic
  smoke, installed-reference V25 `Release|x64` build with `0 warnings / 0
  errors`, and offline WPF qualification on 2026-08-21. Adapter/Core
  ProductVersion matched
  `0.1.0-preview.10081+8b5ece70d1aaf489e14ac68d7606053def1d08ba`;
  adapter/Core SHA-256 values were
  `292BBFFF4903A4C596165C4EECAB7BCFED4BA177E01085343F94124A074D5AB9` /
  `BE49748E84C58C61B02CD6F096A74C9E760470AC4A1E78F14264E3C51FD4D27A`.
  The official NETLOAD/Ribbon/Palette step is `NO_RESULT`, not a product
  verdict: its `CodexSandboxOffline` host timed out after 120 seconds before
  `QS3DRUNTIMEPROBE`, emitted no runtime metadata, cleaned its launched process,
  and a post-run scan found zero BricsCAD processes. The earlier licensed PASS
  at `7820b72f894534443b53e315608f6a2228533248` is precursor-only and cannot
  qualify this candidate. All current-candidate native/interactive scenarios
  in this block therefore remain `PENDING_LOCAL`.
  The publication carrier was subsequently refreshed onto then-current `main`
  `e4bfb1fb59c61f03a47ae99a196dfeed3b2b1ad6` while retaining the prior remote
  carrier as second parent. That combined source tree passed `git diff --check`,
  all `960/960` aggregate gates, registration of all 838 runnable smoke classes,
  and the Core deterministic smoke suite with `ALL PASS`. This does not replace
  the exact licensed evidence boundary above; native/interactive acceptance
  remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`.
  A final remote audit observed `main`
  `ccf3c8e182415aab2dca5a7d7f363fb56d0bf97a`, ten commits beyond that
  validated carrier. The five changed semantic documentation/health
  source/test/preflight paths do not overlap this lane's two documentation
  paths. Git HTTPS and the managed GitHub write API were both unavailable, so
  the local carrier remains unpushed and must be refreshed by the next
  write-capable session before publication.
  Continuation host activation reached a responsive `BricsCAD Launcher` in the
  interactive Windows session and successfully dismissed it through the real
  focused 2D Drafting action. The test-owned process then remained responsive
  without creating a CAD/document HWND under the managed
  `CodexSandboxOffline` token. No QS3D DLL/command/DWG/customer assertion ran;
  all owned processes were removed and the final BricsCAD count was zero. This
  is a narrower post-launcher/pre-CAD-UI `NO_RESULT`, not product-failure or
  `LOCAL_PASS` evidence. Git HTTPS and GitHub write-API retries remained blocked,
  At the capability retry, remote `main` and the canonical remote branch were
  respectively `ccf3c8e182415aab2dca5a7d7f363fb56d0bf97a` and
  `7820b72f894534443b53e315608f6a2228533248` with no open PR. Before closeout,
  `main` advanced six more commits to
  `afff082096998fa404f08a5e29bcfd9fbc3830dd`; a future write-capable
  continuation must repin that then-current branch rather than publish this
  stale local carrier unchanged.
  On 2026-08-25 the unchanged canonical runner was invoked exactly once from a
  clean detached checkout of the post-#3985 source-ready SHA
  `d52a0065a3f63575885761bc59fab2c08a32f4a4`. Exact-SHA/manual-CI/generic
  source checks, all `1043/1043` aggregate gates, Core Release and deterministic
  smoke, the V25 `Release|x64` adapter build with `0 warnings / 0 errors`, and
  offline WPF qualification passed. The licensed NETLOAD/Ribbon/Palette row is
  `NO_RESULT`: one concurrently opened BricsCAD V25 process appeared before the
  hosted boundary, so the canonical dedicated-runner precondition stopped the
  attempt before launching its own host. The worker performed one blocker audit,
  did not touch that process, and did not rerun. DemandLoad and the installed
  loader hash were restored, the exact-SHA tree remained clean, and no source or
  runner file changed. This is environmental interference, not
  `SOURCE_FIX_REQUIRED`; post-#3985 Interchange continuation and the remaining
  current-candidate matrix remain `PENDING_LOCAL` under #3924/#72.
- Related docs: `docs/LOCAL-V25-QUALIFICATION.md`; `docs/EXISTING-PROJECT-MUTATION-CONTEXT.md`; `docs/INTERCHANGE-FIELD-PRECEDENCE.md`; `scripts/preflight-interchange-field-merge-execution.py`; `scripts/preflight-room-finish-project-lifecycle.py`; `scripts/preflight-grid-auto-number-project-lifecycle.py`; `scripts/preflight-room-auto-project-lifecycle.py`; `docs/ROOM-AUTO-PREVIEW-COMMIT-FRESHNESS.md`; `scripts/preflight-save-project-lifecycle.py`; `docs/LOCAL-AGENT-CONTINUE-ALL-2026-08-10.md`
- Updated: 2026-08-25

## LOCAL-002 — Curtain whole-command atomicity and native panels

- Priority: P0
- Status: PASS
- Area: Fault injection / rollback / modeless UI / generated replacement atomicity
- Evidence: Refer to the canonical LOCAL-002 content preserved in source history and current main. 

## LOCAL-003 — shared Level Z-chain in native geometry

- Priority: P0
- Status: IN_PROGRESS
- Area: Structural / Wall / Opening / Rebar vertical placement
- Evidence: PENDING_LOCAL

## LOCAL-004 — source reconcile native atomicity

- Priority: P0
- Status: IN_PROGRESS
- Area: Source Reconcile / Modify
- Evidence: PENDING_LOCAL

## LOCAL-005 — polygon Slab/Foundation native reinforcement

- Priority: P1
- Status: OPEN
- Area: Rebar 3D / Slab / Foundation
- Evidence: PENDING_LOCAL

## LOCAL-006 — native documentation objects

- Priority: P1
- Status: OPEN
- Area: Documentation / Tags / Tables / Sheets
- Evidence: PENDING_LOCAL

## LOCAL-007 — physical L/T/X wall junction output

- Priority: P1
- Status: OPEN
- Area: Wall junctions
- Evidence: PENDING_LOCAL

## LOCAL-008 — Direct Draw transient preview and repeated mode

- Priority: P1
- Status: OPEN
- Area: Direct Draw UX
- Evidence: PENDING_LOCAL

## LOCAL-009 — clean-machine install/sign/update qualification

- Priority: P1
- Status: OPEN
- Area: Packaging / Release / Trust
- Evidence: PENDING_LOCAL

## LOCAL-010 — large-model performance and UI matrix

- Priority: P2
- Status: OPEN
- Area: Performance / UI / HiDPI
- Evidence: PENDING_LOCAL

## LOCAL-011 — staged native rollback and post-commit UI isolation

- Priority: P1
- Status: PASS
- Area: Fault injection / rollback / modeless UI / generated replacement atomicity
- Evidence: `LOCAL_PASS`

## LOCAL-012 — Project Browser native workspace and CAD selection bridge

- Priority: P1
- Status: IN_PROGRESS
- Area: Project Browser / Workspace / modeless selection
- Evidence: PENDING_LOCAL

## LOCAL-013 — clean-room BRC public capability and eligible CAD quantity round-trip

- Priority: P0
- Status: IN_PROGRESS
- Area: BRC public capability / Recognition / eligible CAD B4D / ED2 / Excel Locate
- Evidence: PENDING_LOCAL

## LOCAL-014 — Plan-to-3D preview-to-commit and batch compensation

- Priority: P1
- Status: OPEN
- Area: `QS3DCONVERT2D` / `QS3DPLAN2WALLS` quick path, `QS3DCONVERT2DADV`, immediate native wall creation
- Evidence: PENDING_LOCAL

## LOCAL-015 — Construction Reference Search browser/modeless runtime

- Priority: P2
- Status: OPEN
- Area: `QS3DREFSEARCH` / modeless browser launcher
- Evidence: PENDING_LOCAL

## LOCAL-016 — BricsCAD V26 native authoring and dependent-output qualification

- Priority: P0
- Status: IN_PROGRESS
- Area: issue `#1462`; V26 `.NET 8` native authoring/semantic/generated-geometry lifecycle
- Evidence: PENDING_LOCAL

## LOCAL-017 — BricsCAD V26 native Slab POLYLINE qualification

- Priority: P0
- Status: PASS
- Area: issues `#80`, `#1462`, and bounded carrier `#3576`; V26 `.NET 8` native source-edit lifecycle
- Evidence: `LOCAL_PASS`

## LOCAL-018 — exact V26 LINE and repeated Direct Draw lifecycle

- Priority: P0
- Status: PASS
- Area: issues `#80`, `#1462`, completed `#3578`, and carrier `#3612`; V26 native editor/document/repeated lifecycle
- Evidence: `LOCAL_PASS`

## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate

- Priority: P0
- Status: PASS
- Area: issue `#3536`; `QS3DREVIEWEXPORT` / `QS3DREVIEWLOCATE`; BricsCAD V25 + V26 host bridge
- Evidence: `LOCAL_PASS`

## LOCAL-021 — Móng Bè workflow and Quantity Insight viewport highlight

- Priority: P0
- Status: IN_PROGRESS
- Area: issue `#4041`; BricsCAD V25 Móng Bè Add/Edit/native 3D/Quantity Insight viewport highlight
- Evidence: PENDING_LOCAL

## LOCAL-022 — Móng đơn placement/edit/save-reopen on V25/V26

- Priority: P0
- Status: BLOCKED
- Area: issue `#4034`; BricsCAD V25/V26 Móng đơn Add/placement/edit/regenerate/save-reopen
- Evidence: PENDING_LOCAL

## LOCAL-020 — Grid pair-owned intersection marker native lifecycle

- Priority: P1
- Status: OPEN
- Area: Grid / pair-owned native intersection markers (#3771)
- Evidence: PENDING_LOCAL

## LOCAL-023 — Beam formwork behavior matrix on preview.10228

- Priority: P1
- Status: PASS
- Area: issue `#4093`; BricsCAD V25 Beam formwork M1–M8 behavior matrix
- Evidence: `LOCAL_PASS`

## P1 — #3480 Quantity Review exact native BREP face highlight

- Priority: P1
- Status: OPEN
- Area: Quantity Review / formwork exact native BREP subentity highlight
- Evidence: `PENDING_LOCAL_AGENT`

## Close-out rule

Closing all `OPEN` P0/P1 items does not automatically mean the product is commercially released. Release publication still follows `CI_POLICY.md` and requires the owner's separate explicit release authorization. This inbox only records local engineering qualification truth.

## LOCAL-024 — #4352 ChatGPT MCP full-agent qualification

- Priority: P0
- Status: OPEN
- Area: issue #4352; ChatGPT MCP embedded full-CAD agent / Cloudflare onboarding
- Source/runner status: `SOURCE_READY / PENDING_LOCAL`
- Remote disposition: `PENDING_LOCAL / DO_NOT_RETRY_REMOTE`
- Exact-source rule: Run only from a clean checkout of the exact candidate SHA that is intended for merge/release qualification; hosted/static CI is source evidence only.
- Why local: Final acceptance requires licensed BricsCAD V25/V26, Windows desktop/UI input, a real Cloudflare account/tunnel/browser login, and ChatGPT MCP connectivity.
- Scenario: Follow `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md` and `docs/MCP-FULL-CAD-AGENT.md`; prove embedded loopback MCP, authenticated protocol/read-only probe, Cloudflare Named Tunnel + Quick fallback, ChatGPT discovery, direct CAD API and bounded command/UI fallback, timeout uncertainty/no-auto-retry, emergency stop/resume/cancel, save/reopen, shutdown and cleanup on the exact candidate SHA.
- Evidence required: exact candidate SHA; licensed BricsCAD V25/V26 host/plugin identity; sanitized MCP/Cloudflare/ChatGPT results; no bearer token, Cloudflare credential, private path, customer DWG, proprietary binary, raw Handle/ProjectId or unsanitized screenshot.
- Evidence: `PENDING_LOCAL`
- Related docs: `docs/agent-work-claims/issue-4352-chatgpt-mcp-session-handoff.md`; `docs/MCP-FULL-CAD-AGENT.md`; issue #4352.
- Updated: 2026-08-29

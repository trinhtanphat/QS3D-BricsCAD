# LOCAL-021 Móng Bè V25 runtime qualification claim

- Status: IN_PROGRESS / LOCAL_ONLY / OFFICIAL_10228_NO_RESULT / STARTUP_WPF_VIRTUALIZATION_EXCEPTION / SOURCE_FIX_ACTIVE_4173 / WAITING_DESCENDANT_PREVIEW
- Lane-Key: `issue-4041-raft-foundation-v25-runtime`
- Owner/session: historical `codex-01a03c3f`; offline harness handoff from `codex-01a03d89`; owner-directed continuation `interactive-01a04259`
- Issue: `#4041`
- Branch: `agent/codex/issue4041-raft-v25-runtime`
- Current coordination baseline: `origin/main@2601121d1ae6489c1e150f943bec45bc1f464ecf`
- Owner-repointed runtime candidate: release `v0.1.0-preview.10228` at exact source/tag commit `7dacdce17a6403d19681732ca7bad22cdb6f1499`
- Owner-repointed ZIP/adapter/Core SHA-256: `EC7385FC6085A838B94F84FC20B77E61E728952CC3A580FEC695031280FBC39E` / `010F729470B0644CD0ECBFF7395F4DCFAE39E81AA1B230C7219AC18C11C1340A` / `FF4801217E123275D1F2E92313C6FFF4A95DECCF3BBF656ACF5AB1FC580A1F86`
- Historical tested runtime candidate: release `v0.1.0-preview.10222` at `3de0221bbda90957b801796a1ec3a8d726dddcc3`
- Published V25 ZIP SHA-256: `9313921C076719E107DF658906F98B68E52E9F7F39CED0058F5F95727B5FD5BE`
- Historical adapter/Core SHA-256: `9F1418DA42C23E3987F18B7EC6562F84EFDAE939B162678A01223A0638A43A0E` / `61E35FE7C678EFF725B9432C4145CCD60DF077994E21CDC5851D23CF8F5C449E`
- Required next candidate: a newly published descendant preview containing the landed #4173 Workspace virtualization correction; official `.10228` has now been attempted and must not be rerun
- Superseded historical candidate: `c3282420909100332c03885e8acc7079d7fdb780` (must not be launched)
- Source feature: issue `#4023`, merged PR `#4026`, merge commit `e646712222797c297c6f7543882aa6c49c789615`

## Reserved scope

Run the bounded licensed BricsCAD V25 interaction matrix against exact published `.10228`: `Móng → Móng Bè → Add` must create and auto-select `Móng Bè-N`; Properties must expose only the dedicated Móng Bè schema (`Tên Family`, `Loại=Móng Bè`, `Tầng`, `Dày`, `Cách đặt`, `Cao độ đầu` with strict `bottom_level`/`top_level`, Display, Mark/Comment/WBS and material fields), not generic Foundation/`Bề dày`/`Offset đáy`. Create one disposable 4 m × 6 m × 0.8 m native raft, prove `19.2 m³`, exact bottom/top Z, thickness edit/regeneration, Quantity Insight/highlight, save and fresh-process cold reopen. Changing the semantic element, detail row, DWG or panel lifecycle must clear stale transients, and no action may persistently alter model color/material, semantic state or drawing bytes.

Raw drawings, sidecars, screenshots, Handles, ProjectIds and machine paths remain ignored under `artifacts/`; only sanitized aggregate evidence may enter Git.

## Exclusions and stop rules

- No opportunistic production source work before a runtime defect exists. If `.10228` exposes a reproducible defect, the owner explicitly requires the same task to capture sanitized evidence, fix the source on a branch/PR, obtain green CI, merge through the protected PR path under current authorization, and rerun only on a newly published descendant preview.
- No direct write to `main`; any conditional defect correction must use the protected task PR lifecycle.
- No GitHub Actions dispatch, rerun or cancellation; no package signing/release.
- V25 only; no V26 verdict is implied.
- LOCAL-012/#3936 is separately owned and is not taken over.
- P1/#3480 remains the broader exact-face qualification authority; this lane exercises only the exact-face rows needed by the owner's Móng Bè completion gate.
- Host contention or unavailable prerequisites produce `NO_RESULT`, never `LOCAL_PASS`.
- A general source defect is handed off with the smallest sanitized reproduction before any source change.
- `HOST_RELEASED` is justified only after Loader/DemandLoad/profile restoration and stable zero BricsCAD processes.

## Host queue

The exact `.10228` allocation completed as `NO_RESULT / STARTUP_WPF_VIRTUALIZATION_EXCEPTION_BEFORE_BASELINE`; #72 records protected cleanup and literal `HOST_RELEASED`. Do not launch the same binary again. After #4173 lands and a descendant preview is published, #4041 must obtain a new literal allocation naming that exact preview/source identity before touching BricsCAD, CurProfile, profile subtrees, DemandLoad, registry or QS3D UI layout.

## Runtime checkpoint

- Official `v0.1.0-preview.10228` / source `7dacdce17a6403d19681732ca7bad22cdb6f1499` passed package integrity and matching ProductVersion/source identity. Licensed BricsCAD V25 reached script-started, product-NETLOAD-complete and probe-NETLOAD-complete sentinels.
- During `QS3D`, before the baseline marker and before Workspace/Móng Bè/geometry/QTO/highlight assertions, the owned host exited with an unhandled `System.InvalidOperationException`. Windows .NET Runtime event 1026 begins at `VirtualizingStackPanel.SetVirtualizationState`, `GetOwners`, and `MeasureOverrideImpl`; WER classified `CLR20r3 / PresentationFramework`. Exact disposition is `NO_RESULT / STARTUP_WPF_VIRTUALIZATION_EXCEPTION_BEFORE_BASELINE`, not a Móng Bè product verdict.
- Dump/crash-report SHA-256 values are `D93279A8E48B47EFD7CBC59515106AFC4BA5A674480FD86387A9B6C95D38DE08` / `7D2613185DFDD0C4EE4528D2F443DFEDBE7CA45209E229CD6DB65C767A219571`; raw files remain ignored/private. Protected cleanup passed: the owned PID exited, BricsCAD stayed zero for ten samples, the source profile and profile inventory were restored, the nonce was removed, and the exact DemandLoad/UI-layout fingerprints were restored.
- Issue #4173 owns the non-overlapping source correction. Its current implementation locally passes the focused ModelTree virtualization guard; its latest observed CI failure is an outdated-baseline LOCAL-AGENT-INBOX guard, so the canonical source owner must reconcile current main and obtain normal green branch/PR CI before landing. This runtime lane made no production-source write.

- Release `v0.1.0-preview.10222` / exact source `3de0221bbda90957b801796a1ec3a8d726dddcc3` launched exactly one owned licensed BricsCAD V25 process.
- The cell timed out waiting for the exact baseline marker before any registered feature stage. Exact disposition: `NO_RESULT / BASELINE_MARKER_TIMEOUT`; `stages=[]`; `one_atomic_session=false`. Workspace, `+ Add`, property form, boundary authoring, QTO and yellow/red viewport behavior are all `NOT_RUN`, not failed and not passed.
- Cleanup restored the pre-existing CurProfile pointer and active profile content, returned the profile inventory to `88` with the owned nonce absent, restored the installed AppData Loader with `LoadCtrls=2`, preserved the UI-layout SHA-256 `AE6B1D34BC8F1966D56449D8208C3E8FBB2814BF38742CD3CE0C4235944E2400`, exited the owned process and passed an independent `10/10` zero-process audit. #72 accepted that boundary before reclaiming the host.
- Offline review identified a visible `+ Add` route mismatch and stale source guard in the then-current source lane. Issue `#4078` owns the ordinary source correction; this local lane does not patch it.
- The refreshed `#4078` source-lane head `87e04938498ff5e00c6fe9d46cd7d2480642501d` adds a narrow routed-event bridge for the rendered `+ Add` label and updates the cross-file preflight to bind that visible route, primary dedicated Móng Bè property rendering and one real Level binding. Fresh offline verification on that exact head passed the focused preflight, Core `Release|x64` build and V25 `Release|x64` build with `0` warnings / `0` errors. This is a source-ready checkpoint only; it is not merged/released runtime evidence.
- The ignored retry harness now uses a `900 s` startup bound, explicit ProductVersion/source-SHA binding, owned-process early-exit detection, optional strict post-#4078 `Cách đặt` / `Cao độ đầu` Level-binding assertions and `60/60` registered-stage parity. Its rebuilt probe passed with `0` warnings / `0` errors and SHA-256 `47FD4342019835645549BD99EF6A3DE50F0239C5CC43EDD04617B7AB6D4EC98B`. This is preparation only and is not runtime evidence.
- Current-main comparison for `.10228`: `7dacdce17a6403d19681732ca7bad22cdb6f1499` is an ancestor of `origin/main@2601121d1ae6489c1e150f943bec45bc1f464ecf`; no later production/source or focused-guard delta exists in the Móng Bè Add/property/Level/native-geometry/Quantity Insight acceptance surfaces. This comparison is eligibility evidence only, not runtime evidence.

## Validation plan

1. Reconcile the canonical carrier with current main, freeze exact `.10228` package/source/hash identity, and publish this registration before host access.
2. Obtain a fresh literal #72 allocation naming #4041 and exact `.10228`; then reconfirm package identity, matching installed V25 host, restored pre-state and exclusive PID state.
3. Run the owner acceptance matrix: Add/auto-selection/dedicated schema, 4×6×0.8 m native raft, 19.2 m³, Z placement, thickness regeneration, Quantity Insight/highlight, save and fresh-process cold reopen.
4. If runtime exposes a defect, retain the exact failure verdict and follow the owner-directed source-fix/green-PR/protected-merge/new-preview/rerun chain; never relabel the failing binary.
5. Clean scoped residue, restore host state, verify stable zero processes, then publish exactly one allowed verdict: `LOCAL_PASS`, `RUNTIME_FAIL`, `NO_RESULT`, or `BLOCKED`.

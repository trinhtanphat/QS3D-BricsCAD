# LOCAL-012 exact-SHA runtime checkpoint

- Carrier issue: `#3936`
- Lane-Key: `issue-3936`
- Source-fix issues: `#4027`, `#4032`
- Canonical branch: `agent/interactive-20260825-01a03821/issue-3936-local012-workspace-ui`
- Exact tested source SHA: `fc979ff465873ad3c32507064926292d9f10b3cb`
- Exact V25 adapter SHA-256: `DA5D2A92F0D65C02449787576D03D4AE512282A5E3A17A78E95EDBDD7DDC3733`
- Current-source drift checked through: `origin/main@ee4ef5553dcaa2fc801d7e0567cd9f2409eff9e5`
- Host: BricsCAD Ultimate V25.2.10, Windows x64, CLR 4.0.30319.42000
- Outcome: `PARTIAL_LOCAL_EVIDENCE / SOURCE_FIX_REQUIRED / LOCAL-012 IN_PROGRESS`

This is a sanitized checkpoint for the licensed/modeless campaign. It supersedes neither the historical pre-runtime `NO_RESULT` attempt nor the remaining LOCAL-012 matrix. It is not a full `LOCAL_PASS`, customer-release qualification, or permission to merge.

## Exact-load and prerequisite gates

The pinned qualification first stopped fail-closed when its runtime identity check detected that the installed DemandLoad entry had loaded a different installed QS3D DLL instead of the exact candidate. No runtime PASS is claimed from that stopped attempt. With a zero-process host, a clean explicit `NETLOAD` of the exact candidate then passed the hosted runtime marker. A separate temporary DemandLoad rewire to that same exact candidate also passed. The installed Loader and `LoadCtrls=2` were restored after the cell.

| Gate | Result |
| --- | --- |
| Exact SHA / clean tree, manual-CI policy, generic source precheck | `PASS` |
| Aggregate feature preflights (1,036 discovered gates) | `PASS` |
| Core Release build | `PASS`, 0 warnings / 0 errors |
| Core deterministic smoke suite | `ALL PASS` |
| V25 adapter `Release|x64` build | `PASS`, 0 warnings / 0 errors |
| Offline WPF Workspace / RightPanel smoke | `PASS` |
| Clean exact-DLL `NETLOAD` runtime identity | `PASS` |
| Temporary exact-DLL DemandLoad runtime identity | `PASS` |
| Installed DemandLoad state after the cell | restored Loader, `LoadCtrls=2` |

## Licensed selection and property-scope observations

All rows below used real native PICKFIRST selection and the production `QS3DINSPECT` path over a disposable synthetic drawing/project.

| Scenario | Runtime result |
| --- | --- |
| Exactly one live semantic object | `PASS`: Workspace showed `1 chọn`, resolved the exact semantic context, and entered `Đối tượng / Instance` |
| Empty native selection | `PASS`: fell back to `Family / Type` |
| One non-semantic native entity | `PASS`: fell back to `Family / Type` |
| One semantic plus one non-semantic entity | `PASS`: fell back to `Family / Type` |
| Two semantic objects in the same live Family | `FAIL / SOURCE_FIX_REQUIRED #4027`: single-instance context cleared, but the scope combo incorrectly remained `Đối tượng / Instance` |
| Same-Family multi-selection presentation | `PASS`: the matching Family row remained selected/visible and showed `2 cấu kiện` |

The serialized semantic element subtree remained byte-equivalent across these presentation transitions. A no-change control showed that `QS3DSAVE` itself increments project `ChangeVersion` by one, so save-version deltas were not attributed to selection presentation.

Issue `#4027` received the ordinary source correction through PR `#4033`, merged as `dd510bad3e56cae241d176764064db6b1c5d8fe6`. That later source does not retroactively change the failed result on the tested candidate; the multi-semantic row remains pending an exact-SHA licensed rerun on a descendant containing the fix. This local lane made no source patch.

## Hosted Project Browser integration boundary

The exact candidate and the current-source drift checkpoint both contain the deterministic Core `ProjectBrowserWorkspaceCoordinator`, state store, query/grouping, selection-reveal and virtualization/paging planners. Neither revision contains any `ProjectBrowser` reference under the production V25 or V26 hosted adapters. In current source, coordinator usages are limited to its Core declaration and Core smoke tests.

The real loaded V25 Workspace matched that source boundary: it exposed the existing model tree, Family search/list, property search/list and inspector, but no Project Browser query/grouping/page controls or bound visible-row surface. Therefore Browser -> CAD, CAD -> Browser reveal/expand, filter/group/query, paging/large-node and persisted browser-presentation rows are `NOT RUN / SOURCE_FIX_REQUIRED #4032` on this exact candidate, not local runtime failures of the Core planners. Issue `#4032` owns the missing production adapter; this local lane made no source patch.

## Foundation subtype and native Solid3D cell

- The real modeless Workspace selected `Móng > Móng Bè`.
- Add exposed exactly `Tham số` and `Solid3D`.
- `Tham số` created and selected `Móng Bè-1`.
- Two semantic raft elements were created from two distinct native closed polylines and assigned to the same live Family.
- Add -> `Solid3D` dispatched the native `QS3DBUILD3D` path over those two semantics.
- Two native outputs resolved as `AcDb3dSolid` before and after same-host close/reopen.
- The sidecar hash was unchanged by close/reopen; Family count remained 6, element count remained 2, and the newly created Solid3D Family occurred exactly once.
- The new Solid3D Family was `Móng Bè-1-2` because this reopened atomic cell had not reselected the `Móng > Móng Bè` subtype node. This proves native dispatch and bounded reopen stability only; exact subtype naming/filtering for the expected `Móng Bè-N` sequence remains unclaimed.
- A stale dead-process sidecar lock from the prior launcher did not block reopening the valid project; the next save replaced lock ownership with the live host process.

## Remaining LOCAL-012 rows

The following rows remain `PENDING_LOCAL` and must not be inferred from the bounded evidence above:

- after a source-ready `#4032` candidate, Browser -> CAD selection, CAD -> Browser reveal/expand, zoom, stale/deleted/ambiguous semantic IDs, active-DWG affinity, filter/group/query and bounded paging/virtualization;
- on an exact descendant containing merged fix `dd510bad3e56cae241d176764064db6b1c5d8fe6`, rerun same-Family multi-semantic selection and prove the Workspace stays out of Instance scope;
- missing/deleted Family fallback and proof that a previously selected Instance cannot be mutated;
- live-Family Instance Reset after another modeless writer changes the Family value, removed-property refusal, failed post-commit UI refresh, and post-`QS3DRELOAD` stale-row refusal;
- unavailable-project activation/recovery, modeless continuity across DWG switches, and stale callback refusal;
- presentation-only browser-state `ChangeVersion` and quantity/regeneration-preview invariants after the hosted adapter exists;
- full save/reopen and cache-replacement matrix;
- unsupported non-native Solid3D refusal before project/bootstrap/audit/version mutation;
- exact Foundation subtype-family filtering/naming after reselecting `Móng > Móng Bè`;
- dedicated plugin-owned Properties authority, palette recreation/dock/size persistence, Unicode, narrow/normal/wide layouts, and 100/125/150/200% DPI.

## Interrupted R1 live-Family Reset cell

A later disposable R1 attempt reached only the exact hosted runtime marker before the agent turn stopped. No Instance override, Workspace detach, Family mutation, Reset click, save, reopen, or Reset assertion was executed. The disposable DWG and sidecar remained byte-identical to their pre-launch hashes. This cell is therefore `NO_RESULT / INTERRUPTED_BEFORE_ASSERTION`; it is neither a Reset PASS nor a product failure and must be rerun from a fresh disposable copy after explicit shared-host release.

The interrupted cell's raw marker, input hashes and unusable foreground-desktop screenshot remain ignored. A cleanup audit at `2026-08-26 11:11:12 +07:00` found zero `bricscad.exe` processes, the installed AppData Loader restored, `LoadCtrls=2`, and 487 registered command values.

## Host cleanup and evidence handling

Raw UIAutomation snapshots, native identifiers, disposable fixture paths, registry backups, scripts, and screenshots remain under ignored local `artifacts/`. This committed checkpoint retains only allow-listed product identity and sanitized assertions; it contains no customer drawing, project identifier, raw CAD handle, or machine-specific fixture path.

After gracefully closing the completed atomic cell, the owned helper was removed, the installed DemandLoad Loader and `LoadCtrls=2` were restored, and `bricscad.exe` count was verified as exactly zero at `2026-08-26 08:31:01 +07:00`. The shared licensed host was then yielded to issue `#72`.

Profile evidence correction: those earlier cells did not snapshot the pre-launch `CurProfile` value, so profile-pointer continuity is unproven and this checkpoint makes no claim that it remained unchanged. The later observed pointer was `QS3D-V25-TEST`; LOCAL-012 did not change it during the correction audit. Before any future launch, the runner must snapshot the pointer and profile inventory, use only an owned nonce profile, restore the pointer exactly after graceful quit, and remove only that owned nonce profile. LOCAL-012 must continue to obtain an explicit shared-host release before every later BricsCAD launch or attach.

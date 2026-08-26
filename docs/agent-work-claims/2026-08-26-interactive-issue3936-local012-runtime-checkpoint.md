# LOCAL-012 exact-SHA runtime checkpoint

- Carrier issue: `#3936`
- Lane-Key: `issue-3936`
- Source-fix issue: `#4027`
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

Issue `#4027` owns the ordinary source correction. The relevant Workspace/selection paths were unchanged from the tested SHA through the drift-check SHA above, and this local lane made no source patch.

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

- Browser -> CAD selection, zoom/reveal, stale/deleted/ambiguous semantic IDs, and active-DWG affinity;
- missing/deleted Family fallback and proof that a previously selected Instance cannot be mutated;
- live-Family Instance Reset after another modeless writer changes the Family value, removed-property refusal, failed post-commit UI refresh, and post-`QS3DRELOAD` stale-row refusal;
- unavailable-project activation/recovery, modeless continuity across DWG switches, and stale callback refusal;
- browser filtering, grouping, large-node paging/virtualization, cancellation, and presentation-only `ChangeVersion` invariants;
- full save/reopen and cache-replacement matrix;
- unsupported non-native Solid3D refusal before project/bootstrap/audit/version mutation;
- exact Foundation subtype-family filtering/naming after reselecting `Móng > Móng Bè`;
- dedicated plugin-owned Properties authority, palette recreation/dock/size persistence, Unicode, narrow/normal/wide layouts, and 100/125/150/200% DPI.

## Host cleanup and evidence handling

Raw UIAutomation snapshots, native identifiers, disposable fixture paths, registry backups, scripts, and screenshots remain under ignored local `artifacts/`. This committed checkpoint retains only allow-listed product identity and sanitized assertions; it contains no customer drawing, project identifier, raw CAD handle, or machine-specific fixture path.

After gracefully closing the current atomic cell, the owned helper was removed, no profile change remained, the installed DemandLoad Loader and `LoadCtrls=2` were restored, and `bricscad.exe` count was verified as exactly zero at `2026-08-26 08:31:01 +07:00`. The shared licensed host was then yielded to issue `#72`; LOCAL-012 must not start another BricsCAD session until that canonical runtime smoke reports completion and releases the host.

# LOCAL-021 Móng Bè V25 runtime qualification claim

- Status: IN_PROGRESS / LOCAL_ONLY / OFFICIAL_10228_NO_RESULT / SOURCE_FIX_4173_LANDED / DESCENDANT_10229_FROZEN / WAITING_FRESH_HOST_HANDOFF
- Lane-Key: `issue-4041-raft-foundation-v25-runtime`
- Owner/session: historical `codex-01a03c3f`; offline harness handoff from `codex-01a03d89`; owner-directed continuation `interactive-01a04259`
- Issue: `#4041`
- Branch: `agent/codex/issue4041-raft-v25-runtime`
- Current coordination baseline: `origin/main@a7b7dfb456654509c9eccf33f854350a61d9e59d`
- Exact post-fix runtime candidate: release `v0.1.0-preview.10229` at exact source/tag commit `a7b7dfb456654509c9eccf33f854350a61d9e59d`
- ProductVersion: `0.1.0-preview.10229`
- Official ZIP/adapter/Core SHA-256: `B7FE681B7958908B6E0E84C76CC6B62E8DFE97B42EAD3F80A4E01864D0F670F1` / `E1C97BA45DD4E674FC182CE387004A63D624D3233D592C1367A9C7861EF41FDB` / `31E7E778783380232FA4084EAEC77399A281BC0822C5DC78E214395FCD49D03E`
- Cloud release workflow: `33070762927` / run #445 — `SUCCESS`
- Historical failed runtime candidate: release `v0.1.0-preview.10228` at exact source/tag commit `7dacdce17a6403d19681732ca7bad22cdb6f1499`
- Historical `.10228` ZIP/adapter/Core SHA-256: `EC7385FC6085A838B94F84FC20B77E61E728952CC3A580FEC695031280FBC39E` / `010F729470B0644CD0ECBFF7395F4DCFAE39E81AA1B230C7219AC18C11C1340A` / `FF4801217E123275D1F2E92313C6FFF4A95DECCF3BBF656ACF5AB1FC580A1F86`
- Older tested runtime candidate: release `v0.1.0-preview.10222` at `3de0221bbda90957b801796a1ec3a8d726dddcc3`
- Superseded historical candidate: `c3282420909100332c03885e8acc7079d7fdb780` (must not be launched)
- Source feature: issue `#4023`, merged PR `#4026`, merge commit `e646712222797c297c6f7543882aa6c49c789615`
- Startup correction: issue `#4173`, protected PR `#4179`, landed `main@a7b7dfb456654509c9eccf33f854350a61d9e59d`

## Reserved scope

Run the bounded licensed BricsCAD V25 interaction matrix against exact published `.10229`: `Móng → Móng Bè → Add` must create and auto-select `Móng Bè-N`; Properties must expose only the dedicated Móng Bè schema (`Tên Family`, `Loại=Móng Bè`, `Tầng`, `Dày`, `Cách đặt`, `Cao độ đầu` with strict `bottom_level`/`top_level`, Display, Mark/Comment/WBS and material fields), not generic Foundation/`Bề dày`/`Offset đáy`. Create one disposable 4 m × 6 m × 0.8 m native raft, prove `19.2 m³`, exact bottom/top Z, thickness edit/regeneration, Quantity Insight/highlight, save and fresh-process cold reopen. Changing the semantic element, detail row, DWG or panel lifecycle must clear stale transients, and no action may persistently alter model color/material, semantic state or drawing bytes.

Raw drawings, sidecars, screenshots, Handles, ProjectIds and machine paths remain ignored under `artifacts/`; only sanitized aggregate evidence may enter Git.

## Exclusions and stop rules

- No direct production-source patch from this LOCAL_ONLY carrier. Any new reproducible production defect becomes a separate bounded source handoff.
- No direct write to `main`; source corrections use the protected PR lifecycle.
- No package signing claim and no hosted/cloud result may be promoted to `LOCAL_PASS`.
- V25 only; no V26 verdict is implied.
- LOCAL-012/#3936 and LOCAL-022/#4034 are separately owned and are not taken over.
- P1/#3480 remains the broader exact-face qualification authority; this lane exercises only the exact-face rows needed by the owner's Móng Bè completion gate.
- Host contention or unavailable prerequisites produce `NO_RESULT`, never `LOCAL_PASS`.
- `HOST_RELEASED` is justified only after Loader/DemandLoad/profile restoration and stable zero BricsCAD processes.

## Source-fix checkpoint

The official `.10228` attempt ended `NO_RESULT / STARTUP_WPF_VIRTUALIZATION_EXCEPTION_BEFORE_BASELINE`. Product/probe NETLOAD completed, then the owned V25 process failed during `QS3D` before the Workspace baseline marker with `System.InvalidOperationException`; the observed stack begins at `VirtualizingStackPanel.SetVirtualizationState` → `GetOwners` → `MeasureOverrideImpl`. No Móng Bè Add/property/native-geometry/QTO/highlight acceptance assertion ran on that binary.

Issue #4173 isolated the startup defect. Protected PR #4179 landed the Workspace ModelTree virtualization containment at exact `main@a7b7dfb456654509c9eccf33f854350a61d9e59d`; protected PR CI run `33069632255` passed `preflight` and `core`, including deterministic smoke and the V25 plugin build. This is source/build evidence only, not licensed runtime evidence.

## Exact descendant freeze

The first published post-fix descendant is `v0.1.0-preview.10229` at exact source `a7b7dfb456654509c9eccf33f854350a61d9e59d`. Cloud release workflow `33070762927` completed `SUCCESS` after all discovered source guards, deterministic Core smoke, trusted V25 compile-reference validation, V25 plugin build, package build, release-tag/package-version binding, checksum, package-integrity, upload and prerelease publication.

Frozen identity:
- release: `v0.1.0-preview.10229`
- ProductVersion: `0.1.0-preview.10229`
- exact source/tag: `a7b7dfb456654509c9eccf33f854350a61d9e59d`
- official V25 ZIP SHA-256: `B7FE681B7958908B6E0E84C76CC6B62E8DFE97B42EAD3F80A4E01864D0F670F1`
- packaged V25 adapter SHA-256: `E1C97BA45DD4E674FC182CE387004A63D624D3233D592C1367A9C7861EF41FDB`
- packaged Core SHA-256: `31E7E778783380232FA4084EAEC77399A281BC0822C5DC78E214395FCD49D03E`

Do not rerun `.10228`, do not silently advance to a later preview, and do not reuse any consumed `.10228` host allocation.

## Host queue

The previous `.10228` allocation completed with protected cleanup and literal `HOST_RELEASED`. LOCAL-021 is now offline-frozen on exact `.10229@a7b7dfb...` and requires a **fresh literal named #72 allocation** for `#4041 / LOCAL-021 / scope V25` before touching BricsCAD, CurProfile, profile subtrees, DemandLoad, registry or QS3D UI layout.

## Validation plan

1. Freeze and verify exact `.10229` package/ProductVersion/source/binary identity; do not repin for unrelated main churn.
2. Obtain a fresh literal #72 `HOST_RELEASED` allocation naming #4041 / LOCAL-021 and exact `.10229@a7b7dfb...`; fail closed on any foreign host contention.
3. Run the owner acceptance matrix: Workspace baseline, Add/auto-selection/dedicated schema, 4×6×0.8 m native raft, 19.2 m³, Z placement, thickness regeneration, Quantity Insight/highlight, save and fresh-process cold reopen.
4. If runtime exposes a new reproducible source defect, publish sanitized `SOURCE_FIX_REQUIRED` and stop the affected matrix; do not patch production source from this local carrier.
5. Clean scoped residue, restore Loader/DemandLoad/profile/UI state, verify stable zero owned processes, then publish exactly one allowed verdict: `LOCAL_PASS`, `RUNTIME_FAIL`, `NO_RESULT`, or `BLOCKED`.

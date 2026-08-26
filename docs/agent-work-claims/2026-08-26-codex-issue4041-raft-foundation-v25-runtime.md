# LOCAL-021 Móng Bè V25 runtime qualification claim

- Status: ACTIVE / LOCAL_ONLY
- Lane-Key: `issue-4041-raft-foundation-v25-runtime`
- Owner/session: `codex-01a03d89` (successor to stopped pre-launch session `codex-01a03c3f`)
- Issue: `#4041`
- Branch: `agent/codex/issue4041-raft-v25-runtime`
- Current coordination baseline: `origin/main@3d13f9f84a33819164beffdc2a90673f31c215c0`
- Exact runtime candidate: release `v0.1.0-preview.10222` at `3de0221bbda90957b801796a1ec3a8d726dddcc3`
- Published V25 ZIP SHA-256: `9313921C076719E107DF658906F98B68E52E9F7F39CED0058F5F95727B5FD5BE`
- Superseded historical candidate: `c3282420909100332c03885e8acc7079d7fdb780` (must not be launched)
- Source feature: issue `#4023`, merged PR `#4026`, merge commit `e646712222797c297c6f7543882aa6c49c789615`

## Reserved scope

Run the bounded licensed BricsCAD V25 interaction matrix against the exact published `10222` release: create one disposable Móng Bè target with a validated intersecting counterpart, open `Định Lượng → Tính khối lượng → Xem diễn giải`, click `V gộp`, `Trừ giao`, `V còn`, every exposed `S gộp`/`S còn` exact-face pair and total `Ván khuôn`, then directly verify yellow included/gross/net geometry and red deducted intersection geometry. Changing the semantic element, detail row, DWG or panel lifecycle must clear stale transients, and no action may persistently alter model color/material, semantic state or drawing bytes.

Raw drawings, sidecars, screenshots, Handles, ProjectIds and machine paths remain ignored under `artifacts/`; only sanitized aggregate evidence may enter Git.

## Exclusions and stop rules

- No production source implementation or ordinary source bug fix in this local lane.
- No write or merge to `main`.
- No GitHub Actions dispatch, rerun or cancellation; no package signing/release.
- V25 only; no V26 verdict is implied.
- LOCAL-012/#3936 is separately owned and is not taken over.
- P1/#3480 remains the broader exact-face qualification authority; this lane exercises only the exact-face rows needed by the owner's Móng Bè completion gate.
- Host contention or unavailable prerequisites produce `NO_RESULT`, never `LOCAL_PASS`.
- A general source defect is handed off with the smallest sanitized reproduction before any source change.
- `HOST_RELEASED` is justified only after Loader/DemandLoad/profile restoration and stable zero BricsCAD processes.

## Host queue

Parent task `01a03b74-6e51-7380-81ff-fff748925c1e` / issue `#72` explicitly still owns the shared V25 slot and has **not** released it. The stopped predecessor #4041 task never launched BricsCAD, but that does not imply host availability; `bricscad.exe=0` alone is not `HOST_RELEASED`. This successor remains offline-only and must not launch/attach BricsCAD or read/write CurProfile, profile subtrees, DemandLoad, registry or QS3D UI layout until #72 sends an explicit `HOST_RELEASED` message with restored pre-state and stable zero-process evidence.

## Validation plan

1. Push this registration before starting BricsCAD.
2. While queued, pin the detached worktree and private ignored harness to exact release SHA `3de0221bbda90957b801796a1ec3a8d726dddcc3`; run only source/build/AST/privacy checks that cannot launch or alter BricsCAD, DemandLoad, profile, registry or QS3D UI layout.
3. After explicit #72 `HOST_RELEASED`, reconfirm the release SHA/asset digest, matching installed V25 host, restored pre-state and exclusive PID state.
4. Run exactly one bounded interactive cell and record the loaded assembly identity plus sanitized `V gộp`/`Trừ giao`/`V còn`, per-face `S gộp`/`S còn`, total formwork and yellow/red/cleanup observations.
5. Clean scoped residue, restore host state, verify stable zero processes, then publish the exact-SHA disposition by PR.

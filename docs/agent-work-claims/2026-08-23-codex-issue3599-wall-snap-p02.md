# LOCAL-007 P02 — Wall Snap lifecycle licensed qualification

Status: `LOCAL_PARTIAL / PENDING_REMOTE`

Parent: #73 / LOCAL-007

Licensed qualification issue: #3599

Remote defect issues: #3600 and #3601

Lane-Key: `issue-3599`

Qualification branch: `agent/codex/issue3599-v25-wall-snap-p02`

Exact runtime baseline: `b6cd726ef76c5fc0c9c044d5823b341004c912cd`

## Boundary

This local-only P02 cell exercises the production `QS3DWALLSNAPPREVIEW` and `QS3DWALLSNAPAPPLY` selection, cold-cache project binding, preview publication and prompt-drift boundaries in licensed BricsCAD V25. It uses production `QS3DBUILD3D` to establish two semantic walls with two owned generated solids before the intended successful Preview-to-Apply path. It does not change production source and does not qualify physical junction materialization, whole-group replacement, save/reopen, Undo/Redo or the broader LOCAL-007 matrix.

The run used only disposable copies of the repository-public generated sample. The private probe, runner, raw command logs, drawing copies, sidecars and identifiers remain Git-ignored under `artifacts/`; no customer/private drawing, raw Handle, ProjectId, ElementId, nonce, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed result

`LOCAL_PARTIAL / PENDING_REMOTE` was recorded on the exact clean pushed runtime candidate above, which still equalled refreshed `origin/main` before this claim was written. The host was licensed BricsCAD V25.2.10 x64. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `F3512CE43A497F075AE677CFC328FB6C7E3DE153F3CE8DFDD987A6715CB6ED2F` and `5AF0A2D5B6A2960E425512E9EC932AA5F33288AE0821A4CA4B4F449DB843DC63`. Exact-source PDB checks bound both tested assemblies to the candidate SHA.

The four-session licensed run proved:

- exact-PID ESC from both Preview and Apply interactive selection returned the document to idle with no project cache, sidecar, preview metadata, audit, source geometry or generated ownership mutation;
- removing the sidecar after Preview selection started failed closed without creating a replacement/default project, caching project state, writing preview metadata/audit or changing source geometry;
- the intended cold-cache path preserved canonical same-ProjectId continuity, consumed two production-built semantic wall sources with two owned generated solids, and produced exactly one independently expected endpoint edit;
- Preview nevertheless advanced `ChangeVersion` by `8` while production reserved headroom for `2`, leaving `WallJunctionSnapPreviewChangeVersion` one revision behind the final project revision; unchanged immediate Apply then rejected its own fresh Preview as stale. Preview recorded exactly one audit event while leaving CAD/generated ownership and sidecar bytes unchanged, and the rejected Apply itself remained fail-closed with no CAD, semantic, audit, preview-metadata, generated-ownership or sidecar mutation. Remote source issue #3600 owns this defect;
- replacing the sidecar atomically with another valid project after Apply selection started was correctly rejected without moving CAD, mutating either project generation, changing audit/preview metadata or touching generated ownership; however, the replacement project remained cached after refusal. Remote source issue #3601 owns this cache-lifecycle defect;
- each BricsCAD session closed the disposable document without saving, quit gracefully with numeric exit code `0`, and left zero BricsCAD processes.

These defects block successful Preview-to-Apply, generated-dependent invalidation and resulting `LengthM` qualification. They also block the required no-cache result for the replaced-project drift cell. No local production-source fix was made because both defects are repository-safe work assigned to remote agents; #3599 requires an exact-SHA licensed rerun after their fixes are pushed.

## Repository validation and safety

- `QS3D.BricsCAD.V25` Release|x64 build against the licensed V25 installation: `0 warnings / 0 errors`.
- Private probe Release build: `0 warnings / 0 errors`.
- `QS3D.Core.SmokeTests` direct DLL execution through the repository portable .NET runtime: `ALL PASS`, exit code `0`.
- `scripts/preflight-wall-junctions.py`, `scripts/preflight-wall-junction-selection.py`, `scripts/preflight-wall-junction-ownership.py`, `scripts/preflight-wall-snap-audit-owned-revision.py`, `scripts/preflight-wall-snap-source-metrics.py`, `scripts/preflight-wall-snap-review.py`, `scripts/preflight-wall-snap-atomicity.py` and `scripts/preflight-wall-snap-project-lifecycle.py`: PASS. These static guards did not detect either licensed runtime defect above.
- Public fixture SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; the user's drawing was never opened and its bytes remained unchanged.
- Fixed per-user V25 DemandLoad path/bytes and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Raw evidence and disposable data are excluded from the tracked diff; the root worktree's pre-existing crash artifacts were not touched.

## Remaining scope

Issue #3599 stays open. After #3600 and #3601 land on a clean pushed candidate, rerun canonical Preview-to-Apply, verify generated-dependent invalidation and semantic `LengthM`, and repeat the replacement-project no-cache cell. Parent #73 and overall LOCAL-007 remain open for whole-`GroupToken` physical junction materialization/replacement, dedicated owner/dependency/fingerprint persistence, the full L/T/X/Multi ownership matrix, rebuild/cleanup, cross-DWG and host-retention checks, save/reopen and Undo/Redo.

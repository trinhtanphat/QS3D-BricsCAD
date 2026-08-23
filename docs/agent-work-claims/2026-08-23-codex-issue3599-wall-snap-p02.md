# LOCAL-007 P02 — Wall Snap lifecycle licensed qualification

Status: `COMPLETED / LOCAL_PASS`

Parent: #73 / LOCAL-007

Licensed qualification issue: #3599

Remote defect issues: #3600 and #3601

Lane-Key: `issue-3599`

Qualification branch: `agent/codex/issue3599-v25-wall-snap-p02-rerun`

Historical blocker evidence PR: #3602

Exact runtime baseline: `ddbe528157a29656647ee7da0fcb8b441f512016`

## Boundary

This local-only P02 cell exercises the production `QS3DWALLSNAPPREVIEW` and `QS3DWALLSNAPAPPLY` selection, cold-cache project binding, preview publication and prompt-drift boundaries in licensed BricsCAD V25. It uses production `QS3DBUILD3D` to establish two semantic walls with two owned generated solids before the intended successful Preview-to-Apply path. It does not change production source and does not qualify physical junction materialization, whole-group replacement, save/reopen, Undo/Redo or the broader LOCAL-007 matrix.

The run used only disposable copies of the repository-public generated sample. The private probe, runner, raw command logs, drawing copies, sidecars and identifiers remain Git-ignored under `artifacts/`; no customer/private drawing, raw Handle, ProjectId, ElementId, nonce, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed result

`LOCAL_PASS / BOUNDED_P02_WALL_SNAP` was recorded on the exact clean pushed candidate above, equal to refreshed `origin/main` and containing both #3600 and #3601. The host was licensed BricsCAD V25.2.10 x64. Plugin/Core ProductVersion was `0.1.0-preview.10081`; their SHA-256 values were `A0D27AE55A17C53A201F7C92A3292F4537937403AE3AEEF5F914CEA680A53B01` and `B515E42CE60A50BE61C719BB40D57C864AF58F4AFFB5665DD1DEB66D7B75966C`. Exact-source PDB checks bound both tested assemblies to the candidate SHA.

The four-session licensed run proved:

- exact-PID ESC from both Preview and Apply interactive selection returned the document to idle with no project cache, sidecar, preview metadata, audit, source geometry or generated ownership mutation;
- removing the sidecar after Preview selection started failed closed without creating a replacement/default project, caching project state, writing preview metadata/audit or changing source geometry;
- the cold-cache path preserved canonical same-ProjectId continuity, consumed two production-built semantic wall sources with two owned generated solids, and produced exactly one independently expected endpoint edit;
- Preview advanced `ChangeVersion` by exactly `2`, recorded exactly one audit event and stamped `WallJunctionSnapPreviewChangeVersion` to the final project revision while leaving CAD, generated ownership and sidecar bytes unchanged;
- unchanged immediate Apply accepted that fresh Preview, preserved the source/plan fingerprint checks, snapped only the intended source endpoint, synchronized semantic `LengthM`, invalidated the touched wall's generated solid and preserved the untouched wall's generated solid;
- Apply advanced `ChangeVersion` by exactly one audit-owned revision, emitted exactly one apply audit, cleared all Preview metadata and left sidecar bytes unchanged until explicit save, proving the native transaction plus semantic rollback boundary remained coherent;
- replacing the sidecar atomically with another valid project after Apply selection started was rejected without moving CAD, mutating either project generation, changing audit/preview metadata or touching generated ownership, and the replacement project was absent from `ProjectContextCoordinator` cache afterward;
- each BricsCAD session closed the disposable document without saving, quit gracefully with numeric exit code `0`, and left zero BricsCAD processes.

All 24 allowlisted private markers passed. No production source was changed by this rerun; it qualifies the already merged #3600/#3601 fixes only.

## Repository validation and safety

- `QS3D.BricsCAD.V25` Release|x64 build against the licensed V25 installation: `0 warnings / 0 errors`.
- Private probe Release build: `0 warnings / 0 errors`.
- `QS3D.Core.SmokeTests` direct execution through the repository portable .NET runtime: `ALL PASS`, exit code `0`.
- `scripts/preflight-wall-junctions.py`, `scripts/preflight-wall-junction-selection.py`, `scripts/preflight-wall-junction-ownership.py`, `scripts/preflight-wall-snap-audit-owned-revision.py`, `scripts/preflight-wall-snap-source-metrics.py`, `scripts/preflight-wall-snap-review.py`, `scripts/preflight-wall-snap-atomicity.py` and `scripts/preflight-wall-snap-project-lifecycle.py`: PASS.
- Public fixture SHA-256 remained `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; the user's drawing was never opened and its bytes remained unchanged.
- Fixed per-user V25 DemandLoad path/bytes and `LoadCtrls=2` were preserved; no `SECURELOAD` or `TRUSTEDPATHS` setting changed.
- Raw evidence and disposable data are excluded from the tracked diff; the root worktree's pre-existing crash artifacts were not touched.

## Remaining scope

Issue #3599 can close when this sanitized evidence PR is integrated. Parent #73 and overall LOCAL-007 remain open until the separately qualified physical-output P03 branch/PR is integrated and current `main` is audited for both bounded cells.

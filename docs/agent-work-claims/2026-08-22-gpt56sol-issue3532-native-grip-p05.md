# LOCAL-004 P05 — manual Beam grip cancel/commit handoff

Parent issue: #80  
Source-prep issue: #3532  
Lane-Key: `issue-local004-p05`  
Qualification evidence branch: `agent/codex/issue3532-v25-native-grip-qualification-fix`

## Boundary

P01-P04 already cover native MOVE/ROTATE/STRETCH and dependent redistribution. P05 isolates the remaining editor behavior that those automated command matrices do not prove: a **real endpoint grip drag** cancelled by ESC, followed by a second real grip drag that is committed.

The probe is read-only. It does not invoke a grip, mutate the source, reconcile semantics, erase generated output, or rebuild geometry. Those actions remain the real BricsCAD interaction + production `QS3DSYNCSOURCE` / `QS3DBUILD3D` path.

## Expected state transitions

- Baseline: Beam source = 5 m, semantic/quantities = 5 m, generated host = live 5 m owner output.
- Manual grip + ESC: all of the above remain unchanged.
- Manual grip commit endpoint to 8 m: source = 8 m, but semantic/quantities/generated host remain the old 5 m state until reconcile.
- `QS3DSYNCSOURCE`: source/semantic/quantities = 8 m and the old generated host is invalidated/removed.
- `QS3DBUILD3D`: a new owned 8 m host is created; old host handle must not be reused.
- SAVE / close / cold reopen: source, semantic, quantities and generated host still agree at 8 m.

## Local-agent command

```powershell
$env:BRICSCAD_V25_DIR = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
.\scripts\test-bricscad-v25-source-reconcile-native-grip.ps1
```

The runner first requires a clean checkout, pins exact `git rev-parse HEAD`, executes the feature source guard, and compiles the V25 adapter against the installed licensed references. It then prints the manual native matrix.

Probe commands:

```text
QS3DSRGRIPP05BASELINE
QS3DSRGRIPP05SELECT
QS3DSRGRIPP05CANCELCHECK
QS3DSRGRIPP05EDITCHECK
QS3DSRGRIPP05SYNCCHECK
QS3DSRGRIPP05FINAL
QS3DSRGRIPP05REOPEN
```

Production commands used by the matrix remain `QS3DDRAWBEAM`, `QS3DSYNCSOURCE`, and `QS3DBUILD3D`.

## Evidence contract

Only sanitized markers beginning `QS3D_SOURCE_RECONCILE_NATIVE_GRIP_RUNTIME_V1` may be posted. Keep paths, source/generated handles, ProjectId/ElementId, raw DWGs, proprietary DLLs and stack traces out of GitHub.

A full LOCAL-004 P05 qualification requires the ordered PASS evidence set from the **same exact candidate/run**:

1. `phase=baseline` — verified 5 m source/semantic/quantity/generated baseline;
2. `phase=cancel_check` — includes `manual_grip_cancel_verified=true`;
3. `phase=edit_check` — includes `manual_grip_commit_verified=true` and source-only 8 m state;
4. `phase=sync_check` — includes `source_reconcile_verified=true` and baseline generated invalidation;
5. `phase=final` — includes `rebuild_verified=true` plus `replacement_generated=true`;
6. `phase=reopen` — includes `production_local004_p05_reopen_candidate=true`, `prior_session_phases_replayed=false`, and `cold_reopen_verified=true`.

`QS3DSRGRIPP05REOPEN` runs after a real process restart, so in-memory probe state from the earlier phases no longer exists. Its PASS marker proves current persisted final-state continuity only. It MUST NOT replay or substitute for manual cancel/commit/reconcile/rebuild evidence from the prior process and MUST NOT be treated as a standalone aggregate qualification marker.

## 2026-08-23 exact-current licensed result

`LOCAL_PASS` for this bounded P05 cell was recorded on exact clean pushed SHA `239938e676e632ad34d8c48c11674bf18e8c087c` with BricsCAD V25.2.10. The V25 Release adapter and Core PDBs both contained the exact SourceLink SHA; the focused preflight passed, the installed-reference V25 build completed with `0 warnings / 0 errors`, and the full Core smoke suite returned `ALL PASS`. The exact adapter had ProductVersion `0.1.0-preview.10081` and SHA-256 `14B00F9355BB4CA77DFD413A43FEBD20F8328F800E8FEEEFCDDEB731A9A3807E`.

The same disposable run produced all six ordered PASS markers. A real endpoint hot-grip entered native `STRETCH`, previewed toward 8 m and physical ESC preserved the 5 m source/semantic/generated baseline. The second real endpoint hot-grip committed the source at 8 m while semantic/generated state remained at 5 m until production `QS3DSYNCSOURCE`; reconcile invalidated the baseline output, production `QS3DBUILD3D` created the replacement, and SAVE plus a fresh BricsCAD process retained coherent 8 m source/semantic/generated state. The reopen marker included `prior_session_phases_replayed=false`.

The disposable drawing changed and its sidecar persisted as required. The runner restored DemandLoad mode, left zero V25 processes, and kept raw DWG/log/screenshot evidence outside Git. This closes issue #3532's manual grip cancel/commit boundary only. Parent #80 stays open for remaining topology/category/dependent and failure/multi-DWG matrices.

# LOCAL-004 P05 — manual Beam grip cancel/commit handoff

Parent issue: #80  
Source-prep issue: #3532  
Lane-Key: `issue-local004-p05`  
Canonical branch: `agent/web-gpt56sol-20260822-grip1/issue-3532-native-grip-p05`

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

Remote/source state becomes `SOURCE_READY` only after exact-head CI/build is green. Licensed state remains `PENDING_LOCAL` until an agent performs the real manual grip + ESC/commit sequence on the exact pushed SHA. Parent #80 stays open for additional topology/category matrices.

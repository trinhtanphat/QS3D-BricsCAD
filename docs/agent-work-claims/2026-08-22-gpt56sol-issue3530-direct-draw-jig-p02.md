# LOCAL-008 P02 — Direct Draw DrawJig lifecycle handoff

Parent issue: #74  
Source-prep issue: #3530  
Lane-Key: `issue-local008-p02`  
Canonical branch: `agent/web-gpt56sol-20260822-ddjig1/issue-3530-direct-draw-jig-probe`

## Purpose

This carrier verifies the BricsCAD V25 editor primitive required before production Direct Draw gains transient profile preview and true repeated authoring. The probe is intentionally database-free: it uses `DrawJig` + `Editor.Drag(Jig)` to render a width-aware profile strip, accepts consecutive endpoints in one invocation, and exits on Enter/ESC without creating or owning CAD/QS3D objects.

It does **not** close #74 and must not be interpreted as production Direct Draw completion. Production wiring is allowed only after licensed V25 proves the exact candidate behaves correctly under cursor motion, ESC/Enter, UCS changes and document safety.

## Local-agent entry point

From a clean checkout of the canonical branch:

```powershell
$env:BRICSCAD_V25_DIR = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
.\scripts\test-bricscad-v25-direct-draw-jig-lifecycle.ps1
```

The script pins the clean exact SHA, executes the source guard and performs an installed-reference V25 Release build. It then prints the interactive matrix. The runtime command is:

```text
QS3DPROBEDIRECTDRAWJIG
```

## Required licensed observations

1. Move the pointer before each accepted endpoint: the transient strip must visibly follow the cursor and retain the requested width.
2. Accept at least three segments without leaving the command. Each accepted endpoint becomes the next base point.
3. Run one sequence ending with Enter and another ending with ESC.
4. Verify the probe leaves no database entities, XData, semantic elements, generated ownership or persisted preview residue.
5. Repeat in a rotated UCS and then WCS; the strip must follow the active UCS consistently.
6. Any active-document switch during the probe must fail closed instead of writing to another DWG.
7. Capture only the sanitized marker beginning `QS3D_DIRECT_DRAW_JIG_RUNTIME_V1`; do not publish paths, handles, project IDs or raw stack traces.

## Evidence state

Remote/source state: `SOURCE_READY` once exact-head CI/build is green.  
Licensed V25 state: `PENDING_LOCAL` until an agent executes the matrix against the exact pushed SHA.

No `LOCAL_PASS`, production readiness, or #74 closeout may be inferred from source/build CI alone.

# LOCAL-008 P02 — Direct Draw DrawJig lifecycle handoff

Parent issue: #74  
Source-prep issue: #3530  
Lane-Key: `issue-local008-p02`  
Canonical active hardening branch: `agent/web-gpt56sol-20260822-ddjig1/issue-3530-ucs-coordinate-fix`

## Purpose

This carrier verifies the BricsCAD V25 editor primitive required before production Direct Draw gains transient profile preview and true repeated authoring. The probe is intentionally database-free: it uses `DrawJig` + `Editor.Drag(Jig)` to render a width-aware profile strip, accepts consecutive endpoints in one invocation, and exits on Enter/ESC without creating or owning CAD/QS3D objects.

It does **not** close #74 and must not be interpreted as production Direct Draw completion. Production wiring is allowed only after licensed V25 proves the exact candidate behaves correctly under cursor motion, ESC/Enter, UCS changes and document safety.

## Corrected coordinate contract

The source-hardening review found that the first editor prompt and subsequent DrawJig acquisition do not use the same managed coordinate contract under a non-WORLD UCS:

- `Editor.GetPoint` supplies the first prompt point in the current UCS, so the probe snapshots `Editor.CurrentUserCoordinateSystem` and transforms that first value to WCS exactly once;
- DrawJig `JigPromptPointOptions.BasePoint` and `JigPrompts.AcquirePoint` are treated as WCS, so accepted segment chaining remains in WCS and later jig points are not transformed by the UCS again;
- the two WCS endpoints are transformed into the snapshotted UCS only for local XY profile-width offset math;
- the computed strip corners are transformed back to WCS exactly once for `WorldDraw`;
- the center line is drawn directly from the original WCS endpoints.

The sanitized runtime marker for this contract includes:

```text
coordinate_model=EDITOR_UCS_TO_JIG_WCS_UCS_PLANE
```

The focused source guard rejects both the old later-point double-transform pattern and the intermediate regression where the first `Editor.GetPoint` value was left unconverted.

## Local-agent entry point

From a clean checkout of the exact final merged candidate:

```powershell
$env:BRICSCAD_V25_DIR = 'C:\Program Files\Bricsys\BricsCAD V25 en_US'
.\scripts\test-bricscad-v25-direct-draw-jig-lifecycle.ps1
```

The script pins the clean exact SHA, executes the source guard and performs an installed-reference V25 Release build. It then prints the interactive matrix. The runtime command is:

```text
QS3DPROBEDIRECTDRAWJIG
```

## Required licensed observations

1. Under WCS, move the pointer before each accepted endpoint: the transient strip must visibly follow the cursor and retain the requested width.
2. Accept at least three segments without leaving the command. Each accepted endpoint becomes the next WCS jig base point.
3. Run one sequence ending with Enter and another ending with ESC.
4. Verify the probe leaves no database entities, XData, semantic elements, generated ownership or persisted preview residue.
5. Repeat under a rotated or translated UCS. The **first** picked point and every subsequent DrawJig point must remain anchored to the picked cursor positions; there must be no double-transform jump or offset.
6. Switch back to WCS and repeat to confirm the same cursor-following geometry.
7. Any active-document switch during the probe must fail closed instead of writing to another DWG.
8. Capture only the sanitized marker beginning `QS3D_DIRECT_DRAW_JIG_RUNTIME_V1`; it must include `coordinate_model=EDITOR_UCS_TO_JIG_WCS_UCS_PLANE` and `persistent_writes=0`. Do not publish paths, handles, project IDs or raw stack traces.

## Evidence state

Remote/source state: `SOURCE_READY` once exact-head CI/build is green and the hardening PR is merged.  
Licensed V25 state: `PENDING_LOCAL` until an agent executes the matrix against the exact final merged SHA.

No `LOCAL_PASS`, production readiness, or #74 closeout may be inferred from source/build CI alone.

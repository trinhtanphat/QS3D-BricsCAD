# LOCAL-008 P01 — Direct Draw view/consecutive/cancel qualification

Parent issue: #74

Qualification issue: #3589

Lane-Key: `issue-local008-p01`

Qualification evidence branch: `agent/codex/issue3589-v25-direct-draw-view`

## Boundary

This local-only cell qualifies the current-view preservation and bounded consecutive/cancel behavior of production `QS3DDRAWBEAM` in licensed BricsCAD V25. It does not change product source. It does not claim the broader quick/advanced cancellation, project/context drift, internal repeated-mode, Auto Host/reference, Ribbon, planar-UCS or document-switch matrices required by LOCAL-008.

The run uses only a disposable copy of the public generated sample. Raw markers, scripts and drawing copies remain under ignored local artifacts. No customer/private drawing, raw Handle, ProjectId, ElementId, screenshot, proprietary DLL or machine-specific path is committed.

## 2026-08-23 exact-current licensed result

`LOCAL_PASS / BOUNDED_ROW_2` was recorded on exact clean pushed runtime candidate `ea85175b4dbc601047d0b6945032836dea4765bd` with BricsCAD V25.2.10. The adapter and Core ProductVersion were both `0.1.0-preview.10081`; their SHA-256 values were `23871B963B387BD7DB3685140D2BDB9713AA8447266CD525BBFE59822B4CAF7A` and `056C1C82E39371DCB624AEF4F9451B5D3246A78F3C685AC40DF05481779F6F10`. Both PDBs contained the exact candidate SourceLink SHA.

The same self-contained run proved:

- a non-default Model Space view remained numerically identical in center, size, twist, direction and target;
- two consecutive production Beam draws each added exactly one LINE source and one 3DSOLID, with the completed solid selected and carrying canonical Beam ownership;
- the two owners stayed in the same project and represented distinct semantic elements;
- an exact-PID physical ESC during a third draw returned BricsCAD to `CMDACTIVE=0` with no active command in 2651 ms;
- cancellation added zero LINEs and zero solids and preserved the exact view;
- the dirty disposable project exercised one discard dialog, the asynchronous exact-HWND/exact-document dispatcher exited `0`, BricsCAD exited gracefully, and no V25 process remained;
- the disposable drawing stayed byte-identical to the public fixture SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`, and no sidecar was created.

## Repository validation

- `QS3D.Core` Release build: `0 warnings / 0 errors`.
- `QS3D.Core.SmokeTests` Release build: `0 warnings / 0 errors`; execution: `ALL PASS`.
- `QS3D.BricsCAD.V25` Release|x64 build against the licensed V25 installation: `0 warnings / 0 errors`.
- Seven focused Direct Draw, view-preservation, cancellation, CI-policy and local/remote handoff guards: PASS.
- Post-run fetch: runtime candidate, pushed qualification branch and `origin/main` all resolved to `ea85175b4dbc601047d0b6945032836dea4765bd` before this sanitized documentation carrier.
- Fixed per-user V25 DemandLoad registration remained enabled with `LoadCtrls=2`; no security/trusted-path policy was changed.

## Remaining scope

Issue #3589 closes only this P01/Sheet row 2 cell. Parent #74 and overall LOCAL-008 remain open for internal repeated mode, per-stage quick/advanced cancellation, preview-project and DWG/ModelSpace/unit/UCS drift, Door/Opening/Window Auto Host/reference behavior, Ribbon idempotence, planar UCS and safe document switching.

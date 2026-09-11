# Estimating & Rate Build-up Workspace

Issue: #6404
Parent program: #6127
Runtime boundary: REMOTE_SAFE source/static + hosted compile; licensed BricsCAD interaction remains LOCAL_ONLY.

## Purpose

`QS3DESTIMATING` opens a modeless QS workspace for the TBQ project already bound to the active drawing.
It is a product surface over existing Core authorities, not a second estimating engine.

The workspace exposes:
- bill items and their persisted quantity/rate/total state;
- rate build-up usage analysis;
- rate-reference graph;
- BQ library reference rates;
- trade/CFA cost analysis;
- adjustment and markup preview/apply with project save.

## Authority

All calculations remain in `QS3D.Core` through `ProjectTbqWorkspace` and `TbqProjectWorkspaceState`.
The BricsCAD window only formats Core results and joins reference labels for display.
## Safety contract

The command fences modeless publication to the exact active managed `Document` and native database identity.
The window repeats that affinity check before every refresh, preview and mutation.

Apply uses the same fail-closed project pattern as existing TBQ commands:
1. require the existing canonical project and unchanged backing store;
2. capture `ProjectStateSnapshot`;
3. call `ProjectTbqWorkspace.ApplyAdjustment`;
4. save through `ProjectContextCoordinator.Save`;
5. restore and discard the cached context if save fails.

## Qualification

Run `scripts/preflight-estimating-rate-workspace.py` and the normal repository preflight/Core suite.
Hosted/static evidence must not be reported as licensed BricsCAD runtime PASS.
A later licensed V25/V26 session should verify open, cross-DWG focus rejection, refresh, preview, apply/save, close/reopen and persisted totals on the exact candidate SHA.

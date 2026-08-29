# Issue 4586 Workspace Add route ownership claim

- Status: `SOURCE_CANDIDATE / STATIC_VALIDATED / PENDING_BRANCH_CI / PENDING_LOCAL_RUNTIME`
- Lane-Key: `single-footing-add-routing`
- Owner/session: `codex-01a046ab`
- Issue: `#4586`
- Branch: `agent/codex/issue-4586-single-footing-add-routing`
- Exact main baseline: `27aab6605372ed4139178019bcc347456a7c970d`
- Routing patch commit: `a92629851ef3ab3640de142db5ea1b68aafa3e29`

## Scope

Repair the final owner of the shared Workspace `+ Add` Click route for Móng đơn without regressing Grid or Room routes. The production paths are limited to `WorkspacePanel.Blt3dFamilyWorkspace.cs` and `WorkspacePanel.RoomWorkspacePane.cs`; `preflight-workspace-add-route-ownership.py` is the focused regression guard.

## Root cause and correction

Grid attached `OnGridAwareFamilyAddModeClick` to the shared control. Its non-Grid fallback marked the event handled and opened the generic `Tham số / Solid3D` chooser before the later BLT3D handler could reach `SingleFootingDimensionsDialog`. The final BLT3D and Room owners now detach the stale Grid handler. The Room owner explicitly retains Grid direct creation before delegating non-Room routes to BLT3D.

## Static evidence

- Controlled red: the new guard failed before the correction because the BLT3D owner did not detach the Grid button handler.
- Focused source guards pass: add-route ownership, single-footing workspace/workflow, Grid subtype, and Room workspace guards.
- Release Core, smoke project, V25, and V26 builds pass with zero warnings/errors; the Core smoke executable completed its acceptance cases.
- `python scripts/preflight-all.py` passes with 1,208 discovered feature gates.

## Runtime boundary

None of the above is a licensed runtime result. A future cell must run the exact pushed branch head's matching V25 and V26 binaries with an exclusive host, then prove physical mouse/UI route to the dedicated Móng đơn dimensions dialog, placement/edit/regeneration, generic Foundation control, `QS3DSAVE`/`QSAVE`, cold reopen, and guarded cleanup. Record the immutable runtime SHA in issue `#4586` and the local evidence before any merge/release claim.

# Work claim — basic drawing tools bound to active Family

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-basic-drawing-20260813`
- Registered: `2026-08-13T17:15:00+07:00`
- Completed: `2026-08-13T17:24:00+07:00`
- Baseline main SHA: `092a5d28305ccddac09f79711d310cab93dde6f7`
- Integration PR: `#1033`
- Integration commit: `a456efd50310c92520a903131b0b818157aaec2d`
- Priority: owner-requested QS3D first-version workflow from the supplied UI/command reference: panel -> Add -> properties -> active Family -> Line/Rectangle/Circle.

## Delivered scope

Implemented a narrow BricsCAD V25 basic-drafting surface that reads and freshness-checks the canonical active QS3D Family, creates native LINE / closed rectangular POLYLINE / CIRCLE geometry from normal editor prompts, and persists a versioned QS3D drafting-context marker on the operation-owned entity so the selected Family/category/floor/zone context is not merely cosmetic.

The existing Workspace already supplied the requested Zone/Floor selectors, category tree, Add/Delete Family flow, property pane and existing semantic `Vẽ 3D`/Direct Draw path. To minimize collision with that established UI, the three new basic tools were exposed through the existing Family context menu plus Workspace-focused `Ctrl+1` / `Ctrl+2` / `Ctrl+3`, instead of adding a second toolbar into `WorkspacePanel.xaml`.

## Landed surfaces

- `src/QS3D.BricsCAD.V25/BasicDrawingCommands.cs` — new `QS3DDRAWLINE`, `QS3DDRAWRECT`, `QS3DDRAWCIRCLE` command owner.
- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.QuickDraw.cs` — basic-drawing menu/shortcut dispatch while preserving existing `Ctrl+D` / `Ctrl+Shift+D` semantic Direct Draw.
- `scripts/preflight-basic-drawing-active-family.py` — focused source/static contract gate.
- `docs/BASIC-DRAWING-ACTIVE-FAMILY.md` — command/workflow/runtime-boundary documentation.
- PR `#1033` squash-merged to `main` as `a456efd50310c92520a903131b0b818157aaec2d`.

`docs/COMMANDS.md` and `WorkspacePanel.xaml` were intentionally left untouched because the dedicated workflow document is self-contained and the established Workspace already provides the requested panel/Add/Delete/property surface. This also avoided unnecessary collision with fast-moving shared UI/documentation surfaces.

## Behavior delivered

- `QS3DDRAWLINE`: start/end point -> native BricsCAD `LINE`.
- `QS3DDRAWRECT`: two opposite corners in the current UCS -> closed rectangular `POLYLINE`.
- `QS3DDRAWCIRCLE`: center + cursor/typed radius -> native BricsCAD `CIRCLE`.
- Command dispatch requires an existing canonical QS3D project and Active Family; it does not silently bootstrap a project.
- Before prompt acquisition the command snapshots ProjectId/ChangeVersion, active Family id/category, active Floor/Zone and UCS.
- Immediately before CAD mutation it revalidates active DWG, Model Space, UCS, project version, Family and Floor/Zone; stale modeless state fails closed.
- New entities carry versioned `QS3DBASICDRAW` XData with SHA-256 identity tokens for project/family/floor/zone plus category and primitive kind.
- ESC/cancel before the commit boundary creates no entity and does not mutate QS3D project state.
- Successful entities are selected and Workspace/editor status identifies the Family/category used.

## Preserved boundaries

- No changes to `WorkspacePanel.MultiSelectionProperties.cs`, Curtain selection/Undo, Source Reconcile, Family Manager, Zone/Floor manager, release/versioning, or current Direct Draw semantic/native builders.
- Arbitrary Rectangle/Circle sketches are not reinterpreted as BIM semantics and do not auto-run `SemanticCaptureService`; category-specific `QS3DDRAW*` / `QS3DDRAWACTIVE` remains the semantic/native 3D creation path.
- No V26 UI/command parity claim: the current V26 adapter does not yet contain the V25 Workspace/command surface.
- No GitHub Actions were dispatched.

## Validation evidence

- Exact merged source was reviewed against the existing V25 Direct Draw patterns for BricsCAD `Editor` prompts, UCS transform, Model Space entity transactions and active-document freshness.
- The existing active-Family dispatcher confirms the same canonical `ProjectFamilyActivationService.GetActive(...)` integration used by this lane.
- BricsCAD V25 API documentation was checked for `PromptPointOptions` BasePoint/UseBasePoint and `PromptDistanceOptions`/`Editor.GetDistance` support used by the new interaction flow.
- V25 is an SDK-style net48 WPF project, so the new `.cs` file participates in default SDK compile items.
- The focused preflight was landed but was not represented as an executed CI/runtime PASS in this remote lane.

Exact licensed BricsCAD V25 compilation, palette focus, cursor preview, translated/rotated/tilted UCS behavior and real-DWG interaction remain `LOCAL_ONLY`. No `LOCAL_PASS` or customer-release qualification is implied by this closeout.

## Completion result

The owner-requested first-version Line / Rectangle / Circle workflow is now integrated on `main`, bound to the canonical active Family context, discoverable from the Workspace, and guarded against stale project/Family/Floor/Zone/UCS state. Claim complete.
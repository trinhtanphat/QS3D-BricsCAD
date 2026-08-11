# Work claim — Floor/Level stale modeless project guard

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-level-stale-project-guard`
- Registered: `2026-08-11`
- Baseline main SHA: `7b289bd9a63100eb36d5b3405b7b0dcaa58b66f4`
- Priority: P0 modeless mutation lifecycle correctness

## Confirmed defect

`FloorLevelWindow` is document-bound but not project-bound. Its mutation callbacks validate only that the source DWG is active and then bind whatever canonical `ProjectState` currently exists. If that same DWG reloads/replaces its QS3D project while the Level Picker remains open, stale UI state can therefore write into the replacement project. Family Manager and Zone Manager already reject this lifecycle with a bound `ProjectState` reference; Floor/Level remains the gap.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs`
- one focused source-static preflight for Floor/Level stale-project binding, or a narrowly scoped extension of the existing modeless lifecycle gate if that is the safer integration point
- this claim file for close-out

## Intended repair

- bind the Level Picker to the exact read-only `ProjectState` instance rendered by the most recent successful `RefreshAll`;
- clear that binding when no project is available;
- before every mutation, require the original DWG to be active and the current canonical read-only project to be the same bound project instance;
- fail closed with a Refresh/reopen message when the project was reloaded/replaced;
- preserve the existing assignment revalidation, rollback, audit, regeneration/stale-output and post-commit UI behavior;
- allow this window's own successful commits to continue normally by refreshing/rebinding after commit.

## Exclusions

No changes to Core floor semantics, Family/Zone manager sources, Workspace, Quantity, Ribbon, Direct Draw, persistence, updater/release/signing, GitHub Actions, or native BricsCAD runtime qualification. This lane mirrors the established Family/Zone project-replacement guard rather than introducing a broader ChangeVersion UX policy.

## Validation plan

Re-fetch latest `main` before implementation, verify no concurrent FloorLevel source claim/change overlaps, add a source gate that requires bound-project capture/clear/check and rejects document-only mutation guards, inspect final PR changed paths, and integrate without force push. Do not dispatch GitHub Actions. Native V25/WPF qualification remains LOCAL_ONLY.

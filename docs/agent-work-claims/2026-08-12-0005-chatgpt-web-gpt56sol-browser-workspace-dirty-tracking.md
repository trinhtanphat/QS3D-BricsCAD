# Work claim — Project Browser workspace dirty tracking

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:05:00+07:00`
- Baseline main SHA: `2828143f5df24019ee6cda13f662417dfc8afafa`
- Priority: evidence-driven remote-safe Core persistence correctness

## Reserved scope

Fix Project Browser workspace persistence so changes to persisted workspace metadata participate in `ProjectState.ChangeVersion` / `ProjectPersistenceStamp` dirty tracking. Preserve idempotence: saving identical serialized state and clearing an absent workspace key must remain no-ops that do not advance the project version.

## Exact implementation surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceDirtyTrackingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceDirtyTrackingRegistration.cs`
- this claim file

## Exclusions

- No Project Browser query, selection, grouping, virtualization, XML schema/format, WPF/native/runtime behavior changes.
- No `ProjectState` or `ProjectPersistenceStamp` implementation changes.
- No GitHub Actions dispatch.
- No BricsCAD V25/native runtime claims.

## Evidence

`ProjectBrowserWorkspaceStateStore.Save` writes `ProjectState.Metadata[MetadataKey]` and `Clear` removes it, while `ProjectPersistenceStamp.RequiresSave` observes `ProjectState.ChangeVersion`. The current store does not call `project.Touch()` for either persisted mutation, so workspace metadata can change while the persistence stamp still reports the project clean.

## Completion condition

- Persisted workspace mutation advances `ChangeVersion` exactly once.
- Identical save is a version no-op.
- Clearing an existing workspace advances `ChangeVersion`; clearing again is a no-op.
- Focused smoke demonstrates `ProjectPersistenceStamp.RequiresSave()` flips to true after a workspace mutation.
- Claim is closed with exact commit SHAs and truthful validation limits.

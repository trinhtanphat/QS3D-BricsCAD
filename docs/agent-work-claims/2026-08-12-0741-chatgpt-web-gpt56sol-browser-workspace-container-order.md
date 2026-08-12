# Work Claim: Project Browser Workspace Container Order

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `14de4e113f653a99a2a7278279574b667da7a304`
- Scope: require persisted Project Browser workspace collection containers to use the fixed order emitted by `Serialize(...)`.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceContainerOrderSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceContainerOrderSmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0741-chatgpt-web-gpt56sol-browser-workspace-container-order.md`

## Defect evidence

`Serialize(...)` always emits root collection containers in this order: `Categories`, `FloorIds`, `ZoneIds`, `ExpandedPaths`, `SelectedElementIds`. `Deserialize(...)` currently validates only that the five expected unnamespaced children each appear exactly once, then locates them by name. Therefore persisted XML with the same containers rearranged is accepted and silently returns to serializer order on the next save. This is another lossy persisted representation after query/primary/value-sequence canonicality was hardened.

Recently completed Browser workspace empty-metadata and revision-atomicity lanes are closed. No current claim was found for root collection-container ordering.

## Boundaries

- Navigation/Core persisted XML only; no BricsCAD/native/UI changes.
- Preserve in-memory state behavior, collection-item canonicality, query/primary/grouping/boolean guards, XML shape validation, empty-metadata handling, revision atomicity, project validation and schema/version.
- Do not require XML attribute ordering; only semantic child-container ordering emitted by the serializer is in scope.
- No GitHub Actions dispatch.

## Validation plan

- Require the direct root element sequence to exactly match the five serializer container names.
- Add isolated smoke coverage proving canonical serialized state loads and swapping two valid containers fails closed.
- Review current source again immediately before writing because this file is concurrency-sensitive.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke PASS unless actually available.

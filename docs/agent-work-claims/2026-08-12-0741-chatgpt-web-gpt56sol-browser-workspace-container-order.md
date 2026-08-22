# Work Claim: Project Browser Workspace Container Order

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `14de4e113f653a99a2a7278279574b667da7a304`
- Scope: require persisted Project Browser workspace collection containers to use the fixed order emitted by `Serialize(...)`.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceContainerOrderSmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceContainerOrderSmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0741-chatgpt-web-gpt56sol-browser-workspace-container-order.md`

## Completed work

- `Deserialize(...)` now requires direct root collection containers to appear exactly in serializer order: `Categories`, `FloorIds`, `ZoneIds`, `ExpandedPaths`, `SelectedElementIds`.
- Reordered otherwise-valid containers now fail closed instead of silently returning to serializer order on the next save.
- Existing unsupported/missing/duplicate child checks remain intact before the order guard.
- Existing query/primary/grouping/boolean/value-sequence canonicality, XML shape validation, empty-metadata handling, revision atomicity, project validation and schema/version remain unchanged.
- XML attribute ordering remains intentionally out of scope.
- Added isolated Core smoke coverage plus module-initializer registration without editing shared smoke registries.

## Published commits

- Claim-first commit: `53d6a8e3148c33ba3c9f719799dd77df9d6dd51a`.
- Source fix on `main`: `bbbd416865d25d912607742ee5a905e9fa6bf7a6`.
- Initial smoke commit: `373d03652e5d0f08a0fb242afff09f20337fdb92`.
- Smoke fixture correction: `8ad870506e6740347b743fa814f8251f5fd5ef5b`.
- Smoke registration: `df51603e66643b6f7a1e04c66b31ddc50fd5fa65`.

## Validation notes

- Re-read current `main` source after integration and confirmed the direct-root sequence guard is present.
- Re-read the focused smoke after its fixture correction: canonical serializer output is accepted, then `FloorIds` is moved before `Categories` and deserialization is required to throw `InvalidDataException`.
- The source write used the current source blob after the concurrent empty-metadata and revision-atomicity Browser lanes had completed.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Blocked dependencies

None.

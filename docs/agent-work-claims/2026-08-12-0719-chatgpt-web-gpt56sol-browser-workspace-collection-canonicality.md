# Work Claim: Project Browser Workspace Collection Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `18b1875a2420160ae4a9e55288ede22e6fb82887`
- Scope: fail closed when persisted Project Browser workspace collections are accepted in a representation that the state constructor silently reorders or deduplicates.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCollectionCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCollectionCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0719-chatgpt-web-gpt56sol-browser-workspace-collection-canonicality.md`

## Completed work

- `Deserialize(...)` now requires the persisted `Categories`, `FloorIds`, `ZoneIds`, `ExpandedPaths`, and `SelectedElementIds` sequences to exactly match the canonical sequences produced by `ProjectBrowserWorkspaceState`.
- Unsorted persisted collections now fail closed instead of being silently reordered during load.
- Duplicate persisted categories now fail closed instead of being silently deduplicated by the constructor's `SortedSet` normalization.
- In-memory constructor normalization convenience remains unchanged.
- Existing query/primary/grouping/boolean canonicality, duplicate-ID/path rejection, collection bounds, serializer shape, project validation and workspace schema/version remain unchanged.
- Added isolated Core smoke coverage plus module-initializer registration without editing shared smoke registries.

## Published commits / PR

- Claim-first commit: `3a67d6e910b9a26f1145e66ebcdda1962bf6e556`.
- Source commit: `58d18702a1018256c4499826270c7ee65d7a1a25`.
- Focused smoke: `8900ef3cdb97810b20e4d64d5bbe2ea68db821dd`.
- Smoke registration: `4a9b75392e9f1db1da9d538e10073dd8d0dc240f`.
- PR #613 contained exactly the three reserved source/test files and was squash-merged.
- Published `main` squash SHA: `6d00dad3d8caafbcc677bc9abb22feae1cbaa930`.

## Validation notes

- Reviewed PR #613's exact three-file patch before merge.
- Re-read current `main` immediately before merge and confirmed the reserved source blob still matched the expected pre-fix blob, avoiding overwrite of concurrent work.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Blocked dependencies

None.

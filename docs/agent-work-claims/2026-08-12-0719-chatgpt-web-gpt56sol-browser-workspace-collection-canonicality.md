# Work Claim: Project Browser Workspace Collection Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `18b1875a2420160ae4a9e55288ede22e6fb82887`
- Scope: fail closed when persisted Project Browser workspace collections are accepted in a representation that the state constructor silently reorders or deduplicates.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCollectionCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceCollectionCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0719-chatgpt-web-gpt56sol-browser-workspace-collection-canonicality.md`

## Defect evidence

`Serialize(...)` emits the already-normalized `Categories`, `FloorIds`, `ZoneIds`, `ExpandedPaths`, and `SelectedElementIds` collections. Their constructor paths sort each collection into a deterministic order, and `NormalizeCategories(...)` also silently deduplicates category values. `Deserialize(...)` currently reads persisted collection order then passes it into that constructor without requiring the raw persisted sequence to equal the resulting canonical sequence. As a result, unsorted persisted collections and duplicate categories can load successfully and silently change representation on re-serialize/save.

The completed query/primary canonicality lanes are closed. Current concurrent claims observed around `main` target formulas, semantic schedules, XLSX preflight and other disjoint files; no active claim was found for this workspace collection contract.

## Boundaries

- Navigation/Core persistence only; no BricsCAD/native/UI changes.
- Preserve in-memory constructor normalization convenience; harden only persisted XML acceptance.
- Preserve existing enum/category validation, duplicate-ID/path rejection, collection bounds, query/primary/boolean/grouping guards and workspace schema/version.
- No GitHub Actions dispatch.

## Validation plan

- Construct persisted workspace state through the existing normalization path, then require each raw collection sequence to match the resulting canonical state sequence exactly.
- Add isolated smoke coverage for canonical collections plus unsorted categories/IDs/paths/selection and duplicate-category rejection.
- Review exact PR diff through GitHub connector before merge.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke PASS unless actually available.

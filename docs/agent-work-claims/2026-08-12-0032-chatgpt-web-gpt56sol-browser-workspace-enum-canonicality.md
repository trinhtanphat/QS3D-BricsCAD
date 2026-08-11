# Work Claim: Project Browser Workspace Enum Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `dc6e4d01dea6c1c48dc1a3287ef40fe8fddd741c`
- Scope: fail closed on numeric/non-canonical enum representations in persisted Project Browser workspace state while preserving serializer-emitted enum names.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEnumCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEnumCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0032-chatgpt-web-gpt56sol-browser-workspace-enum-canonicality.md`

## Defect evidence

`Deserialize(...)` currently combines case-sensitive `Enum.TryParse(...)` with `Enum.IsDefined(...)` for `ProjectBrowserGrouping` and category items. .NET enum parsing still accepts defined numeric text such as `"0"`, even though `Serialize(...)` emits canonical enum names through `ToString()`. A persisted workspace payload can therefore load successfully and silently change representation when re-serialized.

## Boundaries

- Navigation/Core only; no BricsCAD/native/UI changes.
- Preserve all current enum values and serializer output.
- Do not change workspace schema/version, collection cardinality, query semantics, or project validation behavior.
- No GitHub Actions dispatch.

## Validation plan

- Require parsed grouping/category text to equal the enum's canonical `ToString()` representation using ordinal comparison.
- Add isolated smoke coverage proving serializer-emitted names still load and defined numeric grouping/category aliases fail closed.
- Review exact diff through GitHub connector.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke pass unless actually available.

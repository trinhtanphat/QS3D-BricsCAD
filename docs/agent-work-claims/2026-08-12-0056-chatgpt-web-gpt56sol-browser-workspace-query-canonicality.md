# Work Claim: Project Browser Workspace Query Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `50ac762364be318d65e046eeb09af5b0f5af0581`
- Scope: fail closed on non-canonical persisted Project Browser workspace `query` text while preserving serializer-emitted trimmed/empty query representation.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceQueryCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceQueryCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0056-chatgpt-web-gpt56sol-browser-workspace-query-canonicality.md`

## Defect evidence

`ProjectBrowserWorkspaceState` canonicalizes its query by mapping whitespace-only input to empty text and trimming non-blank input, and `Serialize(...)` writes that canonical `state.Query`. `Deserialize(...)` currently passes the persisted `query` attribute straight into the constructor without requiring the raw persisted representation to equal the resulting canonical query. XML such as `query=" beam "` or whitespace-only query text therefore loads successfully and silently changes representation on re-serialize/save.

The active Project Browser Family/category integrity lane reserves `ProjectBrowserQueryPlanner.cs` and its existing smoke, not this workspace-state store or the new isolated smoke files.

## Boundaries

- Navigation/Core persistence only; no BricsCAD/native/UI changes.
- Preserve the existing query meaning, 160-character bound, serializer output and all enum/boolean canonicality guards.
- Do not change workspace schema/version, collection handling, project validation or browser search semantics.
- No GitHub Actions dispatch.

## Validation plan

- Capture persisted `query` text, construct the state through the existing normalization/validation path, then require ordinal equality between persisted text and `state.Query` before returning it.
- Add isolated smoke coverage for canonical non-empty/empty queries plus padded and whitespace-only persisted query rejection.
- Review exact PR diff through GitHub connector.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke PASS unless actually available.

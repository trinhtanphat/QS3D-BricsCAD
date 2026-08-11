# Work Claim: Project Browser Workspace Boolean Canonicality

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `d8971203c2703eed71ecbdd84aa47cd8f775130a`
- Scope: fail closed on non-canonical persisted `dirtyOnly` boolean text while preserving serializer-emitted lowercase `true`/`false`.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceBooleanCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceBooleanCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0036-chatgpt-web-gpt56sol-browser-workspace-bool-canonicality.md`

## Defect evidence

`Serialize(...)` writes `dirtyOnly` as lowercase `"true"` or `"false"`, but `Deserialize(...)` relies only on case-insensitive `bool.TryParse(...)`. Persisted values such as `"True"` therefore load and are silently rewritten to a different canonical representation on the next serialize/save.

## Boundaries

- Navigation/Core only; no BricsCAD/native/UI changes.
- Preserve the existing boolean meaning and lowercase serializer format.
- Do not change workspace schema/version, enum canonicality, collection handling, query semantics, or project validation behavior.
- No GitHub Actions dispatch.

## Validation plan

- After boolean parsing, require exact ordinal equality with serializer-emitted lowercase text.
- Add isolated smoke coverage for canonical `true`/`false` plus mixed-case rejection.
- Review exact source/test diff through GitHub connector.
- Do not claim BricsCAD V25 runtime validation or remotely executed smoke pass unless actually available.

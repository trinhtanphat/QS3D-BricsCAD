# Work Claim: Project Browser Workspace Boolean Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `d8971203c2703eed71ecbdd84aa47cd8f775130a`
- Scope: fail closed on non-canonical persisted `dirtyOnly` boolean text while preserving serializer-emitted lowercase `true`/`false`.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceBooleanCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceBooleanCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0036-chatgpt-web-gpt56sol-browser-workspace-bool-canonicality.md`

## Completed work

- `dirtyOnly` still parses through the existing boolean parser, then must exactly equal the serializer's lowercase `"true"` or `"false"` representation.
- Mixed/upper-case aliases such as `"True"` and `"FALSE"` now fail closed instead of being silently canonicalized on a later save.
- Serializer behavior and boolean meaning remain unchanged.
- Added isolated smoke coverage for canonical false/true round-trips and non-canonical casing rejection, with module-initializer registration that avoids shared registry edits.

## Published commits

- Claim-first commit: `eed4676ff4cea79e9bdc17ada59a39c71a4dd33e`.
- Source fix: `40bcc8425d68e97c839358db7b5f33141a31f9e1`.
- Focused smoke: `d7af63bc7dcc8729f22b9c08b297c40f085c4132`.
- Smoke registration: `057c7ae07316a2ec3f2de1d2f44e222ce2af3500`.

## Validation notes

- Re-read the exact `main` parser after the write and confirmed `dirtyOnly` is compared ordinally against serializer-emitted lowercase text after successful parsing.
- Focused smoke covers canonical false/true values and rejects `True`/`FALSE`; its module initializer is isolated from the shared smoke registry.
- Existing-file write used the current blob SHA, so a concurrent same-file update would have been rejected instead of overwritten.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test pass.

## Blocked dependencies

None.

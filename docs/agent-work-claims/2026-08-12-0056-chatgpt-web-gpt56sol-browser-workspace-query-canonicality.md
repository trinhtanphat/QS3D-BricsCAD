# Work Claim: Project Browser Workspace Query Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `50ac762364be318d65e046eeb09af5b0f5af0581`
- Scope: fail closed on non-canonical persisted Project Browser workspace `query` text while preserving serializer-emitted trimmed/empty query representation.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceQueryCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceQueryCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0056-chatgpt-web-gpt56sol-browser-workspace-query-canonicality.md`

## Completed work

- `Deserialize(...)` now captures the persisted `query` attribute, constructs the workspace state through the existing normalization/validation path, and requires exact ordinal equality between the persisted text and resulting canonical `state.Query`.
- Padded query text such as `"  beam  "` and whitespace-only persisted query text now fail closed instead of silently changing representation on re-serialize/save.
- Canonical non-empty and empty query values remain accepted unchanged.
- Existing 160-character query bound, serializer output, enum/boolean canonicality, collection handling and project validation behavior remain unchanged.
- Added isolated Core smoke coverage plus module-initializer registration without touching shared smoke registries or the concurrent `ProjectBrowserQueryPlanner` lane.

## Published commits / PR

- Claim-first commit: `85049989a8bc391209254a5e4d20161a489dfc49`.
- Source commit: `c30504aee785c216b6e4fb85ed6c419fc095ca06`.
- Focused smoke: `a875ed93329fb95220c94923dc5177acea5bedb3`.
- Smoke registration: `afaa2a1fea2035b7e2ab11c225f14dee11ee5a8d`.
- PR #594 contained exactly the three reserved source/test files and was squash-merged.
- Published `main` squash SHA: `7fcb6c2da653abb1f040deca3d4a3f9a46acf98c`.

## Validation notes

- Reviewed PR #594's exact three-file patch before merge.
- Re-read current `main` before merge and confirmed the reserved source file still matched the expected pre-fix blob, avoiding overwrite of concurrent work.
- GitHub Actions were not dispatched.
- This Core-only batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test PASS.

## Blocked dependencies

None.

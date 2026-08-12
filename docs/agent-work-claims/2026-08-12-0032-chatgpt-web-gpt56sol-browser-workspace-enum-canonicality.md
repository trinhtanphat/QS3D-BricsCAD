# Work Claim: Project Browser Workspace Enum Canonicality

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12
- Completed: 2026-08-12
- Mode: Remote source-safe
- Baseline main SHA: `dc6e4d01dea6c1c48dc1a3287ef40fe8fddd741c`
- Scope: fail closed on numeric/non-canonical enum representations in persisted Project Browser workspace state while preserving serializer-emitted enum names.

## Reserved files

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEnumCanonicalitySmoke.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceEnumCanonicalitySmokeRegistration.cs`
- `docs/agent-work-claims/2026-08-12-0032-chatgpt-web-gpt56sol-browser-workspace-enum-canonicality.md`

## Completed work

- Persisted `ProjectBrowserGrouping` text must now equal the enum's canonical `ToString()` representation after parsing.
- Persisted `ElementCategory` items must now equal their canonical enum names after parsing.
- Defined numeric enum aliases such as `"0"` therefore fail closed instead of loading and silently changing representation on re-serialize.
- Serializer-emitted enum names remain accepted unchanged.
- Added an isolated smoke module covering the canonical baseline plus numeric grouping/category rejection, with module-initializer registration that avoids shared smoke registry edits.

## Published commits

- Claim-first commit: `024d4dfd3b9ed21c980f8fa518ba73a56fb84eac`.
- Initial focused smoke: `f546bac4c06f263699158288878d36f7b65066c9`.
- Source fix: `f770152dcda1bd13d8af4e183b41a7d040442252`.
- Smoke registration: `c355efad7ea9b8a4f645b8fc7040d45ab5eca0d9`.
- Smoke fixture simplification: `f59ef7ab112d928605ba93634cb2d6db1d974a7f`.

## Validation notes

- Re-read the exact `main` source after the write and confirmed both grouping and category canonical-name guards are present.
- Re-read the focused smoke and registration files on `main` and confirmed canonical-name success plus numeric-alias rejection are encoded and registered.
- Writes used current blob SHAs for existing files, so stale same-file updates would have been rejected instead of overwriting concurrent work.
- GitHub Actions were not dispatched.
- This Core-only source batch does not claim BricsCAD V25 runtime validation or a remotely executed smoke-test pass.

## Blocked dependencies

None.

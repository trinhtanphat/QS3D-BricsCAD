# Agent Work Claim — Blank dependency health diagnostic

- Agent: ChatGPT Web / GPT-5.6 Sol
- Date: 2026-08-11
- Status: ACTIVE
- Branch/target: direct `main` under current `AGENTS.md` coordination policy

## Scope reservation

- `src/QS3D.Core/Diagnostics/DependencyHealthService.cs`
- `tests/QS3D.Core.SmokeTests/DependencyHealthBlankTargetSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` (shared registry; re-read immediately before update)
- this claim file

## Explicit exclusions

- No Model Health WPF/UI changes.
- No Build3D/regeneration command changes.
- No persistence/project mutation, Reporting, Rebar, Room, Documentation, updater or local-only runtime surfaces.

## Verified defect

Current `QS3DBUILD3D` dependency-scope construction fails closed when a semantic element contains an empty/whitespace dependency ID and instructs users to repair dependencies before rebuild. `DependencyHealthService`, however, currently trims each dependency token and silently `continue`s when the result is empty. Health can therefore report no dependency issue for a project state that native Build3D deterministically rejects.

## Plan

1. Detect at most one blank dependency-token defect per source element while preserving existing case-insensitive de-duplication for non-empty IDs.
2. Emit deterministic `DEPENDENCY_TARGET_BLANK` Error issues keyed to the source element.
3. Preserve missing, ambiguous, self-reference and cycle behavior/order.
4. Add focused Core smoke coverage for null/empty/whitespace tokens, one issue per source element, and no false positive for valid dependencies.
5. Register from the latest shared smoke registry blob.
6. Re-read current `main`, inspect GitHub status evidence and close with exact SHAs; do not claim runtime/CI without execution.
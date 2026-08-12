# Work claim — WallRegenerator HostWallId canonicality

- Status: `RELEASED`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:05:30+07:00`
- Released: `2026-08-12T10:07:30+07:00`
- Baseline main SHA: `7f3e74ce667450d4e78ab9bba883c40b86380021`
- Claim commit: `af5728e07d5582f97ee9b4082ac84e957cf40a5f`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`WallRegenerator.LinkedOpeningArea()` currently compares persisted opening/door `HostWallId` directly with the semantic wall id. A padded or whitespace-only persisted HostWallId can therefore be silently ignored instead of failing visible, under-deducting opening area for ArchitecturalWall / GlassWall / WallPier quantity regeneration. The corresponding StructuralRegenerator path now rejects non-canonical HostWallId values before wall quantity mutation.

## Release reason

The defect remains valid, but `SemanticRegenerators.cs` is a large, high-churn shared file and the available GitHub write connector replaces whole UTF-8 files rather than applying a narrow source hunk. With `main` moving continuously, replacing the full file would create an unnecessary risk of overwriting concurrent edits. This lane is released without implementation changes so another agent with a safe patch/rebase worktree can take it.

## Non-overlap check

Recent HostWallId work covers baseline Model Health, Door/opening reporting, physical opening ownership, and StructuralRegenerator. No claim/commit was found for the `WallRegenerator` path in `SemanticRegenerators.cs` at claim time.

## Reserved scope while ACTIVE

- `src/QS3D.Core/Services/SemanticRegenerators.cs` (`WallRegenerator.LinkedOpeningArea` only)
- one focused Core smoke regression for semantic wall HostWallId canonicality
- smoke registration only if needed
- this claim file

## Excluded scope

- `StructuralRegenerator.cs`
- Model Health / reporting / HostLink / physical opening ownership
- opening geometry and curtain/wall-pier runtime generation
- BricsCAD V25/V26 runtime, packaging, signing, private DWG, GitHub Actions

## Intended contract

- missing `HostWallId` remains unhosted and ignored;
- exact empty string remains optional/unhosted;
- whitespace-only or surrounding-whitespace HostWallId fails closed before semantic wall quantities are committed;
- canonical host id preserves current case-insensitive matching and linked-opening deduction behavior;
- null semantic project elements fail visible rather than being skipped accidentally.

No source/test files were changed under this claim, and no Actions/build/runtime PASS is claimed.

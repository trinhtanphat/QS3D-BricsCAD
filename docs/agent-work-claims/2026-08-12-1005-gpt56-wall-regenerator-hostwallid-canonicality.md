# Work claim — WallRegenerator HostWallId canonicality

- Status: `ACTIVE`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:05:30+07:00`
- Baseline main SHA: `7f3e74ce667450d4e78ab9bba883c40b86380021`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`WallRegenerator.LinkedOpeningArea()` currently compares persisted opening/door `HostWallId` directly with the semantic wall id. A padded or whitespace-only persisted HostWallId can therefore be silently ignored instead of failing visible, under-deducting opening area for ArchitecturalWall / GlassWall / WallPier quantity regeneration. The corresponding StructuralRegenerator path now rejects non-canonical HostWallId values before wall quantity mutation.

## Non-overlap check

Recent HostWallId work covers baseline Model Health, Door/opening reporting, physical opening ownership, and StructuralRegenerator. No claim/commit was found for the `WallRegenerator` path in `SemanticRegenerators.cs`.

## Reserved scope

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

## Intended validation

Add focused Core smoke coverage proving canonical linked openings are deducted and padded/whitespace-only host ids fail closed for the semantic WallRegenerator path. No Actions or BricsCAD runtime PASS will be claimed unless explicitly executed.

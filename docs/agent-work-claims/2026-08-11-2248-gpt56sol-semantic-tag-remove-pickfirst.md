# Work claim — Semantic Tag Remove PICKFIRST

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-semantic-tag-remove-pickfirst-20260811-2248`
- Registered: `2026-08-11T22:48:00+07:00`
- Baseline main SHA: `5bccb132a11babd4d5b69ca13ecf6f34d9a374f0`
- Priority: remove redundant repicking from explicit Semantic Tag removal

## Reserved scope

Allow `QS3DTAGREMOVE` to consume exactly one implied/PICKFIRST generated Semantic Tag MText or authoritative semantic source before falling back to its existing `Editor.GetEntity(...)` picker, preserving complete selection-before-bind and destructive-removal safety boundaries.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SemanticTagRemovalCommands.cs`
- one focused static preflight under `scripts/`
- one focused documentation note under `docs/`
- this claim file for close-out metadata

## Excluded scope

- No `SemanticTagRemovalService` destructive lifecycle rewrite.
- No ownership model, builder, tag content, health or refresh changes.
- No Workspace/Ribbon redesign.
- No broad semantic-tag refactor.
- No GitHub Actions dispatch or BricsCAD V25 runtime PASS claim.

## Defect evidence

`QS3DTAGREMOVE` is currently `CommandFlags.Modal` and always calls `Editor.GetEntity(...)`. Users who preselect the generated Semantic Tag MText or its authoritative source must select the same object a second time before removal.

## Validation plan

- Add `CommandFlags.UsePickSet`.
- Consume exactly one implied selection first; zero implied selection preserves explicit picker fallback.
- Multiple implied selections fail closed before canonical project binding/destructive removal.
- Preserve `ResolveTagOwner(...)` generated-tag slot validation and source ambiguity checks.
- Preserve `SemanticTagRemovalService.Remove(...)` as the only destructive implementation.
- Add static guard for selection-before-bind/remove ordering and no-bootstrap behavior.

## Completion condition

Source, focused regression contract and documentation are merged into current `main`; claim is closed with exact merge SHA and runtime-only behavior remains pending V25 qualification.
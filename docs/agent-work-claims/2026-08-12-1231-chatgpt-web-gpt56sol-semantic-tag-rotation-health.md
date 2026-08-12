# Work claim — Semantic Tag rotation metadata health

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-rotation-health`
- Registered: `2026-08-12T12:31:00+07:00`
- Baseline main SHA: `ed05830886404e3f3c78b2ed8699486bd2c18cd4`
- Priority: P1 — writer-owned Semantic Tag rotation metadata must not bypass health validation.
- Task Key: `CORE-SEMANTIC-TAG-ROTATION-HEALTH`

## Confirmed defect

`SemanticTagBuilder.Build(...)` always validates a finite `rotationRadians` then persists `GeneratedSemanticTagRotationRad` using `double.ToString("R", CultureInfo.InvariantCulture)`. `GeneratedSemanticTagHealthService` never reads that field. `GeneratedSemanticTagRuntimeHealthService` compares live MText rotation only when the stored rotation parses as finite, so missing/non-finite metadata silently skips the runtime drift check as well.

Consequently a generated Semantic Tag can retain missing, `NaN`, `Infinity`, or alternate non-writer rotation text without any rotation-metadata health evidence.

## Non-overlap check

Recent commit search found no Semantic Tag rotation metadata health lane. Open PR #882 owns Bulk Edit ID-target freshness and does not overlap Semantic Tag diagnostics.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs`
- one focused Core smoke regression for `GeneratedSemanticTagRotationRad`
- this claim file

Do not modify Semantic Tag builder/runtime health, owner/template/text/position metadata, generated handle ownership, persistence format, command wrappers, or BricsCAD runtime code.

## Intended contract

- Generated Semantic Tag rotation metadata must be present and parse as a finite invariant number or emit `SEMANTIC_TAG_ROTATION_INVALID` as Error.
- After finite validity, raw text must equal `value.ToString("R", CultureInfo.InvariantCulture)` or emit `SEMANTIC_TAG_ROTATION_NON_CANONICAL` as Error.
- Invalid/missing values do not receive canonicality noise.
- Exact writer-owned round-trip rotation strings, including `0`, preserve existing behavior.
- Elements without generated Semantic Tag handles remain unaffected.

## Completion condition

Missing/non-finite/noncanonical rotation metadata is fail-visible, focused smoke coverage pins those cases plus zero/canonical/no-handles controls, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.

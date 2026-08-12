# Work claim — Grid Naming persisted canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-naming-canonicality`
- Registered: `2026-08-12T08:39:00+07:00`
- Baseline main SHA: `deef1042c1078f2982d03a31907c4001c586f9e2`
- Priority: P1 — persisted Grid naming metadata must match writer-owned canonical spelling.
- Task Key: `CORE-GRID-NAMING-CANONICALITY`

## Confirmed defect

`GridNamingService.Renumber(...)` writes exact `GridLabel` text and writes `GridSequenceIndex` with invariant `int.ToString(...)`. `GridNamingHealthService`, however, trims both stored values before validating them and accepts sequence strings such as `"01"` as integer `1`. Malformed or externally edited persisted Grid metadata can therefore use spellings the writer never emits and still pass naming health.

## Non-overlap check

Existing Grid Naming lanes already completed null-element handling, reserved-label integrity and bounded input enumeration. No recent claim/commit was found for persisted Grid label/sequence canonical spelling.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GridNamingHealthService.cs`
- one focused Core smoke regression for Grid label/sequence canonicality
- this claim file

Do not modify `GridNamingService.Renumber`, Grid commands/UI, annotation generation, persistence format or BricsCAD runtime code.

## Intended contract

- Padded stored Grid labels fail visible as `HealthSeverity.Error` instead of being silently trimmed.
- Padded stored sequence values fail visible.
- Numerically valid but non-writer-canonical sequence spellings such as leading-zero `"01"` fail visible.
- Canonical labels and canonical invariant sequence strings preserve existing duplicate/empty/range behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Malformed persisted Grid label/sequence spellings are fail-visible, focused Core smoke coverage pins padded label, padded sequence, leading-zero sequence and canonical control cases, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.

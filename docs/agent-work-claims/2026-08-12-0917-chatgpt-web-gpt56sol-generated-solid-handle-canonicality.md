# Work claim — Generated Solid handle canonical spacing

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-solid-handle-canonicality`
- Registered: `2026-08-12T09:17:00+07:00`
- Baseline main SHA: `7e1b3ca2f5f1c50a4ef49323fb5dcd738cbf4c21`
- Priority: P1 — persisted Generated Solid handle text must preserve the writer-owned trimmed contract.
- Task Key: `CORE-MODEL-HEALTH-GENERATED-SOLID-HANDLE-CANONICALITY`

## Confirmed defect

`GeneratedGeometryService.CommitReplacement(...)` persists `GeneratedSolidHandle` as `generatedHandle.Trim()`. Baseline `ModelHealthService.ValidateGeneratedGeometry(...)` currently trims the stored handle before hexadecimal validation and ownership checks, so externally edited values such as `" A "` can pass as valid handle `A` without any health evidence even though the canonical writer never emits surrounding whitespace.

## Non-overlap check

Recent generated handle lanes cover empty-list tokens for multi-handle providers and native ownership/fatal runtime behavior. The completed Generated Solid ownership/category canonicality lanes cover different metadata fields. No recent claim/commit was found for surrounding-whitespace canonicality of the scalar `GeneratedSolidHandle` property.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- one focused Core smoke regression for Generated Solid handle spacing
- this claim file

Do not normalize hex case, modify handle ownership semantics, native XData, `GeneratedGeometryService`, builders, persistence format or BricsCAD runtime code.

## Intended contract

- A non-empty hexadecimal handle with leading/trailing whitespace emits a dedicated `HealthSeverity.Error` canonicality diagnostic.
- Existing invalid-handle, duplicate ownership, source-handle overlap and live-handle diagnostics continue to use the trimmed handle value.
- Exact trimmed handles preserve existing behavior.
- Inspection remains read-only and deterministic.

## Completion condition

Padded handles are fail-visible without changing hex-case semantics, focused smoke coverage pins padded/canonical/invalid behavior, source + smoke are read back from merged `main`, ancestry is verified, and this claim is closed with exact commit SHAs.

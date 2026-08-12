# Work claim — Comprehensive health collision-free issue identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-comprehensive-health-key-collision-20260812`
- Registered: `2026-08-12T11:18:00+07:00`
- Baseline main SHA: `f11e000bc4760fb16c7a9e3935427b9ca71666df`
- Task Key: `CORE-COMPREHENSIVE-HEALTH-KEY-COLLISION`

## Defect

`ComprehensiveModelHealthService.Add(...)` de-duplicates provider issues with a newline-delimited string built from severity, upper-cased code, upper-cased element id and ordinary message. `ModelHealthIssue` permits embedded newlines, so distinct provider issues can produce the same identity and one can be silently dropped before callers or baseline capture see it.

## Scope

- `src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ComprehensiveModelHealthStructuralIdentitySmoke.cs`
- this claim file

## Contract

Use collision-free identity encoding while preserving severity sensitivity, case-insensitive code/element identity, exact ordinary-message identity, and existing `*_STALE` message-insensitive de-duplication. Preserve provider ordering, provider failure isolation/redaction, generated-output targeting and input handle normalization.

No GitHub Actions/full build/executable smoke/BricsCAD runtime PASS is claimed unless actually executed.

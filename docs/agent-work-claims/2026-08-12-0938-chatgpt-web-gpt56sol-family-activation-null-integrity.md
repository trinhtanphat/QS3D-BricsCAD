# Work claim — Family activation null integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-activation-null-integrity-20260812-0938`
- Registered: `2026-08-12T09:38:00+07:00`
- Baseline main SHA: `91c1754d03c340e395045ad8b6dcace0a3af7d35`
- Priority: evidence-driven Core malformed-project fail-closed integrity

## Confirmed defect

`ProjectFamilyActivationService.ValidateUniqueFamilyIds(...)` currently skips `null` entries in `project.Families`. As a result, `GetActive(...)`, `SetActive(...)` and `ClearIfMissing(...)` can continue reading or mutating active-family metadata on a malformed project that the persistence/global family-integrity contracts reject.

## Intended fix

Fail closed when the Family collection contains a null entry before resolving or mutating active-family state. Preserve the completed duplicate-family guard, case-insensitive canonical family resolution, existing no-op behavior, `Touch()` semantics and metadata key/value format.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectFamilyActivationService.cs`
- new focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Coordination

The immediately preceding family activation duplicate-integrity lane is `COMPLETED`. This lane does not edit `ProjectFamilyService`, template application, family property-key canonicality, Workspace UI, persistence or local V25/native paths.

## Validation boundary

Source-safe focused regression + exact readback only. No GitHub Actions dispatch; no full Core build/smoke PASS or BricsCAD V25/V26 runtime PASS claimed without execution.
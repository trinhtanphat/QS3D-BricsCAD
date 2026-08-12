# Work claim — Previous Family category integrity

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-family-previous-category-integrity-20260812-1028`
- Registered: `2026-08-12T10:28:00+07:00`
- Priority: confirmed Core mutation-integrity defect

## Confirmed defect

`ModelHealthService.ValidateFamily(...)` reports `FAMILY_CATEGORY_MISMATCH` when an element references a Family whose category differs from the element category, and Project Browser query validation already fails closed on the same relation. `ProjectFamilyService.Assign(...)` validates the target Family category but, while preparing reassignment, resolves a previous Family and consumes its validated property snapshot without requiring `previous.Category == element.Category`. A malformed cross-category previous Family can therefore participate in inherited-default cleanup before the element is moved to a valid target Family.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignmentAtomicitySmoke.cs`
- this claim file

## Intended fix

Fail closed during assignment planning when a nonblank previous Family resolves to a different category than the element, before `ProjectState.Touch()` or any element mutation. Preserve target validation, missing/duplicate Family guards, canonical no-op behavior, stale-enumeration guards, previous-Family property snapshot validation, inherited-default cleanup and Bulk Edit behavior.

## Validation boundary

Add focused atomicity regression proving FamilyId, properties, dirty state, element timestamp, project ChangeVersion and project timestamp remain unchanged on rejection. Source-safe readback only; no GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claimed without execution.

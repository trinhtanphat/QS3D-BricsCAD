# Work claim — Previous Family category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-family-previous-category-integrity-20260812-1028`
- Registered: `2026-08-12T10:28:00+07:00`
- Completed: `2026-08-12T10:34:00+07:00`
- Pull Request: `#760`
- Reviewed head: `28348e19707af7c821bdd31f8e8648b080f877cb`
- Merge SHA: `b831401b0def350991e3912c8bc7544ce454476c`
- Priority: confirmed Core mutation-integrity defect

## Confirmed defect

`ModelHealthService.ValidateFamily(...)` reports `FAMILY_CATEGORY_MISMATCH` when an element references a Family whose category differs from the element category, and Project Browser query validation already fails closed on the same relation. `ProjectFamilyService.Assign(...)` validated the target Family category but previously resolved a nonblank previous Family and consumed its defaults without requiring that previous Family to match the element category.

## Completed implementation

- Assignment planning now rejects a resolved previous Family whose category differs from the element category.
- Rejection occurs before inherited-default cleanup, `ProjectState.Touch()` or any element mutation.
- Target validation, missing/duplicate Family guards, canonical no-op behavior, stale-enumeration guards and previous-Family property snapshot validation remain unchanged.
- Focused Core smoke proves FamilyId, properties, dirty state, element timestamp, project ChangeVersion and project timestamp remain unchanged on rejection.

## Evidence

Current-main-to-PR-head comparison isolated the net diff to `ProjectFamilyService.cs` plus `ProjectFamilyPreviousCategoryIntegritySmoke.cs`, despite stale branch metadata showing unrelated already-main files. PR #760 then squash-merged at `b831401b0def350991e3912c8bc7544ce454476c` with expected-head locking.

## Validation boundary

No GitHub Actions/full build/executable smoke or BricsCAD V25/V26 runtime PASS is claimed.

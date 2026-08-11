# Work claim — ProjectFamilyService assignment null-target integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-assign-null-target`
- Registered: `2026-08-12T00:33:00+07:00`
- Completed: `2026-08-12T00:35:00+07:00`
- Baseline main SHA: `79c778bdbb0bbbeb89792ba971e75f7d741b6f63`
- Reservation commit: `8b6ac403113f94e77ea723d5937be33714482946`
- Priority: P1 — malformed batch assignment targets must fail closed before any semantic mutation.

## Defect fixed

`ProjectFamilyService.Assign(...)` routes caller targets through `ResolveOwnedElements(...)`, where a null entry was previously handled with `if (element == null) continue;`. A malformed target collection could therefore be partially accepted while the null target silently disappeared.

Null assignment targets now throw `ArgumentException` during ownership preflight, before pending assignment planning, `project.Touch()`, or any family/property/dirty-state mutation.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignNullTargetSmoke.cs`
- this claim file

## Published commits

- `31916ec68a8eea4084e2d75addab3ccd24031a1f` — reject null family-assignment targets during preflight.
- `fe27579b44e48c24d2d8b7627add2595c04f7fd9` — add isolated auto-registered regression proving atomic null-target failure plus valid assignment success.

## Delivered contract

- A null target entry fails before project revision or semantic mutation.
- Valid target ownership/category/canonical no-op behavior remains unchanged.
- Duplicate caller references continue to collapse to one owned element as before.

## Validation notes

- Exact source diff changes the prior null-skip branch only; the incidental final newline normalization has no semantic effect.
- Regression checks `ChangeVersion`, dirty flags, family relation and inherited material remain unchanged after malformed input, then confirms a valid assignment changes exactly once.
- Dedicated smoke auto-registers via `ModuleInitializer`; no shared smoke registry was edited.
- No force-push and no GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.

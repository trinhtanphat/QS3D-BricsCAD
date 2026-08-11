# Work claim — ProjectFamilyService assignment null-target integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-assign-null-target`
- Registered: `2026-08-12T00:33:00+07:00`
- Baseline main SHA: `79c778bdbb0bbbeb89792ba971e75f7d741b6f63`
- Priority: P1 — malformed batch assignment targets must fail closed before any semantic mutation.

## Confirmed defect

`ProjectFamilyService.Assign(...)` routes caller targets through `ResolveOwnedElements(...)`, where a null entry is currently handled with `if (element == null) continue;`. A malformed target collection can therefore be partially accepted while the null target silently disappears. Other current batch mutation boundaries reject null targets, and family assignment already validates ownership/category before the single mutation phase.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyAssignNullTargetSmoke.cs` (new auto-registered focused smoke)
- this claim file

## Intended contract

- A null target entry causes `ArgumentException` before `project.Touch()` or any family/property/dirty-state mutation.
- Valid target ownership/category/canonical no-op behavior remains unchanged.
- Duplicate caller references continue to collapse to one owned element as before.

## Coordination

The earlier completed family assignment canonical-noop lane addressed padded/case-equivalent Family references, not null target entries. No current exact claim was found for this source behavior.

## Validation plan

- Pass one valid owned element followed by null and prove family relation, properties, dirty state and project `ChangeVersion` remain unchanged.
- Confirm a valid single-target assignment still succeeds exactly once.
- Re-fetch source before update, SHA-guard write, inspect exact diffs, then close this claim.
- No GitHub Actions dispatch; no executable .NET or BricsCAD V25 runtime PASS claim from this hosted environment.

## Completion condition

Family assignment cannot silently discard null targets and remains atomic before mutation, focused regression is on `main`, and this claim is closed.

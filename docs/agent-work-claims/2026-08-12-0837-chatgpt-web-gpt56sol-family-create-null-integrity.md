# Work claim — Family Create null-collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-create-null-integrity-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Baseline main SHA: `fb9d011d2e251585c61b932c0e14248c25e00110`
- Priority: evidence-driven Core mutation integrity during owner-requested full review/fix continuation

## Confirmed defect

`ProjectFamilyService.Create(...)` dereferences existing entries through `project.Families.Any(x => x.Id ...)` and `EnsureUniqueName(... x.Category ...)` before validating the persisted Family collection. A malformed project containing a null Family entry can therefore fail with an incidental `NullReferenceException` instead of the domain fail-closed integrity contract before mutation. `ProjectState.FindFamily(...)` already treats null Family entries as invalid state, and the sibling Floor/Zone Create paths have explicit null-collection preflight semantics.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — `Create(...)` persisted Family preflight only.
- one focused CAD-independent Core smoke fixture/registration if needed.
- this claim file.

## Contract

Reject null existing Family entries before max-count, duplicate-id/name checks, `project.Touch()`, or collection mutation. Preserve defined-category/name/id limits, duplicate semantics, all Family assignment/property behavior, and true no-op behavior elsewhere.

## Validation plan

Add deterministic smoke coverage proving malformed null-Family state throws `InvalidOperationException`, leaves `ChangeVersion` and Family count unchanged, and ordinary Family Create still succeeds. Re-fetch current source before every write; never force-push. No GitHub Actions dispatch or BricsCAD runtime qualification claim.

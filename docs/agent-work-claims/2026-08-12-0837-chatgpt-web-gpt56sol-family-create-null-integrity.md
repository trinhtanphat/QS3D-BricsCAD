# Work claim — Family Create null-collection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-create-null-integrity-20260812-0837`
- Registered: `2026-08-12T08:37:00+07:00`
- Completed: `2026-08-12T08:40:00+07:00`
- Baseline main SHA: `fb9d011d2e251585c61b932c0e14248c25e00110`
- Claim commit: `c624082fb20b12437de98ab90106c8af385f2ca1`
- Implementation commit: `e8de8effb41facd89927a329ecf4281349445282`
- Regression-test commit: `aee4e574a10b754225fe14ba64f59675d7bfac1b`
- Final pushed product/test SHA: `aee4e574a10b754225fe14ba64f59675d7bfac1b`
- Priority: evidence-driven Core mutation integrity during owner-requested full review/fix continuation

## Confirmed defect

`ProjectFamilyService.Create(...)` dereferenced existing entries through `project.Families.Any(x => x.Id ...)` and `EnsureUniqueName(... x.Category ...)` before validating the persisted Family collection. A malformed project containing a null Family entry could therefore fail with an incidental `NullReferenceException` instead of the domain fail-closed integrity contract before mutation. `ProjectState.FindFamily(...)` already treats null Family entries as invalid state, and sibling Floor/Zone Create paths use explicit null-collection preflight semantics.

## Implemented

`ProjectFamilyService.Create(...)` now rejects any null existing Family entry with the canonical `InvalidOperationException` before max-count, duplicate-id/name checks, `project.Touch()`, or collection mutation. All existing Family id/name/category limits and downstream assignment/property behavior remain unchanged.

## Regression coverage

`ProjectFamilyCreateNullPreflightSmoke` now pins both sides of the contract:

- malformed null-Family state throws the canonical integrity error and leaves Family count, `ChangeVersion`, and `UpdatedUtc` unchanged;
- ordinary Family Create still publishes exactly one Family and advances `ChangeVersion` once.

## Validation boundary

The source and smoke files were re-read from current `main` after their writes. No GitHub Actions workflow was dispatched or re-run. No executable Core build/smoke PASS and no licensed BricsCAD V25/V26 runtime qualification are claimed from this web session.

## Outcome

Family creation now matches Floor/Zone fail-closed persisted-collection integrity semantics without broadening the mutation surface or changing valid authoring behavior.

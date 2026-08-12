# Work claim — Family Create duplicate-existing integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-family-create-duplicate-integrity-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Completed: `2026-08-12T08:50:00+07:00`
- Baseline main SHA: `89123cc38efb8581398398c6075f5487d39e5925`
- Claim commit: `b9cb6ed68605c5f80d117f6f5ce73564f08d08f2`
- Source fix commit: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`
- Focused smoke commit: `15d2f198397748f399efc4f4478ff936d7c21464`
- Priority: P2 — prevent mutation of project state already invalid under canonical Family identity.
- Task Key: `CORE-FAMILY-CREATE-DUPLICATE-EXISTING-ID`

## Confirmed defect

`ProjectFamilyService.Create(...)` rejected null existing Family entries but only checked whether the requested new ID already existed. A project containing case-insensitively duplicate existing Family IDs unrelated to the requested new ID could therefore be touched and extended even though `ProjectState.FindFamily(...)` and QSDB validation reject that identity state.

## Implemented contract

- Existing non-null Family entries are scanned for case-insensitively duplicate IDs before any Create mutation.
- Duplicate existing IDs fail closed with `InvalidOperationException("Project contains duplicate family id: <id>.")` before max-count, requested-ID/name checks, `Touch()` or append.
- Valid Create behavior, null-entry guard, limits, requested-ID collision, category/name semantics and `Duplicate()` delegation remain unchanged.
- Family property/assignment/UI, Floor/Zone services, persistence/interchange and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms the duplicate-existing-ID preflight is present directly after the null-family guard.
- `ProjectFamilyCreateDuplicateIntegritySmoke` is auto-registered with `ModuleInitializer`; it covers a case-only existing duplicate, proves Family count/ChangeVersion/UpdatedUtc remain unchanged on rejection, and preserves valid Create with exactly one revision advance.
- This connector-only session did not execute .NET smoke, GitHub Actions or licensed BricsCAD runtime tests.

## Completion

`COMPLETED`: Family creation no longer mutates a project whose existing Family identity collection is already duplicate-invalid.

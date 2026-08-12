# Work claim — Family Create duplicate-existing integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-create-duplicate-integrity-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Baseline main SHA: `89123cc38efb8581398398c6075f5487d39e5925`
- Priority: P2 — prevent mutation of project state already invalid under canonical Family identity.
- Task Key: `CORE-FAMILY-CREATE-DUPLICATE-EXISTING-ID`

## Confirmed defect

`ProjectFamilyService.Create(...)` rejects null existing Family entries but only checks whether the requested new ID already exists. If the current project already contains case-insensitively duplicate Family IDs unrelated to the requested new ID (for example `F1` and `f1`, then create `F2`), Create currently advances the project revision and appends another Family. `ProjectState.FindFamily(...)` and QSDB validation already fail closed on duplicate Family IDs, so Create must not mutate this invalid state.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` — Create preflight only
- `tests/QS3D.Core.SmokeTests/ProjectFamilyCreateDuplicateIntegritySmoke.cs` — focused regression
- this claim file

## Intended contract

- Existing non-null Family entries must have case-insensitively unique IDs before any Create mutation.
- Duplicate existing IDs fail closed before max-count, requested-ID/name checks, `Touch()` or append.
- Valid Create behavior, null-entry guard, limits, requested-ID collision, category/name semantics and Duplicate() delegation remain unchanged.
- Do not modify Family property/assignment/UI, Floor/Zone services, persistence/interchange, or native BricsCAD code.

## Validation plan

Focused Core smoke covers case-only existing duplicate IDs with Family count/ChangeVersion/UpdatedUtc unchanged, plus valid Family Create control. Re-fetch moving `main` and exact source before each write. No force-push, Actions dispatch, or BricsCAD runtime PASS claim.

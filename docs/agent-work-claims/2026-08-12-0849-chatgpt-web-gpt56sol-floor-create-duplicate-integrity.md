# Work claim — Floor Create duplicate-existing integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-create-duplicate-integrity-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Baseline main SHA: `56bf20302f4b4b9c1d4ed6103eedbaf95cff8af6`
- Priority: P2 — prevent mutation of project state already invalid under canonical Floor identity.

## Confirmed defect

`ProjectFloorService.Create(...)` rejects null existing Floor entries and the requested new ID, but it does not reject case-insensitively duplicate Floor IDs already present elsewhere in the collection. A malformed collection such as `F1` + `f1`, followed by `Create(..., "F2", ...)`, can pass preflight, call `ProjectState.Touch()`, append a Floor and potentially initialize active state even though `ProjectState.FindFloor(...)`, QSDB validation and Browser/reference logic treat duplicate Floor identity as invalid.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs` — Create preflight only
- `tests/QS3D.Core.SmokeTests/ProjectFloorCreateDuplicateIntegritySmoke.cs` — new focused regression
- this claim file

## Intended fix

- Before any Create mutation, require all existing non-null Floor IDs to be case-insensitively unique.
- Duplicate existing IDs fail closed before max/new-ID/name checks, `Touch()`, append or active-floor initialization.
- Preserve valid Create behavior, max-floor limit, requested-ID collision, unique-name rules, finite elevation validation and active-floor initialization.
- Do not modify Zone/Family services, Floor active same-target alias semantics, native BricsCAD code, persistence or UI.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.

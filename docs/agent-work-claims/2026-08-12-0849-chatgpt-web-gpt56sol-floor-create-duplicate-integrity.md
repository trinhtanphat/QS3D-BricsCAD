# Work claim — Floor Create duplicate-existing integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-create-duplicate-integrity-20260812-0849`
- Registered: `2026-08-12T08:49:00+07:00`
- Baseline main SHA: `56bf20302f4b4b9c1d4ed6103eedbaf95cff8af6`
- Priority: P2 — prevent mutation of project state already invalid under canonical Floor identity.

## Confirmed defect

`ProjectFloorService.Create(...)` rejected null existing Floor entries and the requested new ID, but did not reject case-insensitively duplicate Floor IDs already present elsewhere in the collection. A malformed collection such as `F1` + `f1`, followed by `Create(..., "F2", ...)`, could pass preflight and mutate a project already ambiguous under `FindFloor`, QSDB and Browser/reference semantics.

## Implemented fix

- Before any Create mutation, existing non-null Floor IDs are scanned for case-insensitive duplicates.
- Duplicate existing IDs fail closed before max/new-ID/name checks, `Touch()`, append or active-floor initialization.
- Valid Create behavior, max-floor limit, requested-ID collision, unique-name rules, finite elevation validation and active-floor initialization remain unchanged.
- Floor active same-target alias semantics, Zone/Family services, persistence/UI and native BricsCAD code were not modified.

## Integration evidence

- Claim registration: `66b7e97fc1c201244c2015c89cb5b653266e6cda`.
- Branch source commit: `41b4cb19846fdd937b21c432694758d6cc35e008`.
- Source-only compare confirmed exactly +4 lines in `ProjectFloorService.cs` and no incidental churn.
- Focused smoke commit: `ee0afd3c65aa7ca40787989cc72bb76a2052f7ad`.
- PR `#666` squash-merged at `b153f491e14ea114a6e433a9bcac699c9536e954`.
- Smoke covers case-only duplicate existing IDs and preserves Floor count, ActiveFloorId, ChangeVersion and UpdatedUtc on rejection; valid second-Floor creation still advances exactly one project revision.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions were dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.

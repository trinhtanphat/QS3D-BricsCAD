# Work claim — ProjectZoneService.Create null-zone preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-zone-create-null-preflight-20260812-0829`
- Registered: `2026-08-12T08:29:00+07:00`
- Baseline main SHA: `84a48c237072098763cfac564e2399f9e214c08c`
- Priority: P2 — align Zone creation with existing fail-closed project collection integrity.

## Confirmed defect

`ProjectZoneService.Create(...)` performs duplicate-id and duplicate-name checks by dereferencing every existing `project.Zones` entry. If malformed in-memory/persisted state contains a null zone entry, creation leaks a raw `NullReferenceException` before mutation instead of failing closed with a stable project-integrity error. The neighboring Floor Create lifecycle now explicitly preflights the same malformed collection shape.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` — `Create(...)` null-zone preflight only
- `tests/QS3D.Core.SmokeTests/ProjectZoneCreateNullPreflightSmoke.cs` — focused regression
- this claim file

## Intended fix

- Reject any null existing Zone entry before count/duplicate-id/name traversal or project mutation.
- Use a stable `InvalidOperationException("Project zone collection contains a null zone.")` contract.
- Preserve valid Create behavior, maximum-zone bound, duplicate id/name checks, active-zone initialization, timestamps and revision semantics.
- Do not alter Zone Update/Delete/Assign/SetActive, zone identity/casing policy, QSDB/interchange, UI or BricsCAD runtime.

## Validation plan

- Focused auto-registered Core smoke creates malformed Zone state, records collection/active-zone/revision/timestamp state, calls Create, requires the stable integrity exception, and proves no mutation occurred.
- Re-read exact source/test on moving `main` after integration and close this claim with exact SHAs.
- No GitHub Actions dispatch and no BricsCAD runtime PASS claim from this remote lane.

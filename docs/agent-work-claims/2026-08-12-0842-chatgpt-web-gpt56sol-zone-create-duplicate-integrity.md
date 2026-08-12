# Work claim — Zone Create duplicate-existing integrity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-zone-create-duplicate-integrity-20260812-0842`
- Registered: `2026-08-12T08:42:00+07:00`
- Completed: `2026-08-12T08:48:00+07:00`
- Baseline main SHA: `728c47962178c8e6b46d8e739a80d30b2454a61b`
- Claim commit: `e280a96847a1b3b6b59d6dbecc3b1d164a4cf835`
- Source fix commit: `8337ebf89428b485d79941668e32beec5a9e26b9`
- Focused smoke commit: `0045bc88048a88a544641900a26e3fa83f7083a3`
- Priority: P2 — prevent mutation of project state already invalid under canonical Zone identity.
- Task Key: `CORE-ZONE-CREATE-DUPLICATE-EXISTING-ID`

## Confirmed defect

`ProjectZoneService.Create(...)` rejected null existing Zone entries but only checked whether the requested new ID already existed. If the existing collection was already malformed with two case-insensitively duplicate Zone IDs unrelated to the requested new ID (for example `Z1` and `z1`, then create `Z2`), Create could call `ProjectState.Touch()` and append the new Zone even though `ProjectState.FindZone(...)`, QSDB validation and interchange validation reject that project identity state.

## Implemented contract

- Before any Create mutation, existing non-null Zone entries are scanned for case-insensitively duplicate semantic IDs.
- Duplicate existing IDs fail closed with `InvalidOperationException("Project contains duplicate zone id: <id>.")` before max/new-ID/name checks, `Touch()`, append, or active-zone initialization.
- Valid Create behavior, max-zone limit, requested-ID collision, name uniqueness and active-zone initialization remain unchanged.
- Floor/Family services, Floor/Zone active UI, persistence/interchange and native BricsCAD code were not modified.

## Validation evidence

- Current `main` readback confirms `ProjectZoneService.Create(...)` contains the duplicate-existing-ID preflight after the null-entry guard and before mutation.
- `ProjectZoneCreateDuplicateIntegritySmoke` is auto-registered with `ModuleInitializer`; it covers case-only duplicate existing IDs, proves Zone count/ActiveZoneId/ChangeVersion/UpdatedUtc remain unchanged on rejection, and preserves valid second-Zone creation with exactly one project revision advance.
- This remote connector session did not execute the .NET smoke binary or GitHub Actions and does not claim BricsCAD V25/V26 runtime qualification.

## Completion

`COMPLETED`: Zone creation no longer mutates a project whose existing Zone identity collection is already duplicate-invalid.

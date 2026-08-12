# Work claim — Zone Create duplicate-existing integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-create-duplicate-integrity-20260812-0842`
- Registered: `2026-08-12T08:42:00+07:00`
- Baseline main SHA: `728c47962178c8e6b46d8e739a80d30b2454a61b`
- Priority: P2 — prevent mutation of project state already invalid under canonical Zone identity.
- Task Key: `CORE-ZONE-CREATE-DUPLICATE-EXISTING-ID`

## Confirmed defect

`ProjectZoneService.Create(...)` now rejects null existing Zone entries, but it only checks whether the requested new ID already exists. If the existing collection is already malformed with two case-insensitively duplicate Zone IDs unrelated to the requested new ID (for example `Z1` and `z1`, then create `Z2`), Create passes its current checks, calls `ProjectState.Touch()`, and appends the new Zone. `ProjectState.FindZone(...)`, QSDB validation and interchange validation already reject duplicate semantic Zone IDs, so Create must not mutate such a project.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs` — Create preflight only
- `tests/QS3D.Core.SmokeTests/ProjectZoneCreateDuplicateIntegritySmoke.cs` — focused regression
- this claim file

## Intended contract

- Before any Create mutation, existing non-null Zone entries must have case-insensitively unique semantic IDs.
- Duplicate existing IDs fail closed with a stable `InvalidOperationException` before `Touch()`, append, or active-zone initialization.
- Valid Create behavior, max-zone limit, requested-ID collision, name uniqueness and active-zone initialization remain unchanged.
- Do not modify Floor/Family services, Floor/Zone active UI, persistence/interchange, or native BricsCAD code.

## Validation plan

Focused Core smoke will create a malformed project containing exact/case-only duplicate existing Zone IDs, capture Zone count/ActiveZoneId/ChangeVersion/UpdatedUtc, require fail-closed Create, and prove no mutation. A canonical valid control must still create normally. Re-fetch moving `main` and exact source before each write; no force-push, Actions dispatch, or BricsCAD runtime PASS claim.

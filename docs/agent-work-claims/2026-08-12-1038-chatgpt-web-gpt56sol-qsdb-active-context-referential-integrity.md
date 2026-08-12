# Work claim — QSDB active Floor/Zone referential integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-qsdb-active-context-referential-integrity-20260812-1038`
- Registered: `2026-08-12T10:38:00+07:00`
- Baseline main SHA: `021b87c2da05c0b78120160163d9497c30aac0b9`
- Priority: owner-requested continue-all persistence integrity repair

## Confirmed defect

Current `QsdbProjectStore.ValidateProject(...)` rejects non-canonical `ActiveFloorId` / `ActiveZoneId`, but it does not reject a syntactically canonical active ID that references no loaded floor/zone definition. The loader can therefore materialize an orphan active context that the domain mutation APIs cannot create: `ProjectFloorService.SetActive(...)` / `ProjectZoneService.SetActive(...)` require an existing definition, while delete operations reject deletion of the active definition.

This is distinct from the completed active-ID canonicalization lane: exact canonical spelling may still reference nothing.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `tests/QS3D.Core.SmokeTests/QsdbTimestampValidationSmoke.cs` (or one focused QSDB smoke if current registration changes before write)
- this claim file

## Intended repair

- Fail closed when a non-empty `ActiveFloorId` does not resolve to exactly one persisted floor.
- Fail closed when a non-empty `ActiveZoneId` does not resolve to exactly one persisted zone.
- Preserve empty active context, canonical active IDs, duplicate-ID validation, legacy migration behavior, and current canonicalization checks.
- Add focused regression coverage for both orphan active references.

## Evidence

- Baseline `main`: `021b87c2da05c0b78120160163d9497c30aac0b9`.
- `ProjectState.FindFloor(...)` / `FindZone(...)` resolve IDs case-insensitively and reject duplicate matches.
- Floor/Zone `SetActive(...)` requires an existing definition and writes the owned canonical ID.
- Floor/Zone `Delete(...)` rejects deletion of the active definition.
- Commit search found no current QSDB orphan-active/referential-integrity repair; the recent active Floor/Zone work is canonicalization-only.

## Validation boundary

Deterministic source/smoke diff and GitHub readback only. No GitHub Actions/full .NET build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.

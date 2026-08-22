# Work claim — QSDB active Floor/Zone referential integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-qsdb-active-context-referential-integrity-20260812-1038`
- Registered: `2026-08-12T10:38:00+07:00`
- Baseline main SHA: `021b87c2da05c0b78120160163d9497c30aac0b9`
- Priority: owner-requested continue-all persistence integrity repair
- Source integration: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e` (PR #768)
- Regression integration: `6ee1f26110e7c39e0becf74eca6f012f784cffbe` (PR #774)

## Confirmed defect

`QsdbProjectStore.ValidateProject(...)` rejected non-canonical `ActiveFloorId` / `ActiveZoneId`, but it did not reject a syntactically canonical active ID that referenced no loaded floor/zone definition. The loader could therefore materialize an orphan active context that the domain mutation APIs cannot create: `ProjectFloorService.SetActive(...)` / `ProjectZoneService.SetActive(...)` require an existing definition, while delete operations reject deletion of the active definition.

This was distinct from the completed active-ID canonicalization lane: exact canonical spelling could still reference nothing.

## Completed repair

- `QsdbProjectStore.ValidateProject(...)` now fails closed when a non-empty `ActiveFloorId` does not match an existing floor definition (case-insensitive ID identity).
- It likewise fails closed when a non-empty `ActiveZoneId` does not match an existing zone definition.
- Duplicate floor/zone checks still run before referential checks, preserving explicit duplicate-ID failures.
- Empty active context and valid resolved active IDs remain accepted.
- Concurrent QSDB XML-text preflight work that landed after this claim was preserved during source integration.

## Regression coverage

Added and registered `QsdbActiveContextReferentialIntegritySmoke` with:

- `RejectsOrphanActiveFloorId()`
- `RejectsOrphanActiveZoneId()`
- `AcceptsResolvedActiveContextIds()`

Current `main` readback confirmed the source guards, the focused smoke file, and its `SmokeTestRegistration` entry.

## Evidence

- Baseline `main`: `021b87c2da05c0b78120160163d9497c30aac0b9`.
- Source integration: `df0a2626a3fb99bd70af57e75660a3fd0a0f496e` via PR #768.
- Regression integration: `6ee1f26110e7c39e0becf74eca6f012f784cffbe` via PR #774.
- `ProjectState.FindFloor(...)` / `FindZone(...)` resolve IDs case-insensitively and reject duplicate matches.
- Floor/Zone `SetActive(...)` requires an existing definition and writes the owned canonical ID.
- Floor/Zone `Delete(...)` rejects deletion of the active definition.

## Validation boundary

Deterministic source/smoke diff and GitHub readback only. GitHub Actions/full .NET build/release dispatch and licensed BricsCAD V25/V26 runtime were not executed and are not claimed as PASS.

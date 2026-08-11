# Work claim — Core schedule reporting identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-core-schedule-reporting-identity`
- Registered: `2026-08-11T20:20:00+07:00`
- Baseline main SHA: `6d02aa12d792c16597dc4ac7c7f749e333c10a9f`
- Priority: fail closed on blank, duplicate, or case-variant semantic `ProjectElement.Id` values before Core schedule builders aggregate or index project data, preventing silent double-counting and builder-specific incidental failures.

## Reserved scope

Unify the project-element identity boundary used by Core schedule reporting. Material Usage, Curtain Wall, Door Opening and Room Finish schedule builders must reject an invalid semantic project identity set before producing rows. Keep the change read-only/reporting-only and preserve existing quantity, geometry, mutation and UI behavior.

## Expected surfaces

- `src/QS3D.Core/Reporting/` — one focused shared reporting identity guard plus the four schedule builders that consume project elements.
- `tests/QS3D.Core.SmokeTests/` — focused regression coverage and registration only as needed.
- this claim file for close-out.

## Explicit exclusions

- No changes to BricsCAD/WPF/modeless schedule viewers or export UI.
- No Core persistence/mutation, Room Auto regeneration, Direct Draw/Create Similar, material catalog mutation or workspace property behavior.
- No changes to schedule quantity formulas, grouping semantics, CAD measurements, or project mutation paths.
- No GitHub Actions/build/release dispatch.
- No native BricsCAD V25/WPF runtime PASS claim.

## Validation plan

- Re-fetch every target surface immediately before writes and preserve concurrent changes.
- Use a case-insensitive semantic identity set matching the existing `ProjectQuantityReportBuilder` fail-closed contract.
- Add source-level smoke coverage proving duplicate and case-variant IDs cannot silently double-count schedule output, while valid project schedules remain unchanged.
- Review the final branch diff against current `main`; merge only if target files remain non-overlapping with active claims/concurrent commits.

## Coordination

The active Core mutation-atomicity claim owns persistence/mutation hardening and is explicitly excluded here. The active Room Auto regeneration lane is also excluded. The previous repository-audit reporting-identity claim is `COMPLETED`; this claim is narrower and limited to Core schedule builders. UI/modeless schedule surfaces are not reserved by this lane.

## Completion

- PR: `#450` — `fix(reporting): fail closed on duplicate schedule element identities`
- Reviewed feature head before squash: `7c1387c3fcbfc0bbb244bf858c9f56d094b08ee3`
- Squash merge on `main`: `2b15723bc8670d9ce8e8ead967718ec1bd0eaea7`
- Added `ReportingProjectIdentityGuard` and applied it before aggregation in Material Usage, Curtain Wall, Door/Opening and Room Finish schedule builders.
- Added and registered `ScheduleReportingIdentitySmoke` covering exact duplicate IDs, case-variant duplicates and a valid unique-ID semantic project.
- Final reviewed feature diff was 7 files / 96 additions / 0 deletions; the four existing schedule builders each changed by one guard call only.
- Concurrent `main` changes were compared before merge and did not overlap the reserved source/test surfaces.
- GitHub Actions/build/release were not dispatched for this lane.
- No native BricsCAD V25/WPF runtime PASS is claimed.

## Completion condition

Satisfied by PR `#450` and merge `2b15723bc8670d9ce8e8ead967718ec1bd0eaea7`: a single intentional Core reporting identity contract is applied to the targeted schedule builders, focused smoke coverage guards the behavior, the change is merged onto `main` without overwriting concurrent work, and this claim is closed without a false runtime/CI claim.

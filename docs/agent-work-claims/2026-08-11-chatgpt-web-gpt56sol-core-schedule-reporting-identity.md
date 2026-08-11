# Work claim — Core schedule reporting identity

- Status: `ACTIVE`
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

## Completion condition

A single intentional Core reporting identity contract is applied to the targeted schedule builders, focused smoke coverage guards the behavior, the change is merged onto current `main` without overwriting concurrent work, and this claim is marked `COMPLETED` with exact implementation commits and no false runtime/CI claim.

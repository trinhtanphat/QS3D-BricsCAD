# Work claim — Zone target operations null collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-zone-target-null-integrity-20260812-0915`
- Registered: `2026-08-12T09:15:00+07:00`
- Baseline main SHA: `870811fb578f6afa7231fd0b9636139544cdd64f`
- Priority: P1 — Zone target operations must fail closed when the project Zone collection is structurally invalid.

## Reserved scope

Harden `ProjectZoneService` target-based operations so any `null` entry in `project.Zones` is rejected before resolving or using a target Zone. The completed global-duplicate helper currently skips null entries, while Zone Create already rejects null collection entries explicitly.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectZoneService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Zone Create null/duplicate integrity lanes already completed.
- Floor/Family services, Floor/Zone UI audit/no-op behavior, persistence/interchange, native BricsCAD adapters, Actions/release.
- semantic element null/duplicate handling inside `ResolveProjectElements`, which is already fail-closed.

## Validation plan

- Seed a valid target Zone plus an unrelated `null` Zone collection entry.
- Prove representative target mutation paths reject before `ProjectState.ChangeVersion`, Zone state or element assignment changes.
- Prove read-only `ReferenceCount` rejects malformed Zone state.
- Preserve valid Zone rename/assign/reference-count behavior and completed duplicate-ID enforcement.
- Read back exact source/test on moving `main`; no GitHub Actions or licensed BricsCAD runtime PASS claim.

## Coordination

This is the Zone analogue of the completed Family target-null lane (`eb752d4305e91be94ce1011be3ec055a8ec170dc` + `84553f6bd91e1153684b643c9fff7505d27a8325`). Current HostLink/Revision/other claims are independent and do not own `ProjectZoneService` target structural-null behavior.

## Completion condition

The common Zone identity preflight fails closed on null collection entries for all target operations, focused Core smoke coverage is pushed, and this claim is marked `COMPLETED` without dispatching Actions.

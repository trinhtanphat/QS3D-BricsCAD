# Work claim — Floor target operations null collection integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-floor-target-null-integrity-20260812-0918`
- Registered: `2026-08-12T09:18:00+07:00`
- Baseline main SHA: `f734a3c14e517132aae9f597a17cb8a426c1898f`
- Priority: P1 — Floor target operations must fail closed when the project Floor collection is structurally invalid.

## Reserved scope

Harden `ProjectFloorService` target-based operations so any `null` entry in `project.Floors` is rejected before resolving or using a target Floor. The completed global-duplicate helper currently skips null entries, while Floor Create already rejects null collection entries explicitly.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- focused Core smoke coverage under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Excluded scope

- Floor Create null/duplicate integrity and vertical-level numeric/preflight lanes already completed.
- Zone/Family services, Floor/Zone UI audit/no-op behavior, persistence/interchange, native BricsCAD adapters, Actions/release.
- semantic element null/duplicate handling in `ResolveProjectElements`, which is already fail-closed.

## Validation plan

- Seed a valid target Floor plus an unrelated `null` Floor collection entry.
- Prove representative target mutation/assignment paths reject before `ProjectState.ChangeVersion`, Floor state, active Floor, vertical-level metadata or element assignment changes.
- Prove read-only `ReferenceCount` rejects malformed Floor state.
- Preserve valid Floor update/activate/assign/reference-count behavior and completed duplicate-ID enforcement.
- Read back exact source/test on moving `main`; no GitHub Actions or licensed BricsCAD runtime PASS claim.

## Coordination

This is the Floor analogue of the completed Family/Zone target-null lanes. Current unrelated claims do not own `ProjectFloorService` structural-null target behavior; semantic element identity handling and Floor vertical numeric validation remain separate scopes.

## Completion condition

The common Floor identity preflight fails closed on null collection entries for target operations, focused Core smoke coverage is pushed, and this claim is marked `COMPLETED` without dispatching Actions.

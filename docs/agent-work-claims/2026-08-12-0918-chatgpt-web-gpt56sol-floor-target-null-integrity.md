# Work claim — Floor target operations null collection integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-floor-target-null-integrity-20260812-0918`
- Registered: `2026-08-12T09:18:00+07:00`
- Completed: `2026-08-12T09:23:00+07:00`
- Baseline main SHA: `f734a3c14e517132aae9f597a17cb8a426c1898f`
- Priority: P1 — Floor target operations must fail closed when the project Floor collection is structurally invalid.

## Completed scope

`ProjectFloorService` target-based operations now reject any `null` entry in `project.Floors` before resolving or using a target Floor. `ValidateUniqueFloorIds(project)` no longer skips malformed null entries, aligning target operations with the existing Floor Create structural-null contract while preserving vertical-level, dependency, elevation and offset behavior.

## Pushed implementation

- Claim registration: `85b3d62e3fc2ab5820926e543ef4c40d474d4406`
- Source fix: `0fcb532b8659f2ac2f534cb491b6ec53f8ae4f6f`
- Focused Core smoke: `25eb33d7ff14d193b56b7c4a766f71696ad19acb`

## Validation evidence

- Readback from current `main` confirms `ValidateUniqueFloorIds(project)` throws `InvalidOperationException("Project floor collection contains a null floor.")` instead of continuing past a null Floor.
- `ProjectFloorGlobalNullIntegritySmoke` covers `Update`, `SetActive`, `Assign`, `AssignBottomLevel`, `AssignTopLevel`, `Delete`, and `ReferenceCount` against a valid target plus unrelated null Floor state.
- The smoke snapshots Floor count/name/elevation, active Floor, element FloorId/property count, `ChangeVersion`, and `UpdatedUtc` across rejected operations and includes valid update/activate/assign/reference-count controls.
- Connector ancestry check confirmed source commit `0fcb532b8659f2ac2f534cb491b6ec53f8ae4f6f` remains an ancestor of moving `main`; concurrent changes after it did not touch `ProjectFloorService.cs`.
- The first test contents-API write was rejected with a normal moving-main 409; no stale write landed. After refreshing `main`, the regression was pushed successfully and read back from current `main`.

## Excluded / remaining validation

- Floor Create null/duplicate integrity, semantic element integrity, and vertical-level numeric/preflight behavior remain separate lanes.
- Zone/Family services, Floor/Zone UI audit/no-op behavior, persistence/interchange and native BricsCAD adapters were not changed.
- GitHub Actions were not dispatched because `continue all fix bug update code` is not CI authorization under `CI_POLICY.md`.
- No local compile, executable smoke run, or licensed BricsCAD V25/V26 runtime PASS is claimed from this web session.

## Completion condition

`COMPLETED`: target Floor operations fail closed on null Floor collection entries, focused deterministic Core smoke coverage is present on `main`, ownership is released, and no concurrent work was overwritten.

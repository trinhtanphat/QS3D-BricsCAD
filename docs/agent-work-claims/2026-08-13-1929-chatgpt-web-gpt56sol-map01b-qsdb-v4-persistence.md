# Work claim — MAP-01B project-owned mapping persistence / QSDB v4

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map01b-qsdb-v4-20260813-1929`
- Registered UTC: `2026-08-13T12:29:00Z`
- Last updated UTC: `2026-08-13T15:52:00Z`
- Baseline main SHA: `84c2361c2c86a2082aafec723ece532653378950`
- Priority: `MAP-01B P0/P1` — make the verified MAP-01 mapping contract project-owned and persist it deterministically in QSDB

## Confirmed source gap

`MeasurementWorkItemMapping` and `MeasurementWorkItemMappingCatalog` already define canonical mapping identity, duplicate-id rejection, ambiguous category/measurement-item rejection, deterministic catalog ordering, and explicit unmapped resolution. `ProjectState`, however, remains schema v3 and owns no mapping collection; its detached persistence snapshot therefore cannot preserve mappings, `ProjectSchemaMigrator` stops at v3, and QSDB schema validation has no v4 mapping representation. A project can use the mapping contract in memory but cannot round-trip that mapping state as project data.

This is the persistence/schema follow-on intentionally excluded from MAP-01A. BLT3D research remains advisory/reference only; this lane does not broaden the QS3D product boundary.

## Reserved files

- `src/QS3D.Core/Domain/ProjectState.cs`
- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- `tests/QS3D.Core.SmokeTests/QsdbMeasurementWorkItemMappingSmoke.cs`
- `tests/QS3D.Core.SmokeTests/QsdbMeasurementWorkItemMappingRegistration.cs`
- this claim file

## Planned bounded scope

- bump the project schema from v3 to v4 only for project-owned measurement/work-item mapping persistence;
- add a project mapping collection using the existing `MeasurementWorkItemMapping` contract rather than inventing parallel mapping semantics;
- preserve mappings through detached persistence snapshots and QSDB round trips with deterministic output;
- add v3→v4 migration with an empty mapping collection for legacy projects;
- reject duplicate/ambiguous persisted mappings by reusing the existing catalog contract and keep unmapped behavior explicit;
- add focused managed smoke regressions for v4 round-trip, deterministic persistence and v3 migration;
- do not modify MAP-01 resolver/catalog semantics, MAP-02 coverage semantics, rates/cost, geometry, reports/UI, or BricsCAD/native surfaces.

## Validation policy

No GitHub Actions will be dispatched. Managed/native PASS will only be reported for gates actually executed; otherwise the claim closeout will state them as unexecuted. No force-push will be used.

## Coordination update

The original claim used a generic focused smoke placeholder because the former `src/QS3D.TestHarness` surface was no longer current. The live repository uses `tests/QS3D.Core.SmokeTests` with per-lane `ModuleInitializer` registration; the two exact smoke paths above are now reserved before any MAP-01B product-source write. Production overlap was rechecked from the claim commit through current `main` and none of the five reserved production files changed in that interval.

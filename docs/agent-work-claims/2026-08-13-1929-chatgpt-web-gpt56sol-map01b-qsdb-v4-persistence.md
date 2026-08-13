# Work claim — MAP-01B project-owned mapping persistence / QSDB v4

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-map01b-qsdb-v4-20260813-1929`
- Registered UTC: `2026-08-13T12:29:00Z`
- Last updated UTC: `2026-08-13T16:04:00Z`
- Baseline main SHA: `84c2361c2c86a2082aafec723ece532653378950`
- Priority: `MAP-01B P0/P1` — make the verified MAP-01 mapping contract project-owned and persist it deterministically in QSDB

## Confirmed source gap

`MeasurementWorkItemMapping` and `MeasurementWorkItemMappingCatalog` already define canonical mapping identity, duplicate-id rejection, ambiguous category/measurement-item rejection, deterministic catalog ordering, and explicit unmapped resolution. `ProjectState`, however, remains schema v3 and owns no mapping collection; its detached persistence snapshot therefore cannot preserve mappings, `ProjectSchemaMigrator` stops at v3, and QSDB schema validation has no v4 mapping representation. A project can use the mapping contract in memory but cannot round-trip that mapping state as project data.

This is the persistence/schema follow-on intentionally excluded from MAP-01A. BLT3D research remains advisory/reference only; this lane does not broaden the QS3D product boundary.

## Reserved files

- `src/QS3D.Core/Domain/ProjectState.cs`
- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCollection.cs`
- `src/QS3D.Core/Persistence/ProjectStateSnapshot.cs`
- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- `src/QS3D.Core/Persistence/QsdbProjectXmlSchemaValidator.cs`
- focused Mapping/Persistence smoke regression file(s) required for this lane
- this claim file

## Planned bounded scope

- bump the project schema from v3 to v4 only for project-owned measurement/work-item mapping persistence;
- add a project mapping collection using the existing `MeasurementWorkItemMapping` contract rather than inventing parallel mapping semantics;
- preserve mappings through detached persistence snapshots and QSDB round trips with deterministic output;
- add v3→v4 migration with an empty mapping collection for legacy projects;
- reject duplicate/ambiguous persisted mappings by reusing the existing catalog contract and keep unmapped behavior explicit;
- add focused managed smoke regressions for v4 round-trip, deterministic persistence and v3 migration;
- do not modify MAP-01 resolver/catalog semantics, MAP-02 coverage semantics, rates/cost, geometry, reports/UI, or BricsCAD/native surfaces.

## Implementation checkpoint

- Revalidated through live `main` `994b2b20c5aa888cf464d9d61d5c9b58668c14f9`; commits since the prior checkpoint do not touch the reserved MAP-01B source/test scope.
- The v4 representation is narrowed to a canonical reserved project-metadata namespace (`QS3D.Mapping.v1.*`) rather than adding a parallel XML container. This reuses the existing QSDB metadata round-trip and deterministic ordering while a project-owned collection/metadata guard decodes and validates entries through `MeasurementWorkItemMappingCatalog`.
- The existing detached snapshot already copies project metadata, so focused regression will prove mapping detachment without modifying snapshot implementation unless evidence shows otherwise.
- The existing store/XML envelope can remain unchanged if the metadata-backed contract proves safe round-trip and fail-closed malformed/duplicate/ambiguous mapping handling; those reserved files remain protected until closeout in case validation proves a source change is required.
- No substantive source/test change has been published to `main` yet; this claim remains `ACTIVE` until implementation is reconciled, pushed, verified, and closed.

## Validation policy

No GitHub Actions will be dispatched. Managed/native PASS will only be reported for gates actually executed; otherwise the claim closeout will state them as unexecuted. No force-push will be used.

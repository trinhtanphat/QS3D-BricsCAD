# Work claim — MAP-01B project-owned mapping persistence / QSDB v4

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-map01b-qsdb-v4-20260813-1929`
- Registered UTC: `2026-08-13T12:29:00Z`
- Last updated UTC: `2026-08-13T17:05:00Z`
- Baseline main SHA: `84c2361c2c86a2082aafec723ece532653378950`
- Priority: `MAP-01B P0/P1` — make the verified MAP-01 mapping contract project-owned and persist it deterministically in QSDB

## Confirmed source gap

`MeasurementWorkItemMapping` and `MeasurementWorkItemMappingCatalog` already defined canonical mapping identity, duplicate-id rejection, ambiguous category/measurement-item rejection, deterministic catalog ordering, and explicit unmapped resolution. `ProjectState` remained schema v3 and owned no mapping collection, so project mapping state could not participate in the existing QSDB project lifecycle.

This was the persistence/schema follow-on intentionally excluded from MAP-01A. BLT3D research remained advisory/reference only; the implementation does not broaden the QS3D product boundary.

## Completed implementation

- Implementation commit on `main`: `3fc7f282a01749593a4c4822cb2be2545ca6516f` (`feat(mapping): persist project mappings in QSDB v4`).
- Claim/checkpoint commits: `0be74ac84f44fb4158ab74b2c1cc3de93803cca9`, `cc8b5a316256e114ec6ffcd9a399e0f8b45d463d`.
- `ProjectState.CurrentSchemaVersion` is now v4 and exposes a project-owned `MeasurementWorkItemMappings` collection.
- Mapping persistence reuses the existing deterministic QSDB project metadata path through the reserved `QS3D.Mapping.v1.*` namespace rather than introducing a second XML persistence engine.
- Mapping metadata uses the canonical mapping id in the metadata key and a length-prefixed value representation for category, measurement item id, classification id, and work item id, preserving every token accepted by the existing MAP-01A contract without delimiter ambiguity.
- `ProjectMetadataDictionary` validates every reserved mapping mutation against the complete tentative mapping catalog, so malformed, duplicate-id, and ambiguous category/measurement-item persisted states fail closed through the existing `MeasurementWorkItemMappingCatalog` invariant.
- v3→v4 migration preserves an empty mapping collection for normal legacy projects and fails visibly if a v3 file already occupies the newly reserved mapping metadata namespace rather than silently reinterpreting legacy metadata.
- Existing QSDB metadata serialization remains the persistence transport, so no separate report/geometry/rate calculation path was introduced.
- Existing detached snapshot logic already copies project metadata; the focused regression proves mapping state remains detached without changing snapshot implementation.

## Files changed by implementation

- `src/QS3D.Core/Domain/ProjectState.cs`
- `src/QS3D.Core/Domain/ProjectMetadataDictionary.cs`
- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCodec.cs`
- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCollection.cs`
- `src/QS3D.Core/Persistence/ProjectSchemaMigrator.cs`
- `tests/QS3D.Core.SmokeTests/Map01bMappingPersistenceSmoke.cs`
- `tests/QS3D.Core.SmokeTests/Map01bSmokeModuleInitializer.cs`

Reserved `ProjectStateSnapshot`, `QsdbProjectStore`, and `QsdbProjectXmlSchemaValidator` were verified not to require substantive changes for the narrowed metadata-backed representation and were left unchanged.

## Regression coverage added

`Map01bMappingPersistenceSmoke` covers:

- schema v4 project ownership;
- canonical/deterministic metadata projection independent of mapping insertion order;
- reconstruction of project mapping state through the persisted metadata representation;
- detached snapshot preservation after the live mapping collection is cleared;
- fail-closed ambiguous persisted mapping state;
- fail-closed duplicate persisted mapping id.

The smoke is registered using the same `ModuleInitializer` pattern already used by focused smoke registrations in this repository.

## Validation actually performed

- Refreshed/reconciled live `main` repeatedly during implementation and checked intervening commits for reserved-scope overlap.
- Published the final implementation as one non-force fast-forward commit from live parent `9bdcef57cf7f879ff1a90902a2e3808f59ba2b28` to `3fc7f282a01749593a4c4822cb2be2545ca6516f`.
- Re-fetched `main` and verified the remote ref points to the implementation commit.
- Verified the remote diff is exactly one commit touching the seven implementation/regression files listed above.
- Re-fetched live `ProjectState` and `ProjectSchemaMigrator` from the implementation commit and reviewed the v4 ownership/migration state.
- Managed smoke/runtime execution: **not executed in this session**; no local repository/.NET runtime was available for an honest managed PASS claim.
- GitHub Actions: **not dispatched**.
- BricsCAD/native qualification: **not executed** and no native PASS is claimed.
- Force-push: **not used**.

## Completion

MAP-01B is complete and no longer reserves its files/capability. Later MAP-02/coverage/UI work must consume this canonical project-owned mapping state rather than create another mapping persistence path.

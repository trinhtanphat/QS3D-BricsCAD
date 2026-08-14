# Agent work claim — MAP-01 mapping mutation ChangeVersion integrity

Status: `ACTIVE`

Agent: `chatgpt-web-gpt56sol-map01-mapping-changeversion-20260814-1249`

Registered: `2026-08-14T12:49:24+07:00`

Baseline `main`: `fc7e4d2ecf6abd165d65146ee61e991ad3e579ec`

Priority: `P1` Core semantic-integrity hardening / MAP-01 mapping domain contract.

## Confirmed source gap

`ProjectState.MeasurementWorkItemMappings` is project-owned canonical semantic state persisted in QSDB v4 and consumed directly by MAP-02 coverage. Its collection currently mutates the reserved `QS3D.Mapping.v1.*` metadata entries on `Add`, successful `Remove`, and non-empty `Clear` without incrementing `ProjectState.ChangeVersion` or updating the project persistence timestamp.

That permits two different canonical semantic mapping states to carry the same project semantic version. It also diverges from established persisted semantic-catalog mutation policy in this repository, where the project revision is advanced before the persisted write so `ChangeVersion` overflow fails before semantic state changes.

The existing mapping codec/catalog validation remains authoritative; this lane is only about project mutation/version semantics.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectMeasurementWorkItemMappingCollection.cs`
- `src/QS3D.Core/Domain/ProjectState.cs` only as needed to bind the mapping collection to its owning project / semantic revision callback.
- `tests/QS3D.Core.SmokeTests/Map01bMappingPersistenceSmoke.cs`
- this claim file.

## Acceptance

1. Adding a canonical mapping advances project `ChangeVersion` exactly once and updates project persistence state before the persisted mapping write is committed.
2. Removing an existing mapping advances `ChangeVersion` exactly once; removing a missing mapping remains a true no-op and does not advance the version.
3. Clearing a non-empty mapping catalog advances `ChangeVersion` exactly once; clearing an already-empty catalog remains a true no-op.
4. Mutation validation failures and `ChangeVersion` overflow fail before mapping metadata changes.
5. Existing mapping identity, ambiguity, canonical metadata encoding, detached snapshot, QSDB v4 persistence, and unrelated metadata behavior remain unchanged.

## Explicit non-scope

- No mapping schema/codec format change or QSDB schema-version bump.
- No MAP-02/MAP-03 coverage business-logic or report/UI change.
- No recognition/template layer-mapping work.
- No BricsCAD/native host changes or qualification claims.
- No broad `ProjectMetadataDictionary` semantic-versioning change; presentation/non-semantic metadata remains outside this lane.

## Evidence / history

- MAP-01B persistence was introduced by `3fc7f282a01749593a4c4822cb2be2545ca6516f` and completed by `a9ab39b416d8dbab44ed7319db405d250a56f10a`.
- `ProjectStateSnapshot` copies mapping metadata and restores the captured `ChangeVersion`, so same-version/different-mapping-state breaks freshness/version identity assumptions.
- Persisted semantic catalog precedent `42e4e092fc8f68fd6a755d484210fc785c0ba26e` explicitly moved `project.Touch()` before the material-catalog persisted write to keep revision overflow fail-closed.
- Live source at the baseline still has mapping `Add`/`Remove`/`Clear` writes with no project touch/version callback.
- No current commit-history claim was found for measurement/work-item mapping mutation ChangeVersion/persistability; MAP-01A and MAP-01B claims are completed historical lanes.

## Validation plan

- Add focused managed smoke assertions for Add / successful Remove / non-empty Clear revision increments.
- Assert missing Remove and empty Clear remain revision-neutral.
- Assert a forced `ChangeVersion == long.MaxValue` mutation attempt throws before changing mapping metadata.
- Re-read source/test from remote after push and reconcile with current `main` before closing.
- GitHub Actions: `NOT_RUN` / do not dispatch.
- .NET Core smoke execution: `NOT_RUN` unless an executable SDK becomes available in this environment.
- BricsCAD/native runtime: `NOT_RUN`; no native PASS claim.

## Completion condition

Implementation and focused regression are on current `main`, remote content/lineage is verified after concurrent reconciliation, and this claim is updated to `COMPLETED` with exact commit SHAs and only validation actually executed.

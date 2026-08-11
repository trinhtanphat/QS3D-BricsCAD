# Work claim — Native Table single ChangeVersion touch

- Status: `ACTIVE`
- Agent: `gpt56sol-chatgpt-web`
- Registered: `2026-08-11T22:38:00+07:00`
- Baseline main SHA: `8633a08af3b49332231cb24a616082e17a40a98a`
- Priority: prevent successful project-owned native documentation Table mutations from advancing `ProjectState.ChangeVersion` twice for one logical operation.

## Confirmed defect

`AuditTrail.ForProject(project).Record(...)` already calls `ProjectState.Touch()`. `ProjectOwnedNativeTableArtifactService.Build(...)` and `Remove(...)` both call `project.Touch()` again immediately after recording their audit event. A successful create/refresh/remove therefore advances `ChangeVersion` twice for one logical Table mutation, causing unnecessary version churn and making version-based freshness guards observe an artificial extra change.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/ProjectOwnedNativeTableArtifactService.cs`
- one focused auto-discovered source regression gate under `scripts/`
- this claim file

## Intended contract

- Table Build/Remove continue to mutate metadata and append exactly the same audit event.
- The audit-backed `Touch()` remains the single project-version advancement for the logical Table mutation.
- Do not change CAD transaction ordering, ownership/XData, persisted metadata schema, snapshot rendering, geometry, or health diagnostics.

## Excluded scope

- BQ/Quantity window locate work and `Commands.cs`
- Native Table placement lifecycle, semantic schedule placement, ownership-token work, and current active claims
- `AuditTrail` behavior globally
- BricsCAD V25 native/runtime qualification

## Validation plan

- remove only the redundant explicit `project.Touch()` calls after native Table audit records;
- add a source guard proving Build/Remove retain audit records while the service no longer double-touches;
- compare against latest `main` immediately before merge and refuse overlapping writes;
- do not dispatch GitHub Actions.

## Completion condition

The source fix and regression gate are merged to `main`, this claim is marked `COMPLETED`, and exact BricsCAD V25 runtime evidence remains `LOCAL_ONLY` unless produced by a local agent.

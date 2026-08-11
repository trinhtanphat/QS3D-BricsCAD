# Work claim — Room Auto regeneration safety

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-room-auto-regen`
- Registered: `2026-08-11T19:51:00+07:00`
- Completed: `2026-08-11`
- Baseline main SHA: `a6c133cbada6013c45ac55805c9cfa2897d4cc30`
- Implementation SHA: `ca8d9e18e5b0c0a802d54b2ef4b24315be545568`
- Regression SHA: `118823a3b74c30b2b33e3b9550e560872e3f5dba`
- Priority: continue localized semantic-mutation hardening after Room Finish; prevent `QS3DROOMAUTO` from consuming unrelated dirty project state while preserving discovery, provenance, stale-room and generated-finish contracts.

## Reserved scope

Audit and harden regeneration scope in `QS3DROOMAUTO` / `RoomBoundaryCommands.DiscoverRooms`. Determine the exact semantic mutation set created, updated or marked stale by one Room Auto run, and ensure regeneration is limited to that affected set rather than unrelated project dirty elements.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs`
- `scripts/preflight-room-auto-project-lifecycle.py`
- this claim file for close-out

## Excluded scope

- No generated-native source recognition / eligibility work reserved by the generated-source-recognition claim.
- No Core mutation atomicity / transaction primitive changes reserved by the Core mutation-atomicity claim.
- No Direct Draw/Create Similar, Workspace, Material Catalog/refresh, modeless viewer, Level Z-chain or registration-protocol work.
- No intentionally global `QS3DREGEN` / `QS3DREFRESH` behavior changes.
- No BricsCAD V25 runtime PASS, GitHub Actions dispatch, release, installer, signing or private-DWG qualification.

## Implemented contract

- Confirmed the command previously ended a localized Room Auto mutation with `RegenerateDirty(project)`, which could consume unrelated pre-existing dirty semantic state.
- `QS3DROOMAUTO` now builds one explicit regeneration target set from `activeRoomIds` plus the exact Room objects returned by `AutoRoomLifecycle.MarkStaleForSelection(...)`.
- Final Room regeneration now uses `RegenerateDirtySubset(project, regenerationTargets)` instead of project-wide dirty regeneration.
- Existing Room Finish synchronization remains before the final Room subset pass; `SemanticCaptureService.SyncExistingRoomFinishes(...)` continues to regenerate synchronized finishes individually.
- Preview/commit freshness, no-project cancellation/bootstrap behavior, ProjectId guard, full `ProjectStateSnapshot` rollback, provenance collision checks, stale-room audit records, `project.Touch()`, palette refresh and command status ordering remain unchanged.
- Unrelated dirty project elements are not included in this command's final regeneration target set.

## Regression coverage

`scripts/preflight-room-auto-project-lifecycle.py` now requires the command-side sequence:

1. synchronize existing Room Finishes for active Rooms;
2. mark selected obsolete auto-Rooms stale;
3. seed the target set from `activeRoomIds`;
4. add every `staleRooms` id;
5. call `RegenerateDirtySubset(project, regenerationTargets)`.

The same preflight rejects `.RegenerateDirty(project)` and `.RegenerateProject(project)` inside `DiscoverRooms`, while retaining all prior diagnostics, project-bootstrap, ProjectId, drawing-unit and Room-setting freshness checks.

## Validation actually performed

- Re-fetched latest `main`, the active claim directory, `RoomBoundaryCommands.cs` and the focused preflight immediately before implementation.
- Read current `AutoRoomLifecycle.MarkStaleForSelection(...)`: it returns the exact selected-scope stale auto-Rooms and dirties those Rooms only.
- Read current `SemanticCaptureService.SyncExistingRoomFinishes(...)`: synchronized active-Room finishes are regenerated individually before the final Room pass.
- Read current `RegenerationEngine.RegenerateDirtySubset(...)`: it resolves and regenerates only explicit requested semantic ids rather than all project elements.
- Inspected the exact source diff in `ca8d9e18e5b0c0a802d54b2ef4b24315be545568` and the exact focused regression diff in `118823a3b74c30b2b33e3b9550e560872e3f5dba`.
- Verified through GitHub compare that `118823a3b74c30b2b33e3b9550e560872e3f5dba` is an ancestor of the later observed `main` head and concurrent commits were preserved.
- No GitHub Actions/release was dispatched.
- No full C# build, BricsCAD V25 `NETLOAD`, native runtime, installer/signing or private-DWG PASS is claimed from this connector-only lane.
- The static preflight regression is committed as deterministic repository coverage; it was not reported as locally executed runtime evidence in this lane.

## Coordination

This lane changed only Room Auto command-side regeneration scope and its focused preflight. It did not modify generated-source recognition, Core mutation atomicity, Room Family-default integrity, Direct Draw/Create Similar, Workspace, Material or LOCAL_ONLY runtime surfaces.

## Completion condition

`COMPLETED`: Room Auto final regeneration is limited to active plus selected-stale auto-Rooms, unrelated dirty semantic state is preserved, the focused regression guard is committed, exact diffs/ancestry were inspected, and this claim records the implementation evidence.

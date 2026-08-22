# Work claim — Auto Room Family-default integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-auto-room-family-default-integrity`
- Registered: `2026-08-11T20:46:38+07:00`
- Completed: `2026-08-11T21:00:00+07:00`
- Baseline main SHA: `0e98b470ff5fcde09bf1d896180f34b98678e664`
- Implementation PR: `#455`
- Squash merge SHA: `c27bcca0e6d476404c484dbabcdf052f3e3f2819`
- Priority: confirmed Core defect — `AutoRoomLifecycle.SyncFamilyDefaults(...)` consumed `ProjectFamily.Properties` without the canonical key/value validation already enforced by `ProjectFamilyService.Assign(...)`, allowing malformed Family defaults to enter Room semantic state before downstream consumers reject them.

## Reserved scope

Harden only the Core Family-default transfer boundary used by Auto Room. Validate the target Room Family property snapshot completely before `AutoRoomLifecycle.SyncFamilyDefaults(...)` performs any Room, metadata, dirty-state or `ProjectState.Touch()` mutation, while preserving the existing inherited-default/instance-override behavior.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFamilyService.cs` only for extracting/reusing the existing canonical Family property snapshot validation contract
- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- `tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs`
- this claim file for close-out

## Excluded scope

- No `src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs`, `QS3DROOMAUTO` command-side regeneration scope, or Room Auto native regeneration behavior; those remain owned by `chatgpt-web-gpt56sol-room-auto-regen`.
- No QSDB, `ProjectSession`, persistence/recovery, Navigation/Review/Rules/Interchange atomicity surfaces owned by `chatgpt-web-gpt56sol-core-atomicity-20260811-1930`.
- No Room Finish, generated-source recognition, Direct Draw/Create Similar, Workspace, Material, Ribbon, rebar, LOCAL_ONLY runtime qualification, installer/signing, release or GitHub Actions dispatch.
- No broad new numeric-property schema. Existing free-form Family text properties remain supported; this lane reuses the canonical Family property key/value structural contract rather than inventing incompatible semantics.

## Implemented contract

- `ProjectFamilyService` now exposes one internal canonical Family-property snapshot path used by both `Duplicate(...)` and `Assign(...)`, preserving the existing 120-character canonical key and 1000-character value limits.
- `AutoRoomLifecycle.SyncFamilyDefaults(...)` obtains that validated snapshot before it plans Room property changes, AutoRoom metadata changes or a `ProjectState.Touch()`.
- Existing inherited-default versus instance-override behavior remains unchanged on the accepted path.
- Family values remain free-form text; this lane did not introduce a global numeric-property schema.
- `AutoRoomLifecycleSmoke` now includes non-canonical-key and over-limit-value rejection cases and asserts no changes to Room `FamilyId`, Room properties, AutoRoom metadata, dirty flags, Room `UpdatedUtc`, project `ChangeVersion` or project `UpdatedUtc`.

## Validation actually performed

- Re-fetched latest `main`, active claims and target source before implementation.
- Registered this claim on `main` first at `a53c3ffa0c9acb75f8aea7333d1713d36275a9ca`.
- Reviewed each implementation commit diff through the GitHub connector:
  - `86cf25a90efa9b961d7199921ace0e92263071a1` — shared canonical Family-property snapshot validator.
  - `f0200599fdce539bdf39805096f74afb9bf10fc6` — AutoRoom consumes validated snapshot before mutation planning.
  - `3d09d434da7b99131e775bdc4c8a1eaa881f6c1f` — focused rejection atomicity smoke coverage.
- Compared the branch baseline against concurrent `main`; no concurrent commit touched `ProjectFamilyService.cs`, `AutoRoomLifecycle.cs` or `AutoRoomLifecycleSmoke.cs` before merge.
- PR `#455` was squash-merged as `c27bcca0e6d476404c484dbabcdf052f3e3f2819`.
- Verified the later `main` ancestry contains `c27bcca0e6d476404c484dbabcdf052f3e3f2819`.
- No GitHub Actions/release was dispatched.
- No BricsCAD V25/native runtime PASS is claimed.
- The Core smoke regression is committed as deterministic coverage, but this connector-only lane did not execute the smoke binary locally and does not report it as runtime PASS.

## Coordination

The active Room Auto regeneration claim explicitly owns only command-side regeneration and excludes Core mutation work; this claim did not touch its command/preflight surfaces. The active Core mutation-atomicity claim remained on its separately reserved persistence/session surfaces. No competing claim was overwritten.

## Completion condition

`COMPLETED`: malformed target Room Family defaults now fail closed before AutoRoom semantic/persistence mutation, canonical structural validation is shared rather than duplicated, focused regression coverage is merged to current `main`, and the exact implementation/validation evidence is recorded above.
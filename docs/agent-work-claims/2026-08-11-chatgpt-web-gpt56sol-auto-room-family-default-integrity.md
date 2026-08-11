# Work claim — Auto Room Family-default integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-auto-room-family-default-integrity`
- Registered: `2026-08-11T20:46:38+07:00`
- Baseline main SHA: `0e98b470ff5fcde09bf1d896180f34b98678e664`
- Priority: confirmed Core defect — `AutoRoomLifecycle.SyncFamilyDefaults(...)` consumes `ProjectFamily.Properties` without the canonical key/value validation already enforced by `ProjectFamilyService.Assign(...)`, allowing malformed Family defaults to enter Room semantic state before downstream consumers reject them.

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

## Validation plan

- Re-fetch latest `main` and claims before every implementation write.
- Reuse one canonical Family property snapshot validator so `Assign(...)`, `Duplicate(...)` and Auto Room do not drift on key/value limits or canonical-key rules.
- Add a focused AutoRoom Core smoke regression with a deliberately malformed target Family and prove failure occurs before FamilyId, Room properties, AutoRoom family-default metadata, dirty flags, `ChangeVersion` or `UpdatedUtc` change.
- Preserve the existing `FamilyDefaultsPreserveInstanceOverrides` success-path behavior.
- Review the final diff against current `main`; do not claim local BricsCAD V25 execution or CI/Actions execution.

## Coordination

The active Room Auto regeneration claim explicitly owns only command-side regeneration and excludes Core mutation work; this claim does not touch its command/preflight surfaces. The active Core mutation-atomicity claim is currently reserved to QSDB/`ProjectSession` persistence/session work and explicitly excludes Room Auto command-side regeneration; this claim is restricted to `Domain/AutoRoomLifecycle` Family-default transfer plus its focused smoke. If either claim expands onto these exact Domain/test surfaces before implementation, stop and re-scope rather than compete.

## Completion condition

Malformed target Room Family defaults fail closed before any AutoRoom semantic/persistence mutation, canonical Family validation is shared rather than duplicated, focused Core regression coverage is merged to current `main`, existing instance-override semantics remain intact, and this claim is marked `COMPLETED` with exact pushed SHAs and validation actually performed.

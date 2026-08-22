# Work claim — Auto Room dangling previous Family

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-auto-room-dangling-previous-family`
- Registered: `2026-08-12T08:10:00+07:00`
- Last Updated: `2026-08-12T08:10:00+07:00`
- Baseline main SHA: `63b06496c6996bd769a44ef88b88afb7b13c2203`
- Priority: deterministic Core relation-integrity mismatch found during owner-requested continue-all audit
- Task Key: `CORE-AUTO-ROOM-DANGLING-PREVIOUS-FAMILY`

## Confirmed defect

Canonical Family reassignment paths (`ProjectFamilyService.Assign(...)` and `BulkEditService.AssignFamily(...)`) fail before mutation when an element has a non-empty `FamilyId` that no longer resolves to a project Family. This prevents silent repair from losing the distinction between inherited previous-Family defaults and explicit instance overrides.

`AutoRoomLifecycle.SyncFamilyDefaults(...)` has the same reassignment/inheritance responsibility but currently does `project.FindFamily(room.FamilyId)` and treats a missing result as an empty previous-default set. A Room with dangling non-empty `FamilyId` can therefore have its FamilyId, default-snapshot metadata and properties rewritten instead of surfacing the invalid relation.

## Reserved scope

Make Auto Room family synchronization fail before project/Room mutation when the Room's current `FamilyId` is non-empty, differs canonically from the requested target Family, and does not resolve to a project Family. Reuse existing canonical trim/case-insensitive identity semantics and preserve all valid/no-Family/same-Family behavior.

## Expected surfaces

- `src/QS3D.Core/Domain/AutoRoomLifecycle.cs`
- focused Core smoke + isolated registration if needed
- this claim file

## Coordination / exclusions

- Do not modify Auto Room geometry, topology, projection, active-id, stale-room discovery, finish generation or BricsCAD commands.
- Do not modify `ProjectFamilyService.cs` or `BulkEditService.cs`; those canonical assignment contracts are reference behavior only.
- Do not broaden into malformed previous-Family properties; that was already fixed by `4fa587278e84df0ef10bf560a9687dcdc81cbf7f`.
- Preserve legitimate empty `FamilyId` bootstrap and canonical same-Family no-op behavior.
- Do not overwrite any concurrent ACTIVE claim; no force-push, Actions/build/release dispatch, or runtime PASS claim.

## Validation plan

- Room with dangling non-empty previous `FamilyId` + valid target Room Family: require fail-before-mutation.
- Prove FamilyId, Room properties, Room dirty/timestamp, project metadata, `ChangeVersion` and project `UpdatedUtc` remain unchanged.
- Prove empty previous FamilyId can still adopt valid target defaults.
- Prove valid previous Family reassignment still preserves explicit instance overrides and inherited-default semantics.
- Re-fetch `main`, collision state and exact source before every write; read back source/test before closeout.

## Completion condition

Auto Room Family synchronization shares the canonical fail-closed relation contract for dangling previous Family identities without changing valid bootstrap or reassignment semantics.
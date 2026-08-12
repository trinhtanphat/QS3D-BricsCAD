# Work claim — Auto Room dangling previous Family

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-auto-room-dangling-previous-family`
- Registered: `2026-08-12T08:22:53+07:00`
- Last Updated: `2026-08-12T08:25:50+07:00`
- Baseline main SHA: `63b06496c6996bd769a44ef88b88afb7b13c2203`
- Priority: deterministic Core relation-integrity mismatch found during owner-requested continue-all audit
- Task Key: `CORE-AUTO-ROOM-DANGLING-PREVIOUS-FAMILY`

## Confirmed defect

Canonical Family reassignment paths (`ProjectFamilyService.Assign(...)` and `BulkEditService.AssignFamily(...)`) fail before mutation when an element has a non-empty `FamilyId` that no longer resolves to a project Family. This prevents silent repair from losing the distinction between inherited previous-Family defaults and explicit instance overrides.

`AutoRoomLifecycle.SyncFamilyDefaults(...)` had the same reassignment/inheritance responsibility but treated a missing previous Family as an empty previous-default set. A Room with dangling non-empty `FamilyId` could therefore have its FamilyId, default-snapshot metadata and properties rewritten instead of surfacing the invalid relation.

## Implemented scope

`SyncFamilyDefaults(...)` now canonicalizes the Room's current `FamilyId` for identity comparison, distinguishes legitimate empty-Family bootstrap from reassignment, and rejects a non-empty unresolved previous Family before mutation planning. A canonical trim/case-insensitive match with the target Family remains a no-op identity and does not rewrite the persisted raw FamilyId formatting solely for formatting differences.

Valid previous-Family synchronization still uses the existing canonical `ProjectFamilyService.SnapshotProperties(...)` path, preserving inherited-default versus explicit-override semantics.

## Committed evidence

- Claim registration: `6badfa569edddc6af218ce5dc5a74e3f5ed1a2fe` — `chore(agent): claim auto-room dangling previous Family`
- Core fix: `f9e372e7305136b6da465f48a99285693fbe4b69` — `fix(core): reject dangling Auto Room previous Family`
- Focused smoke: `67fdb2ad2190c7ccbd472172f9fd123ddcb73534` — `test(core): guard Auto Room dangling previous Family`
- Isolated smoke registration: `228dae5fb0f14f2e694372bbf57bc3fdd2e2135d` — `test(core): register Auto Room dangling Family smoke`
- Moving-main read-back on `ff0422adbc9814e730cc60c293785053b11749b5` confirmed source, smoke and isolated registration remained present after concurrent commits.

The focused smoke locks three paths: dangling non-empty previous Family fails without changing Room FamilyId/properties/dirty/timestamp, project metadata/version/timestamp; empty previous Family still bootstraps to a valid target; canonical padded/case-varied same-Family state remains a true no-op when defaults/snapshots are already synchronized.

## Preserved behavior / exclusions

- Auto Room geometry, topology, projection, active-id, stale-room discovery, finish generation and BricsCAD commands were not modified.
- `ProjectFamilyService.cs` and `BulkEditService.cs` were not modified.
- Malformed previous-Family property validation remains owned by the completed `4fa587278e84df0ef10bf560a9687dcdc81cbf7f` lane.
- No force-push or GitHub Actions/build/release dispatch was used.
- No local .NET smoke execution or BricsCAD V25 runtime qualification is claimed.

## Completion condition

Satisfied: Auto Room Family synchronization now shares the canonical fail-closed relation contract for dangling previous Family identities without changing valid bootstrap or reassignment semantics.
# Work claim — Manager cold-cache modeless launch binding

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-manager-cold-cache-modeless-launch`
- Registered: `2026-08-12T00:35:00+07:00`
- Last Updated: `2026-08-12T00:36:00+07:00`
- Baseline main SHA: `227cc470be5f69846667962327b7194724f7f5dc`
- Priority: P1 — persisted QS3D projects must be visible on first modeless manager open after a cold process/cache start without creating replacement state.
- Task Key: `BRICSCAD-MANAGER-COLD-CACHE-MODELESS-LAUNCH`

## Confirmed defect

`QS3DLEVELS`, `QS3DFAMILIES`, and `QS3DZONES` currently construct their document-bound modeless windows immediately. Their constructors/refresh paths intentionally use `ProjectContextCoordinator.TryGetReadOnly(...)`, which keeps reads non-creating but returns a detached persisted snapshot when the coordinator cache is cold. Later window writes require the exact canonical cached project. The launch path therefore does not establish the canonical existing-project instance before the modeless lifecycle begins.

## Reserved scope

- `src/QS3D.BricsCAD.V25/FloorLevelCommands.cs`
- `src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs`
- `src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs`
- one new focused static preflight for manager cold-cache launch binding
- `docs/LOCAL-AGENT-INBOX.md` — append/update only the `LOCAL-001` scenario/evidence for these three manager cold-cache launches; preserve every unrelated local item
- this claim file

## Intended contract

- Before constructing each manager window, probe/bind an **already-existing** persisted QS3D project into the canonical coordinator cache.
- If no existing QS3D project exists, do not bootstrap/create one; the modeless window may continue to open in its current empty/read-only state.
- Do not change `ProjectContextCoordinator.TryGetReadOnly(...)`.
- Do not weaken the existing exact-instance stale-project guards in Level/Family/Zone modeless windows.
- Do not change Core Floor/Family/Zone services; current active Core claims remain independent.
- Do not dispatch GitHub Actions/build/release and do not claim BricsCAD V25 runtime PASS remotely.

## Coordination / exclusions

The active Floor elevation-tolerance claim reserves `ProjectFloorService.cs`; the active Family null-target claim reserves `ProjectFamilyService.cs`. This lane does not touch either Core service. Previously completed Level/Family/Zone stale-window hardening remains authoritative and must stay fail-closed. `LOCAL-001` is the canonical V25 queue and is currently `IN_PROGRESS`; do not create a duplicate LOCAL item.

## Validation plan

- Static preflight proves all three commands warm-bind through the existing-project-only boundary before window construction.
- Static preflight rejects direct `ProjectContextCoordinator.GetOrCreate(document)` launch bootstrapping.
- Existing window `TryGetReadOnly` and exact-instance stale guards remain untouched.
- Re-fetch `main` before source edit and before final closeout; inspect exact committed source after write.
- BricsCAD V25 cold-process runtime validation remains LOCAL_ONLY and is appended to `LOCAL-001`; no remote PASS claim.

## Completion condition

All three manager commands make an existing persisted project canonical before the first modeless refresh while remaining non-creating when no project exists, with focused static regression source on `main`, canonical LOCAL_ONLY handoff recorded, and this claim closed.
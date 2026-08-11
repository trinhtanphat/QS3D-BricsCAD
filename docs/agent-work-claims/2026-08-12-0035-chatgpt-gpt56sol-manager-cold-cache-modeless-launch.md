# Work claim — Manager cold-cache modeless launch binding

- Status: `COMPLETE`
- State: `COMPLETE`
- Agent: `chatgpt-gpt56sol-20260812-manager-cold-cache-modeless-launch`
- Registered: `2026-08-12T00:35:00+07:00`
- Completed: `2026-08-12T00:39:00+07:00`
- Last Updated: `2026-08-12T00:39:00+07:00`
- Baseline main SHA: `227cc470be5f69846667962327b7194724f7f5dc`
- Priority: P1 — persisted QS3D projects must be visible on first modeless manager open after a cold process/cache start without creating replacement state.
- Task Key: `BRICSCAD-MANAGER-COLD-CACHE-MODELESS-LAUNCH`

## Confirmed defect

`QS3DLEVELS`, `QS3DFAMILIES`, and `QS3DZONES` constructed their document-bound modeless windows immediately. Their constructors/refresh paths intentionally use `ProjectContextCoordinator.TryGetReadOnly(...)`, which keeps reads non-creating but returns a detached persisted snapshot when the coordinator cache is cold. Later window writes require the exact canonical cached project. The launch path therefore did not establish the canonical existing-project instance before the modeless lifecycle began.

## Reserved scope

- `src/QS3D.BricsCAD.V25/FloorLevelCommands.cs`
- `src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs`
- `src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs`
- `scripts/preflight-manager-cold-cache-modeless-launch.py`
- `docs/LOCAL-AGENT-INBOX.md` — reviewed for canonical LOCAL_ONLY ownership; no duplicate item created
- this claim file

## Implemented contract

- Each launcher now calls `ExistingProjectMutationContext.TryGet(document, out _)` before constructing its modeless window.
- That boundary probes with `TryGetReadOnly` first and calls `GetOrCreate` only after an existing project is proven, so a drawing without a QS3D sidecar/project remains non-creating.
- Direct `ProjectContextCoordinator.GetOrCreate(document)` is not added to any launcher.
- `ProjectContextCoordinator.TryGetReadOnly(...)` and the exact-instance stale-project guards inside Level/Family/Zone windows are unchanged.
- Core Floor/Family/Zone services are untouched; concurrent Core claims remain independent.

## Validation / LOCAL_ONLY disposition

- Added `scripts/preflight-manager-cold-cache-modeless-launch.py` to require warm-bind-before-constructor order for all three commands, reject direct launch bootstrapping, and preserve read-only modeless access plus exact-instance stale guards.
- Exact source readback confirms the three command edits are limited to the existing-project warm-bind line before constructor.
- `LOCAL-001` remains the canonical P0 V25 task and is already `IN_PROGRESS` with cold-cache canonical same-ProjectId true-write plus modeless lifecycle qualification still pending. Because that authoritative task already owns this runtime class, no duplicate LOCAL task was created and no `LOCAL_PASS` is claimed here.
- No GitHub Actions/build/release workflow was dispatched.

## Result

PR #587 (`fix(ui): warm-bind managers on cold-cache launch`) was squash-merged to `main` as `47c0d4e1e160b913d72cf76857362abd8c329be3`. Source-side cold-cache launch binding and focused static regression are complete and the claimed files are released. Licensed BricsCAD V25 cold-process verification remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` under `LOCAL-001`.
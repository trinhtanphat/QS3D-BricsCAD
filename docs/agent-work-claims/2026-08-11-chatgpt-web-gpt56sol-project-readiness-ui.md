# Agent work claim — Project Tools readiness dashboard

- Agent: `chatgpt-web-gpt56sol-project-readiness-2012`
- Registered: `2026-08-11T20:12:00+07:00`
- Status: `COMPLETED`
- Baseline main SHA: `94d0b0d6f712663a38e91570fffa6fa30b4d4e89`
- Registration commit: `a91e27913ff679bd65c737c3e3a706e499a0ecd8`
- Priority: continue the owner-requested BLT3D-inspired UI/UX wave with a compact, source-safe project readiness surface inside the existing document-bound Project Tools window.

## Reserved scope

Enhance Project Tools with a read-only readiness/status dashboard derived only from the current existing canonical QS3D project: active Zone/Floor, catalog counts, semantic dirty-state counts, project change version and last update. Improve hierarchy and actionable status copy without changing project semantics or adding a parallel application shell.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs`
- `scripts/preflight-project-tools.py` only for focused readiness/read-only regression assertions
- `docs/UI-PROJECT-READINESS-2026-08-11.md`
- this claim file for close-out

## Functional contract

- Open/refresh keeps using `ProjectContextCoordinator.TryGetReadOnly` and never bootstraps/caches a missing project.
- Readiness calculations are observation-only: no `Touch`, `MarkDirty`, `MarkClean`, `IsGeneratedGeometryStale`, save/reload, regeneration, metadata mutation, or Core mutation service is used while rendering.
- Active Zone/Floor labels resolve from `ActiveZoneId` / `ActiveFloorId` with explicit fallback when the referenced definition is missing.
- Dirty metrics use the already persisted `ElementDirtyFlags` state and do not mutate generated-output state.
- Command buttons retain the existing document-bound, fail-closed dispatch behavior; no decorative/stub action was introduced.

## Explicit exclusions / coordination

- No `RightPanel*` / `PaletteCoordinator.cs` work reserved by the active right-panel quantity workspace claim.
- No BQ quantity explanation/detail UI, Core reporting builders, Core persistence/atomicity, `Commands.cs` post-commit work, Ribbon/Start Center, Workspace property palette, Direct Draw/Create Similar, Room/recognition, Level native placement, installer/release/signing, or GitHub Actions.
- No BricsCAD V25/WPF runtime PASS claim from this remote lane.

## Coordination note

The initial claim write raced a concurrent `main` merge; GitHub attached registration commit `a91e2791...` to actual parent `94d0b0d6...`. Claim-only correction `b7fb534b72efcbf1bacde65b8f2e28e4e6905ae8` recorded that exact baseline before substantive implementation. Two attempted atomic tree updates were rejected as non-fast-forward while `main` advanced; no force update was used. Product changes were then applied through current-main Contents API updates without overwriting concurrent files.

## Completion record

- `c654f2a2c62d941dac57b0944555b5265fc039f8` — `feat(ui): add Project Tools readiness layout`
  - expanded Project Snapshot with active Zone plus Zone/Floor/Family/Element counts;
  - added a compact `PROJECT READINESS` card for persisted dirty-state, change-version and update-time visibility;
  - preserved every existing Project Tools command button and document-safe footer contract.
- `d04f252cac045eb2c36f3b14bf4e4ec61116f4e8` — `feat(ui): wire Project Tools readiness state`
  - resolves active Zone/Floor from current read-only project state;
  - reports total/geometry/quantity dirty counts from `ProjectElement.Dirty` only;
  - shows `ChangeVersion` and UTC update time;
  - handles missing project and dangling active Zone/Floor references explicitly;
  - keeps command dispatch bound to the DWG that opened the window.
- `31c704f334a97723eae0ef966427931f81e9e94a` — `test(ui): guard Project Tools readiness snapshot`
  - extends the auto-discovered Project Tools preflight with readiness bindings and explicit forbidden mutation tokens.
- `0e98b470ff5fcde09bf1d896180f34b98678e664` — `docs(ui): document Project Tools readiness dashboard`
  - records the UX intent, `CLEAN` semantics and read-only safety boundary.

## Validation actually performed

- Re-read current `ProjectState`/`ProjectElement` contracts before implementation; readiness uses only existing persisted state.
- Re-fetched target source blobs immediately before writes; XAML, code-behind and preflight were unchanged from the claimed surfaces.
- Source review confirms the refresh path uses `ProjectContextCoordinator.TryGetReadOnly`, does not call `GetOrCreate`, and intentionally does not call `IsGeneratedGeometryStale()` because that helper may normalize/remove generated-state metadata.
- Existing Project Tools command tags and document-bound `EnsureBoundDrawingIsActive` dispatch are retained.
- Current main after the documentation commit was exactly `0e98b470ff5fcde09bf1d896180f34b98678e664`, whose parent is the preflight commit; GitHub combined status exposed no status contexts.
- No GitHub Actions, adapter build, release, installer/signing, licensed BricsCAD V25 or WPF runtime rendering was executed or claimed in this lane.

## Remaining LOCAL_ONLY proof

Native Project Tools rendering/HiDPI/text-clipping and document-switch interaction should be included when the next exact-SHA BricsCAD V25 UI qualification is run. This is runtime evidence only; no additional source change is implied by this close-out.
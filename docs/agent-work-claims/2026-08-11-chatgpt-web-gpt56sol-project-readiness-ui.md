# Agent work claim — Project Tools readiness dashboard

- Agent: `chatgpt-web-gpt56sol-project-readiness-2012`
- Registered: `2026-08-11T20:12:00+07:00`
- Status: `ACTIVE`
- Baseline main SHA: `2c367e4d8d40acf4ef4a6ee932ef2aeaef26d8ea`
- Priority: continue the owner-requested BLT3D-inspired UI/UX wave with a compact, source-safe project readiness surface inside the existing document-bound Project Tools window.

## Reserved scope

Enhance Project Tools with a read-only readiness/status dashboard derived only from the current existing canonical QS3D project: active Zone/Floor, catalog counts, semantic dirty-state counts, project change version and last update. Improve hierarchy and actionable status copy without changing project semantics or adding a parallel application shell.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs`
- `scripts/preflight-project-tools.py` only for focused readiness/read-only regression assertions
- `docs/UI-PROJECT-READINESS-2026-08-11.md` (new)
- this claim file for close-out

## Functional contract

- Open/refresh must keep using `ProjectContextCoordinator.TryGetReadOnly` and must never bootstrap/cache a missing project.
- Readiness calculations are observation-only: no `Touch`, `MarkDirty`, `MarkClean`, `IsGeneratedGeometryStale`, save/reload, regeneration, metadata mutation, or Core mutation service is allowed while rendering.
- Active Zone/Floor labels resolve from `ActiveZoneId` / `ActiveFloorId` with explicit fallback when the referenced definition is missing.
- Dirty metrics use the already persisted `ElementDirtyFlags` state and do not mutate generated-output state.
- Command buttons retain the existing document-bound, fail-closed dispatch behavior; no decorative/stub action is introduced.

## Explicit exclusions / coordination

- No `RightPanel*` / `PaletteCoordinator.cs` work reserved by the active right-panel quantity workspace claim.
- No BQ quantity explanation/detail UI, Core reporting builders, Core persistence/atomicity, `Commands.cs` post-commit work, Ribbon/Start Center, Workspace property palette, Direct Draw/Create Similar, Room/recognition, Level native placement, installer/release/signing, or GitHub Actions.
- No BricsCAD V25/WPF runtime PASS claim from this remote lane.

## Validation plan

- Re-fetch newest `main` and active claims before implementation/integration.
- Re-fetch exact Project Tools source blobs and preserve document-bound lifecycle/command dispatch.
- Extend the existing auto-discovered `preflight-project-tools.py` so it guards readiness fields, read-only project lookup and forbidden mutation calls in the refresh path.
- Inspect final pushed diff/ancestry and available commit status metadata. Native visual fit/HiDPI/document-switch behavior remains LOCAL_ONLY.

## Completion condition

Project Tools exposes the new readiness dashboard on current `main`, the source preflight guards the non-creating/non-mutating refresh contract, the design note is committed, and this claim is marked `COMPLETED` with exact implementation SHA(s) and any remaining local V25 proof.
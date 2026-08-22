# Work claim — Project Browser workspace XML namespace guard

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T22:10:00+07:00`
- Baseline main SHA: `aac1e9b148fdf775ba70ae35b867fded02fc92be`
- Priority: continue-all remote-safe persisted-state integrity

## Reserved scope

Make Project Browser workspace-state v1 reject namespaced root child elements instead of accepting them by LocalName and silently ignoring the extra namespaced payload during deserialization.

## Expected surfaces

- `src/QS3D.Core/Navigation/ProjectBrowserWorkspaceStateStore.cs`
- `tests/QS3D.Core.SmokeTests/ProjectBrowserWorkspaceStateStoreSmoke.cs`
- this claim file

## Excluded scope

- No BricsCAD V25 runtime/UI changes.
- No query/selection/virtualization semantics changes.
- No quantity, updater, export, formula, persistence QSDB, Direct Draw, Curtain or release work.
- No GitHub Actions dispatch.

## Validation performed

- Re-read source and focused smoke from current `main` before each write.
- Replaced LocalName-only root-child acceptance with an exact no-namespace `HashSet<XName>` allowlist.
- Added focused smoke tampering a canonical document with an additional `future:Categories` child sharing the supported LocalName; deserialization is required to throw `InvalidDataException`.
- No GitHub Actions or BricsCAD runtime validation was run.

## Completion

- Claim commit: `e2253598e044b845f61cc88bf75cca4524426551`
- Implementation commit: `4f4cc84f3248e94cd6b7a9686d8ce490619b7f83`
- Regression commit: `26ac5ee1c3cbdaf335a8949875f2592eda0ca256`
- Remaining runtime/local gates: none introduced by this Core-only strict-schema change.

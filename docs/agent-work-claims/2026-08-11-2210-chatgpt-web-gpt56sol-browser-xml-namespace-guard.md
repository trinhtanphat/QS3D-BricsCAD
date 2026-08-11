# Work claim — Project Browser workspace XML namespace guard

- Status: `ACTIVE`
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

## Validation plan

- Extend the existing strict-schema smoke so a canonical workspace XML document with an additional namespaced child whose LocalName matches a supported child fails with `InvalidDataException`.
- Preserve canonical no-namespace v1 round-trip behavior and all existing strict-schema failures.

## Coordination

This claim is limited to XML namespace fail-closed behavior in the existing workspace-state serializer/deserializer and its focused smoke.

## Completion condition

Guard + regression are pushed to current `main`, then this claim is closed with exact commits and validation performed.

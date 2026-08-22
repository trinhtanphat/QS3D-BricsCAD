# Work claim — Recognition batch input freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-recognition-batch-input-freshness`
- Registered: `2026-08-12T13:30:00+07:00`
- Baseline main SHA: `e6929c2459e611a1758b1e58b25bdf08aa7d1e47`
- Priority: P1 — caller-controlled lazy input must not invalidate recognition assumptions during enumeration.

## Confirmed defect

`ProjectRecognitionService.SuggestBatch(ProjectState, IEnumerable<EntitySnapshot>, ...)` materializes caller-controlled `snapshots` before recognition but does not verify that `project.ChangeVersion` stayed stable while enumeration ran. A lazy enumerable can mutate/touch the same project while yielding otherwise-valid snapshots, after which batch recognition continues against project metadata/state different from the state at call entry.

## Reserved scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- one focused Core smoke under `tests/QS3D.Core.SmokeTests/` for recognition batch input freshness
- this claim file

## Intended contract

- Capture the project `ChangeVersion` immediately before caller-controlled snapshot enumeration.
- Preserve existing bounded materialization, null-entry rejection, batch caps, duplicate/scoring/category behavior, and stable input behavior.
- Immediately after materialization, reject with `InvalidOperationException` if the project version changed.
- Freshness rejection must occur before empty-batch/recognition semantic processing, so a mutating-empty enumerable also fails closed.
- Do not roll back caller-side project mutation; only refuse recognition on stale assumptions.

## Excluded scope

- Recognition scoring/rule/category semantics and layer-mapping format.
- SourceHandle/health/rebar/Auto Room lanes.
- BricsCAD native/UI behavior or licensed-host qualification.
- GitHub Actions dispatch.

## Validation boundary

Focused source/readback + deterministic Core smoke source only. No full local .NET build/smoke PASS, GitHub Actions PASS, or BricsCAD V25/V26 runtime PASS will be claimed without execution.

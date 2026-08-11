# Agent work claim — Plan-to-3D commit-time source freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-plan-to-3d-source-freshness`
- Registered: `2026-08-11T21:00:00+07:00`
- Baseline main SHA observed before reservation: `650300db165f14f70fae688678cd4838bf57c5d7`
- Mode: remote source/static only; no licensed BricsCAD V25 runtime PASS is claimed.

## Confirmed defect

`PlanTo3DCommands.ConvertPlanWalls(...)` re-reads LINE/open-POLYLINE geometry after prompting, but after `DirectDrawProjectPreviewContext.ResolveForMutation(...)` it currently proceeds to semantic freshness and `ProjectStateSnapshot.Capture(project)` without a second CAD geometry fingerprint re-read. A source edit that lands in the preview-context resolution window can therefore cross the final commit boundary without being rejected.

## Reserved scope

- `src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs`
- `scripts/preflight-plan-to-3d-source-geometry-freshness.py`
- this claim file for close-out

## Intended contract

- preserve the existing initial and post-prompt source preflights;
- after `ResolveForMutation(...)`, re-run `PreflightSources(...)` and require the original deterministic geometry fingerprints to match again before snapshot or semantic/native mutation;
- preserve existing semantic/generated-source rejection, project identity/ChangeVersion/unit/UCS guards, closed-POLYLINE fail-closed behavior, scoped regeneration, ownership and rollback semantics;
- strengthen the focused static preflight so it fails if the post-resolve CAD geometry re-read or its ordering before snapshot/mutation is removed.

## Explicit exclusions

No Direct Draw P0/P1/Opening/Window/ReferenceWall changes, no Ribbon/Workspace/Quantity/Rebar/Updater/Core persistence changes, no GitHub Actions dispatch, and no change to `LOCAL-008`/`LOCAL-014` runtime status.

## Completion condition

The post-resolve CAD source-fingerprint guard and focused regression contract are integrated on current `main`, this claim is closed with exact source/merge evidence, and BricsCAD V25 interactive qualification remains LOCAL_ONLY.

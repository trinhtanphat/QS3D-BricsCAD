# Work claim — CAD handle fatal exception propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cad-handle-fatal-propagation-20260812-0902`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `573e1cd7dfe8da01dd6ca6c94c53f4a12d6d1c85`
- Priority: owner-requested continue-all shared CAD read/selection integrity hardening

## Confirmed defect

`CadHandleService` uses bare catches while resolving ObjectIds, opening live entities, reading `ObjectId.Handle`, and opening live `Solid3d` objects. These catches intentionally make stale/unreadable handles disappear from read-only selection/live-handle results, but they also swallow fatal runtime exceptions (`OutOfMemoryException`, `StackOverflowException`, `AccessViolationException`) and can turn a fatal runtime condition into an ordinary empty/partial result. `GeneratedCurtainPanelRuntimeHealthService` is one direct consumer of `Resolve(...)`, so this can also create false missing/unresolved health results.

## Reserved scope

- Preserve all current recoverable skip behavior in `Resolve`, `GetLiveHandles`, and `GetLiveSolidHandles`.
- Do not catch `OutOfMemoryException`, `StackOverflowException`, or `AccessViolationException` inside these read-only helper paths.
- Apply one shared `IsRecoverableDiagnosticFailure(Exception)` predicate to all four existing catch sites.
- Preserve normalization, dedupe, `OpenMode.ForRead`, selection behavior and return types.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs`
- `scripts/preflight-cad-handle-fatal-propagation.py`
- this claim file

## Excluded scope

- No changes to selection UI behavior other than fatal exceptions no longer being swallowed.
- No changes to handle canonicalization rules.
- No changes to Curtain Panel ownership/materialization logic.
- No unrelated active claim changes.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Add a shared recoverable-exception predicate and filter all four current broad catches.
- Add a focused source preflight requiring four filtered catch sites, all three fatal exclusions, current `OpenMode.ForRead`, and unchanged `NormalizeHexHandle` semantics.
- Re-fetch final source/preflight from current `main`, verify ancestry, then close with exact SHAs.

## Completion condition

Completed only when current `main` preserves recoverable stale/unreadable-handle skip behavior without swallowing fatal runtime exceptions, focused regression source pins that contract, and this claim is `COMPLETED` with exact integration evidence.

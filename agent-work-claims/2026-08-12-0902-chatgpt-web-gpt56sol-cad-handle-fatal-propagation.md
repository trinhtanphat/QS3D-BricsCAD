# Work claim — CAD handle fatal exception propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-cad-handle-fatal-propagation-20260812-0902`
- Registered: `2026-08-12T09:02:00+07:00`
- Baseline main SHA: `573e1cd7dfe8da01dd6ca6c94c53f4a12d6d1c85`
- Priority: owner-requested continue-all shared CAD read/selection integrity hardening

## Confirmed defect

`CadHandleService` used bare catches while resolving ObjectIds, opening live entities, reading `ObjectId.Handle`, and opening live `Solid3d` objects. Those catches intentionally made stale/unreadable handles disappear from read-only selection/live-handle results, but also swallowed fatal runtime exceptions (`OutOfMemoryException`, `StackOverflowException`, `AccessViolationException`) and could turn a fatal runtime condition into an ordinary empty/partial result. `GeneratedCurtainPanelRuntimeHealthService` is one direct consumer of `Resolve(...)`, so this could also create false missing/unresolved health results.

## Implemented scope

- Current recoverable skip behavior in `Resolve`, `GetLiveHandles`, and `GetLiveSolidHandles` remains unchanged.
- `OutOfMemoryException`, `StackOverflowException`, and `AccessViolationException` are no longer caught inside these read-only helper paths.
- All four previous broad catch sites now use one shared `IsRecoverableDiagnosticFailure(Exception)` predicate.
- Normalization, dedupe, `OpenMode.ForRead`, selection behavior and return types remain intact.
- Added one focused static regression preflight.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs`
- `scripts/preflight-cad-handle-fatal-propagation.py`
- this claim file

## Integration evidence

- Claim registration: `98f6004da8dbfa3775fc58313f84f2881bcea0e7`
- Source fix: `65ffdc74eb2ae28fd76006193bb9bc2104860d95`
- Focused regression preflight: `c309f009041596cf72f577d6751d4725dfcefe68`

## Validation performed

- Re-fetched source after claim registration; blob `11c0ef8ceae29d3bb1f8870e7917fada90176eb9` still contained all four bare catches before editing.
- Re-fetched final source from current `main`; blob `b2c5ec064ebf552cded3fb4f59b21dd79e698a6a` contains four filtered catch sites, all three fatal exclusions, and retains normalization/dedupe plus `OpenMode.ForRead` behavior.
- Re-fetched `scripts/preflight-cad-handle-fatal-propagation.py`; blob `c28a6e3c7d4aff9fc4fe3a3ba875625210596d2e` requires four filtered catches and explicitly pins the existing `0x` handling, hexadecimal parsing, positive-handle requirement and uppercase canonical output.
- `GeneratedCurtainPanelRuntimeHealthService` remains a direct `Resolve(...)` consumer, so recoverable unresolved handles continue to produce its existing diagnostic while fatal failures now propagate instead of becoming false unresolved results.
- V26 links the shared V25 adapter source, so this hardening is shared by V26 source build without duplicate code.

## Validation boundary

Remote source/static readback only. This session did not execute the preflight process, a full .NET build/test, GitHub Actions, or licensed BricsCAD V25/V26 runtime. No native runtime, private-DWG, installer, signing or release PASS is claimed.

## Excluded scope

- No selection UI semantic change beyond fatal exceptions no longer being swallowed.
- No handle canonicalization changes.
- No Curtain Panel ownership/materialization changes.
- No unrelated active claim changes.
- No GitHub Actions, release publication or force push.

## Completion condition

Satisfied on the source/static contract: current `main` preserves recoverable stale/unreadable-handle skip behavior without swallowing fatal runtime exceptions, focused regression source pins the contract, and exact integration evidence is recorded above.

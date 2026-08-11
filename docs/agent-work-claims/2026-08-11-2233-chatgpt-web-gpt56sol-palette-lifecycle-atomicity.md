# Work claim — palette lifecycle atomicity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-palette-lifecycle-atomicity`
- Registered: `2026-08-11T22:33:00+07:00`
- Baseline main SHA: `129a091e7a6ea4fbf4cd3e39acf3fe922e2ffca8`
- Priority: P1 deterministic lifecycle/resource ownership hardening found during owner-requested `continue all` audit.

## Confirmed defects

`PaletteCoordinator.EnsureCreated()` publishes `_workspace`, `_right`, `_quantityInsight` and their panel references incrementally. If a later `PaletteSet` construction, size assignment, or `AddVisual(...)` call throws, the already-created palette resources remain published until some later retry or plugin teardown happens.

`PaletteCoordinator.Dispose()` also disposes the three palette sets sequentially without per-resource isolation. If one native `PaletteSet.Dispose()` throws, later palette sets are not disposed and the panel references are not cleared. A teardown/reset path can therefore leave partially owned native UI state behind.

Both defects are visible from source ownership/order alone; this lane does not depend on a claim that BricsCAD normally throws.

## Reserved scope

- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `scripts/preflight-palette-lifecycle-atomicity.py` (new)
- this claim file for close-out

## Intended contract

- `EnsureCreated()` either finishes all three palette/panel creations or immediately tears down every partially published palette before rethrowing.
- Palette teardown is best-effort per palette: one native dispose failure cannot prevent the other palette resources from being released.
- All palette and panel static references are cleared deterministically after teardown.
- Existing visibility, layout persistence, panel refresh and user-facing behavior remain unchanged.

## Excluded scope

- No Workspace/RightPanel/QuantityInsight presentation edits, Theme/Ribbon/updater/Core/project semantics, installer/signing/release or LOCAL inbox changes.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

Re-fetch current `main` and `PaletteCoordinator.cs` immediately before source write. Add a focused auto-discovered static preflight that requires creation rollback, isolated palette disposal and final reference clearing while preserving existing layout persistence/visibility contracts. Inspect exact commit diffs and verify ancestry after concurrent integration without force-push.

## Completion condition

Palette creation/teardown ownership is fail-atomic at source level, focused regression source is merged on `main`, this claim is closed with exact SHAs, and native failure-injection qualification remains local-only.

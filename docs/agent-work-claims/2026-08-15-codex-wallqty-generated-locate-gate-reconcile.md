# Work claim — Wall Quantity generated-locate gate reconciliation

- Status: `COMPLETED`
- Agent: `Codex /root`
- Registered: `2026-08-15T10:36:00+07:00`
- Baseline main SHA: `d73420a2dce589fd74e220efdcca3071b828b335`
- Related issue: `#1435` (closed by source PR `#1526`)
- Priority: exact-main integration gate correctness

## Confirmed drift

PR `#1526` correctly moved `WallQuantityWindow.LocateSelected` from direct
`SourceHandleResolver.Resolve(...)` use to `Resolve3DLocateHandles(...)`. That
helper prefers a current owned generated `Solid3d`, fails closed for a
configured stale, malformed, missing, or foreign generated handle, and falls
back to source handles only when `GeneratedSolidHandle` is absent.

The new focused gate passes, but the older aggregate
`scripts/preflight-wall-quantity-window.py` still requires the removed direct
source-resolver call inside `LocateSelected`. Exact merge SHA
`7e9dfadc9151b3fd2585d15ad25ed0d5146b7deb` therefore fails only that stale
Wall Quantity assertion plus the independently owner-controlled V25 release
sync gate.

## Reserved scope

- `scripts/preflight-wall-quantity-window.py` only: require
  `Resolve3DLocateHandles(currentProject, currentElement, currentRow)` in the
  locate flow and preserve the order current-project revalidation -> current
  row -> current element -> guarded 3D handle resolution -> CAD select -> zoom.
- This claim record for registration and closeout evidence.

## Explicit exclusions

- No production, XAML, command, Core, native geometry, ownership, fallback,
  runtime, LOCAL runner/probe, release, workflow, or GitHub Actions changes.
- Do not modify `scripts/preflight-wallqty-3d-locate-generated-solid.py`; it
  remains the detailed generated-first/fail-closed contract.
- Do not touch the independently failing
  `scripts/preflight-v25-preview-release-sync.py` owner-controlled release gate.

## Validation plan

- Run both Wall Quantity gates.
- Run aggregate `scripts/preflight-all.py` and require the Wall Quantity gate to
  pass, recording the unrelated release-sync result without expanding scope.
- Verify the diff contains only this gate plus the claim record.

## Completion evidence

- Claim PR `#1545` merged at
  `49135b378a479fa6c4da78d2d8713ad65b5bba61` before the gate edit.
- Gate PR `#1547` merged at exact main SHA
  `425a529c72de934e5bf634f661f15ca5e283ab17`.
- `preflight-wall-quantity-window.py`: PASS.
- `preflight-wallqty-3d-locate-generated-solid.py`: PASS.
- Aggregate discovered 814 feature gates. The Wall Quantity failure was removed;
  the only remaining failure was the independent owner-controlled
  `preflight-v25-preview-release-sync.py` guard. No release or Actions surface
  was modified or operated.
- Production remained unchanged in this successor lane.

# Work claim — #79 Grid V25 UI planner integration

- Status: `ACTIVE`
- Phase: `SOURCE_FIXED / FRESH_CI_IN_PROGRESS`
- Agent: `chatgpt-20260814-grid79-v25ui-56sol`
- Registered: `2026-08-14T08:45:32Z`
- Lease: `2h`
- Baseline main SHA: `71dcb3b4cd2b06c8510bf60a6b1e1851a0f7f55e`
- Issue: `#79`
- Priority: remote-safe V25 Grid command/UI planner-consumption correctness
- Source fix SHA: `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82`

## Reserved scope

- `src/QS3D.BricsCAD.V25/GridSystemCommands.cs`
- `scripts/preflight-grid-system-v25-ui-planner.py` (new remote-safe regression guard)
- this claim document closeout only

## Collision check

- The adjacent #79 Grid reference Unicode lane is completed on `main`; its Core intersection-identity scope is excluded here.
- The recent Project Tools Grid workflow-parity claim is completed; no current claim found in the refreshed claim history reserves `GridSystemCommands.cs`.
- Claims added during the source race covered Browser reference smoke and Quantity Insight BLT parity, not this V25 Grid command boundary.
- Source commit was rebased through a non-fast-forward race without force-push; `main` accepted the exact fix by fast-forward at `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82`.
- Status is deliberately canonical `ACTIVE` while fresh CI is non-terminal so this lane remains visibly reserved under `docs/AGENT-WORK-REGISTRATION.md`.

## Source evidence

- `QS3DGRIDSYSTEMPREVIEW` still extracts live semantic LINE Grid sources read-only, builds a rectangular preview, plans the system, then previews intersections.
- `BuildPreview` now separates the two rectangular LINE families and routes each family through `GridSpatialOrderingPlanner.OrderParallelLines(...)` with the explicit perpendicular U/V ordering axis.
- Ordered planner output is converted into `GridLinearStation` input for `GridSystemPlanner.PlanRectangular(...)`; the old direct midpoint-to-station bypass is removed.
- `scripts/preflight-grid-system-v25-ui-planner.py` guards both spatial-order calls, ordered-station consumption, system planning, intersection preview, and absence of the direct station-accumulation bypass.
- Read-back of source SHA `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82` confirms only the reserved source + regression guard changed.

## Fresh CI evidence

- Release workflow run `31790719812` / #165 was dispatched independently on descendant SHA `e58f0f24dfa5735ce3d40f932141aa9b7d3dcf50`, which contains source fix `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82` in its ancestry.
- On the latest read, `Generic source guard`, `All discovered feature source guards`, `Restore Core smoke projects`, and `Build Core Release` are `SUCCESS`.
- The run is still non-terminal at `Build Core smoke harness`; therefore this claim is not yet marked `COMPLETED` and no full-CI PASS is inferred early.

## Validation boundary

- Native Grid marker/materialization/runtime acceptance, licensed V25 save/reopen/Undo proof, curved/ARC ordering policy and other LOCAL_ONLY work remain explicitly outside this remote-safe claim.
- No duplicate workflow is dispatched while #165 is active.

## Handoff

Remote-safe #79 V25 Grid UI/planner integration source is fixed on `main` and its discovered feature source guard has passed on a fresh descendant release run. Keep the issue open for its documented native/runtime/local-only work. Promote this claim to `COMPLETED` only after the active fresh run reaches a terminal result and the final integration review verifies source/closeout ancestry on refreshed `main`; do not infer PASS from stale releases.
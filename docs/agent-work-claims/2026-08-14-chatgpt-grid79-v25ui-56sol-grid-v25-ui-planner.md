# Work claim — #79 Grid V25 UI planner integration

- Status: `SOURCE_FIXED / PENDING_FRESH_CI`
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
- Claims added during the race cover Browser reference smoke and Quantity Insight BLT parity, not this V25 Grid command boundary.
- Source commit was rebased through a non-fast-forward race without force-push; `main` accepted the exact fix by fast-forward at `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82`.

## Source evidence

- `QS3DGRIDSYSTEMPREVIEW` still extracts live semantic LINE Grid sources read-only, builds a rectangular preview, plans the system, then previews intersections.
- `BuildPreview` now separates the two rectangular LINE families and routes each family through `GridSpatialOrderingPlanner.OrderParallelLines(...)` with the explicit perpendicular U/V ordering axis.
- Ordered planner output is converted into `GridLinearStation` input for `GridSystemPlanner.PlanRectangular(...)`; the old direct midpoint-to-station bypass is removed.
- `scripts/preflight-grid-system-v25-ui-planner.py` guards both spatial-order calls, ordered-station consumption, system planning, intersection preview, and absence of the direct station-accumulation bypass.
- Read-back of source SHA `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82` confirms only the reserved source + regression guard changed.

## Validation boundary

- Exact source SHA `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82` currently has no GitHub status/check entries and no Actions run.
- `.github/workflows/release-v25-cloud.yml` is `workflow_dispatch` only, so this source push does not automatically produce release CI evidence; no stale run was rerun and no release was dispatched from this lane.
- Native Grid marker/materialization/runtime acceptance, licensed V25 save/reopen/Undo proof, curved/ARC ordering policy and other LOCAL_ONLY work remain explicitly outside this remote-safe claim.

## Handoff

Remote-safe #79 V25 Grid UI/planner integration source is fixed on `main`. Keep the issue open for its documented native/runtime/local-only work. Promote this claim to CI-validated only when a fresh workflow/check actually runs against a descendant that contains `cc4d5fe3ce9d008a1d4f40288f3109aa01c9fa82`; do not infer PASS from older releases.

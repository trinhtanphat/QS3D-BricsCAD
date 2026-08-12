# Work claim — release #32 QS3DLOCATE PICKFIRST preservation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release32-locate-pickfirst`
- Registered: `2026-08-12T11:18:00+07:00`
- Baseline main SHA: `591e1a8916cc79e61c882371b4b3415b5449a214`
- Priority: release #32 reports `scripts/preflight-locate-selection.py` failing; current source also contains a confirmed zero-match PICKFIRST contradiction.

## Confirmed defect

`QS3DLOCATE` reports that a zero-match Locate keeps the current selection, but calls `CadHandleService.Select(...)`. Current `Select(...)` always calls `SetImpliedSelection(...)`, including with an empty resolved id set, so a zero-match Locate clears PICKFIRST before reporting that it was preserved. `CadHandleService.SelectIfAny(...)` already implements the required no-change-on-zero contract.

The release gate is also stale in the opposite direction: it requires global `Select(...) => SelectIfAny(...)`, which would remove the intentionally available explicit-empty selection path used by validation-failure pre-clear flows. The correct contract is call-site specific.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Commands.cs`: only the `QS3DLOCATE` selection call.
- `scripts/preflight-locate-selection.py`: reconcile PICKFIRST assertions with explicit `Select` versus `SelectIfAny` semantics.
- this claim file for close-out.

## Contract

- QS3DLOCATE resolves canonical source/boundary/generated owner handles as today.
- QS3DLOCATE uses `SelectIfAny` so zero live handles do not mutate existing PICKFIRST.
- Positive live selection still sets implied selection and dispatches zoom.
- `CadHandleService.Select` remains the explicit selection API that may clear when handed an empty set; do not change its production semantics in this lane.
- `SelectIfAny` must continue to return before `SetImpliedSelection` on zero resolved ids.
- Preserve existing source/boundary/generated fallback smoke coverage.

## Excluded scope

No edits to resolver traversal, health/BQ locate flows, Quantity validation pre-clear behavior, Excel locate, selection-state Core, or unrelated #32 gates. No Actions dispatch/build/release/runtime PASS claim.

## Completion condition

The zero-match QS3DLOCATE path actually preserves PICKFIRST, the gate pins the call-site-specific behavior without weakening explicit clear flows, changes are read back on current `main`, and this claim is closed with exact SHAs.
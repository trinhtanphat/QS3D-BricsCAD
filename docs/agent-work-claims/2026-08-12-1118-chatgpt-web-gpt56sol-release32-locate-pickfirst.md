# Work claim — release #32 QS3DLOCATE PICKFIRST preservation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release32-locate-pickfirst`
- Registered: `2026-08-12T11:18:00+07:00`
- Baseline main SHA: `591e1a8916cc79e61c882371b4b3415b5449a214`
- Priority: release #32 reports `scripts/preflight-locate-selection.py` failing; current source also contains a confirmed zero-match PICKFIRST contradiction.

## Confirmed defect

`QS3DLOCATE` reports that a zero-match Locate keeps the current selection, but calls `CadHandleService.Select(...)`. Current `Select(...)` always calls `SetImpliedSelection(...)`, including with an empty resolved id set, so a zero-match Locate clears PICKFIRST before reporting that it was preserved. `CadHandleService.SelectIfAny(...)` already implements the required no-change-on-zero contract.

The release gate expects the historical global `Select => SelectIfAny` contract. Quantity validation-failure guards added later intentionally used `Select(empty)` to clear stale selection, which overloaded `Select` with two incompatible semantics. The robust reconciliation is to restore no-change-on-zero semantics for normal `Select` and provide an explicit `ClearSelection` API for the two validation pre-clear flows.

## Expanded reserved scope

- `src/QS3D.BricsCAD.V25/Cad/CadHandleService.cs`: restore `Select => SelectIfAny` and add explicit `ClearSelection`.
- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs`: migrate intentional pre-clear to `ClearSelection`.
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.LocateSelectionFailureGuard.cs`: migrate intentional pre-clear to `ClearSelection`.
- `scripts/preflight-locate-selection.py`: retain/pin PICKFIRST-safe normal selection semantics.
- `scripts/preflight-quantity-locate-validation-failure-clear.py`: pin explicit-clear semantics instead of relying on `Select(empty)`.
- this claim file for close-out.

## Contract

- Normal `CadHandleService.Select` preserves existing PICKFIRST when no live handles resolve by delegating to `SelectIfAny`.
- Positive normal selection still sets implied selection and returns the live count.
- `ClearSelection(document)` is the only explicit empty-selection API and directly clears implied selection.
- Quantity Summary/Insight validation pre-clear use `ClearSelection` only after their active-DWG/trigger guards.
- QS3DLOCATE can continue using normal `Select` and its existing zero-match message becomes truthful.
- Source/boundary/generated owner fallback, zoom-on-positive-only, and existing smoke coverage remain unchanged.

## Excluded scope

No edits to resolver traversal, health/BQ canonical locate behavior beyond the two dedicated pre-clear guards, Excel locate, selection-state Core, or unrelated #32 gates. No Actions dispatch/build/release/runtime PASS claim.

## Completion condition

Zero-match normal Locate preserves PICKFIRST, explicit validation-failure pre-clear still clears only the same active DWG, both gates pin the separated semantics, changes are read back on current `main`, and this claim is closed with exact SHAs.
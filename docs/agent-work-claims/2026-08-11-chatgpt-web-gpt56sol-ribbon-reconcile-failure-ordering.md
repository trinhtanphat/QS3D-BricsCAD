# Work claim — Ribbon reconciliation failure ordering

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-failure-ordering`
- Registered: `2026-08-11T20:59:00+07:00`
- Baseline main SHA: `46175c80dcc42c1fd99d252c3d70731c1fddbd75`
- Priority: keep a usable legacy QS3D Ribbon intact until grouped reconciliation has successfully established the replacement panel set.

## Confirmed defect

Current `RibbonBootstrapper.ReconcileTab(...)` removes the exact legacy `<TAB>_PANEL_SOURCE` before it ensures all current grouped panels/buttons. If reflection/native collection work throws while creating or reconciling a grouped panel, `TryInitialize()` catches the exception and returns `false`, but the old working flat panel has already been removed. A hot reload can therefore degrade from an old-but-usable QS3D Ribbon to a partially reconciled Ribbon solely because the migration failed midway.

## Reserved scope

Make legacy flat-panel retirement the final destructive step of a successful per-tab reconciliation. Grouped panel/button reconciliation must happen first; only after all `EnsurePanel(...)` calls complete may the exact old QS3D-owned flat panel be removed. Preserve all completed grouped information architecture, Start Center, dedicated augmenter panels, unknown/user/vendor panels and click-time active-document routing.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-ribbon-information-architecture.py`
- this claim file for close-out

## Excluded scope

- No command regrouping, rename/removal or new Ribbon button.
- No edits to `QuickWorkflowRibbonAugmenter.cs`, Reference Wall, Project Tools, Start Center, updater, Quantity/BQ, Workspace, Core, release/signing or LOCAL inbox.
- No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim.

## Validation plan

- Re-fetch both target files immediately before writes.
- Move `RemoveLegacyFlatPanel(...)` after the grouped `EnsurePanel(...)` loop and retain the narrow exact-ID deletion contract.
- Extend the existing auto-discovered Ribbon preflight so source order proves grouped reconciliation precedes legacy retirement.
- Re-fetch current `main` and compare ancestry after writes; never force-push or overwrite concurrent changes.

## Coordination

The grouped Ribbon information-architecture, legacy augmenter compatibility, Start Center Ribbon-entry and existing-tab reconciliation claims are completed. The Create Similar claim still reserves only QuickWorkflow/Create Similar surfaces. The active GitHub auto-update claim explicitly forbids Ribbon edits but does not own Ribbon source. No current active claim found in the repository claim index reserves `RibbonBootstrapper.cs` for product changes.

## Completion condition

A failed grouped reconciliation cannot retire the old flat panel before the replacement panel set is established; the source-order guard is merged on `main`, final ancestry is verified, and native V25 hot-reload proof remains explicitly local-only.
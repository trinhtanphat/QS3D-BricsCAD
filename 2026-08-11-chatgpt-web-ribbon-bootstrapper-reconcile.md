# Work claim — Ribbon bootstrap reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-ribbon-reconcile`
- Registered: `2026-08-11T20:35:00+07:00`
- Completed: `2026-08-11T20:40:00+07:00`
- Baseline main SHA: `7d7bcd2e5bcda8075b5680b4b3e6d442420ed09c`
- Priority: make the grouped Ribbon architecture actually reconcile an already-loaded QS3D Ribbon instead of skipping every existing tab

## Confirmed defect

The pre-fix `RibbonBootstrapper.TryInitialize()` stopped at any already-existing QS3D tab ID. After plugin reload/update in the same BricsCAD session, that meant newer grouped panels/buttons could remain absent until a full BricsCAD restart. `Reset()` only cleared `_initialized`; it could not bypass the existing-tab shortcut.

## Source implementation

- Reservation: `af0fc42ea0ee94ea67e5a0bcc4bde42760568e0a`.
- `f262b8ca37e2265e86f30f60d62c9e69af842489` — `fix(ribbon): reconcile existing grouped tabs`
  - `TryInitialize()` now runs every current `RibbonTabSpec` through `ReconcileTab(...)` instead of skipping an existing tab;
  - existing tabs have Name/Title reconciled, missing current grouped panels are added, and existing current panels have Name/Title/buttons reconciled by deterministic IDs;
  - known buttons are created only when absent and otherwise receive the current Text/command/handler contract;
  - only the exact legacy QS3D-owned flat `<TAB>_PANEL_SOURCE` is removed, so dedicated augmenter panels such as Quick Workflow/Project Tools and unknown user/vendor panels are preserved;
  - fresh tabs are still created normally and added once;
  - click-time `MdiActiveDocument` dispatch remains unchanged.
- `f7026c4a25e81ace19189e8b8491e3eb1575b57c` — `test(ribbon): guard existing-tab reconciliation`
  - requires existing-tab reconciliation, exact grouped panel lookup, known-button reconciliation and narrow legacy-flat-panel cleanup;
  - rejects the old top-level `CollectionContainsId(...)/continue` shortcut and whole-collection `.Clear()`;
  - preserves the grouped panel catalog, all required command bindings plus `QS3DSTART`, and exactly one Start Center binding.

## Final source review

After concurrent BQ work moved `main` to `b0ebaa6043cc933cc4bf017ee9aa5ca50b1d4e07`, both reconciliation files were re-fetched from `main` and still contain the intended contracts. The newer BQ commit is a direct descendant of the Ribbon source/preflight sequence, so no concurrent Ribbon work was overwritten.

Current `RibbonBootstrapper.cs` has `ReconcileTab`, `EnsurePanel`, `EnsurePanelButtons`, exact legacy-flat-panel removal and no create-only existing-tab skip. Current `scripts/preflight-ribbon-information-architecture.py` locks those contracts and retains the current grouped command inventory including Start Center.

## Runtime / execution boundary

- The focused Python preflight was authored and merged but was not executed in this connector-only lane.
- No GitHub Actions, local checkout/build, BricsCAD V25 launch, Ribbon render/reload test, installer or release was executed.
- Exact hot-reload convergence, Ribbon visual layout, DPI/Unicode and active-document click behavior remain LOCAL_ONLY under the existing V25 qualification process; this claim does not manufacture `LOCAL_PASS`.

## Coordination

The grouped Ribbon information-architecture, Start Center Ribbon-entry and legacy augmenter compatibility claims are completed. The Create Similar claim still separately reserves `QuickWorkflowRibbonAugmenter.cs` and remains blocked only on its canonical `LOCAL-008` handoff; that file was not touched by this reconciliation lane.

## Completion condition

Satisfied for remote/source scope: fresh and already-loaded QS3D Ribbon states now converge through idempotent tab/panel/button reconciliation while unknown/dedicated augmenter panels are preserved, static regression coverage is merged, and native V25 proof remains explicitly unclaimed/local-only.

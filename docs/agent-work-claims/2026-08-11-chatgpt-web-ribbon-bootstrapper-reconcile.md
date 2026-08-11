# Work claim — Ribbon bootstrap reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-reconcile`
- Registered: `2026-08-11T20:35:00+07:00`
- Baseline main SHA: `7d7bcd2e5bcda8075b5680b4b3e6d442420ed09c`
- Priority: make the grouped Ribbon architecture actually reconcile an already-loaded QS3D Ribbon instead of skipping every existing tab

## Confirmed defect

Current `RibbonBootstrapper.TryInitialize()` does:

```text
if (CollectionContainsId(tabs, tabSpec.Id))
    continue;
```

After plugin reload/update in the same BricsCAD session, existing QS3D tab IDs can therefore prevent all newer grouped panels/buttons from being applied. That can leave a previous flat Ribbon or a stale grouped Ribbon in memory until a full BricsCAD restart, including missing later additions such as Start Center. `Reset()` only clears `_initialized`; it does not solve this because the existing tab still causes `continue`.

## Reserved scope

Reconcile existing QS3D-owned tabs/panels/buttons idempotently against current `CreateSpecs()` while preserving unknown augmenter panels and unrelated user/vendor Ribbon content.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs`
- `scripts/preflight-ribbon-information-architecture.py`
- this claim file for close-out

## Intended contract

- Find existing tab by exact QS3D tab ID; create it only when absent.
- For every current panel spec, find exact `<TAB>_<PANEL>_PANEL_SOURCE`; create missing panels and reconcile title/name/buttons in existing panels.
- Reconcile known button ID/text/command/handler instead of only checking tab existence.
- Remove only the exact legacy QS3D-owned flat panel source `<TAB>_PANEL_SOURCE` from an existing tab; do not remove unknown/dedicated augmenter panels such as Quick Workflow or Project Tools.
- Preserve current grouped specs and click-time `MdiActiveDocument` command routing.
- Repeated initialization after `Reset()` must not duplicate tabs, grouped panels or known buttons.

## Excluded scope

- No command regrouping, rename/removal, new feature button, Quick/Reference/Project augmenter edits, Direct Draw/Create Similar behavior, Core model, Workspace, Start Center business logic, local inbox, release or CI.
- No licensed BricsCAD V25 Ribbon runtime PASS claim.

## Validation plan

- Re-fetch `RibbonBootstrapper.cs` immediately before write.
- Extend the existing auto-discovered Ribbon information-architecture preflight with existing-tab reconciliation, exact panel lookup, known-button reconciliation and legacy-flat-panel cleanup requirements.
- Reject return of the top-level existing-tab `continue` shortcut.
- Preserve all 103+ existing command bindings plus the current Start Center binding.
- No GitHub Actions/build/release dispatch.

## Coordination

The grouped Ribbon information-architecture and Start Center Ribbon-entry claims are `COMPLETED`. The newly completed legacy augmenter compatibility lane reserves no files now. The Create Similar claim still reserves `QuickWorkflowRibbonAugmenter.cs`, which is explicitly excluded here. Current Core/UI claims do not reserve these two files.

## Completion condition

Existing and fresh Ribbon states converge to the same current grouped QS3D-owned tab/panel/button definitions without deleting unknown/dedicated augmenter panels, static regression coverage is merged, and this claim is marked `COMPLETED` without claiming native V25 execution.
